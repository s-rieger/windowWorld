using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class ScreenDetector : MonoBehaviour
{
    public RawImage webcamDisplay;      // To display the webcam feed
    public RawImage resultDisplay;      // To display the green-pixel result

    private WebCamTexture webcamTexture;
    private Texture2D outputTexture;

    public float redThreshold = 0.8f;   // How red a pixel needs to be
    public float greenMax = 0.3f;       // Max green value to still be considered "red"
    public float blueMax = 0.3f;        // Max blue value to still be considered "red"

    [SerializeField] private Button JoinBtn;
    int width;
    int height;

    Color[] webcamPixels;
    Color[] resultPixels;


    [SerializeField] int xOff;
    [SerializeField] int yOff;
    [SerializeField] int screenWidth;
    [SerializeField] int screenHeight;

    [SerializeField] int currentPlayers = 0;

    [Header("Checks")]
    public int scanCompleteValue = 0;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        webcamDisplay.texture = webcamTexture;
        webcamTexture.Play();

        outputTexture = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.RGBA32, // <-- must be this or similar (not RGB24!)
            false
        );
        resultDisplay.texture = outputTexture;

        JoinBtn.onClick.AddListener(() => { JoinPlayer(); });

        webcamPixels = webcamTexture.GetPixels();
        resultPixels = new Color[webcamPixels.Length];


        //InvokeRepeating(nameof(ProcessFrame), 1f, 0.1f); // Process 10 times a second
    }

    private void Update()
    {
        //ProcessFrame();
    }

    void JoinPlayer()
    {
        webcamPixels = webcamTexture.GetPixels();
        resultPixels = new Color[webcamPixels.Length];

        width = webcamTexture.width;
        height = webcamTexture.height;

        for (int i = 0; i < resultPixels.Length; i++)
        {
            resultPixels[i] = Color.clear; // Default transparent
        }

        outputTexture.SetPixels(resultPixels);
        outputTexture.Apply();


        for (int x = xOff; x < xOff + screenWidth; x++)
        {
            for (int y = yOff; y < yOff + screenHeight; y++)
            {
                int index = y * width + x;
                resultPixels[index] = Color.blue; // Default transparent
            }
        }

        outputTexture.SetPixels(resultPixels);
        outputTexture.Apply();


        StartCoroutine(ScanJoinArea());

        JoinBtn.gameObject.SetActive(false);
    }


    IEnumerator ScanJoinArea()
    {
        yield return new WaitForSeconds(1);

        Debug.Log("StartScanJoinArea");

        // Expand bounds by 50 pixels
        int padding = 50;
        int s1minX = Mathf.Max(0, xOff - padding);
        int s1maxX = Mathf.Min(width - 1, xOff + screenWidth + padding);

        int s1minY = Mathf.Max(0, yOff - padding);
        int s1maxY = Mathf.Min(height - 1, yOff + screenHeight + padding);

        int scanGood = 0;
        int screenScanJoinBuffer = 5000;

        while(scanCompleteValue < 100)
        {
            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            width = webcamTexture.width;
            height = webcamTexture.height;

            // Second pass: loop over cropped region and set green where red was
            for (int x = s1minX; x <= s1maxX; x++)
            {
                for (int y = s1minY; y <= s1maxY; y++)
                {
                    int index = y * width + x;
                    Color pixel = webcamPixels[index];

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        scanGood++;
                        resultPixels[index] = Color.green;
                    }
                    else
                    {
                        resultPixels[index] = Color.clear; // Default transparent
                    }
                }
            }
            Debug.Log("Scan entire Arean|good pixels: " + scanGood);


            if (scanGood > screenWidth*screenHeight - screenScanJoinBuffer) { scanCompleteValue++; Debug.Log("Scan good"); }
            
            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();

        }

        Debug.Log("Scan complete");

        yield return null;
    }


    void ProcessFrame()
    {
        if (!webcamTexture.isPlaying || webcamTexture.width < 10) return;

        webcamPixels = webcamTexture.GetPixels();
        resultPixels = new Color[webcamPixels.Length];

        width = webcamTexture.width;
        height = webcamTexture.height;

        // Track red pixel bounds
        int minX = 0, minY = 0, maxX = width, maxY = height;

        // First pass: detect red pixels and calculate bounds
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color pixel = webcamPixels[index];

                if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                {
                    // Update bounds
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                resultPixels[index] = Color.clear; // Default transparent
            }
        }

       

        // Expand bounds by 50 pixels
        int padding = 50;
        minX = Mathf.Max(0, minX - padding);
        maxX = Mathf.Min(width - 1, maxX + padding);
        minY = Mathf.Max(0, minY - padding);
        maxY = Mathf.Min(height - 1, maxY + padding);

        // Second pass: loop over cropped region and set green where red was
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int index = y * width + x;
                Color pixel = webcamPixels[index];

                if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                {
                    resultPixels[index] = Color.green;
                }
            }
        }

        outputTexture.SetPixels(resultPixels);
        outputTexture.Apply();
    }
}