using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using System;
using static ScreenDetector;
using TMPro;
using NUnit.Framework;
using Unity.VisualScripting;
using System.Linq;
using static Unity.Burst.Intrinsics.X86.Avx;

public class ScreenDetector : MonoBehaviour
{
    Color[] webcamPixels;
    Color[] resultPixels;
    public bool isDebug = false;

    [Header("Player Stuff")]
    [SerializeField] public int currentPlayers = 0;
    public GameObject PlayerGO;
    public List<PlayerHandler> PlayerHandlers = new List<PlayerHandler>(6);
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
    public float colorPuffer = 0.1f;
    //public float redThreshold = 0.8f;   // How red a pixel needs to be
    //public float greenMax = 0.3f;       // Max green value to still be considered "red"
    //public float blueMax = 0.3f;        // Max blue value to still be considered "red"
    public float whiteThreshold = 0.3f;        // Max blue value to still be considered "red"
    public float grayscaleThreshold = 0.8f;        // Max blue value to still be considered "red"

    [Header("UI Interactions")]
    [SerializeField] private Button JoinBtn;
    int uiWidth;
    int uiHeight;
    int totalPixelAmount = 0;
    public GameObject ScanProgressUIElement;
    public List<PieChartHandler> PieChartHandlers = new List<PieChartHandler>(6);
    public Transform ScanProgressContainer;
    
    public struct PlayerScreen
    {
        public int scanFrameMinX, scanFrameMaxX, scanFrameMinY, scanFrameMaxY;
        public Vector2Int topL, topR, botL, botR;
        public Vector2Int initTopL, initTopR, initBotL, initBotR;
        public float redPixelValue;
        public float greenPixelValue;
        public float bluePixelValue;
        public float height;
        public float width;
        public float ratio;
        public bool isCurrentlyActive;
    }
    PlayerScreen[] playerScreens = new PlayerScreen[6];

    public struct PlayerInput
    {
        //public Vector2 rotInput;
        public float rotInput;
        public float tiltUpDownInput;
        public float tiltLeftRightInput;
    }
   PlayerInput[] playerInputs = new PlayerInput[6];

    [Header("Colors")]
    Color[] colorList = new Color[6];
    [SerializeField] private Color p1;
    [SerializeField] private Color p2;
    [SerializeField] private Color p3;
    [SerializeField] private Color p4;    
    [SerializeField] private Color p5;
    [SerializeField] private Color p6;

    [Header("Screen Scan Box")]
    bool[] traceActive = new bool[6];
    bool[] scanActive = new bool[6];
    int[] playerScreenScanProcess = new int[6];
    [SerializeField] private int scanCompleteMaxValue = 100;
    [SerializeField] float  neededScanPercentage =.2f;
    [SerializeField] int screenWidth;
    [SerializeField] int screenHeight;
    [SerializeField] public int xOff;
    [SerializeField] public int yOff;
    [SerializeField] int newPlayerOffset = 0;
    [SerializeField] int xSpacing = 50;

    [Header("Coroutines")]
    [SerializeField] private float stopScanningTimeThreshold = 10;
    [SerializeField] private float angleLeftRight = 10;
    [SerializeField] private float pixelBufferTiltUpDownPrecentage = .1f;
    [SerializeField] private float pixelBufferTiltLeftRightPercentage = .1f;
    Coroutine[] stopCallibratePlayerCoroutines = new Coroutine[6];
    Coroutine[] callibratePlayerCoroutines = new Coroutine[6];
    Coroutine[] tracePlayerCoroutines = new Coroutine[6];
    Coroutine[] outOfBoundsPlayerCoroutines = new Coroutine[6];
    Coroutine[] positionCorners = new Coroutine[6];

    [Header("Window Covers")]
    [SerializeField] private List<WindowCoverHandler> windowCoverHandlers = new List<WindowCoverHandler>(6);

    [Header("References")]
    public RectTransform rt;
    public static ScreenDetector Instance;

    float timer = 0;
    [SerializeField] float webcamRefreshRate = 0.1f;

    [Header("Debug")]
    public RectTransform targetPixelAnalysis;
    public TextMeshProUGUI targetPixelAnalysisText;


    private void Awake()
    {
        if(Instance != null && Instance != this) {Destroy(Instance);}
        else {Instance = this;}
    }

