// ########################################################################################################################
// ########################################################################################################################
// ########################################################################################################################
//
//  This is a programme to measure eye movements with HTC Vive Pro Eye VR headset.
//  The programme is developed by Yu Imaoka and Andri Flury at D-HEST, ETH Zurich.
//  12th of October 2019.
//
//  Software information
//      - HTC SDK:      SRanipal_SDK_1.1.0.1
//      - SR Runtime:   1.1.2.0
//      - Steam VR:     1.11.11
//      - Unity:        2019.2.5f1 
// 
//  Conditions
//      - Tracking:               Disabled.
//      - Calibration:            Performed in the first place.
//      - Main script:            Attached to an empty object.
//      - Tracking script:        Attached to the main camera.
//      - Initial waiting time:   Wait for 30 seconds before the saccade tasks start.
//
//  General notes:
//      * Timestamp recorded by eye tracking device does not show the proper values. HTC also recognised the issue.
//      * This programme follows the standardised measurement protocol of saccadic eye movement analysis 
//        proposed in the literature: An internationally standardised antisaccade protocol
//        (https://doi.org/10.1016/j.visres.2013.02.007)
//
// ########################################################################################################################
// ########################################################################################################################
// ########################################################################################################################



// ************************************************************************************************************************
//  Call namespaces.
// ************************************************************************************************************************
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using System;
using System.IO;
using ViveSR.anipal.Eye;
using ViveSR.anipal;
using ViveSR;

// for rs232 communication
using System.IO.Ports;
using System.Threading;

// for String to float
using System.Globalization; // to access CultureInfo

// for ellapsedMilliseconds
using System.Diagnostics;

namespace Test120FPS{
    public class Saccade_measure_rev1 : MonoBehaviour
    {
        //public Sample_GetDataThread DataThread = null;
        //public SerialController serialController;  // rx eyeData
        // serial port write to teensy	
        //static SerialPort _serialPort;             // tx targetData

        // ********************************************************************************************************************
        //
        //  Define user ID information.
        //  - The developers can define the user ID format such as "ABC_001". The ID is used for the name of text file 
        //    that records the measured eye movement data.
        // 
        // ********************************************************************************************************************
        public static string UserID = "1";       // Definte ID number such as 001, ABC001, etc.
        public static string Path = Directory.GetCurrentDirectory();
        string File_Path = Directory.GetCurrentDirectory() + "\\StandardSaccade_" + UserID + ".txt";
        string valueTime, valueTimeVert;

        // ********************************************************************************************************************
        //
        //  Parameters for the targets (stimuli).
        //
        // ********************************************************************************************************************
        private GameObject RightTarget;
        private GameObject CenterTarget;
        private GameObject LeftTarget;

        //private float Targetduration = 3;       // Duration (seconds) until a targets appears or disappears.    Recommended: 1s
        //private float Angle = 10;                // Angle (degrees) between center target and L/R target.        Recommended: 8-10°
        //private float Distance = 5;             // Distance between origin of coordinate and targets.           Recommended: 10m
        //private float Distance_camera = 7;      // Distance between camera and targets.
        //private float target_size = 1.0f;       // Size of target in degree.                                    Recommended: 0.5°


        // ********************************************************************************************************************
        //
        //  Parameters for saccade task flow.
        //
        // ********************************************************************************************************************
        //private float Taskdelay = 30;           // Recording of eye movement starts 30 seconds before the saccade task starts.
        //private float Breaktime = 60;           // Break time between the sets of saccade tests.

        public static long Targettime;


        


        // ********************************************************************************************************************
        //
        //  Parameters for time-related information.
        //
        // ********************************************************************************************************************
        public static int cnt_callback = 0;
        //public int cnt_saccade = 0, Endbuffer = 3, SaccadeTimer = 10, Angle = 15;
        //float Timeout = 1.0f, InitialTimer = 0.0f;
        //private static long SaccadeEndTime = 0;
        private static long MeasureTime, CurrentTime;//, MeasureEndTime = 0;
        private static float time_stamp;
        private static int frame;

