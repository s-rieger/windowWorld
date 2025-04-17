using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using static ScreenDetector;

public class ScreenDetector : MonoBehaviour
{
    public RawImage webcamDisplay;      // To display the webcam feed
    public RawImage resultDisplay;      // To display the green-pixel result

    private WebCamTexture webcamTexture;
    private Texture2D outputTexture;

    public RectTransform webcamRect;
    public RectTransform playRect;

    public float redThreshold = 0.8f;   // How red a pixel needs to be
    public float greenMax = 0.3f;       // Max green value to still be considered "red"
    public float blueMax = 0.3f;        // Max blue value to still be considered "red"

    [SerializeField] private Button JoinBtn;
    int width;
    int height;

    Color[] webcamPixels;
    Color[] resultPixels;

    [System.Serializable]
    public struct PlayerScreen
    {
        public Vector2 topL, topR, botL, botR;
        public float ratio;
    }
    List<PlayerScreen> playerScreens = new List<PlayerScreen>();

    [SerializeField] private Color p1;
    [SerializeField] private Color p2;
    [SerializeField] private Color p3;
    [SerializeField] private Color p4;    
    [SerializeField] private Color p5;
    [SerializeField] private Color p6;
    [SerializeField] private Color p7;
    [SerializeField] private Color p8;

    [SerializeField] public int currentPlayers = 1;
    public List<Color> colorList = new List<Color>(4);

    [SerializeField] int xOff;
    [SerializeField] int yOff;
    [SerializeField] int screenWidth;
    [SerializeField] int screenHeight;
    [SerializeField] float  neededScanPercentage =.2f;

    [SerializeField] int newPlayerOffset = 0;
    [SerializeField] int xSpacing = 50;


    [Header("Checks")]
    public int scanCompleteValue = 0;

    public static ScreenDetector Instance;

    private void Awake()
    {
        if(Instance != null && Instance != this) {Destroy(Instance);}
        else {Instance = this;}
    }

    void Start()
    {
        //webcamRect.localScale = new Vector3(1, -1, 1);
        //playRect.localScale=new Vector3(1,-1,1);


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
        TracePlayers();
    }
    void TracePlayers()
    {
        if (playerScreens.Count == 0 || currentPlayers == 0) { return; }

        for (int i = 0; i < playerScreens.Count; i++)
        {
            xOff = i * screenWidth + xSpacing; // Adds offset fo 2nd or 3rd player

            int padding = 50;         // Expand bounds by 50 pixels
            int currentFrameMinX = Mathf.Max(0, xOff - padding);
            int currentFrameMaxX = Mathf.Min(width - 1, xOff + screenWidth + padding);
            int currentFrameMinY = Mathf.Max(0, yOff - padding);
            int currentFrameMaxY = Mathf.Min(height - 1, yOff + screenHeight + padding);

            int playerMinX = currentFrameMaxX;
            int playerMaxX = currentFrameMinX;
            int playerMinY = currentFrameMaxY;
            int playerMaxY = currentFrameMinY;

            int PlayerScreenWith;
            int PlayerScreenHeight;
            float PlayerScreenRatioCurrent;
            float PlayerScreenRatioMax = 1;

            int scan = 0;
            int scanGood = 0;
            int screenScanJoinBuffer = Mathf.FloorToInt(screenWidth * screenHeight * neededScanPercentage);

            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            width = webcamTexture.width;
            height = webcamTexture.height;

            // Second pass: loop over cropped region and set green where red was
            for (int x = currentFrameMinX; x <= currentFrameMaxX; x++)
            {
                for (int y = currentFrameMinY; y <= currentFrameMaxY; y++)
                {
                    int index = y * width + x;
                    Color pixel = webcamPixels[index];

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        scanGood++;
                        resultPixels[index] = colorList[i];

                        // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                        // Update bounds
                        if (x < playerMinX) playerMinX = x;
                        if (x > playerMaxX) playerMaxX = x;
                        if (y < playerMinY) playerMinY = y;
                        if (y > playerMaxY) playerMaxY = y;
                    }
                    else
                    {
                        resultPixels[index] = Color.clear; // Default transparent
                    }

                    PlayerScreenWith = playerMaxX - playerMinX;
                    PlayerScreenHeight = playerMaxY - playerMinY;

                    PlayerScreenRatioCurrent = (float)PlayerScreenWith / (float)PlayerScreenHeight;
                    if (PlayerScreenRatioCurrent > PlayerScreenRatioMax)
                    {
                        PlayerScreenRatioMax = PlayerScreenRatioCurrent;
                    }

                    scan++;
                }
            }

            playerScreens[i] = new PlayerScreen {
                topL = new Vector2(playerMinX, playerMaxY), // playerMinX
                topR = new Vector2(playerMaxX, playerMaxY), // playerMinX, // playerMaxX
                botL = new Vector2(playerMinX, playerMinY), // playerMinX, // playerMinY
                botR = new Vector2(playerMaxX, playerMinY), // playerMinX// playerMaxY
                ratio = PlayerScreenRatioMax
            };

            i++;
        }

