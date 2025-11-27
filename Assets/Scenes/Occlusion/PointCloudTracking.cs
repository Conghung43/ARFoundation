using System;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class PointCloudTracking : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager m_CameraManager;

    /// <summary>
    /// GetKey or set the <c>ARCameraManager</c>.
    /// </summary>
    public ARCameraManager cameraManager
    {
        get => m_CameraManager;
        set => m_CameraManager = value;
    }

    [SerializeField]
    [Tooltip("The AROcclusionManager which will produce human depth and stencil textures.")]
    AROcclusionManager m_OcclusionManager;

    public AROcclusionManager occlusionManager
    {
        get => m_OcclusionManager;
        set => m_OcclusionManager = value;
    }

    public static float depth;


    XRCpuImage.Transformation m_Transformation = XRCpuImage.Transformation.MirrorY;

    /// <summary>
    /// Cycles the image transformation to the next case.
    /// </summary>
    public void CycleTransformation()
    {
        m_Transformation = m_Transformation switch
        {
            XRCpuImage.Transformation.None => XRCpuImage.Transformation.MirrorX,
            XRCpuImage.Transformation.MirrorX => XRCpuImage.Transformation.MirrorY,
            XRCpuImage.Transformation.MirrorY => XRCpuImage.Transformation.MirrorX | XRCpuImage.Transformation.MirrorY,
            _ => XRCpuImage.Transformation.None
        };
    }


    void OnEnable()
    {

        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    void UpdateEnvironmentDepthImage()
    {

        // Attempt to get the latest environment depth image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        if (m_OcclusionManager && m_OcclusionManager.TryAcquireEnvironmentDepthCpuImage(out var image))
        {
            using (image)
            {
                UpdateRawImage(image, m_Transformation);
            }
        }
    }

    public static Texture2D texture;
    void UpdateRawImage(XRCpuImage cpuImage, XRCpuImage.Transformation transformation)
    {

        // If the texture hasn't yet been created, or if its dimensions have changed, (re)create the texture.
        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, cpuImage.format.AsTextureFormat(), false);
        }

        // For display, we need to mirror about the vertical access.
        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, cpuImage.format.AsTextureFormat(), transformation);

        // GetKey the Texture2D's underlying pixel buffer.
        var rawTextureData = texture.GetRawTextureData<byte>();

        // Make sure the destination buffer is large enough to hold the converted data (they should be the same size)
        //Debug.Assert(rawTextureData.Length == cpuImage.GetConvertedDataSize(conversionParams.outputDimensions, conversionParams.outputFormat),
        //    "The Texture2D is not the same size as the converted data.");

        // Perform the conversion.
        cpuImage.Convert(conversionParams, rawTextureData);

        // "Apply" the new pixel data to the Texture2D.
        texture.Apply();

        try
        {
            depth = ReadDepthValue(texture, (int)texture.width / 2, (int)texture.height / 2);
            //DeviceLog.Instance.Log(1, texture.width.ToString() + "  " + texture.height.ToString());
        }
        catch
        {
            //logMessage.text += ex.Message;
        }

    }


    public static float ReadDepthValue(Texture2D depthTexture, int x, int y)
    {
        // Convert the pixel coordinates to the corresponding UV coordinates
        Vector2 uv = new Vector2(x / (float)depthTexture.width, y / (float)depthTexture.height);

        // Read the depth value at the UV coordinates from the depth texture
        float depthValue = depthTexture.GetPixelBilinear(uv.x, uv.y).r;

        return depthValue;
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {

        UpdateEnvironmentDepthImage();
    }
}