        // ********************************************************************************************************************
        //
        //  Parameters for eye data.
        //
        // ********************************************************************************************************************
        private static EyeData_v2 eyeData = new EyeData_v2();
        public EyeParameter eye_parameter = new EyeParameter();
        public GazeRayParameter gaze = new GazeRayParameter();
        private static bool eye_callback_registered = false;
        private const int maxframe_count = 120 * 10000000;                  // Maximum number of samples for eye tracking (120 Hz * time in seconds).
        private static UInt64 eye_valid_L, eye_valid_R;                 // The bits explaining the validity of eye data.
        private static float openness_L, openness_R;                    // The level of eye openness.
        private static float pupil_diameter_L, pupil_diameter_R;        // Diameter of pupil dilation.
        private static Vector2 pos_sensor_L, pos_sensor_R;              // Positions of pupils.
        private static Vector3 gaze_origin_L, gaze_origin_R;            // Position of gaze origin.
        private static Vector3 gaze_direct_L, gaze_direct_R;            // Direction of gaze ray.
        private static float frown_L, frown_R;                          // The level of user's frown.
        private static float squeeze_L, squeeze_R;                      // The level to show how the eye is closed tightly.
        private static float wide_L, wide_R;                            // The level to show how the eye is open widely.
        private static double gaze_sensitive;                           // The sensitive factor of gaze ray.
        private static float distance_C;                                // Distance from the central point of right and left eyes.
        private static bool distance_valid_C;                           // Validity of combined data of right and left eyes.
        public bool cal_need;                                           // Calibration judge.
        public bool result_cal;                                         // Result of calibration.
        private static int track_imp_cnt = 0;
        private static TrackingImprovement[] track_imp_item;

        // Threading
        public EyeData data = new EyeData();
        //private Thread thread; //12/21/22
        private const int FrequencyControl = 1;
        private const int MaxFrameCount = 3600;

        // Targeting
        private String Target;
        private float TargetAngle;

        // serial port write to teensy	

        public SerialPort _serialPort;
        public SerialPort _serialPortVertical;
        public string EyeDataCOMPort = "COM6"; // COM16 for RS232, COM6 for USB
        public string EyeDataCOMPortVert = "COM14"; // COM16 for RS232, COM6 for USB
        public bool enablePortHorizontal = true;   // Flag to enable/disable the first serial port
        public bool enablePortVertical = true;   // Flag to enable/disable the second serial port


        // stopwatch

        Stopwatch stopwatch = new Stopwatch();

        // ********************************************************************************************************************
        //
        //  Start is called before the first frame update. The Start() function is performed only one time.
        //
        // ********************************************************************************************************************
        void Start()
        {           
            UnityEngine.Debug.Log("Saccade Measure: Started");
            Invoke("SystemCheck", 0.5f);                // System check.
            UnityEngine.Debug.Log("Saccade Measure: System Checked");
            //SRanipal_Eye_v2.LaunchEyeCalibration();     // Perform calibration for eye tracking.
            //Calibration();

            if (enablePortHorizontal)
            {
                _serialPort = new SerialPort(EyeDataCOMPort); //hard coded COM6
                _serialPort.BaudRate = 115200;
                _serialPort.ReadTimeout = 100;
                _serialPort.WriteTimeout = 100;
                _serialPort.DtrEnable = true;
                _serialPort.RtsEnable = true;
            }

            if (enablePortVertical)
            {
                _serialPortVertical = new SerialPort(EyeDataCOMPortVert); //hard coded COM6
                _serialPortVertical.BaudRate = 115200;
                _serialPortVertical.ReadTimeout = 100;
                _serialPortVertical.WriteTimeout = 100;
                _serialPortVertical.DtrEnable = true;
                _serialPortVertical.RtsEnable = true;
            }
            //thread = new Thread(TargetPosition);
            //thread.Start();  // moved to its own thread
            //DataThread = FindObjectOfType<Sample_GetDataThread>();
            //serialController = GameObject.Find("SerialController").GetComponent<SerialController>();
            //TargetPosition();                         // Implement the targets on the VR view.
            //Debug.Log("Saccade Measure: Invoking Measurement...");
            //thread = new Thread(Measurement);

            //Thread thread = new Thread(new ThreadStart(Measurement));
            //thread.Start();  // moved to its own thread

            UnityEngine.Debug.Log("Saccade Measure: Starting Measurement...");
            stopwatch.Start();
            Invoke("Measurement", 0.5f);                // Start the measurement of ocular movements in a separate callback function.

        }