        outputTexture.SetPixels(resultPixels);
        outputTexture.Apply();
    }

    void JoinPlayer()
    {
        ClearScreen();
        StartCoroutine(ScanJoinArea());

        JoinBtn.gameObject.SetActive(false);
    }

    void ClearScreen()
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
    }

    IEnumerator ScanJoinArea()
    {
        xOff = currentPlayers * screenWidth + xSpacing; // Adds offset fo 2nd or 3rd... players

        int padding = 50;         // Expand bounds by 50 pixels
        int currentFrameMinX = Mathf.Max(0, xOff - padding);
        int currentFrameMaxX = Mathf.Min(width - 1, xOff + screenWidth + padding);
        int currentFrameMinY = Mathf.Max(0, yOff - padding);
        int currentFrameMaxY = Mathf.Min(height - 1, yOff + screenHeight + padding);

        int playerMinX = currentFrameMaxX;
        int playerMaxX = currentFrameMinX;
        int playerMinY = currentFrameMaxY;
        int playerMaxY = currentFrameMinY;

        int PlayerScreenWith;
        int PlayerScreenHeight;
        float PlayerScreenRatioCurrent;
        float PlayerScreenRatioMax = 0;

        int scan = 0;
        int scanGood = 0;
        int screenScanJoinBuffer = Mathf.FloorToInt(screenWidth * screenHeight * neededScanPercentage);

        while (scanCompleteValue < 100)
        {
            scan = 0;
            scanGood = 0;
            playerMinX = currentFrameMaxX;
            playerMaxX = currentFrameMinX;
            playerMinY = currentFrameMaxY;
            playerMaxY = currentFrameMinY;


            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            width = webcamTexture.width;
            height = webcamTexture.height;

            // Second pass: loop over cropped region and set green where red was
            for (int x = currentFrameMinX; x <= currentFrameMaxX; x++)
            {
                for (int y = currentFrameMinY; y <= currentFrameMaxY; y++)
                {
                    int index = y * width + x;
                    Color pixel = webcamPixels[index];

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        scanGood++;
                        resultPixels[index] = colorList[currentPlayers-1];

                        // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                        // Update bounds
                        if (x < playerMinX) playerMinX = x;
                        if (x > playerMaxX) playerMaxX = x;
                        if (y < playerMinY) playerMinY = y;
                        if (y > playerMaxY) playerMaxY = y;
                    }
                    else
                    {
                        resultPixels[index] = Color.clear; // Default transparent
                    }

                    PlayerScreenWith = playerMaxX - playerMinX;
                    PlayerScreenHeight = playerMaxY - playerMinY;

                    PlayerScreenRatioCurrent = (float)PlayerScreenWith / (float)PlayerScreenHeight;
                    if (PlayerScreenRatioCurrent > PlayerScreenRatioMax)
                    {
                        PlayerScreenRatioMax = PlayerScreenRatioCurrent;
                    }

                    scan++;
                }
            }

            Debug.Log("Scan entire Arean|good pixels: " + scan + "|" + scanGood);


            if (scanGood > screenWidth * screenHeight - screenScanJoinBuffer)
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


        PlayerScreen newPlayer = new PlayerScreen
        {
            topL = new Vector2(playerMinX, playerMaxY), // playerMinX
            topR = new Vector2(playerMaxX, playerMaxY), // playerMinX, // playerMaxX
            botL = new Vector2(playerMinX, playerMinY), // playerMinX, // playerMinY
            botR = new Vector2(playerMaxX, playerMinY), // playerMinX// playerMaxY
            ratio = PlayerScreenRatioMax
        };

        playerScreens.Add(newPlayer);


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