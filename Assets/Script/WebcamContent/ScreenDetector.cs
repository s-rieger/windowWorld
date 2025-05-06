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
        public Vector2Int topL, topR, botL, botR, minMin, maxMax;
        public Vector2Int initTopL, initTopR, initBotL, initBotR;
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
    [SerializeField] private float angleLeftRight = 10;
    [SerializeField] private float pixelBufferTiltUpDownPrecentage = .1f;
    [SerializeField] private float pixelBufferTiltLeftRightPercentage = .1f;

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
        //WebCamDevice[] devices = WebCamTexture.devices;

        //Debug.Log(devices);

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

        webcamPixels = webcamTexture.GetPixels();
        uiWidth = webcamTexture.width;
        uiHeight = webcamTexture.height;
        totalPixelAmount = uiWidth * uiHeight;

        //Debug.Log("UiWidth, uiHeight, totalPixels: " + uiWidth + ", " + uiHeight + ", " + totalPixelAmount);

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
            if (!playerScreens.Any(x => x.isCurrentlyActive))
            {
                Debug.Log("No player active");
                yield return new WaitForSeconds(1);
            }


            webcamPixels = webcamTexture.GetPixels();
            resultPixels = new Color[webcamPixels.Length];

            uiWidth = webcamTexture.width;
            uiHeight = webcamTexture.height;

            int yHit = 0;
            int xHit = 0;
            int missScans = 0;
            int pixelRange = 0;
            Color pixel;

            for (int i = 0; i < playerScreens.Count; i++)
            {
                if (playerScreens[i].isCurrentlyActive == false) { continue; } // Debug.Log($"Player:{i} not playing"); 
                //Debug.Log("Start Tracing for Player: " + i);

                xOff = i * screenWidth + xSpacing; // Adds offset fo 2nd or 3rd player

                // GET PLAYER SCREEN OBJECT
                PlayerScreen ps = playerScreens[i];
                bool isOutOfBounds = false;
                int maxAdditionalScans = 3;

                // TOP LEFT TRACKER
                missScans = 0;
                pixelRange = 10;
                yHit = playerScreens[i].topL.y - pixelRange;
                xHit = playerScreens[i].topL.x + pixelRange;
                bool isTopLSearching = true;

                while (isTopLSearching)
                {
                    for (int y = playerScreens[i].topL.y - pixelRange; y < playerScreens[i].topL.y + pixelRange; y++)
                    {
                        for (int x = playerScreens[i].topL.x - pixelRange; x < playerScreens[i].topL.x + pixelRange; x++)
                        {
                            int index = y * uiWidth + x;
                            if (index >= totalPixelAmount || index < 0) {continue; }
   
                            pixel = webcamPixels[index];

                            if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                            {
                                if (y > yHit) yHit = y;
                                if (x < xHit) xHit = x;
                            }
                        }
                    }

                    if (xHit < playerScreens[i].scanFrameMinX || xHit > playerScreens[i].scanFrameMaxX || 
                        yHit < playerScreens[i].scanFrameMinY || yHit > playerScreens[i].scanFrameMaxY)
                    {
                        isOutOfBounds = true;
                    }
                    else
                    {
                        ps.topL = new Vector2Int(xHit, yHit);
                        isTopLSearching = false;
                    }

                    if (isTopLSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++;}
                    else if (isOutOfBounds == true || missScans > maxAdditionalScans) { ps.isCurrentlyActive = false; playerScreens[i] = ps; StartCoroutine(OutOfBoundsCorrection(i)); break; }
                }

                if (ps.isCurrentlyActive == false) { continue; } // Skips the iteration for the current player


                // TOP RIGHT TRACKER
                missScans = 0;
                pixelRange = 10;
                yHit = playerScreens[i].topR.y - pixelRange;
                xHit = playerScreens[i].topR.x - pixelRange;
                bool isTopRSearching = true;
                while (isTopRSearching)
                {
                    for (int y = playerScreens[i].topR.y - pixelRange; y < playerScreens[i].topR.y + pixelRange; y++)
                    {
                        for (int x = playerScreens[i].topR.x - pixelRange; x < playerScreens[i].topR.x + pixelRange; x++)
                        {
                            int index = y * uiWidth + x;
                            if (index > totalPixelAmount) { continue; }

                            pixel = webcamPixels[index];

                            if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                            {
                                if (y > yHit) yHit = y;
                                if (x > xHit) xHit = x;
                            }
                        }
                    }

                    if (xHit < playerScreens[i].scanFrameMinX || xHit > playerScreens[i].scanFrameMaxX ||
                        yHit < playerScreens[i].scanFrameMinY || yHit > playerScreens[i].scanFrameMaxY)
                    {
                        isOutOfBounds = true;
                    }
                    else
                    {
                        ps.topR = new Vector2Int(xHit, yHit);
                        isTopRSearching = false;
                    }

                    if (isTopRSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++; }
                    else if (isOutOfBounds == true || missScans > maxAdditionalScans) { ps.isCurrentlyActive = false; playerScreens[i] = ps; StartCoroutine(OutOfBoundsCorrection(i)); break; }


                }
                if (ps.isCurrentlyActive == false) { continue; } // Skips the iteration for the current player


                // BOTTOM RIGHT TRACKER
                missScans = 0;
                pixelRange = 10;
                yHit = playerScreens[i].botR.y + pixelRange;
                xHit = playerScreens[i].botR.x - pixelRange;
                bool isBotRSearching = true;
                while (isBotRSearching)
                {
                    for (int y = playerScreens[i].botR.y - pixelRange; y < playerScreens[i].botR.y + pixelRange; y++)
                    {
                        for (int x = playerScreens[i].botR.x - pixelRange; x < playerScreens[i].botR.x + pixelRange; x++)
                        {
                            int index = y * uiWidth + x;
                            if (index > totalPixelAmount) { continue; }

                            pixel = webcamPixels[index];

                            if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                            {
                                if (y < yHit) yHit = y;
                                if (x > xHit) xHit = x;
                            }
                        }
                    }


                    if (xHit < playerScreens[i].scanFrameMinX || xHit > playerScreens[i].scanFrameMaxX ||
                        yHit < playerScreens[i].scanFrameMinY || yHit > playerScreens[i].scanFrameMaxY)
                    {
                        isOutOfBounds = true;
                    }
                    else
                    {
                        ps.botR = new Vector2Int(xHit, yHit);
                        isBotRSearching = false;
                    }

                    if (isBotRSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++; }
                    else if (isOutOfBounds == true || missScans > maxAdditionalScans) { ps.isCurrentlyActive = false; playerScreens[i] = ps; StartCoroutine(OutOfBoundsCorrection(i)); break; }

                }
                if (ps.isCurrentlyActive == false) { continue; } // Skips the iteration for the current player


                // BOTTOM Left TRACKER
                missScans = 0;
                pixelRange = 10;
                yHit = playerScreens[i].botL.y + pixelRange;
                xHit = playerScreens[i].botL.x + pixelRange;
                bool isBotLSearching = true;
                while (isBotLSearching)
                {
                    for (int y = playerScreens[i].botL.y - pixelRange; y < playerScreens[i].botL.y + pixelRange; y++)
                    {
                        for (int x = playerScreens[i].botL.x - pixelRange; x < playerScreens[i].botL.x + pixelRange; x++)
                        {
                            int index = y * uiWidth + x;
                            if (index > totalPixelAmount) { continue; }

                            pixel = webcamPixels[index];

                            if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                            {
                                if (y < yHit) yHit = y;
                                if (x < xHit) xHit = x;
                            }
                        }
                    }

                    if (xHit < playerScreens[i].scanFrameMinX || xHit > playerScreens[i].scanFrameMaxX ||
                        yHit < playerScreens[i].scanFrameMinY || yHit > playerScreens[i].scanFrameMaxY)
                    {
                        isOutOfBounds = true;
                    }
                    else
                    {
                        ps.botL = new Vector2Int(xHit, yHit);
                        isBotLSearching = false;
                    }

                    if (isBotLSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++; }
                    else if (isOutOfBounds == true || missScans > maxAdditionalScans) { ps.isCurrentlyActive = false; playerScreens[i] = ps; StartCoroutine(OutOfBoundsCorrection(i)); break; }

                }
                if (ps.isCurrentlyActive == false) { continue; } // Skips the iteration for the current player



                // OVERWRITE PLAYER SCREEN DATA (including corners)
                playerScreens[i] = ps;




                #region caluclate movement
                float tiltLeftRightTmp = 0;
                if (Vector2.Distance(playerScreens[i].topR, playerScreens[i].topL) >= playerScreens[i].width - (playerScreens[i].width * pixelBufferTiltLeftRightPercentage))
                {
                    tiltLeftRightTmp = 0;

                }
                else
                {
                    tiltLeftRightTmp = 1;
                }
                //Debug.Log("LeftRigh: " + Vector2.Distance(playerScreens[i].topR, playerScreens[i].topL) + " / " + playerScreens[i].width);


                float tiltUpDownTmp = 0;
                if (Vector2.Distance(playerScreens[i].topR, playerScreens[i].botR) >= playerScreens[i].height - (playerScreens[i].height * pixelBufferTiltUpDownPrecentage))
                {
                    tiltUpDownTmp = 0;
                }
                else
                {
                    tiltUpDownTmp = 1;
                }
                Debug.Log("Up|Down: " + Vector2.Distance(playerScreens[i].topR, playerScreens[i].botR) + " | " + (playerScreens[i].height - (playerScreens[i].height * pixelBufferTiltUpDownPrecentage)) +  " | normHeight: " + playerScreens[i].height);


                float rotInputTest = 0;
                //Vector2 tmpBot = playerScreens[i].botR - playerScreens[i].botL;
                //Vector2 tmpTop = playerScreens[i].topR - playerScreens[i].topL;
                Vector2 tmpRightEdge = playerScreens[i].topR - playerScreens[i].botR;
                Vector2 tmpLeftEdge = playerScreens[i].topR - playerScreens[i].botR;

                //float angle = Vector2.SignedAngle(tmpBot - Vector2.up, tmpTop + Vector2.up);
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

                playerInputs[i] = tmpInput;
                PlayerHandlers[i].thisPlayerInput = tmpInput;

                #endregion

                PieChartHandlers[i].TopLeftTracker.anchoredPosition = (playerScreens[i].topL - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));
                PieChartHandlers[i].TopRightTracker.anchoredPosition = (playerScreens[i].topR - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));
                PieChartHandlers[i].BottomLeftTracker.anchoredPosition = (playerScreens[i].botL - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY)); // * new Vector2(-1, 1) in case of flipping
                PieChartHandlers[i].BottomRightTracker.anchoredPosition = (playerScreens[i].botR - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));

                PieChartHandlers[i].rotInputValue.text = Math.Round(playerInputs[i].rotInput, 2).ToString();
                PieChartHandlers[i].tiltUDInputValue.text = Math.Round(playerInputs[i].tiltUpDownInput, 2).ToString();
                PieChartHandlers[i].tiltLRInputValue.text = Math.Round(playerInputs[i].tiltLeftRightInput, 2).ToString();

                i++;

                //Debug.Log("Finished player Tracing");

                yield return null;

            }

            outputTexture.SetPixels(resultPixels);
            outputTexture.Apply();

            yield return new WaitForSeconds(.1f);
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
                        //else if(pixel.r < whiteThreshold && pixel.g < whiteThreshold && pixel.b < whiteThreshold)
                        //{
                        //    resultPixels[index] = Color.magenta;
                        //}
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


                    if (stopScanningCoroutine != null) { StopCoroutine(stopScanningCoroutine); }
                    stopScanningCoroutine = StartCoroutine(StopScanningCoroutine());
                }

                float fillPercentage = (float)playerScreenScanProcess[i] / (float)scanCompleteMaxValue;

                if (fillPercentage >= 1)
                {
                    //ArduinoSetup.instance.SetLedColor("GREEN");

                    PlayerScreen tmp = playerScreens[i];
                    //Debug.Log("++++++++++++++++");
                    //Debug.Log("PlayerSCreenHeightMax: " + PlayerScreenHeightMax);
                    //Debug.Log("PlayerSCreenWIdthMax: " + PlayerScreenWidthMax);
                    //Debug.Log("PlayerSCreenHeightMax/Width Ratio: " + PlayerScreenHeightMax / PlayerScreenWidthMax);
                    //Debug.Log("tmp.height.norm " + tmp.height/ scanCompleteMaxValue);
                    //Debug.Log("tmp.width.norm " + tmp.width/ scanCompleteMaxValue);
                    //Debug.Log("tmp.ratio.norm " + tmp.ratio/ scanCompleteMaxValue);
                    //Debug.Log("++++++++++++++++");



                    tmp.isCurrentlyActive = true;
                    tmp.initTopL = new Vector2Int(playerMinX, playerMaxY); // playerMinX, // playerMaxX // Flipped because camera is inverted as well
                    tmp.initTopR = new Vector2Int(playerMaxX, playerMaxY); // playerMinX, // playerMinY
                    tmp.initBotL = new Vector2Int(playerMinX, playerMinY); // playerMinX
                    tmp.initBotR = new Vector2Int(playerMaxX, playerMinY); // playerMinX// playerMaxY

                    tmp.topL = tmp.initTopL; //new Vector2Int(playerMinX, playerMaxY); // playerMinX, // playerMaxX // Flipped because camera is inverted as well
                    tmp.topR = tmp.initTopR; //new Vector2Int(playerMaxX, playerMaxY); // playerMinX, // playerMinY
                    tmp.botL = tmp.initBotL; //new Vector2Int(playerMinX, playerMinY); // playerMinX
                    tmp.botR = tmp.initBotR; //new Vector2Int(playerMaxX, playerMinY); // playerMinX// playerMaxY

                    tmp.minMin = Vector2Int.zero;
                    tmp.maxMax = Vector2Int.zero;
                    //tmp.height = PlayerScreenHeightMax;
                    //tmp.width = PlayerScreenWidthMax;
                    //tmp.ratio = PlayerScreenWidthMax / PlayerScreenHeightMax;
                    tmp.height = tmp.height / scanCompleteMaxValue;
                    tmp.width = tmp.width/ scanCompleteMaxValue;

                    Debug.Log("Width/Height: " + tmp.width + "/" + tmp.height);
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

                    PieChartHandlers[i].CornerTrackers.SetActive(true);
                    PieChartHandlers[i].TopLeftTracker.anchoredPosition = (playerScreens[i].topL - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));
                    PieChartHandlers[i].TopRightTracker.anchoredPosition = (playerScreens[i].topR - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));
                    PieChartHandlers[i].BottomLeftTracker.anchoredPosition = (playerScreens[i].botL - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY)); // * new Vector2(-1, 1) in case of flipping
                    PieChartHandlers[i].BottomRightTracker.anchoredPosition = (playerScreens[i].botR - new Vector2(playerScreens[i].scanFrameMinX, playerScreens[i].scanFrameMinY));

                    yield return new WaitForSeconds(3);

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

    IEnumerator OutOfBoundsCorrection(int playerIndex)
    {
        PieChartHandlers[playerIndex].infoText.text = "Out of \nBounds";
        PieChartHandlers[playerIndex].CornerTrackers.SetActive(false);

        PlayerScreen ps = playerScreens[playerIndex];
        ps.isCurrentlyActive = false;
        ps.topL = ps.initTopL;
        ps.topR = ps.initTopR;
        ps.botL = ps.initBotL;
        ps.botR = ps.initBotR;

        PlayerInput tmpInput = new PlayerInput
        {
            rotInput = 0,
            tiltLeftRightInput = 0,
            tiltUpDownInput = 0,
        };

        playerInputs[playerIndex] = tmpInput;
        PlayerHandlers[playerIndex].thisPlayerInput = tmpInput;


        playerScreens[playerIndex] = ps;

        yield return new WaitForSeconds(1f);
        PieChartHandlers[playerIndex].infoText.text = "Keep \nCentered";

        PieChartHandlers[playerIndex].CornerTrackers.SetActive(true);
        PieChartHandlers[playerIndex].TopLeftTracker.anchoredPosition = (playerScreens[playerIndex].initTopL - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY));
        PieChartHandlers[playerIndex].TopRightTracker.anchoredPosition = (playerScreens[playerIndex].initTopR - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY));
        PieChartHandlers[playerIndex].BottomLeftTracker.anchoredPosition = (playerScreens[playerIndex].initBotL - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY));
        PieChartHandlers[playerIndex].BottomRightTracker.anchoredPosition = (playerScreens[playerIndex].initBotR - new Vector2(playerScreens[playerIndex].scanFrameMinX, playerScreens[playerIndex].scanFrameMinY));

        playerScreens[playerIndex] = ps;
        playerScreens[playerIndex] = ScanScreen(playerScreens[playerIndex], playerIndex);

        yield return new WaitForSeconds(1f);
        PieChartHandlers[playerIndex].infoText.text = "Ready";


        yield return new WaitForSeconds(1f);
        PieChartHandlers[playerIndex].infoText.text = "";
    }



    PlayerScreen ScanScreen(PlayerScreen currentPS, int playerIndex)
    {
        webcamPixels = webcamTexture.GetPixels();
        Color pixel;
        bool isOutOfBounds = false;
        int maxAdditionalScans = 3;


        // TOP LEFT TRACKER
        int missScans = 0;
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

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        if (y > yHit) yHit = y;
                        if (x < xHit) xHit = x;
                    }
                }
            }

            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                isOutOfBounds = true;
            }
            else
            {
                currentPS.topL = new Vector2Int(xHit, yHit);
                isTopLSearching = false;
            }

            if (isTopLSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++; }
            else if (isOutOfBounds == true || missScans > maxAdditionalScans) { currentPS.isCurrentlyActive = false;  break; }
        }


        // TOP RIGHT TRACKER
        missScans = 0;
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

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        if (y > yHit) yHit = y;
                        if (x > xHit) xHit = x;
                    }
                }
            }

            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                isOutOfBounds = true;
            }
            else
            {
                currentPS.topR = new Vector2Int(xHit, yHit);
                isTopRSearching = false;
            }

            if (isTopRSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++; }
            else if (isOutOfBounds == true || missScans > maxAdditionalScans) { currentPS.isCurrentlyActive = false; break; }


        }


        // BOTTOM RIGHT TRACKER
        missScans = 0;
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

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        if (y < yHit) yHit = y;
                        if (x > xHit) xHit = x;
                    }
                }
            }


            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                isOutOfBounds = true;
            }
            else
            {
                currentPS.botR = new Vector2Int(xHit, yHit);
                isBotRSearching = false;
            }

            if (isBotRSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++; }
            else if (isOutOfBounds == true || missScans > maxAdditionalScans) { currentPS.isCurrentlyActive = false; break; }

        }


        // BOTTOM Left TRACKER
        missScans = 0;
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

                    if (pixel.r > redThreshold && pixel.g < greenMax && pixel.b < blueMax)
                    {
                        if (y < yHit) yHit = y;
                        if (x < xHit) xHit = x;
                    }
                }
            }

            if (xHit < currentPS.scanFrameMinX || xHit > currentPS.scanFrameMaxX ||
                yHit < currentPS.scanFrameMinY || yHit > currentPS.scanFrameMaxY)
            {
                isOutOfBounds = true;
            }
            else
            {
                currentPS.botL = new Vector2Int(xHit, yHit);
                isBotLSearching = false;
            }

            if (isBotLSearching == true && isOutOfBounds == false) { pixelRange += 10; missScans++; }
            else if (isOutOfBounds == true || missScans > maxAdditionalScans) { currentPS.isCurrentlyActive = false; break; }
        }


        currentPS.height = (Vector2.Distance(currentPS.topL, currentPS.botL) + Vector2.Distance(currentPS.topR, currentPS.botR)) / 2;
        currentPS.width = (Vector2.Distance(currentPS.topL, currentPS.topR) + Vector2.Distance(currentPS.botL, currentPS.botR)) / 2;

        currentPS.ratio = currentPS.height / currentPS.width;
        Debug.Log("NEW Height/Width/ratio: " + currentPS.height + "/" + currentPS.width + "/" + currentPS.ratio);




        currentPS.isCurrentlyActive = true;

        return currentPS;
    }

}