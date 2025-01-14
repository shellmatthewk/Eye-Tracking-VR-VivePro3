// /**
//  * Ardity (Serial Communication for Arduino + Unity)
//  * Author: Daniel Wilches <dwilches@gmail.com>
//  *
//  * This work is released under the Creative Commons Attributions license.
//  * https://creativecommons.org/licenses/by/2.0/
//  */

// using UnityEngine;

// using System.IO.Ports;

// using System.Collections.Generic;

// /**
//  * This class contains methods that must be run from inside a thread and others
//  * that must be invoked from Unity. Both types of methods are clearly marked in
//  * the code, although you, the final user of this library, don't need to even
//  * open this file unless you are introducing incompatibilities for upcoming
//  * versions.
//  * 
//  * For method comments, refer to the base class.
//  */
// public class SerialThreadLines : AbstractSerialThread
// {
//     public SerialThreadLines(string portName,
//                              int baudRate,
//                              int delayBeforeReconnecting,
//                              int maxUnreadMessages)
//         : base(portName, baudRate, delayBeforeReconnecting, maxUnreadMessages, true)
//     {
//     }

//     protected override void SendToWire(object message, SerialPort serialPort)
//     {
//         serialPort.WriteLine((string) message);
//     }

//     /*OLD WAY
//     protected override object ReadFromWire(SerialPort serialPort)
//     {
//         return serialPort.ReadLine();
//     }
//     */

//     /*NEW WAY*/
//     private List<byte> buffer = new List<byte>();
//     protected override object ReadFromWire(SerialPort serialPort)
//     {
//         buffer.Add( (byte)serialPort.ReadByte() );

//         // Search for the separator in the buffer
//         int index = System.Array.FindIndex<byte>(buffer.ToArray(), 0, buffer.Count, IsSeparator);
//         if (index == -1)
//             return null;

//         buffer.RemoveAt (buffer.Count - 1); //remove CR
// 	buffer.RemoveAt (buffer.Count - 1); //remove LF

//         var output = buffer.ToArray ();
//         buffer.Clear ();

//         return output;
//     }

//     private bool IsSeparator(byte aByte)
//     {
//         return aByte == '\n';
//     }
// }


/**
 * Ardity (Serial Communication for Arduino + Unity)
 * Author: Daniel Wilches <dwilches@gmail.com>
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using UnityEngine;

using System.IO.Ports;

/**
 * This class contains methods that must be run from inside a thread and others
 * that must be invoked from Unity. Both types of methods are clearly marked in
 * the code, although you, the final user of this library, don't need to even
 * open this file unless you are introducing incompatibilities for upcoming
 * versions.
 * 
 * For method comments, refer to the base class.
 */
public class SerialThreadLines : AbstractSerialThread
{
    public SerialThreadLines(string portName,
                             int baudRate,
                             int delayBeforeReconnecting,
                             int maxUnreadMessages)
        : base(portName, baudRate, delayBeforeReconnecting, maxUnreadMessages, true)
    {
    }

    protected override void SendToWire(object message, SerialPort serialPort)
    {
        serialPort.WriteLine((string) message);
    }

    public override string ReadFromWire(SerialPort serialPort)
    {
        return serialPort.ReadLine();
    }
}
