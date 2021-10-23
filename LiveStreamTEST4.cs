/*
* Filename: PlayHandLive_v2.cs
* Author: Joshua Hyde
* Date Created: 15/07/2021
* Date Modified: 24/08/2021
* Parts of Code Adapted From: Microsoft .NET Documentation
*/


using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using QuaternionToAngle;
using AngleDataTEST;
using ServerReceiver;

/// <summary>
/// The main part of the code that has changed is instead of receiving a UDP packet via Local IP on the router, it receives a TCP packet directly through bluetooth.
/// All calculations and Unity interaction is done in this script so that stays the same.
/// </summary>

public class LiveStreamTEST4 : MonoBehaviour
{

    // Line reading object

    // This has changed so it can receive two inputs, one for each IMU via Bluetooth

    private string _lineread1;
    private string[] _splitter1;
    private string _lineread2;
    private string[] _splitter2;

    ////////

    private float[] _temparray = new float[8];

    // Flag

   private bool _recangle = false;

    // Game Object

    public Transform ref1;
    public Transform ref2;

    public Text text1;
    public Text text2;
    public Text text3;
    // public Text text4;
    public Text text5;

    public GameObject good;
    public GameObject marginal;
    public GameObject bad;
    public GameObject showtxt1;
    public GameObject showtxt2;
    public GameObject showtxt3;
    // public GameObject showtxt4;
    public GameObject showtxt5;
    public GameObject showtxt6;
    public GameObject showtxt7;
    public GameObject recordinglight;
    
    // Vector Object
    private Vector3 _vect1 = new Vector3(0,0,0);
    private Quaternion _quat1 = new Quaternion(0,0,0,0);
    private Vector3 _vect2 = new Vector3(0,0,0);
    private Quaternion _quat2 = new Quaternion(0,0,0,0);

    // Angle Object

    private QuaternionConversion _angle = new QuaternionConversion();
    private double _finalxangle;
    private double _finalyangle;
    private double _finalzangle;
    // private double _finalanyangle;
    // private float testangle;

    // Angle Data Object

    private AngleData _angledata = new AngleData();

    // Filestream

    private FileStream _fobjang;
    private StreamWriter _strang;

    private char[] _delimiter = {',', ':', '{', '}', '[', ']', '\"', ' '};

    // UDP Members

    // private IPAddress _ipAddress;
    // private IPEndPoint _ipLocalEndPoint;
    // private UdpClient _udpClient;
    // private IPEndPoint _RemoteIpEndPoint;
    // private UdpClient _receivingUdpClient;
    // private IPEndPoint _RemoteIpEndPoint;
    // private Byte[] _receiveBytes;
    // private string _returnData;

    // REPLACE WITH TCP

    private Server _bluetoothobj;

    // Timestamp and time reference

    private string _timestamp;
    private string _timestamp_print;

    // Time elapsed since 1/1/2000

    private DateTime _datetime = new DateTime(2000,1,1,0,0,0,0,DateTimeKind.Utc);
    private DateTime _datetime_now;
    private TimeSpan _timeelapsed;
    private double _timeelapsed_inseconds;

    // Keystroke Check for Exercise Measurement
    private bool _flex = true;
    private bool _ext = false;
    private bool _pron = false;
    private bool _sup = false;
    private bool _rad = false;
    private bool _uln = false;

    // Keystroke for Show or Hide Text

    private bool _hidetextcheck = false;

    // Angle value
    private double _correctangle;

    // Keystroke Flags
    private bool _hflag = false;
    private bool _aflag = false;
    private bool _fflag = false;
    private bool _eflag = false;
    private bool _pflag = false;
    private bool _sflag = false;
    private bool _rflag = false;
    private bool _uflag = false;
    private bool _alphaoneflag = false;
    private bool _alphatwoflag = false;
    private bool _alphathreeflag = false;
    private bool _alphafourflag = false;

    // Keystroke change flag

