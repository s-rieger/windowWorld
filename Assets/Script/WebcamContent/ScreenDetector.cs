using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using System;
using static ScreenDetector;
using TMPro;

public class ScreenDetector : MonoBehaviour
{
    Color[] webcamPixels;
    Color[] resultPixels;

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
    public float redThreshold = 0.8f;   // How red a pixel needs to be
    public float greenMax = 0.3f;       // Max green value to still be considered "red"
    public float blueMax = 0.3f;        // Max blue value to still be considered "red"
    public float grayscaleThreshold = 0.8f;        // Max blue value to still be considered "red"

    [Header("UI Interactions")]
    [SerializeField] private Button JoinBtn;
    int uiWidth;
    int uiHeight;
    public GameObject ScanProgressUIElement;
    public List<PieChartHandler> PieChartHandlers = new List<PieChartHandler>(6);
    public Transform ScanProgressContainer;
    
    public struct PlayerScreen
    {
        public int scanFrameMinX, scanFrameMaxX, scanFrameMinY, scanFrameMaxY;
        public Vector2 topL, topR, botL, botR, minMin, maxMax;
        public float height;
        public float width;
        public float ratio;
        public bool isCurrentlyActive;
    }
    List<PlayerScreen> playerScreens = new List<PlayerScreen>(6);

    public struct PlayerInput
    {
        //public Vector2 rotInput;
        public float rotInput;
        public float tiltInput;
        public float yawInput;
    }
    List<PlayerInput> playerInputs = new List<PlayerInput>(6);

    [Header("Colors")]
    public List<Color> colorList = new List<Color>(6);
    [SerializeField] private Color p1;
    [SerializeField] private Color p2;
    [SerializeField] private Color p3;
    [SerializeField] private Color p4;    
    [SerializeField] private Color p5;
    [SerializeField] private Color p6;

    [Header("Screen Scan Box")]
    [SerializeField] private bool traceActive;
    [SerializeField] private bool scanActive;
    List<int> playerScreenScanProcess = new List<int>(6);
    [SerializeField] private int scanCompleteMaxValue = 100;
    [SerializeField] float  neededScanPercentage =.2f;
    [SerializeField] int screenWidth;
    [SerializeField] int screenHeight;
    [SerializeField] public int xOff;
    [SerializeField] public int yOff;
    [SerializeField] int newPlayerOffset = 0;
    [SerializeField] int xSpacing = 50;
    Coroutine stopScanningCoroutine;
    Coroutine scanCoroutine;
    Coroutine traceCoroutine;
    [SerializeField] private float stopScanningTimeThreshold = 10;

    [Header("References")]
    public RectTransform rt;
    public static ScreenDetector Instance;

    public RectTransform targetPixelAnalysis;
    public TextMeshProUGUI targetPixelAnalysisText;

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