        // ********************************************************************************************************************
        //
        //  Check if the system works properly.
        //
        // ********************************************************************************************************************
        void SystemCheck()
        {
            if (SRanipal_Eye_API.GetEyeData_v2(ref eyeData) == ViveSR.Error.WORK)
            {
                UnityEngine.Debug.Log("Device is working properly.");
            }

            if (SRanipal_Eye_API.GetEyeParameter(ref eye_parameter) == ViveSR.Error.WORK)
            {
                UnityEngine.Debug.Log("Eye parameters are measured.");
            }

            //  Check again if the initialisation of eye tracking functions successfully. If not, we stop playing Unity.
            Error result_eye_init = SRanipal_API.Initial(SRanipal_Eye_v2.ANIPAL_TYPE_EYE_V2, IntPtr.Zero);

            if (result_eye_init == Error.WORK)
            {
                UnityEngine.Debug.Log("[SRanipal] Initial Eye v2: " + result_eye_init);
            }
            else
            {
                UnityEngine.Debug.LogError("[SRanipal] Initial Eye v2: " + result_eye_init);

                if (UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEditor.EditorApplication.isPlaying = false;    // Stops Unity editor.
                }
            }
        }



        // ********************************************************************************************************************
        //
        //  Calibration is performed if the calibration is necessary.
        //
        // ********************************************************************************************************************
        void Calibration()
        {
            SRanipal_Eye_API.IsUserNeedCalibration(ref cal_need);           // Check the calibration status. If needed, we perform the calibration.

            if (cal_need == true)
            {
                result_cal = SRanipal_Eye_v2.LaunchEyeCalibration();

                if (result_cal == true)
                {
                    UnityEngine.Debug.Log("Calibration is done successfully.");
                }

                else
                {
                    UnityEngine.Debug.Log("Calibration is failed.");
                    if (UnityEditor.EditorApplication.isPlaying)
                    {
                        UnityEditor.EditorApplication.isPlaying = false;    // Stops Unity editor if the calibration if failed.
                    }
                }
            }

            if (cal_need == false)
            {
                UnityEngine.Debug.Log("Calibration is not necessary");
            }
        }



