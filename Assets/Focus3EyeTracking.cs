using UnityEngine;
using System;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using Wave.Essence.Eye;

public class Focus3EyeTracking : MonoBehaviour
{
    // Serial port configuration for data output
    public SerialPort _serialPort;
    public SerialPort _serialPortVertical;
    public string EyeDataCOMPortHorz = "COM6";
    public string EyeDataCOMPortVert = "COM25";
    public bool enablePortHorizontal = true;
    public bool enablePortVertical = true;

    // File output configuration
    public static string UserID = "1";
    string File_Path;
    Stopwatch stopwatch;

    // Eye tracking data
    private Vector3 eyeDirection;
    //private Vector3 leftEyeDirection;
    //private Vector3 rightEyeDirection;
    private Vector3 leftEyePosition;
    private Vector3 rightEyePosition;
    private float leftEyeOpenness;
    private float rightEyeOpenness;
    private float convergenceDistance;
    //private bool eyesValid;

    void Start()
    {
        UnityEngine.Debug.Log("Eye Tracking: Started");
        // start streaming with serial ports 
        InitializeSerialPorts();
        InitializeFileOutput();
        stopwatch = new Stopwatch(); // initialize time
        stopwatch.Start();

        // request permission 
        if (!EyeManager.Instance.IsEyeTrackingAvailable())
        {
            UnityEngine.Debug.LogError("Eye tracking is not available on this device.");
            return;
        }

        UnityEngine.Debug.Log("Eye tracking is enabled.");

    }

    // Data is sent to computer using COM20 and is returned in COM6 and COM25
    void InitializeSerialPorts()
    {
        if (enablePortHorizontal)
        {
            _serialPort = new SerialPort(EyeDataCOMPortHorz, 115200)
            {
                ReadTimeout = 100,
                WriteTimeout = 100,
                DtrEnable = true,
                RtsEnable = true
            };
            _serialPort.Open();
        }

        if (enablePortVertical)
        {
            _serialPortVertical = new SerialPort(EyeDataCOMPortVert, 115200)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                DtrEnable = true,
                RtsEnable = true
            };
            _serialPortVertical.Open();

        }
    }
    // these might have gotten edited, but creates file with eye tracking data

    //extra vars:
    /* 
    eyeDirection
    */

    void InitializeFileOutput()
    {
        File_Path = $"{Directory.GetCurrentDirectory()}\\\\Focus3EyeTracking_{UserID}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string header = "time(100ns),convergenceDistance,eyeOpenness\n";
        File.WriteAllText(File_Path, header);
    }
/*
    void Update()
    {
        if (!EyeManager.Instance.IsEyeTrackingAvailable())
        {
            UnityEngine.Debug.LogWarning("Eye tracking not enabled!");
            return;
        }

        Vector3 eyeDirection = EyeManager.Instance.GetEyeDirectionCombined();
        //Vector3 leftEyeDirection = EyeManager.GetLeftEyeDirectionNormalized();
        long timestamp = DateTime.Now.Ticks;
        // {eyeGazeDirection.x},{eyeGazeDirection.y},{eyeGazeDirection.z}
        // Get eye tracking data

        string logEntry = $"{timestamp}," +
                            $"{Vector3.Distance(leftEyePosition, rightEyePosition)},{(leftEyeOpenness + rightEyeOpenness) / 2}\n";
            File.AppendAllText(File_Path, logEntry);

        string logEntry = $"{timestamp},{eyeDirection.x:F2},{eyeDirection.y:F2},{Vector3.Distance(leftEyePosition, rightEyePosition)},{(leftEyeOpenness + rightEyeOpenness) / 2}\n";
        File.AppendAllText(File_Path, logEntry);
        // SendToSerialPorts(eyeDirection.x.ToString(), eyeDirection.y.ToString()); // check for error here
        SendToSerialPorts(eyeDirection.x.ToString("F2"), eyeDirection.y.ToString("F2"));
    }
*/
    void Update()
    {
        // Check if eye tracking is available
        if (!EyeManager.Instance.IsEyeTrackingAvailable())
        {
            UnityEngine.Debug.LogWarning("Eye tracking not available!");
            return;
        }

        // Enable eye tracking if not already enabled
        EyeManager.Instance.EnableEyeTracking = true;

        // Retrieve combined eye direction
        Vector3 eyeDirection = Vector3.zero;
        if (EyeManager.Instance.GetCombindedEyeDirectionNormalized(out eyeDirection))
        {
            long timestamp = DateTime.Now.Ticks;

            // Log the eye direction data
            string logEntry = $"{timestamp},{eyeDirection.x:F3},{eyeDirection.y:F3},{eyeDirection.z:F3}\n";
            File.AppendAllText(File_Path, logEntry);

            // Send to COM ports
            SendToSerialPorts(eyeDirection.x.ToString("F3"), eyeDirection.y.ToString("F3"));

            UnityEngine.Debug.Log($"Eye Direction: {eyeDirection}");
        }
        else
        {
            UnityEngine.Debug.LogWarning("Failed to retrieve eye tracking data.");
        }
    }


    void SendToSerialPorts(string eyeDataHorizontal, string eyeDataVertical)
    {
        try
        {
            if (enablePortHorizontal && _serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.WriteLine(eyeDataHorizontal);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Serial port error (horizontal): {e.Message}");
        }

        try
        {
            if (enablePortVertical && _serialPortVertical != null && _serialPortVertical.IsOpen)
            {
                _serialPortVertical.WriteLine(eyeDataVertical);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Serial port error (vertical): {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (_serialPort != null && _serialPort.IsOpen)
            _serialPort.Close();

        if (_serialPortVertical != null && _serialPortVertical.IsOpen)
            _serialPortVertical.Close();

        stopwatch.Stop();
    }
}
