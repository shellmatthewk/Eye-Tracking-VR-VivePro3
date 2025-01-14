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

public class TargetPosition : MonoBehaviour
{
    // ----------------------------------------------------------------------------------------------------------------
    //  Assigning the game objects (spheres) to the script.
    // ----------------------------------------------------------------------------------------------------------------

    public GameObject CenterTarget;
    public GameObject CenterTarget2;

    #region Properties

    // The serial port

    public static SerialPort Instance;
    public SerialPort SerialPort;

    
    public string TargetPositionCOMPort  = "COM20"; // COM20 for USB

    #endregion Properties

    float H1Angle;
    float H2Angle;
    float TargetInRad;
    float TargetInRad2;
    float V1Angle;
    float V2Angle;
    float TargetVInRad;
    float TargetVInRad2;
    float TargOrTime;
    string Target;
    string value;

    // heap allocation for GameObject properties
    float NewScale;
    Vector3 CenterScale;
    Vector3 CenterScale2;
    
    // Start is called before the first frame update
    void Start()
    {
        //SerialPort.ReadTimeout = 1; 
        // very short time, may read empty buffers
        CenterTarget = GameObject.Find("CenterTarget");
        CenterTarget2 = GameObject.Find("CenterTarget2");
        InvokeRepeating(nameof(TargetPosition1), 0.1f, 0.004f); //time sensitive loop
    }

