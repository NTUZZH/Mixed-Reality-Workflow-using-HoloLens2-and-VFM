using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;
using TMPro;
using System.Threading;
using UnityEngine.Windows.WebCam;
using UnityEngine.Networking;

/// <summary>
/// DepthCapture Class is used to realize the Night Mode in the paper. The scenario under this condition is usually dark, which means the simple RGB image is lack of robustness
/// to be segmented by AI model successfully; Thus, we leveraged the Time-of-Flight camera equipped on the HoloLens2 to retrieve the depth information,and used this depth
/// information to generate a Surface Normal Image to replace the RGB image to do the segmentation and the back-projection process. The SN process is on the external server built within a python-based IDE.
/// </summary>
public class DepthCapture : MonoBehaviour
{
    ResearchModeSensors myHololens;
    bool captureRequested = false;
    public ImageProcessing imageprocessing;
    public Texture2D myTexture; //..
    public Texture2D NextTexture; //..
    private int clickCount = 0; // .. 用于跟踪点击次数的变量
    public Matrix4x4 depthCameraToWorldMatrix; // save transformation matrix
    public Matrix4x4 depthCameraProjectionMatrix; // save projection matrix
    private byte[] snimageBytes;// save the SN image in Byte format
    private byte[] binaryUint16arr;// save Depth image
    public BoundingBoxHandler boundingBoxHandler;

    void Start()
    {
        // Initialization at the beginning
        myHololens = new ResearchModeSensors(false, true, false); // Long Throw Depth Mode
    }

    // Each frame is detecting if the button has been pressed.
    void Update()
    {
        if (captureRequested && myHololens.outputDepth != null)
        {
            CaptureDepthData();
            captureRequested = false;
        }
    }
    //public void DepthButtonHL2() // Function used to activate the Night Mode.
    //{
    //    imageprocessing.Information.text = "Surface Normal Image Successfully Generated";
    //    captureRequested = true;
    //    imageprocessing.isDepth = true;
    //    myHololens.StartSensorStream(); // capture the depth image

    //    boundingBoxHandler.FitForNightModeRawImage(); // resize the Unity component

    //    imageprocessing.rawImage.texture = myTexture; // .. Here is the SN image
    //}

    public void OnButtonClick() //..
    {
        clickCount++; // 增加点击计数

        if (clickCount == 1)
        {
            DepthButtonHL2(); // 第一次点击调用此函数
        }
        else if (clickCount == 2)
        {
            DepthButtonNext(); // 第二次点击调用此函数
            clickCount = 0; // 可选：重置计数以便循环操作
        }
    }
    public void DepthButtonHL2() // ..
    {
        imageprocessing.Information.text = "Surface Normal Image Successfully Generated";

        boundingBoxHandler.FitForNightModeRawImage(); // resize the Unity component

        imageprocessing.rawImage.texture = myTexture; // .. Here is the SN image
    }

    public void DepthButtonNext()
    {
        imageprocessing.rawImage.texture = NextTexture; // .. Here is the Segmented Image

        imageprocessing.Information.text = "Finished!";
    }


    /// <summary>
    /// Save depth information
    /// </summary>
    private void CaptureDepthData()
    {
        // binaryUnit16arr is the final form of the depth information, transferred from the original depth information with Unit16 format.
        binaryUint16arr = new byte[myHololens.depthW * myHololens.depthH * 2];

        for (int i = 0; i < myHololens.outputDepth.Length; i++)
        {
            UInt16 depthValue = myHololens.outputDepth[i];
            binaryUint16arr[2 * i + 1] = (byte)(depthValue >> 8);// 保存高8位
            binaryUint16arr[2 * i] = (byte)(depthValue);// 保存低8位
        }

        depthCameraToWorldMatrix = myHololens.C2W; // pass the value of related matrices from DLL to Unity Environment for 
        depthCameraProjectionMatrix = myHololens.PM; // retrieving the 3D coordinates of the corresponding segmented 2D target on SN image.

        myHololens.StopAllSensorStream(); // Stop the sensor stream after capturings.
        SendToServerNightMode(); 
    }

