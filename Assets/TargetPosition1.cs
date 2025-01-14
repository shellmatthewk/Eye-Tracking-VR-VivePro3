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

public class TargetPosition2 : MonoBehaviour
{
    // ----------------------------------------------------------------------------------------------------------------
    //  Assigning the game objects (spheres) to the script.
    // ----------------------------------------------------------------------------------------------------------------

    public GameObject CenterTarget;
    //private GameObject CenterTarget2;
    
    // Init a static reference if script is to be accessed by others when used in a 
    // none static nature eg. its dropped onto a gameObject. The use of "Instance"
    // allows access to public vars as such as those available to the unity editor.

    //public static TargetPosition Instance;//UnitySerialPort Instance;

    #region Properties

    // The serial port

    public static SerialPort Instance;
    public SerialPort SerialPort;

    
    public string TargetPositionCOMPort  = "COM21"; // COM21 for USB, COM17 for RS232
    //public string TargetPosition2COMPort = "COM21"; // COM21 for USB

    #endregion Properties

    float TargetAngle;
    float TargetInRad;
    float TargOrTime;
    string Target;
    string value;

    Stopwatch stopwatch = new Stopwatch();

    // heap allocation for GameObject properties
    float NewScale;
    Vector3 CenterScale;
    
    // Start is called before the first frame update
    void Start()
    {
        //SerialPort.ReadTimeout = 1; 
        // very short time, may read empty buffers
        CenterTarget = GameObject.Find("CenterTarget2");
        InvokeRepeating(nameof(TargetPosition1), 0.1f, 0.004f);
    }

    // Update is called once per frame
    //void Update()
    //{
        //TargetPosition1(); // moved to invokeRepeating call in Start()
    //}

    void TargetPosition1()//string Target)
    {            

                
        if (SerialPort != null)
            if (SerialPort.IsOpen)
                SerialPort.Close();

        try
        {
            SerialPort = new SerialPort(TargetPositionCOMPort, 9600)
            {
                Encoding = System.Text.Encoding.UTF8, // important for reading serial string data correctly
                DtrEnable = true,                      // data terminal ready, important for access
                RtsEnable = true
            };
            //Debug.Log("Connected to COM17");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e.Message);
        }
        
        //_serial.Open(); // moved into a try statement        

        try
        {
            SerialPort.Open(); // try-catch block, if port is already opened it throws exception
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.Log("Error opening serial2\n" + ex.Message + "\nExiting program...");
            return;
        }
        //Debug.Log("BTR: " + SerialPort.ReadTimeout); // output is -1 // ?? unexpected

        Target = SerialPort.ReadLine();
        
        SerialPort.Close();
        //Target = new string(Target.Where(c => char.IsDigit(c)).ToArray());
        Target = new string(Target.Where(c => char.IsDigit(c) || char.IsPunctuation(c)).ToArray());
        // above may cause StackTrace crashing with constant new string making
        // accepts numbers and decimals and negative symbol

        //UnityMainThreadDispatcher.Instance().Enqueue(() =>Target = serialController1.ReadSerialMessage());   
        //Debug.Log("Target: " + Target + " Unix Time(100ns): " + DateTime.Now.Ticks);

        if(Target != null){
            // try
            // {
            //     //TargetAngle = float.Parse(Target, CultureInfo.InvariantCulture.NumberFormat);
            //     //TargOrTime = float.Parse(Target, CultureInfo.InvariantCulture.NumberFormat); // holder to print time
            //     bool success = float.TryParse(Target, out TargOrTime);
            //     if (success)
            //     {
            //         //Console.WriteLine($"Converted '{Target}' to {TargOrTime}.");
            //     }
            //     else
            //     {
            //         Console.WriteLine($"Attempted conversion of '{Target ?? "<null>"}' failed.");
            //     }
            //     // converts to float
            // }
            // catch (Exception e)
            // {
            //     Console.Write(e);// look into how to resolve the question marks in black diamonds that are caught
            // }
            // for arduino w/ RTC
            TargOrTime = float.Parse(Target, CultureInfo.InvariantCulture.NumberFormat); // holder to print time
            //Debug.Log("Unix Time: " + TargOrTime);
            if (TargOrTime < 23){
                CenterTarget.SetActive(true);                
                TargetAngle = TargOrTime; // if TargOrTime is > 20, it is a Unix val, Do not pass
            }
            if (TargOrTime > 100){
                CenterTarget.SetActive(false);
                UnityEngine.Debug.Log("blank");
            }
            stopwatch.Stop();
            UnityEngine.Debug.Log("Target Angle2: " + TargetAngle + " Unix Time(100ns): " + stopwatch.ElapsedMilliseconds);//DateTime.Now.Ticks); // print to unity console TargetData
/*             value = 
            "Target Angle: " + TargetAngle + 
            " Unix Time(ms): " + stopwatch.ElapsedMilliseconds + //DateTime.Now.Ticks +
            Environment.NewLine; */

            value = 
            "Target Angle: " + TargetAngle + 
            " Unix Time(100ns): " + DateTime.Now.Ticks +
            Environment.NewLine;

            File.AppendAllText("TargetData_" + DateTime.Now.ToString("m") + ".txt", value);
            //UnityEngine.Debug.Log(stopwatch.ElapsedMilliseconds);
            stopwatch.Start();
        }
        
        // ----------------------------------------------------------------------------------------------------------------
        //  Calculating target position.
        // ----------------------------------------------------------------------------------------------------------------
        // 1. "Angle" input gets converted from degrees to radians because Unitys' Mathf. works with radians, not degrees.

        TargetInRad = TargetAngle * (Mathf.PI / 180);
        //Debug.Log("TargetInRad: " + TargetInRad);
        
        // 2. Center target is placed at desired distance from the user.
        //CenterTarget.transform.position = center = new Vector3(0, 41.9f, 5); // z equal to 5, distance from camera = 7

        // 3. One target, receiving targeting data from Sactrack through Serial communication
        
        CenterTarget.transform.localPosition = new Vector3(0 * Mathf.Cos(TargetInRad) + 5 * Mathf.Sin(TargetInRad), 0f, (-1) * 0 * Mathf.Sin(TargetInRad) + 5 * Mathf.Cos(TargetInRad));
        
        // ----------------------------------------------------------------------------------------------------------------
        //  Change the scale of targets.
        // ----------------------------------------------------------------------------------------------------------------
        //float NewScale = 2 * Distance_camera * Mathf.Tan(target_size / 2 * (Mathf.PI / 180));
        NewScale = 7 * Mathf.Tan(1.0f / 2 * (Mathf.PI / 180)); // made half the size

        CenterScale = CenterTarget.transform.localScale;

        CenterScale = new Vector3(NewScale, NewScale, NewScale);

        CenterTarget.transform.localScale = CenterScale;
    }
}
