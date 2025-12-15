using System.Collections.Generic;
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
        private float m_updatedImageTargetAngleThreshold = 25f;
        // Trackable distance y = 3.90625x  - 0.246875; with x is image size in meters
        private float m_DistanceCoefficient = 3.90625f;
        private float m_DistanceOffset = 0.246875f;
        
        [SerializeField]
        [Tooltip("Number of recent poses to average for smoothing.")]
        int m_PoseSmoothingWindow = 10;
        
        // Per-image pose history for smoothing
        Dictionary<TrackableId, Queue<Vector3>> m_PositionHistory = new Dictionary<TrackableId, Queue<Vector3>>();
        Dictionary<TrackableId, Queue<Quaternion>> m_RotationHistory = new Dictionary<TrackableId, Queue<Quaternion>>();
        

        [SerializeField]
        [Tooltip("Color for the angle text.")]
        Color m_TextColor = Color.white;

        ARTrackedImageManager m_TrackedImageManager;
        System.Collections.Generic.Dictionary<TrackableId, Text> m_AngleTexts = new System.Collections.Generic.Dictionary<TrackableId, Text>();

                [SerializeField]
        [Tooltip("If not null, instantiates this prefab for each detected image.")]
        GameObject m_OriginTransformPrefab;

        /// <summary>
        /// If not null, instantiates this Prefab for each detected image.
        /// </summary>
        /// <remarks>
        /// The purpose of this property is to extend the functionality of <see cref="ARTrackedImage"/>s.
        /// It is not the recommended way to instantiate content associated with an <see cref="ARTrackedImage"/>.
        /// </remarks>
        public GameObject originTransformPrefab
        {
            get => m_OriginTransformPrefab;
            set => m_OriginTransformPrefab = value;
        }

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

        float angle = 90f;
        float distance = 10f;

        void Update()
        {
            if (m_Camera == null)
                return;

            // Update angles for all tracked images
            foreach (var trackedImage in m_TrackedImageManager.trackables)
            {
                if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                {
                    angle = CalculateAngle(trackedImage);
                    distance = Vector3.Distance(m_Camera.transform.position, trackedImage.transform.position);
                    
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
                AddCornerSpheres(trackedImage.transform);
                // Instantiate OriginTransform prefab if set
                if (m_OriginTransformPrefab != null)
                {
                    // Note: this gameObject will not be a child of the tracked image
                    var originTransform = Instantiate(m_OriginTransformPrefab);
                    originTransform.transform.localPosition = Vector3.zero;
                    originTransform.transform.localRotation = Quaternion.identity;
                    originTransform.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);// Adjust scale if necessary
                    m_InstantiatedOriginTransforms[trackedImage.trackableId] = originTransform;
                }
            }

            foreach (var trackedImage in eventArgs.updated)
            {
                // Set condition to update origin transform here
                if (AreAllCornersInsideScreen(trackedImage.transform) && 
                    IsCameraLookingAtImage(trackedImage.transform) &&
                    angle < m_updatedImageTargetAngleThreshold &&
                    distance < (m_DistanceCoefficient * trackedImage.size.x - m_DistanceOffset)
                    )
                {
                    // Update smoothing buffers
                    AddPoseSample(trackedImage.trackableId, trackedImage.transform.position, trackedImage.transform.rotation);

                    // Apply smoothed pose if we have an instantiated origin transform
                    if (m_InstantiatedOriginTransforms.TryGetValue(trackedImage.trackableId, out var originTransform) && originTransform != null)
                    {
                        var smoothedPosition = GetAveragePosition(m_PositionHistory[trackedImage.trackableId]);
                        var smoothedRotation = GetAverageRotation(m_RotationHistory[trackedImage.trackableId]);
                        originTransform.transform.localPosition = smoothedPosition;
                        originTransform.transform.localRotation = smoothedRotation;
                    }
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
                if (m_InstantiatedOriginTransforms.TryGetValue(trackedImagePair.Value.trackableId, out var originTransform))
                {
                    if (originTransform != null)
                        Destroy(originTransform);
                    m_InstantiatedOriginTransforms.Remove(trackedImagePair.Value.trackableId);
                }
                // Clear pose history
                m_PositionHistory.Remove(trackedImagePair.Value.trackableId);
                m_RotationHistory.Remove(trackedImagePair.Value.trackableId);
            }
        }

        void AddPoseSample(TrackableId id, Vector3 position, Quaternion rotation)
        {
            if (!m_PositionHistory.TryGetValue(id, out var posQueue))
            {
                posQueue = new Queue<Vector3>(m_PoseSmoothingWindow);
                m_PositionHistory[id] = posQueue;
            }
            if (!m_RotationHistory.TryGetValue(id, out var rotQueue))
            {
                rotQueue = new Queue<Quaternion>(m_PoseSmoothingWindow);
                m_RotationHistory[id] = rotQueue;
            }

            if (posQueue.Count >= m_PoseSmoothingWindow)
                posQueue.Dequeue();
            if (rotQueue.Count >= m_PoseSmoothingWindow)
                rotQueue.Dequeue();

            posQueue.Enqueue(position);
            rotQueue.Enqueue(rotation);
        }

        Vector3 GetAveragePosition(Queue<Vector3> positions)
        {
            if (positions == null || positions.Count == 0)
                return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var p in positions)
                sum += p;
            return sum / positions.Count;
        }

        Quaternion GetAverageRotation(Queue<Quaternion> rotations)
        {
            if (rotations == null || rotations.Count == 0)
                return Quaternion.identity;

            Quaternion first = default;
            bool hasFirst = false;
            float x = 0f, y = 0f, z = 0f, w = 0f;

            foreach (var q in rotations)
            {
                var qn = q;
                if (!hasFirst)
                {
                    first = qn;
                    hasFirst = true;
                }
                // Ensure same hemisphere to avoid cancellation
                if (Quaternion.Dot(qn, first) < 0f)
                {
                    qn.x = -qn.x; qn.y = -qn.y; qn.z = -qn.z; qn.w = -qn.w;
                }
                x += qn.x; y += qn.y; z += qn.z; w += qn.w;
            }

            float mag = Mathf.Sqrt(x * x + y * y + z * z + w * w);
            if (mag > 1e-6f)
            {
                x /= mag; y /= mag; z /= mag; w /= mag;
            }
            return new Quaternion(x, y, z, w);
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

        Vector3[] GetImageCorners(Transform transform)
        {
            // because the tracked image size is 1x1 in local space
            var halfSizeX = 0.5f;//transform.localScale.x * 0.5f;
            var halfSizeY = 0.5f;//transform.localScale.y * 0.5f;
            return new Vector3[]
            {
                new Vector3(-halfSizeX, 0, -halfSizeY),
                new Vector3(halfSizeX, 0, -halfSizeY),
                new Vector3(-halfSizeX, 0, halfSizeY),
                new Vector3(halfSizeX, 0, halfSizeY)
            };
        }

        bool IsCornerOnScreen(Transform transform, Vector3 localCorner)
        {
            var worldCornerPos = transform.TransformPoint(localCorner);
            var screenPoint = Camera.main.WorldToScreenPoint(worldCornerPos);
            return screenPoint.z > 0 &&
                   screenPoint.x > 0 && screenPoint.x < Screen.width &&
                   screenPoint.y > 0 && screenPoint.y < Screen.height;
        }

        bool IsCameraLookingAtImage(Transform transform)
        {
            if (Camera.main == null)
                return false;

            // Get the image center position
            var imageCenterWorldPos = transform.position;

            // Get the vector from camera to image center
            var directionToImage = (imageCenterWorldPos - Camera.main.transform.position).normalized;

            // Get the camera's forward direction
            var cameraForward = Camera.main.transform.forward;

            // Calculate the angle between camera forward and direction to image
            float dotProduct = Vector3.Dot(cameraForward, directionToImage);

            // Get half of the camera's field of view in radians
            float halfFOV = Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad;

            // Check if the angle is within the camera's field of view
            float angleToImage = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f));

            return angleToImage <= halfFOV;
        }

        bool AreAllCornersInsideScreen(Transform transform)
        {
            var corners = GetImageCorners(transform);

            foreach (var corner in corners)
            {
                if (!IsCornerOnScreen(transform, corner))
                {
                    return false;
                }
            }

            return true;
        }

        public void AddCornerSpheres(Transform transform)
        {
            var corners = GetImageCorners(transform);

            foreach (var corner in corners)
            {
                if (true)//(IsCornerOnScreen(transform, corner))
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.SetParent(transform, false);
                    sphere.transform.localPosition = corner;
                    sphere.transform.localScale = Vector3.one * 0.03f;
                }
            }
        }

        public Dictionary<TrackableId, GameObject> m_InstantiatedOriginTransforms = new Dictionary<TrackableId, GameObject>();
    }
}