        // ********************************************************************************************************************
        //
        //  Setup for the targets (stimuli).
        //
        // ********************************************************************************************************************
/*         void TargetPosition()
        {
            //Vector3 right;      // Position of right target.
            Vector3 center;     // Position of center target.
            //Vector3 left;       // Position of left target.

            // ----------------------------------------------------------------------------------------------------------------
            //  Assigning the game objects (spheres) to the script.
            // ----------------------------------------------------------------------------------------------------------------
            //RightTarget = GameObject.Find("RightTarget");
            CenterTarget = GameObject.Find("CenterTarget");
            //LeftTarget = GameObject.Find("LeftTarget");


            // ----------------------------------------------------------------------------------------------------------------
            //  Calculating target position.
            // ----------------------------------------------------------------------------------------------------------------
            // 1. "Angle" input gets converted from degrees to radians because Unitys' Mathf. works with radians, not degrees.
            float AngleInRad = Angle * (Mathf.PI / 180);

            // 2. Center target is placed at desired distance from the user.
            CenterTarget.transform.position = center = new Vector3(0, 41.9f, Distance);

            // 3. Right target is placed at desired angle from the center target.
            //RightTarget.transform.position = right = new Vector3(center.x * Mathf.Cos(AngleInRad) + center.z * Mathf.Sin(AngleInRad), 41.9f, (-1) * center.x * Mathf.Sin(AngleInRad) + center.z * Mathf.Cos(AngleInRad));

            // 4. Left target is placed at desired angle from the center target. Either by calculating or by mirroring the right target position (-right.x)
            //LeftTarget.transform.position = left = new Vector3(-right.x, 41.9f, right.z);

            // 5. One target, receiving targeting data from Sactrack through Serial communication
            //Target = serialController.ReadSerialMessage();
            Debug.Log(Target);
            //Target = serialController.ReadSerialMessage();
            TargetAngle = float.Parse(Target, CultureInfo.InvariantCulture.NumberFormat);
            float TargetInRad = TargetAngle * (Mathf.PI / 180);
            
            CenterTarget.transform.position = center = new Vector3(1 * Mathf.Cos(TargetInRad) + 5 * Mathf.Sin(TargetInRad), 41.9f, (-1) * 1 * Mathf.Sin(TargetInRad) + 5 * Mathf.Cos(TargetInRad));
            
            // ----------------------------------------------------------------------------------------------------------------
            //  Change the scale of targets.
            // ----------------------------------------------------------------------------------------------------------------
            //float NewScale = 2 * Distance_camera * Mathf.Tan(target_size / 2 * (Mathf.PI / 180));
            float NewScale = Distance_camera * Mathf.Tan(target_size / 2 * (Mathf.PI / 180));

            Vector3 CenterScale = CenterTarget.transform.localScale;
            Vector3 RightScale = RightTarget.transform.localScale;
            Vector3 LeftScale = LeftTarget.transform.localScale;

            CenterScale = new Vector3(NewScale, NewScale, NewScale);
            RightScale = new Vector3(NewScale, NewScale, NewScale);
            LeftScale = new Vector3(NewScale, NewScale, NewScale);

            CenterTarget.transform.localScale = CenterScale;
            RightTarget.transform.localScale = RightScale;
            LeftTarget.transform.localScale = LeftScale;


            // ----------------------------------------------------------------------------------------------------------------
            //  Hide the targets.
            // ----------------------------------------------------------------------------------------------------------------
            if (CenterTarget.activeInHierarchy)
                CenterTarget.SetActive(false);
            if (LeftTarget.activeInHierarchy)
                LeftTarget.SetActive(false);
            if (RightTarget.activeInHierarchy)
                RightTarget.SetActive(false);
        } */



        // ********************************************************************************************************************
        //
        //  Create a text file and header names of each column to store the measured data of eye movements.
        //
        // ********************************************************************************************************************
        void Data_txt()
        {
            string variable =
            "time(100ns)" + "," +
            "time_stamp(ms)" + "," +
            "frame" + "," +
            "eye_valid_L" + "," +
            "eye_valid_R" + "," +
            "openness_L" + "," +
            "openness_R" + "," +
            "pupil_diameter_L(mm)" + "," +
            "pupil_diameter_R(mm)" + "," +
            "pos_sensor_L.x" + "," +
            "pos_sensor_L.y" + "," +
            "pos_sensor_R.x" + "," +
            "pos_sensor_R.y" + "," +
            "gaze_origin_L.x(mm)" + "," +
            "gaze_origin_L.y(mm)" + "," +
            "gaze_origin_L.z(mm)" + "," +
            "gaze_origin_R.x(mm)" + "," +
            "gaze_origin_R.y(mm)" + "," +
            "gaze_origin_R.z(mm)" + "," +
            "gaze_direct_L.x" + "," +
            "gaze_direct_L.y" + "," +
            "gaze_direct_L.z" + "," +
            "gaze_direct_R.x" + "," +
            "gaze_direct_R.y" + "," +
            "gaze_direct_R.z" + "," +
            "gaze_sensitive" + "," +
            "frown_L" + "," +
            "frown_R" + "," +
            "squeeze_L" + "," +
            "squeeze_R" + "," +
            "wide_L" + "," +
            "wide_R" + "," +
            "distance_valid_C" + "," +
            "distance_C(mm)" + "," +
            "track_imp_cnt" +
            Environment.NewLine;
            
            /* string variable =
            "time(100ns)" + "," +
            "gaze_origin_L.x(mm)" + "," +
            "gaze_origin_L.y(mm)" + "," +
            "gaze_origin_L.z(mm)" + "," +
            "gaze_direct_L.x" + "," +
            "gaze_direct_L.y" + "," +
            "gaze_direct_L.z" + "," +
            "gaze_origin_R.x(mm)" + "," +
            "gaze_origin_R.y(mm)" + "," +
            "gaze_origin_R.z(mm)" + "," +
            "gaze_direct_R.x" + "," +
            "gaze_direct_R.y" + "," +
            "gaze_direct_R.z" + "," +
            Environment.NewLine; */

            File.AppendAllText("StandardSaccade_" + UserID + ".txt", variable);
        }



