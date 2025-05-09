using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PieChartHandler : MonoBehaviour
{    
    [Header("General")]
    [SerializeField] private Image pieChart;
    public Color pieChartColor;
    public TextMeshProUGUI infoText;

    [Header("Input")]
    public GameObject InputDebug;
    public TextMeshProUGUI rotInputValue;
    public TextMeshProUGUI tiltUDInputValue;
    public TextMeshProUGUI tiltLRInputValue;

    [Header("Tracker")]
    public GameObject CornerTrackers;
    public RectTransform CornerRect;
    public RectTransform TopLeftTracker;
    public RectTransform BottomLeftTracker;
    public RectTransform TopRightTracker;
    public RectTransform BottomRightTracker;

    [Header("Pixel Color Scanner")]
    public RectTransform PixelScannerTracker;
    public TextMeshProUGUI PixelScannerValueText;


    void Start()
    {
        pieChart.fillAmount = 0f;
        pieChart.color = pieChartColor;
        infoText.text = "";
        InputDebug.SetActive(false);
        CornerTrackers.SetActive(false);
    }

    public void FillScanProgress(float fillValue)
    {
        pieChart.fillAmount = fillValue;
    }
}
