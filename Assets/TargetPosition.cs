using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Diagnostics;

using Wave.Native;
using Wave.Essence;
using Wave.Essence.TrackableMarker;
using Wave.Essence.Controller;
using Wave.Essence.Input;
using Wave.XR;
using Wave.OpenXR;

using UnityEngine.Android;
using System.Runtime.InteropServices; // Android Libraries  

public class TargetPosition : MonoBehaviour
{
    // ----------------------------------------------------------------------------------------------------------------
    //  Assigning the game objects (spheres) to the script.
    // ----------------------------------------------------------------------------------------------------------------

    public GameObject CenterTarget;
    public GameObject CenterTarget2;

    // The serial port - adapted to android

    public SerialPort SerialPort;
    public string TargetPositionCOMPort = "/dev/ttyUSB0";
    private bool isSerialInitialized = false;

    float H1Angle;
    float H2Angle;
    float V1Angle;
    float V2Angle;
    float TargetInRad;
    float TargetInRad2;
    float TargetVInRad;
    float TargetVInRad2;
    float TargOrTime;
    string Target;
    string value;

    // heap allocation for GameObject properties
    float NewScale;
    Vector3 CenterScale;
    Vector3 CenterScale2;

    //Andoird requries access to COM20
    private const string ACTION_USB_PERMISSION = "com.android.example.USB_PERMISSION";
    private bool hasUsbPermission = false;

    // Start is called before the first frame update
    void Start()
    {
        CenterTarget = GameObject.Find("CenterTarget");
        CenterTarget2 = GameObject.Find("CenterTarget2");

        if (Application.platform == RuntimePlatform.Android)
        {
            RequestUsbPermission();
        }
        InitializeSerialPort();

        InvokeRepeating(nameof(UpdateTargetPositions), 0.1f, 0.004f);

    }
    private void RequestUsbPermission()
    {
        if (!Permission.HasUserAuthorizedPermission("android.permission.USB_PERMISSION"))
        {
            Permission.RequestUserPermission("android.permission.USB_PERMISSION");
        }
    }
    private void InitializeSerialPort()
    {
        try
        {
            SerialPort = new SerialPort(TargetPositionCOMPort, 9600)
            {
                Encoding = System.Text.Encoding.UTF8,
                DtrEnable = true,
                RtsEnable = true
            };
            isSerialInitialized = true;
            Debug.Log("Serial port initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing serial port: {e.Message}");
            isSerialInitialized = false;
        }
    }
    private void UpdateTargetPositions()
    {
        if (!isSerialInitialized || !ReadSerialData())
            return;

        // Convert angles to radians
        TargetInRad = H1Angle * (Mathf.PI / 180);
        TargetInRad2 = H2Angle * (Mathf.PI / 180);
        TargetVInRad = V1Angle * (Mathf.PI / 180);
        TargetVInRad2 = V2Angle * (Mathf.PI / 180);

        // Update target positions
        UpdateTargetPosition(CenterTarget, TargetInRad, TargetVInRad, H1Angle, V1Angle);
        UpdateTargetPosition(CenterTarget2, TargetInRad2, TargetVInRad2, H2Angle, V2Angle);

        // Log data
        LogTargetData();

        // Update scales
        UpdateTargetScales();
    }
    private bool ReadSerialData()
    {
        try
        {
            if (!SerialPort.IsOpen)
                SerialPort.Open();

            Target = SerialPort.ReadLine();
            SerialPort.Close();

            if (string.IsNullOrEmpty(Target))
                return false;

            ParseSerialData(Target);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading serial data: {e.Message}");
            return false;
        }
    }
    private void ParseSerialData(string data)
    {
        string[] inputValues = data.Split(' ');
        
        foreach (string value in inputValues)
        {
            if (value.StartsWith("H1:"))
            {
                float H1Value = ExtractValue(value);
                if (H1Value < 23)
                {
                    CenterTarget.SetActive(true);
                    H1Angle = H1Value;
                }
                else if (H1Value > 100)
                {
                    CenterTarget.SetActive(false);
                    H1Angle = H1Value;
                }
            }
            else if (value.StartsWith("V1:"))
            {
                float V1Value = ExtractValue(value);
                if (V1Value < 23)
                {
                    CenterTarget.SetActive(true);
                    V1Angle = V1Value;
                }
                else if (V1Value > 100)
                {
                    CenterTarget.SetActive(false);
                    V1Angle = V1Value;
                }
            }
            else if (value.StartsWith("H2:"))
            {
                float H2Value = ExtractValue(value);
                if (H2Value < 23)
                {
                    CenterTarget2.SetActive(true);
                    H2Angle = H2Value;
                }
                else if (H2Value > 100)
                {
                    CenterTarget2.SetActive(false);
                    H2Angle = H2Value;
                }
            }
            else if (value.StartsWith("V2:"))
            {
                float V2Value = ExtractValue(value);
                if (V2Value < 23)
                {
                    CenterTarget2.SetActive(true);
                    V2Angle = V2Value;
                }
                else if (V2Value > 100)
                {
                    CenterTarget2.SetActive(false);
                    V2Angle = V2Value;
                }
            }
        }
    }
    private void UpdateTargetPosition(GameObject target, float horizontalRad, float verticalRad, float hAngle, float vAngle)
    {
        if (hAngle > 100 || vAngle > 100)
            return;

        Vector3 targetPosition = new Vector3(
            0 * Mathf.Cos(horizontalRad) + 5 * Mathf.Sin(horizontalRad),
            5 * Mathf.Sin(verticalRad),
            5
        );

        target.transform.localPosition = targetPosition;
    }
    private void UpdateTargetScales()
    {
        NewScale = 7 * Mathf.Tan(1.0f / 2 * (Mathf.PI / 180));
        Vector3 newScale = new Vector3(NewScale, NewScale, NewScale);

        CenterTarget.transform.localScale = newScale;
        CenterTarget2.transform.localScale = newScale;
    }
    private float ExtractValue(string data)
    {
        string[] valueParts = data.Split(':');
        return float.Parse(valueParts[1]);
    }
    private void LogTargetData()
    {
        string value = 
            $"H1 Angle: {H1Angle} H2 Angle: {H2Angle} " +
            $"V1 Angle: {V1Angle} V2 Angle: {V2Angle} " +
            $"Unix Time(100ns): {DateTime.Now.Ticks}" +
            Environment.NewLine;

        File.AppendAllText($"TargetData_{DateTime.Now.ToString("m")}.txt", value);
    }

    private void OnDestroy()
    {
        if (SerialPort != null && SerialPort.IsOpen)
        {
            SerialPort.Close();
        }
    }
}