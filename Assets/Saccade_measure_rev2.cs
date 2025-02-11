using UnityEngine;
using System;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using Meta.XR;

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
    private Vector3 leftEyeDirection;
    private Vector3 rightEyeDirection;
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
        stopwatch = new Stopwatch();
        stopwatch.Start();

        // Request eye tracking permissions
        OVRPermissionsRequester.RequestPermissions(new[] {
            OVRPermissionsRequester.Permission.EyeTracking
        });
    }

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

    void InitializeFileOutput()
    {
        File_Path = $"{Directory.GetCurrentDirectory()}\\Focus3EyeTracking_{UserID}.txt";
        string header = "time(100ns),eyeDirectionX,eyeDirectionY,eyeDirectionZ,convergenceDistance,eyeOpenness\n";
        File.WriteAllText(File_Path, header);
    }

    void Update()
    {
        if (!OVREyeGaze.instance.EyeTrackingEnabled)
        {
            UnityEngine.Debug.LogWarning("Eye tracking not enabled!");
            return;
        }

        // Get eye tracking data
        eyesValid = OVREyeGaze.instance.GetEyeGazePosition(out Vector3 eyeGazePosition) &&
                    OVREyeGaze.instance.GetEyeGazeDirection(out Vector3 eyeGazeDirection);

        if (eyesValid)
        {
            // Format eye tracking data
            string eyeDataHorizontal = eyeGazeDirection.x.ToString();
            string eyeDataVertical = eyeGazeDirection.y.ToString();
            long timestamp = DateTime.Now.Ticks;

            // Write to file
            string logEntry = $"{timestamp},{eyeGazeDirection.x},{eyeGazeDirection.y},{eyeGazeDirection.z}," +
                            $"{Vector3.Distance(leftEyePosition, rightEyePosition)},{(leftEyeOpenness + rightEyeOpenness) / 2}\n";
            File.AppendAllText(File_Path, logEntry);

            // Send to serial ports
            if (enablePortHorizontal && !_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                    _serialPort.WriteLine(eyeDataHorizontal);
                    _serialPort.Close();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Serial port error (horizontal): {e.Message}");
                }
            }

            if (enablePortVertical && !_serialPortVertical.IsOpen)
            {
                try
                {
                    _serialPortVertical.Open();
                    _serialPortVertical.WriteLine(eyeDataVertical);
                    _serialPortVertical.Close();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Serial port error (vertical): {e.Message}");
                }
            }
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