        StartCoroutine(PixelValueDebug());
    }

    IEnumerator PixelValueDebug()
    {
        while (true)
        {
            yield return new WaitForSeconds(.2f);
            // DEBUG STUFF
            webcamPixels = webcamTexture.GetPixels();
            uiWidth = webcamTexture.width;
            uiHeight = webcamTexture.height;

            int targetHeight = uiHeight / 2 + 300;
            int targetWidth = uiWidth / 2 - 200;
            int targetWidthAnchor = uiWidth / 2 + 200;

            int index = (targetHeight * uiWidth) + targetWidth;

            //targetPixelAnalysis.localScale = new Vector3(-1, 1, 1);
            targetPixelAnalysis.anchoredPosition = new Vector2(targetWidthAnchor, targetHeight); // x,y flipped?!

            targetPixelAnalysisText.text = $"R: {Math.Round(webcamPixels[index].r, 3)}\nG: {Math.Round(webcamPixels[index].g,3)}\nB: {Math.Round(webcamPixels[index].b,3)}";
        }

    }

    IEnumerator TracePlayers()
    { 
        while (traceActive == true)
        {
            yield return new WaitForSeconds(.2f);

            Debug.Log("Tracing Players");
            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            uiWidth = webcamTexture.width;
            uiHeight = webcamTexture.height;

            for (int i = 0; i < playerScreens.Count; i++)
            {
                if (playerScreens[i].isCurrentlyActive == false) { continue; }

                int scan = 0;
                int scanGood = 0;
                //int screenScanJoinBuffer = Mathf.FloorToInt(screenWidth * screenHeight * neededScanPercentage);


                xOff = i * screenWidth + xSpacing; // Adds offset fo 2nd or 3rd player

                //int padding = 50;         // Expand bounds by 50 pixels
                //int currentFrameMinX = Mathf.Max(0, xOff - padding);
                //int currentFrameMaxX = Mathf.Min(uiWidth - 1, xOff + screenWidth + padding);
                //int currentFrameMinY = Mathf.Max(0, yOff - padding);
                //int currentFrameMaxY = Mathf.Min(uiHeight - 1, yOff + screenHeight + padding);

                int playerMinX = playerScreens[i].scanFrameMaxX;
                int playerMaxX = playerScreens[i].scanFrameMinX;
                int playerMinY = playerScreens[i].scanFrameMaxY;
                int playerMaxY = playerScreens[i].scanFrameMinY;

                Vector2 playerMinXVec = new Vector2();
                Vector2 playerMaxXVec = new Vector2();
                Vector2 playerMinYVec = new Vector2();
                Vector2 playerMaxYVec = new Vector2();

                //int PlayerScreenWidthCurrent;
                //int PlayerScreenHeightCurrent;
                //float PlayerScreenRatioCurrent;
                //float PlayerScreenRatioMax = 0;
                //float PlayerScreenWidthMax = 0;
                //float PlayerScreenHeightMax = 0;


                bool isOnEdge = false;
                // Second pass: loop over cropped region and set green where red was
                for (int x = playerScreens[i].scanFrameMinX; x <= playerScreens[i].scanFrameMaxX; x++)
                {
                    for (int y = playerScreens[i].scanFrameMinY; y <= playerScreens[i].scanFrameMaxY; y++)
                    {
                        int index = y * uiWidth + x;
                        Color pixel = webcamPixels[index];

                        if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax) // && pixel.grayscale > grayscaleThreshold
                        {
                            if(x == playerScreens[i].scanFrameMinX || x == playerScreens[i].scanFrameMaxX || y == playerScreens[i].scanFrameMinY || y == playerScreens[i].scanFrameMaxY)
                            {
                                isOnEdge = true;
                            }
                            else
                            {
                                scanGood++;

                                // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                                // Update bounds
                                if (x < playerMinX) {playerMinX = x; playerMinXVec = new Vector2(x, y);}
                                if (x > playerMaxX) {playerMaxX = x; playerMaxXVec = new Vector2(x, y);}
                                if (y < playerMinY) {playerMinY = y; playerMinYVec = new Vector2(x, y);}
                                if (y > playerMaxY) {playerMaxY = y; playerMaxYVec = new Vector2(x, y);}
                            }

                            resultPixels[index] = colorList[i];
                        }
                        else
                        {
                            resultPixels[index] = Color.clear; // Default transparent
                        }

                        scan++;
                    }
                }

                #region Check Scan 
                if (scanGood >= screenWidth * screenHeight * .9f) // Maybe do upperlimit instead of all pixels
                {
                    PieChartHandlers[i].infoText.text = "Too close \nto Camera";
                    PieChartHandlers[i].CornerTrackers.SetActive(false);
                    continue;
                }
                else if (isOnEdge == true)
                {
                    PieChartHandlers[i].infoText.text = "Too close \nto Edge";
                    PieChartHandlers[i].CornerTrackers.SetActive(false);
                    continue;
                }

                // Check if enough Pixels have been detected
                if (scanGood > (screenWidth * screenHeight * neededScanPercentage)) // Good
                {
                    // TODO: FILL PICHART
                    scanCompleteMaxValue++;
                    PieChartHandlers[i].infoText.text = "";
                    PieChartHandlers[i].CornerTrackers.SetActive(true);
                }
                else // TODO: CHECK PERFORMANCE, MIGHT BE UNNECCESARRAADASDASD // Bad Scan
                {
                    scanCompleteMaxValue = 0;
                    PieChartHandlers[i].infoText.text = "Unable to \ndetect screen";

                    for (int x = playerScreens[i].scanFrameMinX; x <= playerScreens[i].scanFrameMaxX; x++)
                    {
                        for (int y = playerScreens[i].scanFrameMinY; y <= playerScreens[i].scanFrameMaxY; y++)
                        {
                            int index = y * uiWidth + x;
                            resultPixels[index] = colorList[i];
                            resultPixels[index].a = .2f;
                        }
                    }
                    continue;
                }
                #endregion


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
                    tiltInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].topR),
                    yawInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].botL),
                };


                //for (int k = 0; k < 4; k++) 
                //{
                //    Vector2 targetPoint;
                //    if (k == 0) targetPoint = playerScreens[i].topL;
                //    else if (k == 1) targetPoint = playerScreens[i].topR;
                //    else if (k == 2) targetPoint = playerScreens[i].botL;
                //    else targetPoint = playerScreens[i].botR;


                //    Vector2 localPoint;
                //    bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                //        PieChartHandlers[i].CornerRect,
                //        targetPoint,
                //        Camera.main,
                //        out localPoint
                //    );

                //    if (success)
                //    {
                //        if (k == 0) PieChartHandlers[i].TopLeftTracker.anchoredPosition = localPoint;
                //        else if (k == 1) PieChartHandlers[i].TopRightTracker.anchoredPosition = localPoint;
                //        else if (k == 2) PieChartHandlers[i].BottomLeftTracker.anchoredPosition = localPoint;
                //        else PieChartHandlers[i].BottomRightTracker.anchoredPosition = localPoint;
                //    }
                //}
                PieChartHandlers[i].TopLeftTracker.anchoredPosition = (playerScreens[i].topL - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));
                PieChartHandlers[i].TopRightTracker.anchoredPosition = (playerScreens[i].topR - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));
                PieChartHandlers[i].BottomLeftTracker.anchoredPosition = (playerScreens[i].botL - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY)); // * new Vector2(-1, 1) in case of flipping
                PieChartHandlers[i].BottomRightTracker.anchoredPosition = (playerScreens[i].botR - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));
                //PieChartHandlers[i].BottomLeftTracker.anchoredPosition = (new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY) - playerScreens[i].botL); // * new Vector2(-1, 1) in case of flipping
                //PieChartHandlers[i].BottomRightTracker.anchoredPosition = (new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY) - playerScreens[i].botR);

                PieChartHandlers[i].rotInputValue.text = playerInputs[i].rotInput.ToString();
                PieChartHandlers[i].tiltInputValue.text = playerInputs[i].tiltInput.ToString();
                PieChartHandlers[i].yawInputValue.text = playerInputs[i].yawInput.ToString();

                i++;
            }

            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();
        }
    }

    float GetDirectionalValue(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return 0f;

        dir.Normalize();

        float angle = Vector2.SignedAngle(Vector2.right, dir);

        Debug.Log("Angle: " + angle);
        // angle is 0 at right, positive going counter-clockwise, negative clockwise

        if (angle >= 15f && angle <= 60f)
            return 1f; // From right to up (up-right sector)
        else if (angle < -15f && angle >= -60f)
            return -1f; // From right to down (down-right sector)
        else
            return 0f; // All other directions
    }

    void JoinPlayer()
    {
        traceActive = false;
        scanActive = true;

        ClearScreen();
        scanCoroutine = StartCoroutine(ScanJoinArea());

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
        int maxPlayerPossible = 6;

        for (int i = 0; i < maxPlayerPossible; i++)
        {
            xOff = (i * screenWidth) + (i * xSpacing) + xSpacing;
            
            // TODO: MAKE THIS DYNAMIC!!!
            // Area of scanning for i Player (padding?!)
            int currentFrameMinX = Mathf.Max(0, xOff);
            int currentFrameMaxX = Mathf.Min(uiWidth - 1, xOff + screenWidth);
            int currentFrameMinY = Mathf.Max(0, yOff);
            int currentFrameMaxY = Mathf.Min(uiHeight - 1, yOff + screenHeight);

            if (playerScreens.Count < i+1)
            {
                PlayerScreen newPlayerScreen = new PlayerScreen
                {
                    scanFrameMaxX = currentFrameMaxX,
                    scanFrameMinX = currentFrameMinX,
                    scanFrameMaxY = currentFrameMaxY,
                    scanFrameMinY = currentFrameMinY,
                    isCurrentlyActive = false
                };
                playerScreens.Add(newPlayerScreen);

                playerScreenScanProcess.Add(0);
                playerInputs.Add(new PlayerInput());

            }

            // Instantiate new Scan Progress UI Element and position
            GameObject newScanProgressUIElement = Instantiate(ScanProgressUIElement, ScanProgressContainer);
            PieChartHandler pieChartHandler = newScanProgressUIElement.GetComponent<PieChartHandler>();
            pieChartHandler.pieChartColor = colorList[i];
            PieChartHandlers.Add(pieChartHandler);
            RectTransform newScanProgressRectTransform = newScanProgressUIElement.GetComponent<RectTransform>();
            newScanProgressRectTransform.anchoredPosition = new Vector2(uiWidth - screenWidth - xOff, yOff); // x,y flipped?!
        }


        while (scanActive == true)
        {
            yield return new WaitForSeconds(.2f);

            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            uiWidth = webcamTexture.width;
            uiHeight = webcamTexture.height;

            for (int i = 0; i < playerScreens.Count; i++)
            {
                if(playerScreens[i].isCurrentlyActive == true) { continue; }

                int scanGood = 0;
                int playerMinX = playerScreens[i].scanFrameMaxX;
                int playerMaxX = playerScreens[i].scanFrameMinX;
                int playerMinY = playerScreens[i].scanFrameMaxY;
                int playerMaxY = playerScreens[i].scanFrameMinY;

                int PlayerScreenWidthCurrent;
                int PlayerScreenHeightCurrent;
                float PlayerScreenWidthMax = 0;
                float PlayerScreenHeightMax = 0;
                bool isOnEdge = false;

                int scan = 0; 

                // Second pass: loop over cropped region and set green where red was
                for (int x = playerScreens[i].scanFrameMinX; x <= playerScreens[i].scanFrameMaxX; x++)
                {
                    for (int y = playerScreens[i].scanFrameMinY; y <= playerScreens[i].scanFrameMaxY; y++)
                    {
                        int index = y * uiWidth + x;
                        Color pixel = webcamPixels[index];
                        if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                        {
                            if (x == playerScreens[i].scanFrameMinX || x == playerScreens[i].scanFrameMaxX || y == playerScreens[i].scanFrameMinY || y == playerScreens[i].scanFrameMaxY)
                            {
                                isOnEdge = true;
                            }
                            else
                            {
                                //Debug.Log("Scan Good: " + pixel);
                                //yield return new WaitForSeconds(.5f);
                                scanGood++;

                                // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
                                if (x < playerMinX) playerMinX = x;
                                if (x > playerMaxX) playerMaxX = x;
                                if (y < playerMinY) playerMinY = y;
                                if (y > playerMaxY) playerMaxY = y;
                            }

                            resultPixels[index] = colorList[i];
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

                outputTexture.SetPixels(resultPixels);
                outputTexture.Apply();

                //Debug.Log("Good Scan for Player: " + i + " - " + scanGood);
                //Debug.Log("Scan entire Arean|good pixels: " + scan + "|" + scanGood);

                if (scanGood == screenWidth * screenHeight)
                {
                    PieChartHandlers[i].infoText.text = "Too close \nto Camera";
                    continue;
                }
                else if(isOnEdge == true)
                {
                    PieChartHandlers[i].infoText.text = "Too close \nto Edge";
                    continue;
                }

                if (scanGood > (screenWidth * screenHeight * neededScanPercentage)) // Good Scan
                {
                    // TODO: FILL PICHART
                    playerScreenScanProcess[i]++;
                    PieChartHandlers[i].infoText.text = "";
                }
                else // TODO: CHECK PERFORMANCE, MIGHT BE UNNECCESARRAADASDASD // Bad Scan
                {
                    playerScreenScanProcess[i] = 0;
                    PieChartHandlers[i].infoText.text = "Unable to \ndetect screen";

                    for (int x = playerScreens[i].scanFrameMinX; x <= playerScreens[i].scanFrameMaxX; x++)
                    {
                        for (int y = playerScreens[i].scanFrameMinY; y <= playerScreens[i].scanFrameMaxY; y++)
                        {
                            int index = y * uiWidth + x;
                            resultPixels[index] = colorList[i];
                            resultPixels[index].a = .2f;
                        }
                    }
                }

                if(playerScreenScanProcess[i] >= scanCompleteMaxValue)
                {
                    PlayerScreen tmp = playerScreens[i];
                    tmp.isCurrentlyActive = true;
                    tmp.topL = new Vector2(playerMaxX, playerMaxY); // playerMinX, // playerMaxX
                    tmp.topR = new Vector2(playerMinX, playerMinY); // playerMinX, // playerMinY
                    tmp.botL = new Vector2(playerMinX, playerMaxY); // playerMinX
                    tmp.botR = new Vector2(playerMaxX, playerMinY); // playerMinX// playerMaxY
                    tmp.minMin = Vector2.zero;
                    tmp.maxMax = Vector2.zero;
                    tmp.height = PlayerScreenHeightMax;
                    tmp.width = PlayerScreenWidthMax;
                    tmp.ratio = PlayerScreenWidthMax / PlayerScreenHeightMax;
                    playerScreens[i] = tmp;

                    PieChartHandlers[i].infoText.text = "ready";

                    PlayerInput tmpInput = playerInputs[i];
                    tmpInput.rotInput = 0;
                    tmpInput.tiltInput = 0;
                    tmpInput.yawInput = 0;
                    playerInputs[i] = tmpInput;

                    GameObject newPlayer = Instantiate(PlayerGO, PlayerContainer);
                    newPlayer.transform.localPosition = new Vector3(i*-180,0,0);
                    PlayerHandler newPlayerHandler = newPlayer.GetComponent<PlayerHandler>();
                    newPlayerHandler.PlayerColor = colorList[i];
                    PlayerHandlers.Add(newPlayerHandler);


                    if (stopScanningCoroutine != null) {StopCoroutine(stopScanningCoroutine); }

                    stopScanningCoroutine = StartCoroutine(StopScanningCoroutine());
                }

                float fillPercentage = (float)playerScreenScanProcess[i] / (float)scanCompleteMaxValue;
                PieChartHandlers[i].FillScanProgress(fillPercentage);
            }


            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();
        }

        //xOff = currentPlayers * screenWidth + xSpacing; // Adds offset fo 2nd or 3rd... players

        //// Padding around Player Area
        //// TODO: MAKE THIS DYNAMIC!!!
        //int padding = 0;         // Expand bounds by 50 pixels
        //int currentFrameMinX = Mathf.Max(0, xOff - padding);
        //int currentFrameMaxX = Mathf.Min(uiWidth - 1, xOff + screenWidth + padding);
        //int currentFrameMinY = Mathf.Max(0, yOff - padding);
        //int currentFrameMaxY = Mathf.Min(uiHeight - 1, yOff + screenHeight + padding);

        //// SCan Progress UI Element
        //GameObject newScanProgressUIElement = Instantiate(ScanProgressUIElement, ScanProgressContainer);
        //PieChartHandler pieChartHandler = newScanProgressUIElement.GetComponent<PieChartHandler>();
        //pieChartHandler.pieChartColor = colorList[currentPlayers];
        //PieChartHandlers.Add(pieChartHandler);

        //RectTransform newScanProgressRectTransform = newScanProgressUIElement.GetComponent<RectTransform>();
        //newScanProgressRectTransform.anchoredPosition = new Vector2(uiWidth - screenWidth - xOff, yOff); // x,y flipped?!
        ////newScanProgressRectTransform.anchoredPosition = new Vector2(uiWidth - screenWidth - padding, uiHeight - screenHeight - padding); // x,y flipped?!
        ////newScanProgressRectTransform.anchoredPosition = new Vector2(screenWidth - xOff, screenHeight - yOff); // x,y flipped?!

        //int playerMinX = currentFrameMaxX;
        //int playerMaxX = currentFrameMinX;
        //int playerMinY = currentFrameMaxY;
        //int playerMaxY = currentFrameMinY;

        //int PlayerScreenWidthCurrent;
        //int PlayerScreenHeightCurrent;
        //float PlayerScreenWidthMax = 0;
        //float PlayerScreenHeightMax = 0;

        //int scan = 0;
        //int scanGood = 0;
        //int screenScanJoinBuffer = Mathf.FloorToInt(screenWidth * screenHeight * neededScanPercentage);

        //while (scanCompleteMaxValue < 100)
        //{
        //    yield return new WaitForSeconds(.1f); 

        //    scan = 0;
        //    scanGood = 0;
        //    playerMinX = currentFrameMaxX;
        //    playerMaxX = currentFrameMinX;
        //    playerMinY = currentFrameMaxY;
        //    playerMaxY = currentFrameMinY;


        //    webcamPixels = webcamTexture.GetPixels();
        //    resultPixels = new Color[webcamPixels.Length];

        //    uiWidth = webcamTexture.width;
        //    uiHeight = webcamTexture.height;

        //    // Second pass: loop over cropped region and set green where red was
        //    for (int x = currentFrameMinX; x <= currentFrameMaxX; x++)
        //    {
        //        for (int y = currentFrameMinY; y <= currentFrameMaxY; y++)
        //        {
        //            int index = y * uiWidth + x;
        //            //Debug.Log("Index: " +  index);
        //            Color pixel = webcamPixels[index];

        //            if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
        //            {
        //                scanGood++;
        //                resultPixels[index] = colorList[currentPlayers];

        //                // TODO: ADD MINX&Y & MAXX&Y TO PLAYER ARRAY | SET SCREEN SIZE & RATIO!
        //                if (x < playerMinX) playerMinX = x;
        //                if (x > playerMaxX) playerMaxX = x;
        //                if (y < playerMinY) playerMinY = y;
        //                if (y > playerMaxY) playerMaxY = y;
        //            }
        //            else
        //            {
        //                resultPixels[index] = Color.clear; // Default transparent
        //            }

        //            // TODO: CALIBRATING SHOULD NOT OVERWRITE BECAUSE THE CLOSER TO CAMERA, THE MORE PIXELS
        //            PlayerScreenWidthCurrent = playerMaxX - playerMinX;
        //            if (PlayerScreenWidthCurrent > PlayerScreenWidthMax){
        //                PlayerScreenWidthMax = PlayerScreenWidthCurrent;
        //            }
        //            PlayerScreenHeightCurrent = playerMaxY - playerMinY;
        //            if (PlayerScreenHeightCurrent > PlayerScreenHeightMax){
        //                PlayerScreenHeightMax = PlayerScreenHeightCurrent;
        //            }
        //            scan++;
        //        }
        //    }

        //    //Debug.Log("Scan entire Arean|good pixels: " + scan + "|" + scanGood);
        //    if (scanGood > screenWidth * screenHeight - screenScanJoinBuffer) // Good Scan
        //    {
        //        // TODO: FILL PICHART
        //        scanCompleteMaxValue++;
        //        PieChartHandlers[currentPlayers].infoText.text = "";
        //    }
        //    else if(scanGood == screenWidth * screenHeight)
        //    {
        //        PieChartHandlers[currentPlayers].infoText.text = "Too close \nto Camera";
        //    }
        //    else // TODO: CHECK PERFORMANCE, MIGHT BE UNNECCESARRAADASDASD // Bad Scan
        //    {
        //        scanCompleteMaxValue = 0;
        //        PieChartHandlers[currentPlayers].infoText.text = "Unable to \ndetect screen";

        //        for (int x = currentFrameMinX; x <= currentFrameMaxX; x++)
        //        {
        //            for (int y = currentFrameMinY; y <= currentFrameMaxY; y++)
        //            {
        //                int index = y * uiWidth + x;
        //                resultPixels[index] = colorList[currentPlayers];
        //                resultPixels[index].a = .2f;
        //            }
        //        }
        //    }

        //    PieChartHandlers[currentPlayers].FillScanProgress(scanCompleteMaxValue);

        //    outputTexture.SetPixels(resultPixels);
        //    outputTexture.Apply();

        //}

        //Debug.Log($"Scan Player {currentPlayers} complete | width: {PlayerScreenWidthMax} | height: {PlayerScreenHeightMax} | ratio: {PlayerScreenWidthMax/ PlayerScreenHeightMax}");

        //scan = 0;
        //scanGood = 0;
        //scanCompleteMaxValue = 0;
        //currentPlayers++;

        //// TODO: HEIGHT, WIDTH & RATIO ARE ALL WRONG!!

        //PlayerScreen newPlayerScreen = new PlayerScreen
        //{
        //    topL = new Vector2(playerMaxX, playerMaxY), // playerMinX, // playerMaxX
        //    topR = new Vector2(playerMinX, playerMinY), // playerMinX, // playerMinY
        //    botL = new Vector2(playerMinX, playerMaxY), // playerMinX
        //    botR = new Vector2(playerMaxX, playerMinY), // playerMinX// playerMaxY
        //    minMin = Vector2.zero,
        //    maxMax = Vector2.zero,
        //    height = PlayerScreenHeightMax,
        //    width = PlayerScreenWidthMax,
        //    ratio = PlayerScreenWidthMax / PlayerScreenHeightMax
        //};
        //playerScreens.Add(newPlayerScreen);

        //PlayerInput newPlayerInput = new PlayerInput
        //{
        //    //rotInput = Vector2.zero,
        //    rotInput = 0,
        //    yInput = 0,
        //    zInput = 0
        //};
        //playerInputs.Add(newPlayerInput);


        //GameObject newPlayer = Instantiate(PlayerGO, PlayerContainer);
        //PlayerHandler newPlayerHandler = newPlayer.GetComponent<PlayerHandler>();
        //newPlayerHandler.PlayerColor = colorList[currentPlayers];
        //PlayerHandlers.Add(newPlayerHandler);

        //JoinBtn.gameObject.SetActive(true);

        //PieChartHandlers[currentPlayers].infoText.text = "Ready";
        //yield return new WaitForSeconds(2);
        //PieChartHandlers[currentPlayers].infoText.text = "";
    }

    IEnumerator StopScanningCoroutine()
    {
        float timer = 0;
        while(timer < stopScanningTimeThreshold)
        {
            timer += .1f;
            Debug.Log("Stop Scan in: " + (stopScanningTimeThreshold - timer));
            yield return new WaitForSeconds(.1f);
        }

        for (int i = 0; i < playerScreens.Count; i++) 
        {
            if (playerScreens[i].isCurrentlyActive == false)
            {
                PieChartHandlers[i].gameObject.SetActive(false);
            }
            else
            {
                PieChartHandlers[i].InputDebug.SetActive(true);
                PieChartHandlers[i].CornerTrackers.SetActive(true);
            }
        }


        StopCoroutine(scanCoroutine);
        yield return new WaitForSeconds(.1f);

        scanActive = false;
        if (traceActive == false)
        {
            traceActive = true;
            traceCoroutine = StartCoroutine(TracePlayers());
        }
        ClearScreen();
        JoinBtn.gameObject.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        // Draw spheres at each point
        if (currentPlayers == 0) { return; }


        List<Vector2> cornerList = new List<Vector2>(4);
        cornerList.Add(playerScreens[0].topL);
        cornerList.Add(playerScreens[0].topR);
        cornerList.Add(playerScreens[0].botL);
        cornerList.Add(playerScreens[0].botR);

        Vector3 worldPos;
        for (int i = 0; i < cornerList.Count; i++) 
        { 
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, cornerList[i], Camera.main, out worldPos))
            {
                worldPos = new Vector3(-1 * worldPos.x, 1 * worldPos.y, 1 * worldPos.z); // FLippingx-axis
                Gizmos.DrawSphere(worldPos, 10);

//#if UNITY_EDITOR
                // Draw labels using Handles
                if(i == 0) Handles.Label(worldPos, "Top Left");
                else if(i == 1) Handles.Label(worldPos, "Top Right");
                else if (i == 2) Handles.Label(worldPos, "Bottom Left");
                else if (i == 3) Handles.Label(worldPos, "Bottom Right");
//#endif
            }


        }
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