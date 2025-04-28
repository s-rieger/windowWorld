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
    List<PlayerScreen> playerScreens = new(6);

    public struct PlayerInput
    {
        //public Vector2 rotInput;
        public float rotInput;
        public float tiltUpDownInput;
        public float tiltLeftRightInput;
    }
    List<PlayerInput> playerInputs = new(6);

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
    [SerializeField] private float pixelBufferCentered = 10;
    [SerializeField] private float pixelBufferTiltUpDown = 20;
    [SerializeField] private float pixelBufferTiltLeftRight = 20;

    [Header("Window Covers")]
    [SerializeField] private List<WindowCoverHandler> windowCoverHandlers = new List<WindowCoverHandler>(6);

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
        for (int i = 0; i < 6; i++) {
            playerScreens.Add(default(PlayerScreen));
            playerScreenScanProcess.Add(default(int));
            playerInputs.Add(default(PlayerInput));
            PieChartHandlers.Add(default(PieChartHandler));
            PlayerHandlers.Add(default(PlayerHandler));
        }

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
                                PieChartHandlers[i].infoText.text = "Too close \nto Edge";
                                PieChartHandlers[i].CornerTrackers.SetActive(false);
                                continue;
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

                Debug.Log("playerMinXVec: " + playerMinXVec);
                Debug.Log("playerMaxXVec: " + playerMaxXVec);
                Debug.Log("playerMinYVec: " + playerMinYVec);
                Debug.Log("playerMaxYVec: " + playerMaxYVec);

                float rotInputTest = 0;

                if((playerMinXVec.y <= playerMaxXVec.y + pixelBufferCentered && playerMinXVec.y >= playerMaxXVec.y - pixelBufferCentered) ||
                    Vector2.Distance(playerMaxXVec, playerMinYVec) < 30 ||
                    Vector2.Distance(playerMaxXVec, playerMaxYVec) < 30 ||
                    Vector2.Distance(playerMinXVec, playerMinYVec) < 30 ||
                    Vector2.Distance(playerMinXVec, playerMaxYVec) < 30)
                    // More or less centered
                {
                    ps.botL = new Vector2(playerMinXVec.x, playerMinYVec.y);
                    ps.botR = new Vector2(playerMaxXVec.x, playerMinYVec.y);
                    ps.topR = new Vector2(playerMaxXVec.x, playerMaxYVec.y);
                    ps.topL = new Vector2(playerMinXVec.x, playerMaxYVec.y);
                    Debug.Log("Is Centered");
                    rotInputTest = 0;
                }
                else if (playerMinYVec.x < playerMaxYVec.x) // turned Right
                {
                    ps.botL = playerMinYVec;
                    ps.topR = playerMaxYVec;
                    ps.topL = playerMinXVec;
                    ps.botR = playerMaxXVec;
                    Debug.Log("Is Truend Right");
                    rotInputTest = 1;

                }
                else // turned left
                {
                    ps.botR = playerMinYVec;
                    ps.topL = playerMaxYVec;
                    ps.botL = playerMinXVec;
                    ps.topR = playerMaxXVec;
                    Debug.Log("Is Truend Left");
                    rotInputTest = -1;
                }
                playerScreens[i] = ps; // Assign the modified copy back

                float tiltLeftRightTmp = 0;
                if(Vector2.Distance(playerScreens[i].topR, playerScreens[i].topL) < Vector2.Distance(playerScreens[i].botR, playerScreens[i].botL))
                {
                    tiltLeftRightTmp = 1;

                }
                else if (Vector2.Distance(playerScreens[i].topR, playerScreens[i].topL) > Vector2.Distance(playerScreens[i].botR, playerScreens[i].botL))
                {
                    tiltLeftRightTmp = -1;
                }
                else
                {
                    tiltLeftRightTmp = 0;
                }

                float tiltUpDownTmp = 0;
                if (Vector2.Distance(playerScreens[i].topR, playerScreens[i].botR) < Vector2.Distance(playerScreens[i].topL, playerScreens[i].botL))
                {
                    tiltUpDownTmp = 1;

                }
                else if (Vector2.Distance(playerScreens[i].topR, playerScreens[i].botR) > Vector2.Distance(playerScreens[i].topL, playerScreens[i].botL))
                {
                    tiltUpDownTmp = -1;
                }
                else
                {
                    tiltUpDownTmp = 0;
                }


                PlayerInput tmpInput = new PlayerInput
                {
                    //rotInput = (playerScreens[i].topL - playerScreens[i].topR).normalized,
                    //rotInput = GetDirectionalValue(playerScreens[i].topR - playerScreens[i].topL),
                    rotInput = rotInputTest,
                    //tiltInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].topR),
                    //tiltLeftRightInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].topR) < playerScreens[i].width + pixelBufferTiltLeftRight ? 1 : 0,
                    //tiltLeftRightInput = Vector2.Distance(playerScreens[i].topR, playerScreens[i].topL) < Vector2.Distance(playerScreens[i].botR, playerScreens[i].botL) ? 1 : 0,
                    tiltLeftRightInput = tiltLeftRightTmp,
                    //yawInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].botL),
                    //tiltUpDownInput = Vector2.Distance(playerScreens[i].topL, playerScreens[i].botL) < playerScreens[i].height + pixelBufferTiltUpDown ? 1 : 0,
                    tiltUpDownInput = tiltUpDownTmp,
                };

                playerInputs[i] = tmpInput;
                PlayerHandlers[i].thisPlayerInput = tmpInput;

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

                PieChartHandlers[i].rotInputValue.text = Math.Round(playerInputs[i].rotInput, 2).ToString();
                PieChartHandlers[i].tiltUDInputValue.text = Math.Round(playerInputs[i].tiltUpDownInput, 2).ToString();
                PieChartHandlers[i].tiltLRInputValue.text = Math.Round(playerInputs[i].tiltLeftRightInput, 2).ToString();

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

        //Debug.Log("Angle: " + angle);
        // angle is 0 at right, positive going counter-clockwise, negative clockwise

        if (angle >= 15f && angle <= 60f)
            return 1f; // From right to up (up-right sector)
        else if (angle < -15f && angle >= -60f)
            return -1f; // From right to down (down-right sector)
        else
            return 0f; // All other directions
    }

    public void JoinPlayer()
    {
        //traceActive = false; // So people can continue to play
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
        for (int i = 0; i < 6; i++)
        {
            if(playerScreens[i].isCurrentlyActive == true) { Debug.Log("Found ready player: " + i); continue; }

            windowCoverHandlers[i].OpenWindows(i);

            xOff = (i * screenWidth) + (i * xSpacing) + xSpacing;
            
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
            playerScreens[i] = newPlayerScreen;
            playerScreenScanProcess[i] = 0;
            playerInputs[i] = new PlayerInput();

            // Instantiate new Scan Progress UI Element and position
            GameObject newScanProgressUIElement = Instantiate(ScanProgressUIElement, ScanProgressContainer);
            PieChartHandler pieChartHandler = newScanProgressUIElement.GetComponent<PieChartHandler>();
            pieChartHandler.pieChartColor = colorList[i];
            PieChartHandlers[i] = pieChartHandler;

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

                int scan = 0; 
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
                // TODO: ADD BOOLEAN TO SKIP FOR LOOP IF ON EDGE 

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
                                PieChartHandlers[i].infoText.text = "Too close \nto Edge";
                                continue;
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

                if (scanGood < (screenWidth * screenHeight * neededScanPercentage)) // Bad Scan
                {
                    playerScreenScanProcess[i] = 0;
                    PieChartHandlers[i].infoText.text = "Unable to \ndetect screen";

                    if (stopScanningCoroutine == null) { stopScanningCoroutine = StartCoroutine(StopScanningCoroutine()); }


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
                else // TODO: CHECK PERFORMANCE, MIGHT BE UNNECCESARRAADASDASD // Good Scan
                {
                    // TODO: FILL PICHART
                    playerScreenScanProcess[i]++;
                    PieChartHandlers[i].infoText.text = "";
                    PlayerScreen tmp = playerScreens[i];
                    tmp.height += playerMaxY - playerMinY;
                    tmp.width += playerMaxX - playerMinX;
                    tmp.ratio += tmp.height / tmp.width;
                    playerScreens[i] = tmp;


                    if (stopScanningCoroutine != null) { StopCoroutine(stopScanningCoroutine); } // Stops stopScanCoro if available  
                }

                float fillPercentage = (float)playerScreenScanProcess[i] / (float)scanCompleteMaxValue;

                if (fillPercentage >= 1)
                {
                    ArduinoSetup.instance.SetLedColor("GREEN");

                    PlayerScreen tmp = playerScreens[i];
                    Debug.Log("++++++++++++++++");
                    Debug.Log("PlayerSCreenHeightMax: " + PlayerScreenHeightMax);
                    Debug.Log("PlayerSCreenWIdthMax: " + PlayerScreenWidthMax);
                    Debug.Log("PlayerSCreenHeightMax/Width Ratio: " + PlayerScreenHeightMax / PlayerScreenWidthMax);
                    Debug.Log("tmp.height.norm " + tmp.height/ scanCompleteMaxValue);
                    Debug.Log("tmp.width.norm " + tmp.width/ scanCompleteMaxValue);
                    Debug.Log("tmp.ratio.norm " + tmp.ratio/ scanCompleteMaxValue);
                    Debug.Log("++++++++++++++++");



                    tmp.isCurrentlyActive = true;
                    tmp.topL = new Vector2(playerMaxX, playerMaxY); // playerMinX, // playerMaxX
                    tmp.topR = new Vector2(playerMinX, playerMinY); // playerMinX, // playerMinY
                    tmp.botL = new Vector2(playerMinX, playerMaxY); // playerMinX
                    tmp.botR = new Vector2(playerMaxX, playerMinY); // playerMinX// playerMaxY
                    tmp.minMin = Vector2.zero;
                    tmp.maxMax = Vector2.zero;
                    //tmp.height = PlayerScreenHeightMax;
                    //tmp.width = PlayerScreenWidthMax;
                    //tmp.ratio = PlayerScreenWidthMax / PlayerScreenHeightMax;
                    tmp.height = tmp.height / scanCompleteMaxValue;
                    tmp.width = tmp.width/ scanCompleteMaxValue;
                    tmp.ratio = tmp.ratio/ scanCompleteMaxValue;
                    playerScreens[i] = tmp;

                    PieChartHandlers[i].infoText.text = "ready";

                    PlayerInput tmpInput = playerInputs[i];
                    tmpInput.rotInput = 0;
                    tmpInput.tiltUpDownInput = 0;
                    tmpInput.tiltLeftRightInput = 0;
                    playerInputs[i] = tmpInput;

                    GameObject newPlayer = Instantiate(PlayerGO, PlayerContainer);
                    newPlayer.transform.localPosition = new Vector3(i*-180,0,0);

                    PlayerHandler newPlayerHandler = newPlayer.GetComponent<PlayerHandler>();
                    newPlayerHandler.PlayerColor = colorList[i];
                    newPlayerHandler.playerIndex = i;
                    newPlayerHandler.thisPlayerInput = tmpInput;
                    PlayerHandlers[i] = newPlayerHandler;

                    PieChartHandlers[i].FillScanProgress(1);

                    if (stopScanningCoroutine != null) {StopCoroutine(stopScanningCoroutine); }
                    stopScanningCoroutine = StartCoroutine(StopScanningCoroutine());
                }
                else
                {
                    PieChartHandlers[i].FillScanProgress(fillPercentage);
                }


            }


            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();
        }
    }

    IEnumerator StopScanningCoroutine()
    {
        float timer = 0;
        while(timer < stopScanningTimeThreshold)
        {
            timer += .1f;
            //Debug.Log("Stop Scan in: " + (stopScanningTimeThreshold - timer));
            yield return new WaitForSeconds(.1f);
        }

        for (int i = 0; i < playerScreens.Count; i++) 
        {
            if (playerScreens[i].isCurrentlyActive == false)
            {
                PieChartHandlers[i].gameObject.SetActive(false);
                windowCoverHandlers[i].CloseWindows(i);
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

}