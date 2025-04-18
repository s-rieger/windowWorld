using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using static ScreenDetector;
using UnityEngine.Windows;
using UnityEditor;

public class ScreenDetector : MonoBehaviour
{
    [Header("Player Stuff")]
    [SerializeField] public int currentPlayers = 0;
    public GameObject PlayerGO;
    public List<PlayerHandler> PlayerHandlers = new List<PlayerHandler>();
    public Transform PlayerContainer;

    [Header("UI Screens")]
    public RectTransform UIScreen;
    public RawImage webcamDisplay;      // To display the webcam feed
    public RawImage resultDisplay;      // To display the green-pixel result

    private WebCamTexture webcamTexture;
    private Texture2D outputTexture;

    public RectTransform webcamRect;
    public RectTransform playRect;

    [Header("Target Color Values")]
    public float redThreshold = 0.8f;   // How red a pixel needs to be
    public float greenMax = 0.3f;       // Max green value to still be considered "red"
    public float blueMax = 0.3f;        // Max blue value to still be considered "red"

    [Header("UI Interactions")]
    [SerializeField] private Button JoinBtn;
    int uiWidth;
    int uiHeight;
    public GameObject ScanProgressUIElement;
    public Transform ScanProgressContainer;

    Color[] webcamPixels;
    Color[] resultPixels;

    [System.Serializable]
    public struct PlayerScreen
    {
        public Vector2 topL, topR, botL, botR, minMin, maxMax;
        public float height;
        public float width;
        public float ratio;
    }
    List<PlayerScreen> playerScreens = new List<PlayerScreen>();

    [System.Serializable]
    public struct PlayerInput
    {
        //public Vector2 rotInput;
        public float rotInput;
        public float yInput;
        public float zInput;
    }
    List<PlayerInput> playerInputs = new List<PlayerInput>();

    [Header("Colors")]
    [SerializeField] private Color p1;
    [SerializeField] private Color p2;
    [SerializeField] private Color p3;
    [SerializeField] private Color p4;    
    [SerializeField] private Color p5;
    [SerializeField] private Color p6;
    [SerializeField] private Color p7;
    [SerializeField] private Color p8;

    public List<Color> colorList = new List<Color>(4);

    [Header("Screen Scan Box")]
    [SerializeField] int screenWidth;
    [SerializeField] int screenHeight;
    [SerializeField] public int xOff;
    [SerializeField] public int yOff;
    [SerializeField] int newPlayerOffset = 0;
    [SerializeField] int xSpacing = 50;

    [SerializeField] float  neededScanPercentage =.2f;



    [Header("Checks")]
    public int scanCompleteValue = 0;


    [Header("References")]
    public RectTransform rt;
    public static ScreenDetector Instance;

    private void Awake()
    {
        if(Instance != null && Instance != this) {Destroy(Instance);}
        else {Instance = this;}
    }

    void Start()
    {
        webcamRect.localScale = new Vector3(-1, 1, 1);
        playRect.localScale = new Vector3(-1, 1, 1);


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
        //InvokeRepeating(nameof(ProcessFrame), 1f, 0.1f); // Process 10 times a second
        ClearScreen();
    }

