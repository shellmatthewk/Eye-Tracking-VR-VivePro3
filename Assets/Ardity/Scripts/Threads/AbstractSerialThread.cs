/**
 * Ardity (Serial Communication for Arduino + Unity)
 * Author: Daniel Wilches <dwilches@gmail.com>
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using UnityEngine;

using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Threading;

using System.Globalization;

/**
 * This class contains methods that must be run from inside a thread and others
 * that must be invoked from Unity. Both types of methods are clearly marked in
 * the code, although you, the final user of this library, don't need to even
 * open this file unless you are introducing incompatibilities for upcoming
 * versions.
 */
public abstract class AbstractSerialThread
{
    // Parameters passed from SerialController, used for connecting to the
    // serial device as explained in the SerialController documentation.
    private string portName;
    private int baudRate;
    private int delayBeforeReconnecting;
    private int maxUnreadMessages;

    // Object from the .Net framework used to communicate with serial devices.
    private SerialPort serialPort;

    // Amount of milliseconds alloted to a single read or connect. An
    // exception is thrown when such operations take more than this time
    // to complete.
    private const int readTimeout = 100;

    // Amount of milliseconds alloted to a single write. An exception is thrown
    // when such operations take more than this time to complete.
    private const int writeTimeout = 100;

    // Internal synchronized queues used to send and receive messages from the
    // serial device. They serve as the point of communication between the
    // Unity thread and the SerialComm thread.
    private Queue inputQueue, outputQueue;

    // Indicates when this thread should stop executing. When SerialController
    // invokes 'RequestStop()' this variable is set.
    private bool stopRequested = false;

    private bool enqueueStatusMessages = false;
    
    //public TargetPosition TargetPosition = new TargetPosition();
    private GameObject CenterTarget;
    public SerialController serialController1;  // tx targetingData
    //string Target;
    float TargetAngle;

    /**************************************************************************
     * Methods intended to be invoked from the Unity thread.
     *************************************************************************/

    // ------------------------------------------------------------------------
    // Constructs the thread object. This object is not a thread actually, but
    // its method 'RunForever' can later be used to create a real Thread.
    // ------------------------------------------------------------------------
    public AbstractSerialThread(string portName,
                                int baudRate,
                                int delayBeforeReconnecting,
                                int maxUnreadMessages,
                                bool enqueueStatusMessages)
    {
        this.portName = portName;
        this.baudRate = baudRate;
        this.delayBeforeReconnecting = delayBeforeReconnecting;
        this.maxUnreadMessages = maxUnreadMessages;
        this.enqueueStatusMessages = enqueueStatusMessages;

        inputQueue = Queue.Synchronized(new Queue());
        outputQueue = Queue.Synchronized(new Queue());
    }
    void Start(){
        CenterTarget = GameObject.Find("CenterTarget");
    }
    // ------------------------------------------------------------------------
    // Invoked to indicate to this thread object that it should stop.
    // ------------------------------------------------------------------------
    public void RequestStop()
    {
        lock (this)
        {
            stopRequested = true;
        }
    }

    // ------------------------------------------------------------------------
    // Handles exception calls only for Timeout within ReadMessage()
    // ------------------------------------------------------------------------
    private static void Handler(Exception exception)
    {
        Console.WriteLine(exception);
    }

    // ------------------------------------------------------------------------
    // Polls the internal message queue returning the next available message
    // in a generic form. This can be invoked by subclasses to change the
    // type of the returned object.
    // It returns null if no message has arrived since the latest invocation.
    // ------------------------------------------------------------------------
    public object ReadMessage()
    {
        //if (inputQueue.Count == 0)
            //return null;

        //return inputQueue.Dequeue(); 

        try
        {
            return serialPort.ReadLine();
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine("It's too long. Timeout!" + ex);
            return null;
        }
        catch (Exception ex)
        {
            Handler(ex);
            return null;
        }
        //return ReadFromWire(serialPort);
    }

    // ------------------------------------------------------------------------
    // Schedules a message to be sent. It writes the message to the
    // output queue, later the method 'RunOnce' reads this queue and sends
    // the message to the serial device.
    // ------------------------------------------------------------------------
    public void SendMessage(object message)
    {
        //outputQueue.Enqueue(message);
        //SendToWire(message, serialPort);

        serialPort.WriteLine((string)message);
    }


    /**************************************************************************
     * Methods intended to be invoked from the SerialComm thread (the one
     * created by the SerialController).
     *************************************************************************/

    // ------------------------------------------------------------------------
    // Enters an almost infinite loop of attempting connection to the serial
    // device, reading messages and sending messages. This loop can be stopped
    // by invoking 'RequestStop'.
    // ------------------------------------------------------------------------
    public void RunForever()
    {
        // This 'try' is for having a log message in case of an unexpected
        // exception.
        try
        {
            while (!IsStopRequested())
            {
                try
                {
                    AttemptConnection();

                    // Enter the semi-infinite loop of reading/writing to the
                    // device.
                    while (!IsStopRequested())
                        RunOnce();
                }
                catch (Exception ioe)
                {
                    // A disconnection happened, or there was a problem
                    // reading/writing to the device. Log the detailed message
                    // to the console and notify the listener.
                    Debug.LogWarning("Exception: " + ioe.Message + " StackTrace: " + ioe.StackTrace);
                    if (enqueueStatusMessages)
                        inputQueue.Enqueue(SerialController.SERIAL_DEVICE_DISCONNECTED);

                    // As I don't know in which stage the SerialPort threw the
                    // exception I call this method that is very safe in
                    // disregard of the port's status
                    CloseDevice();

                    // Don't attempt to reconnect just yet, wait some
                    // user-defined time. It is OK to sleep here as this is not
                    // Unity's thread, this doesn't affect frame-rate
                    // throughput.
                    Thread.Sleep(delayBeforeReconnecting);
                }
            }

            // Before closing the COM port, give the opportunity for all messages
            // from the output queue to reach the other endpoint.
            while (outputQueue.Count != 0)
            {
                SendToWire(outputQueue.Dequeue(), serialPort);
            }

            // Attempt to do a final cleanup. This method doesn't fail even if
            // the port is in an invalid status.
            CloseDevice();
        }
        catch (Exception e)
        {
            Debug.LogError("Unknown exception: " + e.Message + " " + e.StackTrace);
        }
    }