    void Start()
    {
        // Set Up Variables
        colorList = new Color[] { p1, p2, p3, p4, p5, p6 };

        // Set Up Webcam Variables
        webcamTexture = new WebCamTexture();
        webcamDisplay.texture = webcamTexture;
        webcamTexture.Play();
        webcamRect.localScale = new Vector3(-1, 1, 1); // Flip left right
        webcamPixels = webcamTexture.GetPixels();
        resultPixels = new Color[webcamPixels.Length];
        uiWidth = webcamTexture.width;
        uiHeight = webcamTexture.height;
        totalPixelAmount = uiWidth * uiHeight;


        // Initializing Output texture
        outputTexture = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.RGBA32, // <-- must be this or similar (not RGB24!)
            false
        );
        resultDisplay.texture = outputTexture;

        // Flip Rect of
        playRect.localScale = new Vector3(-1, 1, 1); // Flip left right

        // Initialize Lists
        for (int i = 0; i < 6; i++) { 
            PieChartHandlers.Add(default(PieChartHandler));
            PlayerHandlers.Add(default(PlayerHandler));


            InitializePlayerData(i);
        }

        //
        JoinBtn.onClick.AddListener(() => { JoinPlayer(); });

        //Debug.Log("UiWidth, uiHeight, totalPixels: " + uiWidth + ", " + uiHeight + ", " + totalPixelAmount);
        //StartCoroutine(PixelValueDebug());
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if(timer > webcamRefreshRate)
        {
            webcamPixels = webcamTexture.GetPixels();
            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();
            timer = 0;
        }
    }

    //IEnumerator PixelValueDebug()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(.2f);
    //        // DEBUG STUFF
    //        webcamPixels = webcamTexture.GetPixels();
    //        uiWidth = webcamTexture.width;
    //        uiHeight = webcamTexture.height;

    //        int targetHeight = uiHeight / 2 + 300;
    //        int targetWidth = uiWidth / 2 - 200;
    //        int targetWidthAnchor = uiWidth / 2 + 200;

    //        int index = (targetHeight * uiWidth) + targetWidth;

    //        //targetPixelAnalysis.localScale = new Vector3(-1, 1, 1);
    //        targetPixelAnalysis.anchoredPosition = new Vector2(targetWidthAnchor, targetHeight); // x,y flipped?!

    //        targetPixelAnalysisText.text = $"R: {Math.Round(webcamPixels[index].r, 3)}\nG: {Math.Round(webcamPixels[index].g,3)}\nB: {Math.Round(webcamPixels[index].b,3)}";
    //    }

    //}

    IEnumerator TracePlayers(int playerIndex)
    {
        int pixelColorScanIndex = ((playerScreens[playerIndex].scanFrameMinY + (screenHeight / 2)) * uiWidth) + playerScreens[playerIndex].scanFrameMinX + (screenWidth / 2);

        while (traceActive[playerIndex] == true)
        {
            // DEBUG
            PieChartHandlers[playerIndex].PixelScannerValueText.text = $"R: {Math.Round(webcamPixels[pixelColorScanIndex].r, 3)}\nG: {Math.Round(webcamPixels[pixelColorScanIndex].g, 3)}\nB: {Math.Round(webcamPixels[pixelColorScanIndex].b, 3)}";
            //playerScreens[playerIndex].redPixelValue = webcamPixels[pixelColorScanIndex].r;
            //playerScreens[playerIndex].bluePixelValue = webcamPixels[pixelColorScanIndex].b;
            //playerScreens[playerIndex].greenPixelValue = webcamPixels[pixelColorScanIndex].g;
            // DEBUG

            playerScreens[playerIndex] = TraceCorners(playerIndex);
            PositionCornerTrackers(playerIndex, true);

            // Left Right Flipping/Tilting
            float tiltLeftRightTmp = 0;
            if (Vector2.Distance(playerScreens[playerIndex].topR, playerScreens[playerIndex].topL) >= playerScreens[playerIndex].width - (playerScreens[playerIndex].width * pixelBufferTiltLeftRightPercentage))
            {
                tiltLeftRightTmp = 0;
            }
            else
            {
                tiltLeftRightTmp = 1;
            }

            // Up Down Flipping/Tilting
            float tiltUpDownTmp = 0;
            if (Vector2.Distance(playerScreens[playerIndex].topR, playerScreens[playerIndex].botR) >= playerScreens[playerIndex].height - (playerScreens[playerIndex].height * pixelBufferTiltUpDownPrecentage))
            {
                tiltUpDownTmp = 0;
            }
            else
            {
                tiltUpDownTmp = 1;
            }
            //Debug.Log("Up|Down: " + Vector2.Distance(playerScreens[playerIndex].topR, playerScreens[playerIndex].botR) + " | " + (playerScreens[playerIndex].height - (playerScreens[playerIndex].height * pixelBufferTiltUpDownPrecentage)) +  " | normHeight: " + playerScreens[playerIndex].height);


            float rotInputTest = 0;
            Vector2 tmpRightEdge = playerScreens[playerIndex].topR - playerScreens[playerIndex].botR;
            Vector2 tmpLeftEdge = playerScreens[playerIndex].topR - playerScreens[playerIndex].botR;

            float angleRight = Vector2.SignedAngle(Vector2.up, tmpRightEdge);
            float angleLeft = Vector2.SignedAngle(Vector2.up, tmpLeftEdge);

            float angle = (angleLeft + angleRight) / 2;
            if (angle > angleLeftRight)
            {
                rotInputTest = 1;
            }
            else if (angle < -angleLeftRight)
            {
                rotInputTest = -1;
            }
            else
            {
                rotInputTest = 0;
            }
            //Debug.Log("Angle: " + angle);



            PlayerInput tmpInput = new PlayerInput
            {
                rotInput = rotInputTest,
                tiltLeftRightInput = tiltLeftRightTmp,
                tiltUpDownInput = tiltUpDownTmp,
            };

            playerInputs[playerIndex] = tmpInput;
            PlayerHandlers[playerIndex].thisPlayerInput = tmpInput;

            PieChartHandlers[playerIndex].rotInputValue.text = Math.Round(playerInputs[playerIndex].rotInput, 2).ToString();
            PieChartHandlers[playerIndex].tiltUDInputValue.text = Math.Round(playerInputs[playerIndex].tiltUpDownInput, 2).ToString();
            PieChartHandlers[playerIndex].tiltLRInputValue.text = Math.Round(playerInputs[playerIndex].tiltLeftRightInput, 2).ToString();

            yield return null;
        }
    }



    public void JoinPlayer()
    {
        outputTexture = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.RGBA32, // <-- must be this or similar (not RGB24!)
            false
        );
        resultDisplay.texture = outputTexture;

        for (int i = 0; i < 6; i++)
        {
            if (playerScreens[i].isCurrentlyActive == true) { continue; }
            else
            {
                PieChartHandlers[i].gameObject.SetActive(true);
                callibratePlayerCoroutines[i] = StartCoroutine(CallibratePlayer(i));
            }
        }

        JoinBtn.gameObject.SetActive(false);
    }

    void ClearScreen()
    {
        uiWidth = webcamTexture.width;
        uiHeight = webcamTexture.height;

        for (int i = 0; i < resultPixels.Length; i++)
        {
            resultPixels[i] = Color.clear; // Default transparent
        }

        outputTexture.SetPixels(resultPixels);
        outputTexture.Apply();
    }

    void ClearFrame(PlayerScreen currentPlayerScreen)
    {
        int index;
        for (int x = currentPlayerScreen.scanFrameMinX; x < currentPlayerScreen.scanFrameMaxX; x++)
        {
            for (int y = currentPlayerScreen.scanFrameMinY; y < currentPlayerScreen.scanFrameMaxY; y++)
            {
                index = y * uiWidth + x;
                resultPixels[index] = Color.clear; // Default transparent
            }
        }

        //outputTexture.SetPixels(resultPixels);
        //outputTexture.Apply();
    }

    void InitializePlayerData(int playerIndex)
    {
        xOff = (playerIndex * screenWidth) + (playerIndex * xSpacing) + xSpacing;

        // TODO: MAKE THIS DYNAMIC!!!
        // Area of scanning for i Player (padding?!)
        int currentFrameMinX = Mathf.Max(0, xOff);
        int currentFrameMaxX = Mathf.Min(uiWidth - 1, xOff + screenWidth);
        int currentFrameMinY = Mathf.Max(0, yOff);
        int currentFrameMaxY = Mathf.Min(uiHeight - 1, yOff + screenHeight);

        PlayerScreen newPlayerScreen = new PlayerScreen
        {
            scanFrameMaxX = currentFrameMaxX,
            scanFrameMinX = currentFrameMinX,
            scanFrameMaxY = currentFrameMaxY,
            scanFrameMinY = currentFrameMinY,
            isCurrentlyActive = false
        };
        playerScreens[playerIndex] = newPlayerScreen;
        playerScreenScanProcess[playerIndex] = 0;
        playerInputs[playerIndex] = new PlayerInput();

        // Instantiate new Scan Progress UI Element and position
        GameObject newScanProgressUIElement = Instantiate(ScanProgressUIElement, ScanProgressContainer);
        PieChartHandler pieChartHandler = newScanProgressUIElement.GetComponent<PieChartHandler>();
        pieChartHandler.pieChartColor = colorList[playerIndex];
        PieChartHandlers[playerIndex] = pieChartHandler;

        RectTransform newScanProgressRectTransform = newScanProgressUIElement.GetComponent<RectTransform>();
        newScanProgressRectTransform.anchoredPosition = new Vector2(uiWidth - screenWidth - xOff, yOff); // x,y flipped?!
    }

    void PositionCornerTrackers(int playerIndex, bool isSmooth)
    {
        if (isSmooth == true)
        {
            positionCorners[playerIndex] = StartCoroutine(MoveCornerSmooth(playerIndex, 0.1f));
        }
        else
        {
            positionCorners[playerIndex] = StartCoroutine(MoveCornerSmooth(playerIndex, 0));
        }
    }

    IEnumerator CallibratePlayer(int playerIndex)
    {
        PieChartHandlers[playerIndex].PixelScannerTracker.anchoredPosition = new Vector2(screenWidth / 2, screenHeight / 2); // x,y flipped?!
        windowCoverHandlers[playerIndex].OpenWindows(playerIndex);

        // Scan Countdown
        for (int i = 3; i > 0; i--) 
        {
            PieChartHandlers[playerIndex].infoText.text = i.ToString();

            yield return new WaitForSeconds(1);
        }
        PieChartHandlers[playerIndex].infoText.text = "";


        scanActive[playerIndex] = true;
        int index;
        int scan;
        int scanGood;
        int playerMinX;
        int playerMaxX;
        int playerMinY;
        int playerMaxY;
        int PlayerScreenWidthCurrent;
        int PlayerScreenHeightCurrent;
        float PlayerScreenWidthMax; // Obsolete?!
        float PlayerScreenHeightMax; // Obsolete?!
        bool isOnEdge;

        int pixelColorScanIndex = ((playerScreens[playerIndex].scanFrameMinY + (screenHeight/2)) * uiWidth) + playerScreens[playerIndex].scanFrameMinX + (screenWidth/2);

        while (scanActive[playerIndex] == true)
        {
            yield return null;

            PieChartHandlers[playerIndex].PixelScannerValueText.text = $"R: {Math.Round(webcamPixels[pixelColorScanIndex].r, 3)}\nG: {Math.Round(webcamPixels[pixelColorScanIndex].g, 3)}\nB: {Math.Round(webcamPixels[pixelColorScanIndex].b, 3)}";
            playerScreens[playerIndex].redPixelValue = webcamPixels[pixelColorScanIndex].r;
            playerScreens[playerIndex].bluePixelValue = webcamPixels[pixelColorScanIndex].b;
            playerScreens[playerIndex].greenPixelValue = webcamPixels[pixelColorScanIndex].g;

            scan = 0; 
            scanGood = 0;

            playerMinX = playerScreens[playerIndex].scanFrameMaxX;
            playerMaxX = playerScreens[playerIndex].scanFrameMinX;
            playerMinY = playerScreens[playerIndex].scanFrameMaxY;
            playerMaxY = playerScreens[playerIndex].scanFrameMinY;


            PlayerScreenWidthMax = 0;
            PlayerScreenHeightMax = 0;

            isOnEdge = false;
            // TODO: ADD BOOLEAN TO SKIP FOR LOOP IF ON EDGE 

            // Second pass: loop over cropped region and set green where red was
            for (int x = playerScreens[playerIndex].scanFrameMinX; x <= playerScreens[playerIndex].scanFrameMaxX; x++)
            {
                for (int y = playerScreens[playerIndex].scanFrameMinY; y <= playerScreens[playerIndex].scanFrameMaxY; y++)
                {
                    index = y * uiWidth + x;
                    Color pixel = webcamPixels[index];

                    if (pixel.r > playerScreens[playerIndex].redPixelValue - colorPuffer && pixel.g < playerScreens[playerIndex].greenPixelValue + colorPuffer && pixel.b < playerScreens[playerIndex].bluePixelValue + colorPuffer)
                    {
                        if (x == playerScreens[playerIndex].scanFrameMinX || x == playerScreens[playerIndex].scanFrameMaxX || 
                            y == playerScreens[playerIndex].scanFrameMinY || y == playerScreens[playerIndex].scanFrameMaxY)
                        {
                            isOnEdge = true;
                            continue;
                        }
                        else
                        {
                            scanGood++;

                            // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                            if (x < playerMinX) playerMinX = x;
                            if (x > playerMaxX) playerMaxX = x;
                            if (y < playerMinY) playerMinY = y;
                            if (y > playerMaxY) playerMaxY = y;
                        }

                        resultPixels[index] = colorList[playerIndex];
                    }
                    else
                    {
                        resultPixels[index] = Color.clear; // Default transparent
                    }

                    // TODO: CALIBRATING SHOULD NOT OVERWRITE BECAUSE THE CLOSER TO CAMERA, THE MORE PIXELS
                    PlayerScreenWidthCurrent = playerMaxX - playerMinX;
                    if (PlayerScreenWidthCurrent > PlayerScreenWidthMax)
                    {
                        PlayerScreenWidthMax = PlayerScreenWidthCurrent;
                    }
                    PlayerScreenHeightCurrent = playerMaxY - playerMinY;
                    if (PlayerScreenHeightCurrent > PlayerScreenHeightMax)
                    {
                        PlayerScreenHeightMax = PlayerScreenHeightCurrent;
                    }
                    scan++;
                }
            }
            //Debug.Log("Scan entire Arean|good pixels: " + scan + "|" + scanGood);

            if (scanGood == screenWidth * screenHeight)
            {
                PieChartHandlers[playerIndex].infoText.text = "Too close \nto Camera";
                continue;
            }
            else if(isOnEdge == true)
            {
                PieChartHandlers[playerIndex].infoText.text = "Too close \nto Edge";
                continue;
            }

            if (scanGood < (screenWidth * screenHeight * neededScanPercentage)) // Bad Scan
            {
                playerScreenScanProcess[playerIndex] = 0;
                PieChartHandlers[playerIndex].infoText.text = "Unable to \ndetect screen";

                if (stopCallibratePlayerCoroutines[playerIndex] == null) 
                {
                    stopCallibratePlayerCoroutines[playerIndex] = StartCoroutine(StopCallibrationCoroutine(playerIndex)); 
                }
            }
            else // TODO: CHECK PERFORMANCE, MIGHT BE UNNECCESARRAADASDASD // Good Scan
            {
                // TODO: FILL PICHART
                playerScreenScanProcess[playerIndex]++;
                PieChartHandlers[playerIndex].infoText.text = "";
                PlayerScreen tmp = playerScreens[playerIndex];
                tmp.height += playerMaxY - playerMinY;
                tmp.width += playerMaxX - playerMinX;
                tmp.ratio += tmp.height / tmp.width;
                playerScreens[playerIndex] = tmp;


                if (stopCallibratePlayerCoroutines[playerIndex] != null) { StopCoroutine(stopCallibratePlayerCoroutines[playerIndex]); }
                stopCallibratePlayerCoroutines[playerIndex] = StartCoroutine(StopCallibrationCoroutine(playerIndex));
            }

            float fillPercentage = (float)playerScreenScanProcess[playerIndex] / (float)scanCompleteMaxValue;

            if (fillPercentage >= 1)
            {
                ClearFrame(playerScreens[playerIndex]);

                PlayerScreen tmp = playerScreens[playerIndex];

                tmp.isCurrentlyActive = true;
                tmp.initTopL = new Vector2Int(playerMinX, playerMaxY); // playerMinX, // playerMaxX // Flipped because camera is inverted as well
                tmp.initTopR = new Vector2Int(playerMaxX, playerMaxY); // playerMinX, // playerMinY
                tmp.initBotL = new Vector2Int(playerMinX, playerMinY); // playerMinX
                tmp.initBotR = new Vector2Int(playerMaxX, playerMinY); // playerMinX// playerMaxY

                tmp.topL = tmp.initTopL; //new Vector2Int(playerMinX, playerMaxY); // playerMinX, // playerMaxX // Flipped because camera is inverted as well
                tmp.topR = tmp.initTopR; //new Vector2Int(playerMaxX, playerMaxY); // playerMinX, // playerMinY
                tmp.botL = tmp.initBotL; //new Vector2Int(playerMinX, playerMinY); // playerMinX
                tmp.botR = tmp.initBotR; //new Vector2Int(playerMaxX, playerMinY); // playerMinX// playerMaxY

                tmp.height = tmp.height / scanCompleteMaxValue;
                tmp.width = tmp.width/ scanCompleteMaxValue;
                tmp.ratio = tmp.ratio/ scanCompleteMaxValue;
                playerScreens[playerIndex] = tmp;

                PieChartHandlers[playerIndex].infoText.text = "ready";
                PieChartHandlers[playerIndex].FillScanProgress(1);
                PieChartHandlers[playerIndex].CornerTrackers.SetActive(true);
                PositionCornerTrackers(playerIndex, true);

                yield return new WaitForSeconds(1);


                GameObject newPlayer = Instantiate(PlayerGO, PlayerContainer);
                //newPlayer.transform.localPosition = new Vector3(playerIndex * -180,200,500);
                //newPlayer.transform.localPosition = new Vector3(0, 250, 400);
                //newPlayer.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                //newPlayer.transform.localEulerAngles = new Vector3(0, newPlayer.transform.localEulerAngles.y, newPlayer.transform.localEulerAngles.z);

                PlayerInput tmpInput = playerInputs[playerIndex];
                tmpInput.rotInput = 0;
                tmpInput.tiltUpDownInput = 0;
                tmpInput.tiltLeftRightInput = 0;
                playerInputs[playerIndex] = tmpInput;

                PlayerHandler newPlayerHandler = newPlayer.GetComponent<PlayerHandler>();
                newPlayerHandler.PlayerColor = colorList[playerIndex];
                newPlayerHandler.playerIndex = playerIndex;
                newPlayerHandler.thisPlayerInput = playerInputs[playerIndex];
                newPlayerHandler.SnakeSpawnLocation = new Vector3(playerIndex * -300, 250, 400);
                newPlayerHandler.StartCoroutine(newPlayerHandler.SpawnSnakeCoro());

                PlayerHandlers[playerIndex] = newPlayerHandler;
                ArduinoSetup.instance.SetLedColorForPlayer(playerIndex + 1, "GREEN");
                if (traceActive[playerIndex] == false)
                {
                    traceActive[playerIndex] = true;
                    tracePlayerCoroutines[playerIndex] = StartCoroutine(TracePlayers(playerIndex));
                }

                PieChartHandlers[playerIndex].infoText.text = "Go";
                yield return new WaitForSeconds(1);
                PieChartHandlers[playerIndex].infoText.text = "";
                PieChartHandlers[playerIndex].InputDebug.SetActive(true);


                StopCoroutine(stopCallibratePlayerCoroutines[playerIndex]);
                stopCallibratePlayerCoroutines[playerIndex] = null;

                StopCoroutine(callibratePlayerCoroutines[playerIndex]); // Stop this coroutine;
                callibratePlayerCoroutines[playerIndex] = null;
                scanActive[playerIndex] = false;
            }
            else
            {
                PieChartHandlers[playerIndex].FillScanProgress(fillPercentage);
            }
        }
    }

    IEnumerator MoveCornerSmooth(int playerIndex, float duration)
    {
        RectTransform trackerTL = PieChartHandlers[playerIndex].TopLeftTracker;
        RectTransform trackerTR = PieChartHandlers[playerIndex].TopRightTracker;
        RectTransform trackerBL = PieChartHandlers[playerIndex].BottomLeftTracker;
        RectTransform trackerBR = PieChartHandlers[playerIndex].BottomRightTracker;

        Vector2 startTL = trackerTL.anchoredPosition;
        Vector2 startTR = trackerTR.anchoredPosition;
        Vector2 startBL = trackerBL.anchoredPosition;
        Vector2 startBR = trackerBR.anchoredPosition;

        Vector2 endTL = playerScreens[playerIndex].topL - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY);
        Vector2 endTR = playerScreens[playerIndex].topR - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY);
        Vector2 endBL = playerScreens[playerIndex].botL - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY);
        Vector2 endBR = playerScreens[playerIndex].botR - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            trackerTL.anchoredPosition = Vector2.Lerp(startTL, endTL, elapsed / duration);
            trackerTR.anchoredPosition = Vector2.Lerp(startTR, endTR, elapsed / duration);
            trackerBL.anchoredPosition = Vector2.Lerp(startBL, endBL, elapsed / duration);
            trackerBR.anchoredPosition = Vector2.Lerp(startBR, endBR, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        trackerTL.anchoredPosition = endTL; // ensure final position
        trackerTR.anchoredPosition = endTR; // ensure final position
        trackerBL.anchoredPosition = endBL; // ensure final position
        trackerBR.anchoredPosition = endBR; // ensure final position
    }

    IEnumerator StopCallibrationCoroutine(int playerIndex)
    {
        float timer = 0;
        while(timer < stopScanningTimeThreshold)
        {
            timer += .1f;
            //Debug.Log("Stop Scan in: " + (stopScanningTimeThreshold - timer));
            yield return new WaitForSeconds(.1f);
        }


        if (playerScreens[playerIndex].isCurrentlyActive == false)
        {
            PieChartHandlers[playerIndex].gameObject.SetActive(false);
            windowCoverHandlers[playerIndex].CloseWindows(playerIndex);
        }
        else
        {
            PieChartHandlers[playerIndex].InputDebug.SetActive(true);
            PieChartHandlers[playerIndex].CornerTrackers.SetActive(true);
        }

        StopCoroutine(callibratePlayerCoroutines[playerIndex]);
        ClearFrame(playerScreens[playerIndex]);

        JoinBtn.gameObject.SetActive(true);
    }

    IEnumerator OutOfBoundsCorrection(int playerIndex)
    {
        if (positionCorners[playerIndex] != null) { StopCoroutine(positionCorners[playerIndex]); }
        if (tracePlayerCoroutines[playerIndex] != null) { StopCoroutine(tracePlayerCoroutines[playerIndex]); }
        
        tracePlayerCoroutines[playerIndex] = null;
        traceActive[playerIndex] = false;
        PlayerHandlers[playerIndex].canMove = false;
        PieChartHandlers[playerIndex].infoText.text = "Out of \nBounds";
        yield return new WaitForSeconds(.5f);



        PlayerScreen ps = playerScreens[playerIndex];
        ps.isCurrentlyActive = false;
        ps.topL = new Vector2Int( playerScreens[playerIndex].scanFrameMinX + (screenWidth / 2), playerScreens[playerIndex].scanFrameMinY + screenHeight / 2);
        ps.topR = new Vector2Int( playerScreens[playerIndex].scanFrameMinX + (screenWidth / 2), playerScreens[playerIndex].scanFrameMinY + screenHeight / 2);
        ps.botL = new Vector2Int( playerScreens[playerIndex].scanFrameMinX + (screenWidth / 2), playerScreens[playerIndex].scanFrameMinY + screenHeight / 2);
        ps.botR = new Vector2Int( playerScreens[playerIndex].scanFrameMinX + (screenWidth / 2), playerScreens[playerIndex].scanFrameMinY + screenHeight / 2);
        playerScreens[playerIndex] = ps;

        playerInputs[playerIndex] = new PlayerInput
        {
            rotInput = 0,
            tiltLeftRightInput = 0,
            tiltUpDownInput = 0,
        }; 
        PlayerHandlers[playerIndex].thisPlayerInput = playerInputs[playerIndex];


        PositionCornerTrackers(playerIndex, false);

        yield return new WaitForSeconds(1f);
        PieChartHandlers[playerIndex].infoText.text = "Keep \nCentered";

        int pixelColorScanIndex = ((playerScreens[playerIndex].scanFrameMinY + (screenHeight / 2)) * uiWidth) + playerScreens[playerIndex].scanFrameMinX + (screenWidth / 2);
        PieChartHandlers[playerIndex].PixelScannerValueText.text = $"R: {Math.Round(webcamPixels[pixelColorScanIndex].r, 3)}\nG: {Math.Round(webcamPixels[pixelColorScanIndex].g, 3)}\nB: {Math.Round(webcamPixels[pixelColorScanIndex].b, 3)}";
        playerScreens[playerIndex].redPixelValue = webcamPixels[pixelColorScanIndex].r;
        playerScreens[playerIndex].bluePixelValue = webcamPixels[pixelColorScanIndex].b;
        playerScreens[playerIndex].greenPixelValue = webcamPixels[pixelColorScanIndex].g;


        yield return new WaitForSeconds(1f);
        PieChartHandlers[playerIndex].infoText.text = "Ready";


        yield return new WaitForSeconds(1f);
        PieChartHandlers[playerIndex].infoText.text = "";

        traceActive[playerIndex] = true;
        PlayerHandlers[playerIndex].canMove = true;
        tracePlayerCoroutines[playerIndex] = StartCoroutine(TracePlayers(playerIndex));
    }

    PlayerScreen TraceCorners(int playerIndex)
    {
        PlayerScreen currentPS = playerScreens[playerIndex];
        Color pixel;

        // TOP LEFT TRACKER
        int pixelRange = 20;
        int yHit = currentPS.topL.y - pixelRange;
        int xHit = currentPS.topL.x + pixelRange;
        bool isTopLSearching = true;

        while (isTopLSearching)
        {
            for (int y = currentPS.topL.y - pixelRange; y < currentPS.topL.y + pixelRange; y++)
            {
                for (int x = currentPS.topL.x - pixelRange; x < currentPS.topL.x + pixelRange; x++)
                {
                    int index = y * uiWidth + x;
                    if (index >= totalPixelAmount || index < 0) { continue; }

                    pixel = webcamPixels[index];

                    if (pixel.r > playerScreens[playerIndex].redPixelValue - colorPuffer && pixel.g < playerScreens[playerIndex].greenPixelValue + colorPuffer && pixel.b < playerScreens[playerIndex].bluePixelValue + colorPuffer)
                    {
                        if (y > yHit) yHit = y;
                        if (x < xHit) xHit = x;
                    }
                }
            }

            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                outOfBoundsPlayerCoroutines[playerIndex] = StartCoroutine(OutOfBoundsCorrection(playerIndex)); 
                break;
            }
            else
            {
                currentPS.topL = new Vector2Int(xHit, yHit);
                isTopLSearching = false;
            }
        }


        // TOP RIGHT TRACKER
        pixelRange = 20;
        yHit = currentPS.topR.y - pixelRange;
        xHit = currentPS.topR.x - pixelRange;
        bool isTopRSearching = true;
        while (isTopRSearching)
        {
            for (int y = currentPS.topR.y - pixelRange; y < currentPS.topR.y + pixelRange; y++)
            {
                for (int x = currentPS.topR.x - pixelRange; x < currentPS.topR.x + pixelRange; x++)
                {
                    int index = y * uiWidth + x;
                    if (index > totalPixelAmount) { continue; }

                    pixel = webcamPixels[index];

                    if (pixel.r > playerScreens[playerIndex].redPixelValue - colorPuffer && pixel.g < playerScreens[playerIndex].greenPixelValue + colorPuffer && pixel.b < playerScreens[playerIndex].bluePixelValue + colorPuffer)
                    {
                        if (y > yHit) yHit = y;
                        if (x > xHit) xHit = x;
                    }
                }
            }

            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                outOfBoundsPlayerCoroutines[playerIndex] = StartCoroutine(OutOfBoundsCorrection(playerIndex));
                break;
            }
            else
            {
                currentPS.topR = new Vector2Int(xHit, yHit);
                isTopRSearching = false;
            }
        }


        // BOTTOM RIGHT TRACKER
        pixelRange = 10;
        yHit = currentPS.botR.y + pixelRange;
        xHit = currentPS.botR.x - pixelRange;
        bool isBotRSearching = true;
        while (isBotRSearching)
        {
            for (int y = currentPS.botR.y - pixelRange; y < currentPS.botR.y + pixelRange; y++)
            {
                for (int x = currentPS.botR.x - pixelRange; x < currentPS.botR.x + pixelRange; x++)
                {
                    int index = y * uiWidth + x;
                    if (index > totalPixelAmount) { continue; }

                    pixel = webcamPixels[index];

                    if (pixel.r > playerScreens[playerIndex].redPixelValue - colorPuffer && pixel.g < playerScreens[playerIndex].greenPixelValue + colorPuffer && pixel.b < playerScreens[playerIndex].bluePixelValue + colorPuffer)
                    {
                        if (y < yHit) yHit = y;
                        if (x > xHit) xHit = x;
                    }
                }
            }


            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                outOfBoundsPlayerCoroutines[playerIndex] = StartCoroutine(OutOfBoundsCorrection(playerIndex));
                break;
            }
            else
            {
                currentPS.botR = new Vector2Int(xHit, yHit);
                isBotRSearching = false;
            }
        }


        // BOTTOM Left TRACKER
        pixelRange = 10;
        yHit = currentPS.botL.y + pixelRange;
        xHit = currentPS.botL.x + pixelRange;
        bool isBotLSearching = true;
        while (isBotLSearching)
        {
            for (int y = currentPS.botL.y - pixelRange; y < currentPS.botL.y + pixelRange; y++)
            {
                for (int x = currentPS.botL.x - pixelRange; x < currentPS.botL.x + pixelRange; x++)
                {
                    int index = y * uiWidth + x;
                    if (index > totalPixelAmount) { continue; }

                    pixel = webcamPixels[index];

                    if (pixel.r > playerScreens[playerIndex].redPixelValue - colorPuffer && pixel.g < playerScreens[playerIndex].greenPixelValue + colorPuffer && pixel.b < playerScreens[playerIndex].bluePixelValue + colorPuffer)
                    {
                        if (y < yHit) yHit = y;
                        if (x < xHit) xHit = x;
                    }
                }
            }

            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                outOfBoundsPlayerCoroutines[playerIndex] = StartCoroutine(OutOfBoundsCorrection(playerIndex));
                break;
            }
            else
            {
                currentPS.botL = new Vector2Int(xHit, yHit);
                isBotLSearching = false;
            }
        }

        currentPS.isCurrentlyActive = true;
        return currentPS;
    }

}