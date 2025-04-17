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

    Vector4[] playerScreens;

    [SerializeField] private Color p1;
    [SerializeField] private Color p2;
    [SerializeField] private Color p3;
    [SerializeField] private Color p4;    
    [SerializeField] private Color p5;
    [SerializeField] private Color p6;
    [SerializeField] private Color p7;
    [SerializeField] private Color p8;

    private List<Color> colorList = new List<Color>(4);

    [SerializeField] int xOff;
    [SerializeField] int yOff;
    [SerializeField] int screenWidth;
    [SerializeField] int screenHeight;
    [SerializeField] float  neededScanPercentage =.2f;

    [SerializeField] int newPlayerOffset = 0;
    [SerializeField] int xSpacing = 50;

    [SerializeField] int currentPlayers = 0;

    [Header("Checks")]
    public int scanCompleteValue = 0;

    void Start()
    {
        colorList.Add(p1);
        colorList.Add(p2);
        colorList.Add(p3);
        colorList.Add(p4);
        colorList.Add(p5);
        colorList.Add(p6);
        colorList.Add(p7);
        colorList.Add(p8);


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

        xOff = currentPlayers * screenWidth + xSpacing; // Adds offset fo 2nd or 3rd player

        // SET COLORED BOX FOR PLAYERS TO HOLD PHONE
        // TODO: ADD PI CHART FOR JOINING PROGRESS
        for (int x = xOff; x < xOff + screenWidth; x++)
        {
            for (int y = yOff; y < yOff + screenHeight; y++)
            {
                int index = y * width + x;
                resultPixels[index] = colorList[currentPlayers]; // Default transparent
            }
        }

        outputTexture.SetPixels(resultPixels);
        outputTexture.Apply();


        StartCoroutine(ScanJoinArea());

        JoinBtn.gameObject.SetActive(false);
    }

    void TracePlayers()
    {

    }


    IEnumerator ScanJoinArea()
    {
        Debug.Log("StartScanJoinArea");

        // Expand bounds by 50 pixels
        int padding = 50;
        int s1minX = Mathf.Max(0, xOff - padding );
        int s1maxX = Mathf.Min(width - 1, xOff + screenWidth + padding);

        int s1minY = Mathf.Max(0, yOff - padding);
        int s1maxY = Mathf.Min(height - 1, yOff + screenHeight + padding);

        int scan = 0;
        int scanGood = 0;
        int screenScanJoinBuffer = Mathf.FloorToInt(screenWidth * screenHeight * neededScanPercentage);

        while(scanCompleteValue < 100)
        {
            scan = 0;
            scanGood = 0;

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
                        resultPixels[index] = colorList[currentPlayers];

                        // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                    }
                    else
                    {
                        resultPixels[index] = Color.clear; // Default transparent
                    }

                    scan++;
                }
            }

            Debug.Log("Scan entire Arean|good pixels: " + scan+ "|" + scanGood);


            if (scanGood > screenWidth*screenHeight - screenScanJoinBuffer)
            {                         
                // TODO: FILL PICHART
                scanCompleteValue++; 
            }
            else // TODO: CHECK PERFORMANCE, MIGHT BE UNNECCESARRAADASDASD
            {
                // TODO: DRAIN PI CHART
                scanCompleteValue = 0;
                for (int x = xOff; x < xOff + screenWidth; x++)
                {
                    for (int y = yOff; y < yOff + screenHeight; y++)
                    {
                        int index = y * width + x;
                        resultPixels[index] = colorList[currentPlayers]; // Default transparent
                    }
                }
            }
            
            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();

            yield return null;
        }

        Debug.Log($"scan: {scan}, scannGood: {scanGood}");
        Debug.Log("Scan complete");
        scan = 0;
        scanGood = 0;
        scanCompleteValue = 0;
        currentPlayers++;

        JoinBtn.gameObject.SetActive(true);
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