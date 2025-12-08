using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// Displays the angle between the line from tracked image to camera and the tracked image's normal vector.
    /// Attach this component to an ARTrackedImageManager GameObject.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class TrackedImageAngleDisplay : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera used to calculate the angle. Usually the main AR camera.")]
        Camera m_Camera;

        [SerializeField]
        [Tooltip("Optional: UI Text component to display the angle for all tracked images.")]
        Text m_GlobalAngleText;

        [SerializeField]
        [Tooltip("If true, creates a Text component on each tracked image to display its individual angle.")]
        bool m_ShowIndividualAngles = true;

        [SerializeField]
        [Tooltip("Font size for individual angle displays.")]
        int m_FontSize = 24;

        [SerializeField]
        [Tooltip("Color for the angle text.")]
        Color m_TextColor = Color.white;

        ARTrackedImageManager m_TrackedImageManager;
        System.Collections.Generic.Dictionary<TrackableId, Text> m_AngleTexts = new System.Collections.Generic.Dictionary<TrackableId, Text>();

        void Awake()
        {
            m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
            
            // Find the main camera if not set
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
            }
        }

        void OnEnable()
        {
            m_TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }

        void OnDisable()
        {
            m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }

        void Update()
        {
            if (m_Camera == null)
                return;

            // Update angles for all tracked images
            foreach (var trackedImage in m_TrackedImageManager.trackables)
            {
                if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                {
                    float angle = CalculateAngle(trackedImage);
                    float distance = Vector3.Distance(m_Camera.transform.position, trackedImage.transform.position);
                    
                    // Update individual text if it exists
                    if (m_AngleTexts.TryGetValue(trackedImage.trackableId, out var text))
                    {
                        text.text = $"Angle: {angle:F1}°\nDist: {distance:F2}m\nPos: {trackedImage.transform.position}";
                    }
                }
            }

            // Update global text if available
            if (m_GlobalAngleText != null && m_TrackedImageManager.trackables.count > 0)
            {
                var firstTrackedImage = GetFirstTrackingImage();
                if (firstTrackedImage != null)
                {
                    float angle = CalculateAngle(firstTrackedImage);
                    float distance = Vector3.Distance(m_Camera.transform.position, firstTrackedImage.transform.position);
                    m_GlobalAngleText.text = $"Camera-Image Angle: {angle:F1}°\nDistance: {distance:F2}m\nPosition: {firstTrackedImage.transform.position}";
                }
            }
        }

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            // Handle added images
            foreach (var trackedImage in eventArgs.added)
            {
                if (m_ShowIndividualAngles)
                {
                    CreateAngleTextForImage(trackedImage);
                }
            }

            // Handle removed images
            foreach (var trackedImagePair in eventArgs.removed)
            {
                if (m_AngleTexts.TryGetValue(trackedImagePair.Key, out var text))
                {
                    if (text != null && text.gameObject != null)
                    {
                        Destroy(text.gameObject);
                    }
                    m_AngleTexts.Remove(trackedImagePair.Key);
                }
            }
        }

        void CreateAngleTextForImage(ARTrackedImage trackedImage)
        {
            // Create a canvas for the text
            GameObject canvasObj = new GameObject("AngleCanvas");
            canvasObj.transform.SetParent(trackedImage.transform, false);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = m_Camera;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            // Position the canvas above the tracked image
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2, 0.5f);
            canvasRect.localPosition = new Vector3(0, 0, 0.2f); // Slightly in front
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Create text object
            GameObject textObj = new GameObject("AngleText");
            textObj.transform.SetParent(canvasObj.transform, false);
            
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = m_FontSize;
            text.color = m_TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "Angle: 0°\nDist: 0.00m";

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;
            textRect.localPosition = Vector3.zero;

            // Add outline for better visibility
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);

            m_AngleTexts[trackedImage.trackableId] = text;
        }

        /// <summary>
        /// Calculates the angle between the camera-to-image direction and the image's normal vector.
        /// </summary>
        /// <param name="trackedImage">The tracked image to calculate angle for.</param>
        /// <returns>The angle in degrees.</returns>
        float CalculateAngle(ARTrackedImage trackedImage)
        {
            if (m_Camera == null || trackedImage == null)
                return 0f;

            // Vector from tracked image to camera
            Vector3 imageToCamera = m_Camera.transform.position - trackedImage.transform.position;
            imageToCamera.Normalize();

            // Normal vector of the tracked image (pointing up in local space, which is Z-forward in world space)
            Vector3 imageNormal = trackedImage.transform.up;

            // Calculate the angle between the two vectors
            float angle = Vector3.Angle(imageToCamera, imageNormal);

            return angle;
        }

        ARTrackedImage GetFirstTrackingImage()
        {
            foreach (var trackedImage in m_TrackedImageManager.trackables)
            {
                if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                {
                    return trackedImage;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the current angle for a specific tracked image.
        /// </summary>
        /// <param name="trackedImage">The tracked image.</param>
        /// <returns>The angle in degrees.</returns>
        public float GetAngleForImage(ARTrackedImage trackedImage)
        {
            return CalculateAngle(trackedImage);
        }
    }
}
