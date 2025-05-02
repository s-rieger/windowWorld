using System;
using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;

public class ArduinoSetup : MonoBehaviour
{
    public static ArduinoSetup instance;
    
    SerialPort sp = new SerialPort("COM9", 9600);
    public bool isStreaming = false;

    [Header("PhotoResistor")]
    public ScreenDetector screenDetector;

    private Thread serialThread;
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    private bool isRunning = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OpenConnection()
    {
        if (sp.IsOpen) return;

        sp.ReadTimeout = 100;
        sp.DtrEnable = true;
        sp.Open();
        isStreaming = true;

        isRunning = true;
        serialThread = new Thread(ReadSerialPortInBackground);
        serialThread.Start();
    }

    void Start()
    {
        OpenConnection();
    }

    private void Update()
    {
        if (!isStreaming) return;

        while (messageQueue.TryDequeue(out string value))
        {
            if (value.StartsWith("ALERT:HighLight"))
            {
                Debug.Log("High light detected");
                SetLedColor("RED");
                screenDetector.JoinPlayer();
            }
        }
    }

    void OnDisable()
    {
        isRunning = false;
        if (serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join();
        }

        if (sp != null && sp.IsOpen)
        {
            sp.Close();
        }
    }

    void ReadSerialPortInBackground()
    {
        while (isRunning)
        {
            try
            {
                string message = sp.ReadLine();
                messageQueue.Enqueue(message);
            }
            catch (TimeoutException)
            {
                // Ignore timeouts
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Serial port error: " + ex.Message);
            }
        }
    }
    
    public void SetLedColor(string colorCommand)
    {
        if (!sp.IsOpen) return;

        try
        {
            sp.WriteLine(colorCommand);
            sp.BaseStream.Flush();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Send error: " + ex.Message);
        }
    }
}