    private void Update()
    {
        //ProcessFrame();
        TracePlayers();
    }
    void TracePlayers()
    {
        if (playerScreens.Count == 0 || PlayerHandlers.Count == 0 || currentPlayers == 0 ) { return; }

        for (int i = 0; i < playerScreens.Count; i++)
        {
            int scan = 0;
            int scanGood = 0;
            int screenScanJoinBuffer = Mathf.FloorToInt(screenWidth * screenHeight * neededScanPercentage);

            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            uiWidth = webcamTexture.width;
            uiHeight = webcamTexture.height;


            xOff = i * screenWidth + xSpacing; // Adds offset fo 2nd or 3rd player

            int padding = 50;         // Expand bounds by 50 pixels
            int currentFrameMinX = Mathf.Max(0, xOff - padding);
            int currentFrameMaxX = Mathf.Min(uiWidth - 1, xOff + screenWidth + padding);
            int currentFrameMinY = Mathf.Max(0, yOff - padding);
            int currentFrameMaxY = Mathf.Min(uiHeight - 1, yOff + screenHeight + padding);

            int playerMinX = currentFrameMaxX;
            int playerMaxX = currentFrameMinX;
            int playerMinY = currentFrameMaxY;
            int playerMaxY = currentFrameMinY;

            Vector2 playerMinXVec = new Vector2();
            Vector2 playerMaxXVec = new Vector2();
            Vector2 playerMinYVec = new Vector2();
            Vector2 playerMaxYVec = new Vector2();

            int PlayerScreenWidthCurrent;
            int PlayerScreenHeightCurrent;
            float PlayerScreenRatioCurrent;
            float PlayerScreenRatioMax = 0;
            float PlayerScreenWidthMax = 0;
            float PlayerScreenHeightMax = 0;



            // Second pass: loop over cropped region and set green where red was
            for (int x = currentFrameMinX; x <= currentFrameMaxX; x++)
            {
                for (int y = currentFrameMinY; y <= currentFrameMaxY; y++)
                {
                    int index = y * uiWidth + x;
                    Color pixel = webcamPixels[index];

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        scanGood++;
                        resultPixels[index] = colorList[i];

                        // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                        // Update bounds
                        if (x < playerMinX) {playerMinX = x; playerMinXVec = new Vector2(x, y);}
                        if (x > playerMaxX) {playerMaxX = x; playerMaxXVec = new Vector2(x, y);}
                        if (y < playerMinY) {playerMinY = y; playerMinYVec = new Vector2(x, y);}
                        if (y > playerMaxY) {playerMaxY = y; playerMaxYVec = new Vector2(x, y);}
                    }
                    else
                    {
                        resultPixels[index] = Color.clear; // Default transparent
                    }

                    scan++;
                }
            }



            PlayerScreen ps = playerScreens[i];
            if (playerMinYVec.x < playerMaxYVec.x) // turned Right
            {
                ps.botL = playerMinYVec;
                ps.topR = playerMaxYVec;
                ps.topL = playerMinXVec;
                ps.botR = playerMaxXVec;
            } 
            else // turned left
            {
                ps.botR = playerMinYVec;
                ps.topL = playerMaxYVec;
                ps.botL = playerMinXVec;
                ps.topR = playerMaxXVec;
            }
            playerScreens[i] = ps; // Assign the modified copy back

            playerInputs[i] = new PlayerInput
            {
                //rotInput = (playerScreens[i].topL - playerScreens[i].topR).normalized,
                rotInput = GetDirectionalValue(playerScreens[i].topR - playerScreens[i].topL),
                yInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].topR),
                zInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].botL),
            };

            PlayerHandlers[i].rotInput = playerInputs[i].rotInput;

            Debug.Log(
                $"+++++ Player_{i} stats +++++ |" +
                $" height: {playerScreens[i].height}|" +
                $" width: {playerScreens[i].height}|" +
                $" ratio: {playerScreens[i].ratio}");
            Debug.Log(
                $"+++++ minMin & maxMax ++++" +
                $"minMin: {new Vector2(playerMinX, playerMinY)} | " +
                $"maxMax {new Vector2(playerMaxX, playerMaxY)} | " +
                $"normalized {(playerScreens[i].maxMax - playerScreens[i].minMin).normalized}");
            Debug.Log(
                $"+++++ Input ++++" +
                $"rotInput: {playerInputs[i].rotInput}| " +
                $"yinput: {playerInputs[i].yInput}| " +
                $"zinput: {playerInputs[i].zInput}" );

            i++;
        }

        outputTexture.SetPixels(resultPixels);
        outputTexture.Apply();
    }

    float GetDirectionalValue(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return 0f;

        dir.Normalize();

        float angle = Vector2.SignedAngle(Vector2.right, dir);

        Debug.Log("GetDirectionalValue: " + angle);
        // angle is 0 at right, positive going counter-clockwise, negative clockwise

        if (angle >= 0f && angle <= 90f)
            return 1f; // From right to up (up-right sector)
        else if (angle < 0f && angle >= -45f)
            return -1f; // From right to down (down-right sector)
        else
            return 0f; // All other directions
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

        uiWidth = webcamTexture.width;
        uiHeight = webcamTexture.height;

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

        // Padding around Player Area
        // TODO: MAKE THIS DYNAMIC!!!
        int padding = 50;         // Expand bounds by 50 pixels
        int currentFrameMinX = Mathf.Max(0, xOff - padding);
        int currentFrameMaxX = Mathf.Min(uiWidth - 1, xOff + screenWidth + padding);
        int currentFrameMinY = Mathf.Max(0, yOff - padding);
        int currentFrameMaxY = Mathf.Min(uiHeight - 1, yOff + screenHeight + padding);

        // SCan Progress UI Element
        GameObject newScanProgressUIElement = Instantiate(ScanProgressUIElement, ScanProgressContainer);
        RectTransform newScanProgressRectTransform = newScanProgressUIElement.GetComponent<RectTransform>();
        newScanProgressRectTransform.anchoredPosition = new Vector2(uiWidth - screenWidth - padding, uiHeight - screenHeight - padding); // x,y flipped?!
        //newScanProgressRectTransform.anchoredPosition = new Vector2(screenWidth - xOff, screenHeight - yOff); // x,y flipped?!

        int playerMinX = currentFrameMaxX;
        int playerMaxX = currentFrameMinX;
        int playerMinY = currentFrameMaxY;
        int playerMaxY = currentFrameMinY;

        int PlayerScreenWidthCurrent;
        int PlayerScreenHeightCurrent;
        float PlayerScreenWidthMax = 0;
        float PlayerScreenHeightMax = 0;

        int scan = 0;
        int scanGood = 0;
        int screenScanJoinBuffer = Mathf.FloorToInt(screenWidth * screenHeight * neededScanPercentage);

        while (scanCompleteValue < 100)
        {
            yield return null;

            scan = 0;
            scanGood = 0;
            playerMinX = currentFrameMaxX;
            playerMaxX = currentFrameMinX;
            playerMinY = currentFrameMaxY;
            playerMaxY = currentFrameMinY;


            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            uiWidth = webcamTexture.width;
            uiHeight = webcamTexture.height;

            // Second pass: loop over cropped region and set green where red was
            for (int x = currentFrameMinX; x <= currentFrameMaxX; x++)
            {
                for (int y = currentFrameMinY; y <= currentFrameMaxY; y++)
                {
                    int index = y * uiWidth + x;
                    //Debug.Log("Index: " +  index);
                    Color pixel = webcamPixels[index];

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        scanGood++;
                        resultPixels[index] = colorList[currentPlayers];

                        // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                        if (x < playerMinX) playerMinX = x;
                        if (x > playerMaxX) playerMaxX = x;
                        if (y < playerMinY) playerMinY = y;
                        if (y > playerMaxY) playerMaxY = y;
                    }
                    else
                    {
                        resultPixels[index] = Color.clear; // Default transparent
                    }

                    // TODO: CALIBRATING SHOULD NOT OVERWRITE BECAUSE THE CLOSER TO CAMERA, THE MORE PIXELS
                    PlayerScreenWidthCurrent = playerMaxX - playerMinX;
                    if (PlayerScreenWidthCurrent > PlayerScreenWidthMax){
                        PlayerScreenWidthMax = PlayerScreenWidthCurrent;
                    }
                    PlayerScreenHeightCurrent = playerMaxY - playerMinY;
                    if (PlayerScreenHeightCurrent > PlayerScreenHeightMax){
                        PlayerScreenHeightMax = PlayerScreenHeightCurrent;
                    }
                    scan++;
                }
            }

            //Debug.Log("Scan entire Arean|good pixels: " + scan + "|" + scanGood);
            if (scanGood > screenWidth * screenHeight - screenScanJoinBuffer)
            {
                // TODO: FILL PICHART
                scanCompleteValue++;
            }
            else // TODO: CHECK PERFORMANCE, MIGHT BE UNNECCESARRAADASDASD
            {
                // TODO: DRAIN PI CHART
                scanCompleteValue = 0;
                for (int x = currentFrameMinX; x <= currentFrameMaxX; x++)
                {
                    for (int y = currentFrameMinY; y <= currentFrameMaxY; y++)
                    {
                        int index = y * uiWidth + x;
                        resultPixels[index] = colorList[currentPlayers];
                    }
                }
            }

            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();

        }

        Debug.Log($"Scan Player {currentPlayers} complete | width: {PlayerScreenWidthMax} | height: {PlayerScreenHeightMax} | ratio: {PlayerScreenWidthMax/ PlayerScreenHeightMax}");

        scan = 0;
        scanGood = 0;
        scanCompleteValue = 0;
        currentPlayers++;

        // TODO: HEIGHT, WIDTH & RATIO ARE ALL WRONG!!

        PlayerScreen newPlayerScreen = new PlayerScreen
        {
            topL = new Vector2(playerMaxX, playerMaxY), // playerMinX, // playerMaxX
            topR = new Vector2(playerMinX, playerMinY), // playerMinX, // playerMinY
            botL = new Vector2(playerMinX, playerMaxY), // playerMinX
            botR = new Vector2(playerMaxX, playerMinY), // playerMinX// playerMaxY
            minMin = Vector2.zero,
            maxMax = Vector2.zero,
            height = PlayerScreenHeightMax,
            width = PlayerScreenWidthMax,
            ratio = PlayerScreenWidthMax / PlayerScreenHeightMax
        };
        playerScreens.Add(newPlayerScreen);

        PlayerInput newPlayerInput = new PlayerInput
        {
            //rotInput = Vector2.zero,
            rotInput = 0,
            yInput = 0,
            zInput = 0
        };
        playerInputs.Add(newPlayerInput);


        GameObject newPlayer = Instantiate(PlayerGO, PlayerContainer);
        PlayerHandler newPlayerHandler = newPlayer.GetComponent<PlayerHandler>();
        newPlayerHandler.PlayerColor = colorList[currentPlayers];
        PlayerHandlers.Add(newPlayerHandler);

        JoinBtn.gameObject.SetActive(true);
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        // Draw spheres at each point
        if (currentPlayers == 0) { return; }

        Vector3 worldTopLeft = rt.TransformPoint(playerScreens[0].topL);
        Vector3 worldTopRight = rt.TransformPoint(playerScreens[0].topR);
        Vector3 worldBottomLeft = rt.TransformPoint(playerScreens[0].botL);
        Vector3 worldBottomRight = rt.TransformPoint(playerScreens[0].botR);

        Gizmos.DrawSphere(playerScreens[0].topL, 1);
        Gizmos.DrawSphere(playerScreens[0].topR, 1);
        Gizmos.DrawSphere(playerScreens[0].botL, 1);
        Gizmos.DrawSphere(playerScreens[0].botR, 1);

