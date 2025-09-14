/**
 * Ardity (Serial Communication for Arduino + Unity)
 * Author: Daniel Wilches <dwilches@gmail.com>
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using UnityEngine;

using System.IO.Ports;
using System;
using System.Threading;

/**
 * This class contains methods that must be run from inside a thread and others
 * that must be invoked from Unity. Both types of methods are clearly marked in
 * the code, although you, the final user of this library, don't need to even
 * open this file unless you are introducing incompatibilities for upcoming
 * versions.
 * 
 * For method comments, refer to the base class.
 */
public class SerialThreadBinaryDelimited : AbstractSerialThread
{
    // Messages to/from the serial port should be delimited using this separator.
    private byte separator;
    // Buffer where a single message must fit
    // private byte[] buffer = new byte[1024];
    //private int bufferUsed = 0;
    
    private const byte FRAME_FIRST = 0x55;
    private const byte FRAME_SECOND = 0xAA;
    private const byte MCU_RX_VER = 0x00;
    private const byte MCU_TX_VER = 0x00;

    private const int PROTOCOL_HEAD = 7; // header length



    public SerialThreadBinaryDelimited(string portName,
                                       int baudRate,
                                       int delayBeforeReconnecting,
                                       int maxUnreadMessages,
                                       byte separator)
        : base(portName, baudRate, delayBeforeReconnecting, maxUnreadMessages, false)
    {
        this.separator = separator;
    }

    // ------------------------------------------------------------------------
    // Must include the separator already (as it shold have been passed to
    // the SendMessage method).
    // ------------------------------------------------------------------------
    // protected override void SendToWire(object message, SerialPort serialPort)
    // {
    //     byte[] binaryMessage = (byte[])message;
    //     serialPort.Write(binaryMessage, 0, binaryMessage.Length);
    // }

    // protected override object ReadFromWire(SerialPort serialPort)
    // {
    //     // Try to fill the internal buffer
    //     if(serialPort.BytesToRead < 3 ) return null;
    //     Debug.Log("byte num"+serialPort.BytesToRead);
    //     bufferUsed += serialPort.Read(buffer, bufferUsed, buffer.Length - bufferUsed);
    //     Debug.Log("buff"+BitConverter.ToString(buffer));
    //     // Search for the separator in the buffer
    //     int index = System.Array.FindIndex<byte>(buffer, 0, bufferUsed, IsSeparator);
    //     if (index == -1)
    //         return null;

    //     byte[] returnBuffer = new byte[index];
    //     System.Array.Copy(buffer, returnBuffer, index);

    //     // Shift the buffer so next time the unused bytes start at 0 (safe even
    //     // if there is overlap)
    //     System.Array.Copy(buffer, index + 1, buffer, 0, bufferUsed - index);
    //     bufferUsed -= index + 1;

    //     return returnBuffer;
    // }

    // private bool IsSeparator(byte aByte)
    // {
    //     return aByte == separator;
    // }

    protected override void SendToWire(object message, SerialPort serialPort)
    {
        byte[] binaryMessage = (byte[])message;

        if (binaryMessage.Length == 1)
        {
            // No data payload
            byte[] payload = new byte[7];
            payload[0] = FRAME_FIRST;
            payload[1] = FRAME_SECOND;
            payload[2] = MCU_TX_VER;
            payload[3] = binaryMessage[0];
            payload[4] = 0; // len high
            payload[5] = 0; // len low
            payload[6] = (byte)(0xFF + binaryMessage[0]);

            serialPort.Write(payload, 0, payload.Length);
        }
        else
        {
            int payloadLen = binaryMessage.Length + PROTOCOL_HEAD - 1;
            byte[] dataPayload = new byte[payloadLen];

            int idx = 0;
            dataPayload[idx++] = FRAME_FIRST;
            dataPayload[idx++] = FRAME_SECOND;
            dataPayload[idx++] = MCU_TX_VER;
            dataPayload[idx++] = binaryMessage[0];
            dataPayload[idx++] = (byte)((binaryMessage.Length -1) >> 8);   // length high
            dataPayload[idx++] = (byte)((binaryMessage.Length -1) & 0xFF); // length low

            Array.Copy(binaryMessage, 1, dataPayload, idx, binaryMessage.Length-1);
            idx += binaryMessage.Length-1;

            // Checksum
            dataPayload[payloadLen - 1] = GetCheckSum(0, dataPayload, payloadLen - 1);

            serialPort.Write(dataPayload, 0, dataPayload.Length);
        }
    }

    protected override object ReadFromWire(SerialPort serialPort)
    {
        while (serialPort.BytesToRead >= PROTOCOL_HEAD + 4)
        {
            byte rxByte;
            int cmdDpLenSum = FRAME_FIRST + FRAME_SECOND + MCU_RX_VER;

            // Check first header byte
            rxByte = (byte)serialPort.ReadByte();
            if (rxByte != FRAME_FIRST) continue;

            // Check second header byte
            rxByte = (byte)serialPort.ReadByte();
            if (rxByte != FRAME_SECOND) continue;

            // Check version byte
            rxByte = (byte)serialPort.ReadByte();
            if (rxByte != MCU_RX_VER) continue;

            // Command byte
            byte cmd = (byte)serialPort.ReadByte();
            cmdDpLenSum += cmd;

            // Length high
            rxByte = (byte)serialPort.ReadByte();
            cmdDpLenSum += rxByte;
            int rxDpValueLen = rxByte << 8;

            // Length low
            rxByte = (byte)serialPort.ReadByte();
            cmdDpLenSum += rxByte;
            rxDpValueLen += rxByte;

            // Wait until enough data arrives
            if (serialPort.BytesToRead < rxDpValueLen)
            {
                Thread.Sleep(500); // mimic vTaskDelay
                if (serialPort.BytesToRead < rxDpValueLen) break;
            }

            // Read payload
            byte[] dp = new byte[rxDpValueLen];
            // serialPort.Read(dp, 1, 1);
            for(int i = 0 ; i<rxDpValueLen; i++){
                dp[i] = (byte)serialPort.ReadByte();
            }
            // Read checksum
            byte checksum = (byte)serialPort.ReadByte();
            Debug.Log("cksm"+GetCheckSum(cmdDpLenSum, dp, rxDpValueLen));
            if (GetCheckSum(cmdDpLenSum, dp, rxDpValueLen) != checksum)
            {
                Debug.LogWarning("Checksum failed !!");
                continue;
            }

            // Handle the received data
            DataHandle(cmd, dp, rxDpValueLen);
            
            byte[] returnBytes = new byte[rxDpValueLen+1];
            returnBytes[0] = cmd;
            System.Array.Copy(dp, 0, returnBytes, 1, rxDpValueLen);

            return returnBytes;

        }
        return null;
    }

    // private byte serialPort.ReadByte()
    // {
    //     int val = serialPort.ReadByte();
    //     if (val < 0) throw new TimeoutException("UART read timeout");
    //     return (byte)val;
    // }

    private byte GetCheckSum(int sum, byte[] data, int len)
    {
        int checksum = sum;
        for (int i = 0; i < len; i++)
            checksum += data[i];
        return (byte)(checksum & 0xFF);
    }

    private void DataHandle(byte cmd, byte[] data, int len)
    {
        // TODO: implement your command/data handler
        Debug.Log($"Received CMD {cmd:X2}, DataLen={len}");
    }
}
