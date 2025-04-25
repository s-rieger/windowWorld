using System;
using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;

public class ArduinoSetup : MonoBehaviour
{
    SerialPort sp = new SerialPort("COM9", 9600);
    public bool isStreaming = false;

    [Header("PhotoResistor")]
    public ScreenDetector screenDetector;

    private Thread serialThread;
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    private bool isRunning = false;

    void OpenConnection()
    {
        if (sp.IsOpen) return;

        sp.ReadTimeout = 100;
        sp.DtrEnable = true;
        sp.Open();
        isStreaming = true;

        // Start the background thread
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
}