        // ********************************************************************************************************************
        //
        //  Measure eye movements in a callback function that HTC SRanipal provides.
        //
        // ********************************************************************************************************************
        void Measurement()
        {
            EyeParameter eye_parameter = new EyeParameter();
            SRanipal_Eye_API.GetEyeParameter(ref eye_parameter);
            //Data_txt(); // header line in txt file

            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
            {
                SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = true;
            }

            else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }
        }



        // ********************************************************************************************************************
        //
        //  Callback function to record the eye movement data.
        //  Note that SRanipal_Eye_v2 does not work in the function below. It only works under UnityEngine.
        //
        // ********************************************************************************************************************
        void EyeCallback(ref EyeData_v2 eye_data) // eyeData
        {
            EyeParameter eye_parameter = new EyeParameter();
            SRanipal_Eye_API.GetEyeParameter(ref eye_parameter);
            //eyeData = eye_data;

            // ----------------------------------------------------------------------------------------------------------------
            //  Measure eye movements at the frequency of 120Hz until framecount reaches the maxframe count set.
            // ----------------------------------------------------------------------------------------------------------------
        
            while (cnt_callback < maxframe_count)
            {
                ViveSR.Error error = SRanipal_Eye_API.GetEyeData_v2(ref eyeData);

                if (error == ViveSR.Error.WORK)
                {
                    // --------------------------------------------------------------------------------------------------------
                    //  Measure each parameter of eye data that are specified in the guideline of SRanipal SDK.
                    // --------------------------------------------------------------------------------------------------------
                    MeasureTime = DateTime.Now.Ticks; //ns
                    time_stamp = eyeData.timestamp;   //ms
                    frame = eyeData.frame_sequence;
                    eye_valid_L = eyeData.verbose_data.left.eye_data_validata_bit_mask;
                    eye_valid_R = eyeData.verbose_data.right.eye_data_validata_bit_mask;
                    openness_L = eyeData.verbose_data.left.eye_openness;
                    openness_R = eyeData.verbose_data.right.eye_openness;
                    pupil_diameter_L = eyeData.verbose_data.left.pupil_diameter_mm;
                    pupil_diameter_R = eyeData.verbose_data.right.pupil_diameter_mm;
                    pos_sensor_L = eyeData.verbose_data.left.pupil_position_in_sensor_area;
                    pos_sensor_R = eyeData.verbose_data.right.pupil_position_in_sensor_area;
                    gaze_origin_L = eyeData.verbose_data.left.gaze_origin_mm;
                    gaze_origin_R = eyeData.verbose_data.right.gaze_origin_mm;
                    gaze_direct_L = eyeData.verbose_data.left.gaze_direction_normalized;
                    gaze_direct_R = eyeData.verbose_data.right.gaze_direction_normalized;
                    gaze_sensitive = eye_parameter.gaze_ray_parameter.sensitive_factor;
                    frown_L = eyeData.expression_data.left.eye_frown;
                    frown_R = eyeData.expression_data.right.eye_frown;
                    squeeze_L = eyeData.expression_data.left.eye_squeeze;
                    squeeze_R = eyeData.expression_data.right.eye_squeeze;
                    wide_L = eyeData.expression_data.left.eye_wide;
                    wide_R = eyeData.expression_data.right.eye_wide;
                    distance_valid_C = eyeData.verbose_data.combined.convergence_distance_validity;
                    distance_C = eyeData.verbose_data.combined.convergence_distance_mm;
                    track_imp_cnt = eyeData.verbose_data.tracking_improvements.count;
                    ////track_imp_item = eyeData.verbose_data.tracking_improvements.items;

                    //  Convert the measured data to string data to write in a text file.
                    string value =
                        /*MeasureTime.ToString() + " " +/* ","+ //ns
                        
                        time_stamp.ToString() + "," + //ms
                        frame.ToString() + "," +
                        eye_valid_L.ToString() + "," +
                        eye_valid_R.ToString() + "," +
                        openness_L.ToString() + "," +
                        openness_R.ToString() + "," +
                        pupil_diameter_L.ToString() + "," +
                        pupil_diameter_R.ToString() + "," +
                        pos_sensor_L.x.ToString() + "," +
                        pos_sensor_L.y.ToString() + "," +
                        pos_sensor_R.x.ToString() + "," +
                        pos_sensor_R.y.ToString() + "," +
                        gaze_origin_L.x.ToString() + "," +
                        gaze_origin_L.y.ToString() + "," +
                        gaze_origin_L.z.ToString() + "," +
                        gaze_origin_R.x.ToString() + "," +
                        gaze_origin_R.y.ToString() + "," +
                        gaze_origin_R.z.ToString() + "," +*/

                        gaze_direct_L.y.ToString();/* "," + // send to teensy
                        
                        gaze_direct_L.y.ToString() + "," +  // vertical eye
                        gaze_direct_L.z.ToString() + "," +
                        gaze_direct_R.x.ToString() + "," +
                        gaze_direct_R.y.ToString() + "," +
                        gaze_direct_R.z.ToString() + "," +
                        gaze_sensitive.ToString() + "," +
                        frown_L.ToString() + "," +
                        frown_R.ToString() + "," +
                        squeeze_L.ToString() + "," +
                        squeeze_R.ToString() + "," +
                        wide_L.ToString() + "," +
                        wide_R.ToString() + "," +
                        distance_valid_C.ToString() + "," +
                        distance_C.ToString() + "," +
                        track_imp_cnt.ToString() +*/
                        //track_imp_item.ToString() +
                        //Environment.NewLine; 
                        
                        /* MeasureTime.ToString() + "," +
                        gaze_origin_L.x.ToString() + "," +
                        gaze_origin_L.y.ToString() + "," +
                        gaze_origin_L.z.ToString() + "," +
                        gaze_direct_L.x.ToString() + "," +
                        gaze_direct_L.y.ToString() + "," +
                        gaze_direct_L.z.ToString() + "," +
                        // "," +
                        //gaze_origin_R.x.ToString() + "," +
                        //gaze_origin_R.y.ToString() + "," +
                        //gaze_origin_R.z.ToString() + "," +
                        Environment.NewLine; */
/*                     valueTime = 
                    "Eye Angle: " + value + 
                    " Unix Time(ms): " + stopwatch.ElapsedMilliseconds + //DateTime.Now.Ticks +
                    Environment.NewLine; */

                    //HORIZONTAL EYE 
                    valueTime = 
                    "Eye Angle: " + value + 
                    " Unix Time(100ns): " + DateTime.Now.Ticks +
                    Environment.NewLine;

                    File.AppendAllText("EyeData_" + DateTime.Now.ToString("m") + ".txt", valueTime);
                    //tricky thing with eye data, I can't get it to write to unity console
                    //cant read the eye data unless it is on the console, or on the txt file
                    UnityEngine.Debug.Log(gaze_direct_L.x);
                    //Debug.Log("Left eye (mm): " + eyeData.verbose_data.left.gaze_origin_mm);
                    _serialPort.Open();
                    _serialPort.WriteLine(value); //too slow?
                    //_serialPort.Write(buff, 0, 12);
                    _serialPort.Close();
                    //serialController.SendSerialMessage(value); // temp comment out


                    // VERTICAL EYE
                    string valueVert = gaze_direct_L.y.ToString();

                    valueTimeVert = 
                    "Vert Eye Angle: " + valueVert + 
                    " Unix Time(100ns): " + DateTime.Now.Ticks +
                    Environment.NewLine;

                    File.AppendAllText("EyeData_" + DateTime.Now.ToString("m") + ".txt", valueTimeVert);
                    UnityEngine.Debug.Log(gaze_direct_L.x);
                    //Debug.Log("Left eye (mm): " + eyeData.verbose_data.left.gaze_origin_mm);
                    _serialPortVertical.Open();
                    _serialPortVertical.WriteLine(valueVert); //too slow?
                    //_serialPort.Write(buff, 0, 12);
                    _serialPortVertical.Close();

                    cnt_callback++;
                }

                //  Break while loop 3 seconds after the saccade tasks are completed. We know the timing at this point by time information.
                /* CurrentTime = DateTime.Now.Ticks;
                MeasureEndTime = GetSaccadeEndTime();

                if ((CurrentTime - MeasureEndTime > 3 * 10000000) && MeasureEndTime != 0)
                {
                    break;
                } */
            }
        }