    /// <summary>
    /// Send to Server and Return processed SN image.
    /// </summary>
    public void SendToServerNightMode()
    {
        string serverUrl = "http://10.96.7.116:8000/process"; // workstation IP address

        WWWForm form = new WWWForm(); // use WWWform method send the information to server side.
        form.AddBinaryData("depthData", binaryUint16arr, "depthData.bin", "application/octet-stream");

        UnityWebRequest www = UnityWebRequest.Post(serverUrl, form);
        StartCoroutine(SendDataCoroutine(www));
    }

    private IEnumerator SendDataCoroutine(UnityWebRequest www)
    {
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Return the SN image and incorporate into the Main pipeline.
            byte[] resultData = www.downloadHandler.data;
            Texture2D SNTexture = new Texture2D(2, 2); 
            SNTexture.LoadImage(resultData); 
            imageprocessing.rawImage.texture = SNTexture; // Visualize the SN image
            snimageBytes = SNTexture.EncodeToJPG(); // Convert the Texture2D to a byte array for further passing its values. 

            imageprocessing.imageBytes = snimageBytes; // pass the SN image into the original pipeline
        }
    }

}

/// <summary>
/// Inspired and Modified by wizroad's GitHub project:https://github.com/wizroad/hololens2_depth_imu_in_unity
/// </summary>
public class ResearchModeSensors
{
    [DllImport("dll_Test_UWP")]
    public static extern void GetSensorThread(ref int stopSign, int mode,
     ref float pImuAccX, ref float pImuAccY, ref float pImuAccZ,
     ref float pImuGyroX, ref float pImuGyroY, ref float pImuGyroZ,
     ref float pImuMagX, ref float pImuMagY, ref float pImuMagZ,
     UInt16[] pDepthImg, UInt16[] pAbImg, Matrix4x4 depthCameraToWorldMatrix, Matrix4x4 depthCameraProjectionMatrix); // import from DLL,used to interact with sensors

    public float outputImuAccX, outputImuAccY, outputImuAccZ,
        outputImuGyroX, outputImuGyroY, outputImuGyroZ,
        outputImuMagX, outputImuMagY, outputImuMagZ; // store IMU data

    public Matrix4x4 C2W, PM;// store transformation matrices

    // UInt16 是 C# 中的一种数据类型，表示无符号的 16 位整数。
    //这个数组用于存储深度传感器捕获的每个像素的深度值。
    //例如，如果深度图像的尺寸是 320x288 像素，那么这个数组将包含 320x288 个 UInt16 元素，每个元素代表一个像素点的深度值。
    public UInt16[] outputDepth, outputAb;


    public int depthW, depthH; // dimension of depth image

    bool getImu = false;
    bool getDLT = false;
    bool getAHAT = false;

    Thread hololensThread;
    int stopSign;
    int mode;

    public ResearchModeSensors(bool setImu, bool setDLT, bool setAHAT) // Constructor
    {
        if (setDLT && setAHAT)
            throw new Exception("Note that concurrent access to AHAT and Long Throw is currently not supported");
        getImu = setImu;
        getDLT = setDLT;
        getAHAT = setAHAT;

        if (getDLT)
        {
            depthW = 320;
            depthH = 288;
        }
        if (getAHAT)
        {
            depthW = 512;
            depthH = 512;
        }

        if (getDLT || getAHAT)
        {
            outputDepth = new UInt16[depthW * depthH * 1];
            outputAb = new UInt16[depthW * depthH * 1];
        }

        return;
    }

    ~ResearchModeSensors() // Destructor
    {
        if (stopSign == 0)
            StopAllSensorStream();
    }

  public void StartSensorStream()
    {
        stopSign = 0; // activate

        int mode = 0b0;
        if (getImu) mode += 0b100;
        if (getDLT) mode += 0b10;
        if (getAHAT) mode += 0b1;

        hololensThread = new Thread(() => GetSensorThread(ref stopSign, mode,
            ref outputImuAccX, ref outputImuAccY, ref outputImuAccZ,
        ref outputImuGyroX, ref outputImuGyroY, ref outputImuGyroZ,
        ref outputImuMagX, ref outputImuMagY, ref outputImuMagZ,
        outputDepth, outputAb,C2W,PM));

        hololensThread.Start();
    }
    public void StopAllSensorStream()
    {
        stopSign = 1;
        hololensThread.Join();
    }
}