    private void _KeystrokeCheck()
    {
        if( Input.GetKeyUp(KeyCode.H))
        {
            _hflag = true;
        }

        if( Input.GetKeyUp(KeyCode.A))
        {
            _aflag = true;
        }

        if( Input.GetKeyDown(KeyCode.F))
        {
            _fflag = true;
        }        

        if( Input.GetKeyDown(KeyCode.E))
        {
            _eflag = true;
        }

        if( Input.GetKeyDown(KeyCode.P))
        {
            _pflag = true;
        }

        if( Input.GetKeyDown(KeyCode.S))
        {
            _sflag = true;
        }

        if( Input.GetKeyDown(KeyCode.R))
        {
            _rflag = true;
        }

        if( Input.GetKeyDown(KeyCode.U))
        {
            _uflag = true;
        }

        if( Input.GetKeyDown(KeyCode.Alpha1))
        {
            _alphaoneflag = true;
        }

        if( Input.GetKeyDown(KeyCode.Alpha2))
        {
            _alphatwoflag = true;
        }

        if( Input.GetKeyDown(KeyCode.Alpha3))
        {
            _alphathreeflag = true;
        }

        if( Input.GetKeyDown(KeyCode.Alpha4))
        {
            _alphafourflag = true;
        }

    }

    // Display or hide text

    private void _HideText()
    {

        if( _hflag == true )
        {
            _hflag = false;

            if( _hidetextcheck == false )
            {

                showtxt1.SetActive(false);
                showtxt2.SetActive(false);
                showtxt3.SetActive(false);
                // showtxt4.SetActive(false);
                showtxt5.SetActive(false);
                showtxt6.SetActive(false);
                showtxt7.SetActive(false);
                _hidetextcheck = true;
            }
            else
            {
                showtxt1.SetActive(true);
                showtxt2.SetActive(true);
                showtxt3.SetActive(true);
                // showtxt4.SetActive(true);
                showtxt5.SetActive(true);
                showtxt6.SetActive(true);
                showtxt7.SetActive(true);
                _hidetextcheck = false;
            }
    
        }
    }


    // Check acceptable range

    private void _testAngle()
    {
        if(( _flex == true ) || ( _ext == true ))
        {

            if(( _finalyangle >= _correctangle - 3 ) && ( _finalyangle <= _correctangle + 3 ))
            {

                good.SetActive(true);
                marginal.SetActive(false);
                bad.SetActive(false);

            }
            else if(( _finalyangle >= _correctangle - 5 ) && ( _finalyangle <= _correctangle + 5 ))
            {

                good.SetActive(false);
                marginal.SetActive(true);
                bad.SetActive(false);

            }
            else
            {

                good.SetActive(false);
                marginal.SetActive(false);
                bad.SetActive(true);

            }

        }
        
        if(( _pron == true ) || ( _sup == true ))
        {

            if(( _finalzangle >= _correctangle - 3 ) && ( _finalzangle <= _correctangle + 3 ))
            {

                good.SetActive(true);
                marginal.SetActive(false);
                bad.SetActive(false);

            }
            else if(( _finalzangle >= _correctangle - 5 ) && ( _finalzangle <= _correctangle + 5 ))
            {

                good.SetActive(false);
                marginal.SetActive(true);
                bad.SetActive(false);

            }
            else
            {

                good.SetActive(false);
                marginal.SetActive(false);
                bad.SetActive(true);

            }

        }

        if(( _rad == true ) || ( _uln == true ))
        {

            if(( _finalxangle >= _correctangle - 3 ) && ( _finalxangle <= _correctangle + 3 ))
            {

                good.SetActive(true);
                marginal.SetActive(false);
                bad.SetActive(false);

            }
            else if(( _finalxangle >= _correctangle - 5 ) && ( _finalxangle <= _correctangle + 5 ))
            {

                good.SetActive(false);
                marginal.SetActive(true);
                bad.SetActive(false);

            }
            else
            {

                good.SetActive(false);
                marginal.SetActive(false);
                bad.SetActive(true);

            }

        }

    }

