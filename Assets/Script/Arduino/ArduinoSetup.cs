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
        
        SetAllLedsColor("OFF");
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
                SetAllLedsColor("RED");
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
    
    public void SetLedColorForPlayer(int playerNumber, string colorCommand)
    {
        if (!sp.IsOpen) return;

        try
        {
            sp.WriteLine($"LED{playerNumber}:{colorCommand}");
            sp.BaseStream.Flush();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Send error: " + ex.Message);
        }
    }
    
    public void SetAllLedsColor(string colorCommand)
    {
        if (!sp.IsOpen) return;

        try
        {
            sp.WriteLine($"ALL:{colorCommand}");
            sp.BaseStream.Flush();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Send error: " + ex.Message);
        }
    }
    
}