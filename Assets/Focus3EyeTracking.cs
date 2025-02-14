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
    private bool eyesValid;

    void Start()
    {
        UnityEngine.Debug.Log("Eye Tracking: Started");
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
            _serialPort = new SerialPort(EyeDataCOMPortHorz)
            {
                BaudRate = 115200,
                ReadTimeout = 100,
                WriteTimeout = 100,
                DtrEnable = true,
                RtsEnable = true
            };
        }

        if (enablePortVertical)
        {
            _serialPortVertical = new SerialPort(EyeDataCOMPortVert)
            {
                BaudRate = 115200,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                DtrEnable = true,
                RtsEnable = true
            };
        }
    }
    // these might have gotten edited, but creates file with eye tracking data
    void InitializeFileOutput()
    {
        File_Path = $"{Directory.GetCurrentDirectory()}\\Focus3EyeTracking_{UserID}.txt";
        string header = "time(100ns),eyeDirection,convergenceDistance,eyeOpenness\n";
        File.WriteAllText(File_Path, header);
    }

    void Update()
    {
        if (!EyeManager.Instance.IsEyeTrackingAvailable())
        {
            UnityEngine.Debug.LogWarning("Eye tracking not enabled!");
            return;
        }

        //Vector3 eyeDirection = EyeManager.Instance.CombinedEyeDirection();
        //Vector3 leftEyeDirection = EyeManager.GetLeftEyeDirectionNormalized();
        long timestamp = DateTime.Now.Ticks;
        // {eyeGazeDirection.x},{eyeGazeDirection.y},{eyeGazeDirection.z}
        // Get eye tracking data
        string logEntry = $"{timestamp}," +
                            $"{Vector3.Distance(leftEyePosition, rightEyePosition)},{(leftEyeOpenness + rightEyeOpenness) / 2}\n";
            File.AppendAllText(File_Path, logEntry);

        // SendToSerialPorts(eyeDirection.x.ToString(), eyeDirection.y.ToString()); // check for error here
        SendToSerialPorts(eyeDirection.x.ToString(), eyeDirection.y.ToString());
    }

    void SendToSerialPorts(string eyeDataHorizontal, string eyeDataVertical)
    {   
        try 
        {   //Write to file 
            if (enablePortHorizontal && _serialPort != null && !_serialPort.IsOpen)
            {
                _serialPort.Open();
                _serialPort.WriteLine(eyeDataHorizontal);
                _serialPort.Close();
            }
        } //Cannot send to serial port 
        catch (Exception e) 
        {
                UnityEngine.Debug.LogError($"Serial port error (horizontal): {e.Message}");
        }

        try
        {
            if (enablePortVertical && _serialPortVertical != null && !_serialPortVertical.IsOpen)
            {
                _serialPortVertical.Open();
                _serialPortVertical.WriteLine(eyeDataVertical);
                _serialPortVertical.Close();
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