    // Getting angle values, this is hardcoded for now

    private void _ObtainValue()
    {
        if( _flex == true )
        {
            _correctangle = _angledata.GetFlexAngle();
        }

        if( _ext == true )
        {
            _correctangle = _angledata.GetExtAngle();
        }

        if( _pron == true )
        {
            _correctangle = _angledata.GetPronAngle();
        }

        if( _sup == true )
        {
            _correctangle = _angledata.GetSupAngle();
        }

        if( _rad == true )
        {
            _correctangle = _angledata.GetRadAngle();
        }

        if( _uln == true )
        {
            _correctangle = _angledata.GetUlnAngle();
        }
    }

    // Check the keystroke

    private void _CheckKeyStroke()
    {
        // Set angle to use

        if(_fflag == true)
        {
            _fflag = false;

            _flex = true;
            _ext = false;
            _pron = false;
            _sup = false;
            _rad = false;
            _uln = false;

        }

        if(_eflag == true)
        {
            _eflag = false;

            _flex = false;
            _ext = true;
            _pron = false;
            _sup = false;
            _rad = false;
            _uln = false;

        }

        if(_pflag == true)
        {
            _pflag = false;

            _flex = false;
            _ext = false;
            _pron = true;
            _sup = false;
            _rad = false;
            _uln = false;
        }

        if(_sflag == true)
        {
            _sflag = false;

            _flex = false;
            _ext = false;
            _pron = false;
            _sup = true;
            _rad = false;
            _uln = false;
        }

        if(_rflag == true)
        {
            _rflag = false;

            _flex = false;
            _ext = false;
            _pron = false;
            _sup = false;
            _rad = true;
            _uln = false;
        }

        if(_uflag == true)
        {
            _uflag = false;

            _flex = false;
            _ext = false;
            _pron = false;
            _sup = false;
            _rad = false;
            _uln = true;
        }

        // Set Age Group to Use

        if(_alphaoneflag == true)
        {
            _alphaoneflag = false;

            _angledata.SetAgeOne();
        }

        if(_alphatwoflag == true)
        {
            _alphatwoflag = false;

            _angledata.SetAgeTwo();
        }

        if(_alphathreeflag == true)
        {
            _alphathreeflag = false;

            _angledata.SetAgeThree();
        }

        if(_alphafourflag == true)
        {
            _alphafourflag = false;

            _angledata.SetAgeFour();
        }

    }


    private void _ChangeTransformAndAngle()
    {

        _vect1.x = ref1.position.x;
        _vect1.y = ref1.position.y;
        _vect1.z = ref1.position.z;
        _quat1.x = _temparray[0];
        _quat1.y = _temparray[1];
        _quat1.z = _temparray[2];
        _quat1.w = _temparray[3];
        _vect2.x = ref2.position.x; 
        _vect2.y = ref2.position.y;
        _vect2.z = ref2.position.z;
        _quat2.x = _temparray[4];
        _quat2.y = _temparray[5];
        _quat2.z = _temparray[6];
        _quat2.w = _temparray[7];

        // Project the hand onto end of arm

        // _vect1.x += _vect2.x;
        // _vect1.y += _vect2.y;
        // _vect1.z += _vect2.z;

        // Set Vector Position

        ref1.SetPositionAndRotation( _vect1, _quat1 );
        ref2.SetPositionAndRotation( _vect2, _quat2 );

        // Get Joint Angle

        _angle.xq1 = _temparray[0];
        _angle.yq1 = _temparray[1];
        _angle.zq1 = _temparray[2];
        _angle.wq1 = _temparray[3];
        _angle.xq2 = _temparray[4];
        _angle.yq2 = _temparray[5];
        _angle.zq2 = _temparray[6];
        _angle.wq2 = _temparray[7];

        _angle.refvect.xv = ref2.position.x;
        _angle.refvect.yv = ref2.position.y;
        _angle.refvect.zv = ref2.position.z;

        _angle.refvect = _angle.refvect.UnitVector();

        _finalxangle = _angle.GetXAng();
        _finalyangle = _angle.GetYAng();
        _finalzangle = _angle.GetZAng();
        // _finalanyangle = _angle.GetAnyAng();

        // Convert to degrees

        _finalxangle = 180/Math.PI * _finalxangle;
        _finalyangle = 180/Math.PI * _finalyangle;
        _finalzangle = 180/Math.PI * _finalzangle;
        // _finalanyangle = 180/Math.PI * _finalanyangle;

        // testangle = Quaternion.Angle(_quat1, _quat2);

        // Display Joint Angle

        text1.text = "Joint X Angle: " + _finalxangle.ToString("#.###");
        text2.text = "Joint Y Angle: " + _finalyangle.ToString("#.###");
        text3.text = "Joint Z Angle: " + _finalzangle.ToString("#.###");
        // text4.text = "Joint Angle about Arm Position: " + _finalanyangle.ToString("#.###");

        _ObtainValue();

        _testAngle();

    }

