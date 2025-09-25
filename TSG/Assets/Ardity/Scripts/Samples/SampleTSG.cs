/**
 * Ardity (Serial Communication for Arduino + Unity)
 * Author: Daniel Wilches <dwilches@gmail.com>
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Text;
using System;
/**
 * Sample for reading using polling by yourself, and writing too.
 */

public enum CommandType
{
    ACTUATOR_CTRL_ANGL_NRM = 0,
    ACTUATOR_CTRL_ANGL_FRC,
    ACTUATOR_CTRL_EVT_NRM,
    ACTUATOR_CTRL_EVT_FRC,
    ACTUATOR_CTRL_ALL_ANGL_NRM,
    ACTUATOR_CTRL_ALL_ANGL_FRC,
    ACTUATOR_CTRL_ALL_EVT_NRM,
    ACTUATOR_CTRL_ALL_EVT_FRC,
    ACTUATOR_CTRL_MOV_PLUS,
    ACTUATOR_CTRL_MOV_MINUS,
    ACTUATOR_GET_POS_ALL,
    ACTUATOR_CTRL_CALIB,
    ACTUATOR_CTRL_ALL_CALIB,
    
}

public enum ActuatorID
{
    ACT_ALL = 0,
    ACT_NECK,
    ACT_HANDS,
    ACT_BACK,
}
public enum EventID
{
    EVT_ACT_HOME = 0,
    EVT_ACT_STOP,
    EVT_ACT_WAVE,
    EVT_ACT_BOW,
    EVT_ACT_DANCE,
}
public class TSG : MonoBehaviour
{
    public SerialControllerCustomDelimiter serialController;

    void SendActuatorCommandEvent(CommandType cmd, ActuatorID act, EventID evt, int speed)
    {
        byte[] data = new byte[4];
        data[0] = (byte)cmd;
        data[1] = (byte)act;
        data[2] = (byte)evt;
        data[3] = (byte)speed;
        serialController.SendSerialMessage(data);
    }
    void SendActuatorCommandAngle(CommandType cmd, ActuatorID act, int angle)
    {
        byte[] data = new byte[3];
        data[0] = (byte)cmd;
        data[1] = (byte)act;
        data[2] = (byte)angle;
        serialController.SendSerialMessage(data);
    }
    void SendActuatorCommandMovement(CommandType cmd, ActuatorID act, int rateOfChange)
    {
        byte[] data = new byte[3];
        data[0] = (byte)cmd;
        data[1] = (byte)act;
        data[2] = (byte)rateOfChange;
        serialController.SendSerialMessage(data);
    }
    void GetActuatorPosition()
    {
        byte[] data = new byte[1];
        data[0] = (byte)CommandType.ACTUATOR_GET_POS_ALL;
        serialController.SendSerialMessage(data);
    }

    // Initialization
    void Start()
    {
        serialController = GameObject.Find("SerialController").GetComponent<SerialControllerCustomDelimiter>();

        Debug.Log("Press the SPACEBAR to execute some action");
    }
    // Executed each frame
    void Update()
    {
        //---------------------------------------------------------------------
        // Send data
        //---------------------------------------------------------------------

        // If you press one of these keys send it to the serial device. A
        // sample serial device that accepts this input is given in the README.
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log("Sending neck up");
            SendActuatorCommandMovement(CommandType.ACTUATOR_CTRL_MOV_PLUS, ActuatorID.ACT_NECK, 1);
        }
        else if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log("Sending neck down");
            SendActuatorCommandMovement(CommandType.ACTUATOR_CTRL_MOV_MINUS, ActuatorID.ACT_NECK, 1);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("Sending back left");
            SendActuatorCommandMovement(CommandType.ACTUATOR_CTRL_MOV_PLUS, ActuatorID.ACT_BACK, 10);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("Sending back right");
            SendActuatorCommandMovement(CommandType.ACTUATOR_CTRL_MOV_MINUS, ActuatorID.ACT_BACK, 10);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("Sending hands up");
            SendActuatorCommandMovement(CommandType.ACTUATOR_CTRL_MOV_PLUS, ActuatorID.ACT_HANDS, 1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Sending hands down");
            SendActuatorCommandMovement(CommandType.ACTUATOR_CTRL_MOV_MINUS, ActuatorID.ACT_HANDS, 1);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Getting position");
            GetActuatorPosition();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Sending some action");
            byte[] data = new byte[] { (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'0' };
            // data[11]=GetCheckSum(0,data,11);
            // Debug.Log("Sending "+data[11]);
            // Sends a 65 (ascii for 'A') followed by an space (ascii 32, which 
            // is configured in the controller of our scene as the separator).
            serialController.SendSerialMessage(data);
        }


        //---------------------------------------------------------------------
        // Receive data
        //---------------------------------------------------------------------

        byte[] message = serialController.ReadSerialMessage();

        if (message == null)
            return;

        // StringBuilder sb = new StringBuilder();
        // foreach (byte b in message)
        //     sb.AppendFormat("(#{0}={1})    ", b, (char)b);
        // Debug.Log("Received some bytes, printing their ascii codes: " + sb);
        Debug.Log("Received some bytes, printing their ascii codes: " + BitConverter.ToString(message));

    }
}