        // ********************************************************************************************************************
        //
        //  Update is called once per frame.
        //
        // ********************************************************************************************************************
        void Update()
        {   
            //data = DataThread.data; 
            
            //SaccadeTask();
            // Timer counting down from 30-0 seconds.
            /* InitialTimer += Time.deltaTime; 
            if ((InitialTimer >= Timeout) && (SaccadeTimer > 1))
            {
                SaccadeTimer -= 1;
                Debug.Log("Task will start in " + SaccadeTimer + "seconds.");   // Display the count status on the console.
                InitialTimer = 0.0f;
            } */
            //Application.Quit();
        }



    void OnApplicationQuit(){
        // Close the serial port when the application is quitting
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
        }

        CancelInvoke("Measurement");
        Quit();
        
    }

    public void Quit() {
        #if UNITY_STANDALONE
            Application.Quit();
        #endif
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

        // ********************************************************************************************************************
        //
        //  SaccadeTask starts after 10s.
        //  
        // ********************************************************************************************************************
/*         void SaccadeTask()
        {
            // cnt_saccade is needed to prevent SaccadeTask to be executed more than once.
            // Saccade_Task_Timing file is created.
            if (cnt_saccade == 0)
            {
                string timingSaccadevariable = "Saccade_start_unixtime" + Environment.NewLine;
                File.AppendAllText("Saccade_Start_Time_" + UserID + ".txt", timingSaccadevariable);

                // Initialisation of appearing targets.
                StartCoroutine(Sequence());
                cnt_saccade++;
            }
        } */



        // ********************************************************************************************************************
        //
        //  Saccade task sequence.
        //
        // ********************************************************************************************************************
/*        private IEnumerator Sequence()
        {
            // ======================================================================
            //  Wait for 30 seconds before saccadic assessment starts.
            // ======================================================================
            yield return new WaitForSeconds(Taskdelay);

            Debug.Log("Press space key to start 10 practice trials of pro-saccade task.");
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }


            // ======================================================================
            //  10 practice trials for pro-saccade task
            // ======================================================================
            Debug.Log("Press space key to start 1st pro-saccade task (60 trials).");
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }


            // ======================================================================
            //  1. Perform saccadic eye movement assessment: 60 pro-saccade trials.
            // ======================================================================
            Debug.Log("Take 1 minute break. Press space key to start 4 practice trials of anti-saccade task.");
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }

            //  Take a break of 1 minute before moving to the next phase.
            //yield return new WaitForSeconds(Breaktime);


            // ======================================================================
            //  4 practice trials for anti-saccade task
            // ======================================================================
            Debug.Log("Press space key to start 1st anti-saccade task (40 trials).");
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }


            // ======================================================================
            //  2. Perform saccadic eye movement assessment: 40 anti-saccade trials.
            // ======================================================================

            Debug.Log("Take 1 minute break. Press space key to start 2nd anti-saccade task (40 trials).");
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }

            //  Take a break of 1 minute before moving to the next phase.
            //yield return new WaitForSeconds(Breaktime);


            // ======================================================================
            //  3. Perform saccadic eye movement assessment: 40 anti-saccade trials.
            // ======================================================================
            Debug.Log("Take 1 minute break. Press space key to start 3rd anti-saccade task (40 trials).");
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }

            //  Take a break of 1 minute before moving to the next phase.
            //yield return new WaitForSeconds(Breaktime);


            // ======================================================================
            //  4. Perform saccadic eye movement assessment: 40 anti-saccade trials.
            // ======================================================================
            Debug.Log("Take 1 minute break. Press space key to start 2nd pro-saccade task (60 trials).");
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }

            //  Take a break of 1 minute before moving to the next phase.
            //yield return new WaitForSeconds(Breaktime);


            // ======================================================================
            //  5. Perform saccadic eye movement assessment: 60 pro-saccade trials.
            // ======================================================================
            yield return new WaitForSeconds(Endbuffer);

            SaccadeEndTime = DateTime.Now.Ticks;

            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;    // Stops Unity editor.
            }

        }
*/


        // ********************************************************************************************************************
        //
        //  Return the ending time of saccade task to EyeCallback function.
        //
        // ********************************************************************************************************************
/*         static long GetSaccadeEndTime()
        {
            return SaccadeEndTime;
        } */



        // ********************************************************************************************************************
        //
        //  Allocate the targets (stimuli) in the VR environment.
        //
        // ********************************************************************************************************************
        // private IEnumerator TargetAppear(float fp_time, int direction, int sac_order)
        // {
        //     //  The central target appears for x seconds between 1 and 3.5 seconds.
        //     CenterTarget.SetActive(true);
        //     yield return new WaitForSeconds(fp_time);
        //     CenterTarget.SetActive(false);

        //     //  Unix time is recorded when the red target appears.
        //     Targettime = DateTime.Now.Ticks;
        //     string timingSaccadevalue = Targettime.ToString() + Environment.NewLine;
        //     File.AppendAllText("Saccade_Start_Time_" + UserID + ".txt", timingSaccadevalue);

        //     //  Saccade target appears on either the left or right side. (1: right, -1: left from the user's view.)
        //     if (direction == 1)
        //     {
        //         RightTarget.SetActive(true);
        //         yield return new WaitForSeconds(Targetduration);
        //         RightTarget.SetActive(false);
        //     }

        //     if (direction == -1)
        //     {
        //         LeftTarget.SetActive(true);
        //         yield return new WaitForSeconds(Targetduration);
        //         LeftTarget.SetActive(false);
        //     }

        //     //  Console outputs how many saccade trials have been performed
        //     sac_order += 1;
        //     /* Debug.Log("Left eye (x)(mm): " + eyeData.verbose_data.left.gaze_origin_mm.x);
        //     Debug.Log("Left eye (y)(mm): " + eyeData.verbose_data.left.gaze_origin_mm.y);
        //     Debug.Log("Left eye (z)(mm): " + eyeData.verbose_data.left.gaze_origin_mm.z);
        //     Debug.Log("Left eye (mm): ");// + eyeData.verbose_data.left.gaze_origin_mm); */
        //     Debug.Log("Saccade trial: " + sac_order);

        // }

    }
}