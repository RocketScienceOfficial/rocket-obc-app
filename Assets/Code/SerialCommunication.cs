using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using UnityEngine;

public class SerialCommunication : MonoBehaviour
{
    private const string RP2040_PID = "000A";
    private const string RP2040_VID = "2E8A";

    public static SerialCommunication Instance { get; private set; }

    public event EventHandler OnConnected;
    public event EventHandler OnDisconnected;
    public event EventHandler<SerialCommunicationOnReadEventArgs> OnRead;

    public bool IsConnected => _currentSerialPort != null;
    public string PortName => _currentSerialPort?.PortName;

    private readonly Queue<string> _serialReadQueue = new();
    private readonly object _serialReadQueueLock = new();
    private bool _closePort;
    private readonly object _closePortLock = new();
    private bool _disconnectPort;
    private readonly object _disconnectPortLock = new();
    private SerialPort _currentSerialPort;
    private Thread _serialReadThread;


    private void Awake()
    {
        Instance = this;

        StartCoroutine(FetchPorts());
    }

    private void Update()
    {
        lock (_serialReadQueueLock)
        {
            if (_serialReadQueue.Count > 0)
            {
                var data = _serialReadQueue.Dequeue();

                OnRead?.Invoke(this, new SerialCommunicationOnReadEventArgs { Data = data });
            }
        }

        lock (_closePortLock)
        {
            if (_closePort)
            {
                BeginDisconnect();

                _closePort = false;
            }
        }

        lock (_disconnectPortLock)
        {
            if (_disconnectPort)
            {
                EndDisconnect();

                _disconnectPort = false;
            }
        }
    }

    private void OnApplicationQuit()
    {
        BeginDisconnect();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            BeginDisconnect();
        }
    }


    private void Connect(string port)
    {
        try
        {
            _currentSerialPort = new SerialPort()
            {
                PortName = port,
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                RtsEnable = true,
                DtrEnable = true,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
            };

            _currentSerialPort.Open();

            _serialReadThread = new Thread(SerialReadThread);
            _serialReadThread.Start();

            OnConnected?.Invoke(this, EventArgs.Empty);

            print("COM Port Connected!");
        }
        catch (Exception ex)
        {
            print("Could not find serial port: " + ex);

            _currentSerialPort = null;
        }
    }

    private void BeginDisconnect()
    {
        if (IsConnected)
        {
            var closeThread = new Thread(() =>
            {
                _currentSerialPort.Close();
                _currentSerialPort = null;

                lock (_disconnectPortLock)
                {
                    _disconnectPort = true;
                }
            });

            closeThread.Start();
        }
    }

    private void EndDisconnect()
    {
        _serialReadThread.Join();
        _serialReadThread = null;

        OnDisconnected?.Invoke(this, EventArgs.Empty);

        print("COM Port Disconnected!");
    }


    private void SerialReadThread()
    {
        while (IsConnected)
        {
            try
            {
                var message = _currentSerialPort.ReadLine();

                if (!string.IsNullOrEmpty(message))
                {
                    lock (_serialReadQueueLock)
                    {
                        _serialReadQueue.Enqueue(message);
                    }
                }
            }
            catch (TimeoutException) { }
            catch (InvalidOperationException) { }
            catch (IOException)
            {
                lock (_closePortLock)
                {
                    _closePort = true;
                }

                break;
            }
        }
    }

    public void SerialPortWrite(string data)
    {
        if (IsConnected)
        {
            _currentSerialPort.WriteLine(data + "\r");

            print("Written to serial port: " + data);
        }
    }


    private IEnumerator FetchPorts()
    {
        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            if (!IsConnected)
            {
                var ports = ListSerialPorts();

                print("Fetching serial ports returned: " + ports.Count + " ports");

                if (ports.Count > 0)
                {
                    Connect(ports[0]);
                }
            }

            yield return new WaitForSeconds(1.0f);
        }
    }

    /**
     * REF: https://stackoverflow.com/questions/10350340/identify-com-port-using-vid-and-pid-for-usb-device-attached-to-x64
     */
    private List<string> ListSerialPorts()
    {
        var pattern = string.Format("^VID_{0}.PID_{1}", RP2040_VID, RP2040_PID);
        var _rx = new Regex(pattern, RegexOptions.IgnoreCase);
        var comports = new List<string>();

        var rk1 = Registry.LocalMachine;
        var rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");

        foreach (var s3 in rk2.GetSubKeyNames())
        {
            var rk3 = rk2.OpenSubKey(s3);

            foreach (var s in rk3.GetSubKeyNames())
            {
                if (_rx.Match(s).Success)
                {
                    var rk4 = rk3.OpenSubKey(s);

                    foreach (var s2 in rk4.GetSubKeyNames())
                    {
                        var rk5 = rk4.OpenSubKey(s2);
                        var location = (string)rk5.GetValue("LocationInformation");
                        var rk6 = rk5.OpenSubKey("Device Parameters");
                        var portName = (string)rk6.GetValue("PortName");

                        if (!string.IsNullOrEmpty(portName) && SerialPort.GetPortNames().Contains(portName))
                        {
                            comports.Add(portName);
                        }
                    }
                }
            }
        }

        return comports;
    }
}

public class SerialCommunicationOnReadEventArgs : EventArgs
{
    public string Data { get; set; }
}