    void TargetPosition1() // takes serial string data from Teensy 4.1 and parses to Unity target coordinates
    {            

        try
        {
            SerialPort = new SerialPort(TargetPositionCOMPort, 9600)//115200?
            {
                Encoding = System.Text.Encoding.UTF8, // important for reading serial string data correctly
                DtrEnable = true,                     // data terminal ready, important for access
                RtsEnable = true
            };
            //UnityEngine.Debug.Log("Connected to COM20");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e.Message);
        }     

        try
        {
            SerialPort.Open(); // try-catch block, if port is already opened it throws exception
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.Log("Error opening serial\n" + ex.Message + "\nExiting program...");
            return;
        }

        Target = SerialPort.ReadLine();
        
        SerialPort.Close();

        if(Target != null){
            
            string[] inputValues = Target.Split(' '); // parses input from teensy 4.1
            float H1Value = 0f;
            float H2Value = 0f;
            float V1Value = 0f;
            float V2Value = 0f;

            foreach (string value in inputValues)
            {
                if (value.StartsWith("H1:"))
                {
                    //float.TryParse(value.Substring(3), out H1Value); //works for 2 horizontal values
                    H1Value = ExtractValue(value);
                    //UnityEngine.Debug.Log("H1:" + H1Value + " Unix Time(100ns): " + DateTime.Now.Ticks);

                    if (H1Value < 23){ //if shutter false
                        CenterTarget.SetActive(true);                
                        H1Angle = H1Value;
                    }
                    if (H1Value > 100){
                        CenterTarget.SetActive(false); // target is not active/visible
                        H1Angle = H1Value; // still set equivalent, set off screen just in case
                        //UnityEngine.Debug.Log("blank" + " Unix Time(100ns): " + DateTime.Now.Ticks);
                    }
                }
                if (value.StartsWith("V1:"))
                {
                    V1Value = ExtractValue(value);
                    UnityEngine.Debug.Log("V1:" + V1Value + " Unix Time(100ns): " + DateTime.Now.Ticks);

                    if (V1Value < 23){ //if shutter false
                        CenterTarget.SetActive(true);                
                        V1Angle = V1Value; 
                    }
                    if (V1Value > 100){
                        CenterTarget.SetActive(false); // target is not active/visible
                        V1Angle = V1Value; // still set equivalent, set off screen just in case
                        //UnityEngine.Debug.Log("blank" + " Unix Time(100ns): " + DateTime.Now.Ticks);
                    }
                }
                if (value.StartsWith("H2:"))
                {
                    //float.TryParse(value.Substring(3), out H2Value); //works for 2 horizontal values
                    H2Value = ExtractValue(value);
                    UnityEngine.Debug.Log("H2:" + H2Value + " Unix Time(100ns): " + DateTime.Now.Ticks);

                    if (H2Value < 23){ //if shutter false
                        CenterTarget2.SetActive(true);                
                        H2Angle = H2Value;
                    }
                    if (H2Value > 100){
                        CenterTarget2.SetActive(false); // target is not active/visible
                        H2Angle = H2Value; // still set equivalent, set off screen just in case
                        //UnityEngine.Debug.Log("blank2" + " Unix Time(100ns): " + DateTime.Now.Ticks);
                    }
                }
                if (value.StartsWith("V2:"))
                {
                    V2Value = ExtractValue(value);
                    //UnityEngine.Debug.Log("V2:" + V2Value + " Unix Time(100ns): " + DateTime.Now.Ticks);

                    if (V2Value < 23){ //if shutter false
                        CenterTarget2.SetActive(true);                
                        V2Angle = V2Value;
                    }
                    if (V2Value > 100){
                        CenterTarget2.SetActive(false); // target is not active/visible
                        V2Angle = V2Value; // still set equivalent, set off screen just in case
                        //UnityEngine.Debug.Log("blank2" + " Unix Time(100ns): " + DateTime.Now.Ticks);
                    }
                }
            }

            //record in TargetData_ .txt                
            value = 
            "H1 Angle: " + H1Angle + " H2 Angle: " + H2Angle + 
            "V1 Angle: " + V1Angle + " V2 Angle: " + V2Angle + 
            " Unix Time(100ns): " + DateTime.Now.Ticks +
            Environment.NewLine;

            File.AppendAllText("TargetData_" + DateTime.Now.ToString("m") + ".txt", value);
        }
        
        // ----------------------------------------------------------------------------------------------------------------
        //  Calculating target position.
        // ----------------------------------------------------------------------------------------------------------------
        // 1. "Angle" input gets converted from degrees to radians because Unitys' Mathf. works with radians, not degrees.

        TargetInRad   = H1Angle   * (Mathf.PI / 180);
        TargetInRad2  = H2Angle  * (Mathf.PI / 180);
        TargetVInRad  = V1Angle  * (Mathf.PI / 180);
        TargetVInRad2 = V2Angle * (Mathf.PI / 180);
        //UnityEngine.Debug.Log("TargetInRad: " + TargetInRad);

        // 2. Two targets, receiving targeting data from Sactrack through Serial communication
        Vector3 TargetShown;
        Vector3 Target2Shown;
        CenterTarget.transform.localPosition  = TargetShown = new Vector3(0 * Mathf.Cos(TargetInRad) + 5 * Mathf.Sin(TargetInRad), 5 * Mathf.Sin(TargetVInRad), 5);//(-1) * 0 * Mathf.Sin(TargetInRad) + 0 * Mathf.Cos(TargetInRad));
        CenterTarget2.transform.localPosition = Target2Shown = new Vector3(0 * Mathf.Cos(TargetInRad2) + 5 * Mathf.Sin(TargetInRad2), 5 * Mathf.Sin(TargetVInRad2), 5);//(-1) * 0 * Mathf.Sin(TargetInRad2) + 0 * Mathf.Cos(TargetInRad2));

        //UnityEngine.Debug.Log("X: " + TargetShown.x + " Y: " + TargetShown.y + " Z: " + TargetShown.z);
        //UnityEngine.Debug.Log("X2: "+ Target2Shown.x+ " Y2: "+ Target2Shown.y+ " Z2: "+ Target2Shown.z);

        // ----------------------------------------------------------------------------------------------------------------
        //  Change the scale of targets.
        // ----------------------------------------------------------------------------------------------------------------
        //float NewScale = 2 * Distance_camera * Mathf.Tan(target_size / 2 * (Mathf.PI / 180));
        NewScale = 7 * Mathf.Tan(1.0f / 2 * (Mathf.PI / 180)); // made half the size

        CenterScale  =  CenterTarget.transform.localScale;
        CenterScale2 = CenterTarget2.transform.localScale;

        CenterScale  = new Vector3(NewScale, NewScale, NewScale);
        CenterScale2 = new Vector3(NewScale, NewScale, NewScale);

        CenterTarget.transform.localScale  = CenterScale;
        CenterTarget2.transform.localScale = CenterScale2;
    }

    float ExtractValue(string data) // function used in TargetPosition1()
    {
        string[] valueParts = data.Split(':');
        float extractedValue = float.Parse(valueParts[1]); // Extract value after "H1:" or "V1:"
        return extractedValue;
    }

    void OnApplicationQuit(){ // revise so Unity Editor won't crash when closing
        // Close the serial port when the application is quitting
        if (SerialPort != null && SerialPort.IsOpen)
        {
            SerialPort.Close();
        }
        
    }
}