    private void _setTimeStamp()
    {
        string year;
        string month;
        string date;
        string hour;
        string minute;
        string second;

        year = DateTime.Now.Year.ToString("0000");
        month = DateTime.Now.Month.ToString("00");
        date = DateTime.Now.Day.ToString("00");
        hour = DateTime.Now.Hour.ToString("00");
        minute = DateTime.Now.Minute.ToString("00");
        second = DateTime.Now.Second.ToString("00");

        _timestamp = year + "-" + month + "-" + date + "-" + hour + "-" + minute + "-" + second; 

        _timestamp_print = year + "/" + month + "/" + date + ":- " + hour + ":" + minute + ":" + second; 

    }


    // Start is called before the first frame update
    void Start()
    {

        // Set frame rate to correspond with amount of UDP packets being sent per second

        // Application.targetFrameRate = 37;



        // UDP Objects, setting up host client and end point

        // Below is taken from Microsoft .NET starting here:

        // _ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        // _ipLocalEndPoint = new IPEndPoint(_ipAddress, 8585);
        // _udpClient = new UdpClient(_ipLocalEndPoint);
        // _RemoteIpEndPoint = new IPEndPoint(IPAddress.Any,0);

        // _receivingUdpClient = new UdpClient(8585);
        // _RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0); REPLACE THIS WITH TCP

        // Start receiving from bluetooth of both sensors instead of IP 

        Debug.Log("Starting");

        _bluetoothobj = new Server();

        Debug.Log("Created");

        _bluetoothobj.Start();

        Debug.Log("Starting Bluetooth");
        // _bluetoothobj.Calibrate();

        //////

        good.SetActive(false);
        marginal.SetActive(false);
        bad.SetActive(false);

        recordinglight.SetActive(false);

        _angledata.SetAgeTwo();

        // Ending here // // 


    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
        // Show Clock

        _setTimeStamp();

        text5.text = _timestamp_print;


        if(_aflag == true)
        {
            _aflag = false;

            if( _recangle == false )
            {

                _recangle = true;

                if( _fobjang == null )
                {

                    _fobjang = new FileStream( "rec_angle_" + _timestamp + ".csv", FileMode.OpenOrCreate);

                    _strang = new StreamWriter(_fobjang);

                    _strang.WriteLine("Timestamp,X_Angle,Y_Angle,Z_Angle,,arm_v_x,arm_v_y,arm_v_z,arm_q_x,arm_q_y,arm_q_z,arm_q_w,,hand_v_x,hand_v_y,hand_v_z,hand_q_x,hand_q_y,hand_q_z,hand_q_w");

                }

                recordinglight.SetActive(true);

            }
            else
            {

                _recangle = false;

                recordinglight.SetActive(false);

            }

        }


        // Blocks until a message returns on this socket from a remote host.

        // Below is taken from Microsoft .NET starting here:

        // _receiveBytes = _receivingUdpClient.Receive(ref _RemoteIpEndPoint);
        // _returnData = Encoding.ASCII.GetString(_receiveBytes);
        // _lineread = _returnData.ToString(); REPLACE THIS WITH TCP, NEED TO GET STRING FROM BLUETOOTH

        // Ending here // //

        // Receive from Bluetooth, each string it is assigned to is for each bluetooth 

        _lineread1 = _bluetoothobj.GetSensor1();

        Debug.Log(_lineread1);

        _splitter1 = _lineread1.Split(_delimiter, StringSplitOptions.RemoveEmptyEntries);

        Debug.Log(_splitter1[1]);

        _lineread2 = _bluetoothobj.GetSensor2();

        Debug.Log(_lineread2);

        _splitter2 = _lineread2.Split(_delimiter, StringSplitOptions.RemoveEmptyEntries);

        Debug.Log(_splitter2[1]);

        /////////////

        /*

        Hand Pos x, y, z, Rot x, y, z, w, Arm Pos x, y, z, Rot x, y, z, w

        After splitting: 
        Hand Position x -0.2251 y -0.1122 z 0.4541 Rotation x 0.0786 y -0.6624 z 0.7281 w 0.1578 Arm Position x -0.3808 y -0.1255 z 0.2141 Rotation x -0.7035 y 0.5391 z -0.1485 w 0.4386 
        0       1     2     3   4   5     6     7   8       9   10   11     12 13   14  15  16    17    18    19  20    21  22    23  24    25      26   27   28  29   30   31   32  33
        */

        // THIS NEEDS TO BE MODIFIED TO COLLECT CORRECT STRINGS

        // Parses from two separate strings instead of just one

        _temparray[0] = float.Parse(_splitter1[1]);
        _temparray[1] = float.Parse(_splitter1[3]);
        _temparray[2] = float.Parse(_splitter1[5]);
        _temparray[3] = float.Parse(_splitter1[7]);
        _temparray[4] = float.Parse(_splitter2[1]);
        _temparray[5] = float.Parse(_splitter2[3]);
        _temparray[6] = float.Parse(_splitter2[5]);
        _temparray[7] = float.Parse(_splitter2[7]);

        ////////////////////////////////////////////////////////

        _CheckKeyStroke();

        _ChangeTransformAndAngle();



        if( _recangle == true )
        {
            _datetime_now = DateTime.Now;

            _timeelapsed = _datetime_now.Subtract(_datetime);

            _timeelapsed_inseconds = Convert.ToDouble(_timeelapsed.TotalSeconds);

            _strang.WriteLine( _timeelapsed_inseconds.ToString() + "," + _finalxangle.ToString() + "," + _finalyangle.ToString() 
                                + "," + _finalzangle.ToString() + ",," + ref1.position.x.ToString() + "," + ref1.position.y.ToString() + "," + 
                                ref1.position.z.ToString() + "," + _temparray[0].ToString() + "," + _temparray[1].ToString() + "," + _temparray[2].ToString() + "," + 
                                _temparray[3].ToString() + ",," + ref2.position.x.ToString() + "," + ref2.position.y.ToString() + "," + ref2.position.z.ToString() + "," + 
                                _temparray[4].ToString() + "," + _temparray[5].ToString() + "," + _temparray[6].ToString() + "," + _temparray[7].ToString());

            _strang.Flush();
        }

        // Check for display and hide text keystroke

        _HideText();

        if( Input.GetKey("escape"))
        {

            if( _strang != null )
            {

                _strang.Close();

                _fobjang.Close();
            
            }

            // Stops bluetooth transmission

            _bluetoothobj.Stop(); 

            /////

            Application.Quit();
        }

        // Check for key stroke registration

        _KeystrokeCheck();

    }

}