    // ------------------------------------------------------------------------
    // Try to connect to the serial device. May throw IO exceptions.
    // ------------------------------------------------------------------------
    private void AttemptConnection()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = readTimeout;
        serialPort.WriteTimeout = writeTimeout;
        serialPort.DtrEnable = true;
        serialPort.RtsEnable = true;
        serialPort.Open();

        if (enqueueStatusMessages)
            inputQueue.Enqueue(SerialController.SERIAL_DEVICE_CONNECTED);
    }

    // ------------------------------------------------------------------------
    // Release any resource used, and don't fail in the attempt.
    // ------------------------------------------------------------------------
    private void CloseDevice()
    {
        if (serialPort == null)
            return;

        try
        {
            serialPort.Close();
        }
        catch (IOException)
        {
            // Nothing to do, not a big deal, don't try to cleanup any further.
        }

        serialPort = null;
    }

    // ------------------------------------------------------------------------
    // Just checks if 'RequestStop()' has already been called in this object.
    // ------------------------------------------------------------------------
    private bool IsStopRequested()
    {
        lock (this)
        {
            return stopRequested;
        }
    }

    // ------------------------------------------------------------------------
    // A single iteration of the semi-infinite loop. Attempt to read/write to
    // the serial device. If there are more lines in the queue than we may have
    // at a given time, then the newly read lines will be discarded. This is a
    // protection mechanism when the port is faster than the Unity progeram.
    // If not, we may run out of memory if the queue really fills.
    // ------------------------------------------------------------------------

    private void RunOnce()
    {
        try
        {
            // Send a message.
            if (outputQueue.Count != 0)
            {
                SendToWire(outputQueue.Dequeue(), serialPort);
            }

            // Read a message.
            // If a line was read, and we have not filled our queue, enqueue
            // this line so it eventually reaches the Message Listener.
            // Otherwise, discard the line.
            string inputMessage = "";//ReadFromWire(serialPort);
            if (inputMessage != null)
            {
                if (inputQueue.Count < maxUnreadMessages)
                {
                    //inputQueue.Enqueue(inputMessage);
                    //Debug.Log(inputMessage);
                    // try{ TargetPosition1(inputMessage); }
                    // catch (NullReferenceException){
                    //     //happens
                    // }
                }
                else
                {
                    Debug.LogWarning("Queue is full. Dropping message: " + inputMessage);
                }
            }
        }
        catch (TimeoutException)
        {
            // This is normal, not everytime we have a report from the serial device
        }
    }

    // ------------------------------------------------------------------------
    // Sends a message through the serialPort.
    // ------------------------------------------------------------------------
    protected abstract void SendToWire(object message, SerialPort serialPort);

    // ------------------------------------------------------------------------
    // Reads and returns a message from the serial port.
    // ------------------------------------------------------------------------
    public abstract string ReadFromWire(SerialPort serialPort);

    void TargetPosition1(string Target)
        {
            Vector3 center;     // Position of center target.
            
            // ----------------------------------------------------------------------------------------------------------------
            //  Assigning the game objects (spheres) to the script.
            // ----------------------------------------------------------------------------------------------------------------
            //CenterTarget = GameObject.Find("CenterTarget");


            // ----------------------------------------------------------------------------------------------------------------
            //  Calculating target position.
            // ----------------------------------------------------------------------------------------------------------------
            // 1. "Angle" input gets converted from degrees to radians because Unitys' Mathf. works with radians, not degrees.
            //if(serialController1.ReadSerialMessage() != null){
            //Target = serialController1.ReadSerialMessage();   
            //Debug.Log("Target: " + Target);
 
            //}
            if(Target != null){
                TargetAngle = float.Parse(Target, CultureInfo.InvariantCulture.NumberFormat);
                //TargetAngle.To
                Debug.Log("Target Angle: " + TargetAngle);
            }
            float TargetInRad = TargetAngle * (Mathf.PI / 180);
            //Debug.Log("TargetInRad: " + TargetInRad);
            // 2. Center target is placed at desired distance from the user.
            //CenterTarget.transform.position = center = new Vector3(0, 41.9f, 5); // z equal to 5, distance from camera = 7

            // 3. One target, receiving targeting data from Sactrack through Serial communication
            
            CenterTarget.transform.position = center = new Vector3(0 * Mathf.Cos(TargetInRad) + 5 * Mathf.Sin(TargetInRad), 41.9f, (-1) * 0 * Mathf.Sin(TargetInRad) + 5 * Mathf.Cos(TargetInRad));
            
            // ----------------------------------------------------------------------------------------------------------------
            //  Change the scale of targets.
            // ----------------------------------------------------------------------------------------------------------------
            //float NewScale = 2 * Distance_camera * Mathf.Tan(target_size / 2 * (Mathf.PI / 180));
            float NewScale = 7 * Mathf.Tan(1.0f / 2 * (Mathf.PI / 180)); // made half the size

            Vector3 CenterScale = CenterTarget.transform.localScale;

            CenterScale = new Vector3(NewScale, NewScale, NewScale);

            CenterTarget.transform.localScale = CenterScale;
        }
}
