﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CameraModule : MonoBehaviour
{
    #region Constants
    public const int ColorWidth = 640;
    public const int ColorHeight = 480;

    public const int DepthWidth = CameraModule.ColorWidth / 8;
    public const int DepthHeight = CameraModule.ColorHeight / 8;

    private static readonly Vector2 fieldOfView = new Vector2(69.4f, 42.5f);

    private static float minRange = 1.05f;
    private static float minCode = 0.0f;
    private static float maxRange = 100f;
    private static float maxCode = 0.0f;
    #endregion

    private byte[] colorImageRaw;
    private bool isColorImageRawValid = false;
    private float[][] depthImage;
    private bool isDepthImageValid = false;
    private byte[] depthImageRaw;
    private bool isDepthImageRawValid = false;

    private Camera colorCamera;
    private Camera depthCamera;

    public RenderTexture ColorImage
    {
        get
        {
            return this.colorCamera.targetTexture;
        }
    }

    public byte[] ColorImageRaw
    {
        get
        {
            if (!isColorImageRawValid)
            {
                RenderTexture activeRenderTexture = RenderTexture.active;
                RenderTexture.active = this.ColorImage;

                this.colorCamera.Render();

                Texture2D image = new Texture2D(this.ColorImage.width, this.ColorImage.height);
                image.ReadPixels(new Rect(0, 0, this.ColorImage.width, this.ColorImage.height), 0, 0);
                image.Apply();
                RenderTexture.active = activeRenderTexture;

                byte[] bytes = image.GetRawTextureData();

                int bytesPerRow = CameraModule.ColorWidth * sizeof(float);
                for (int r = 0; r < CameraModule.ColorHeight; r++)
                {
                    Buffer.BlockCopy(bytes, (CameraModule.ColorHeight - r - 1) * bytesPerRow, this.colorImageRaw, r * bytesPerRow, bytesPerRow);
                }

                Destroy(image);
                this.isColorImageRawValid = true;
            }
            return this.colorImageRaw;
        }
    }

    public float[][] DepthImage
    {
        get
        {
            if (!isDepthImageValid)
            {
                for (int r = 0; r < CameraModule.DepthHeight; r++)
                {
                    for (int c = 0; c < CameraModule.DepthWidth; c++)
                    {
                        Ray ray = this.depthCamera.ViewportPointToRay(new Vector3(
                            (float)c / (CameraModule.DepthWidth - 1),
                            (CameraModule.DepthHeight - r - 1.0f) / (CameraModule.DepthHeight - 1),
                            0));

                        if (Physics.Raycast(ray, out RaycastHit raycastHit, CameraModule.maxRange))
                        {
                            this.depthImage[r][c] = raycastHit.distance > CameraModule.minRange ? raycastHit.distance * 10 : CameraModule.minCode;
                        }
                        else
                        {
                            this.depthImage[r][c] = CameraModule.maxCode;
                        }
                    }
                }

                this.isDepthImageValid = true;
            }

            return this.depthImage;
        }
    }

    public byte[] DepthImageRaw
    {
        get
        {
            if (!this.isDepthImageRawValid)
            {
                for (int r = 0; r < CameraModule.DepthHeight; r++)
                {
                    Buffer.BlockCopy(this.DepthImage[r], 0,
                                    this.depthImageRaw, r * CameraModule.DepthWidth * sizeof(float),
                                    CameraModule.DepthWidth * sizeof(float));
                }
                this.isDepthImageRawValid = true;
            }

            return this.depthImageRaw;
        }
    }

    public void VisualizeDepth(Texture2D texture)
    {
        if (texture.width != CameraModule.DepthWidth || texture.height != CameraModule.DepthHeight)
        {
            throw new Exception("texture dimensions must match depth image dimensions");
        }

        Unity.Collections.NativeArray<Color32> rawData = texture.GetRawTextureData<Color32>();

        for (int i = 0; i < rawData.Length; i++)
        {
            rawData[i] = Hud.SensorBackgroundColor;
        }

        for (int r = 0; r < CameraModule.DepthHeight; r++)
        {
            for (int c = 0; c < CameraModule.DepthWidth; c++)
            {
                if (this.DepthImage[r][c] != CameraModule.minCode && this.DepthImage[r][c] != CameraModule.maxCode)
                {
                    rawData[(CameraModule.DepthHeight - r - 1) * texture.width + c] = CameraModule.InterpolateDepthColor(DepthImage[r][c]);
                }
            }
        }

        texture.Apply();
    }

    private void Start()
    {
        Camera[] cameras = this.GetComponentsInChildren<Camera>();
        this.colorCamera = cameras[0];
        this.depthCamera = cameras[1];

        this.colorCamera.fieldOfView = CameraModule.fieldOfView.y;
        this.depthCamera.fieldOfView = CameraModule.fieldOfView.y;

        this.depthImage = new float[CameraModule.DepthHeight][];
        for (int r = 0; r < CameraModule.DepthHeight; r++)
        {
            this.depthImage[r] = new float[CameraModule.DepthWidth];
        }

        this.depthImageRaw = new byte[sizeof(float) * CameraModule.DepthHeight * CameraModule.DepthWidth];
        this.colorImageRaw = new byte[sizeof(float) * CameraModule.ColorWidth * CameraModule.ColorHeight];
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log(this.DepthImage);
        }

        this.isColorImageRawValid = false;
        this.isDepthImageValid = false;
        this.isDepthImageRawValid = false;
    }

    private static Color InterpolateDepthColor(float depth)
    {
        depth /= 10 * CameraModule.maxRange;
        if (depth < 0.05f)
        {
            return Color.Lerp(Color.white, Color.yellow, depth / 0.05f);
        }
        else if (depth < 0.2f)
        {
            return Color.Lerp(Color.yellow, Color.red, (depth - 0.05f) / 0.15f);
        }
        else if (depth < 0.6f)
        {
            return Color.Lerp(Color.red, Color.blue, (depth - 0.2f) / 0.4f);
        }
        else
        {
            return Color.Lerp(Color.blue, Hud.SensorBackgroundColor, (depth - 0.6f) / 0.4f);
        }
    }
}