#if UNITY_EDITOR
        // Draw labels using Handles
        Handles.Label(playerScreens[0].topL, "Top Left");
        Handles.Label(playerScreens[0].topR, "Top Right");
        Handles.Label(playerScreens[0].botL, "Bottom Left");
        Handles.Label(playerScreens[0].botR, "Bottom Right");
#endif
    }




    void ProcessFrame()
    {
        if (!webcamTexture.isPlaying || webcamTexture.width < 10) return;

        webcamPixels = webcamTexture.GetPixels();
        resultPixels = new Color[webcamPixels.Length];

        uiWidth = webcamTexture.width;
        uiHeight = webcamTexture.height;

        // Track red pixel bounds
        int minX = 0, minY = 0, maxX = uiWidth, maxY = uiHeight;

        // First pass: detect red pixels and calculate bounds
        for (int y = 0; y < uiHeight; y++)
        {
            for (int x = 0; x < uiWidth; x++)
            {
                int index = y * uiWidth + x;
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
        maxX = Mathf.Min(uiWidth - 1, maxX + padding);
        minY = Mathf.Max(0, minY - padding);
        maxY = Mathf.Min(uiHeight - 1, maxY + padding);

        // Second pass: loop over cropped region and set green where red was
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int index = y * uiWidth + x;
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