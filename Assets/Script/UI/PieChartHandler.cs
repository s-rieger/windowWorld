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
    public TextMeshProUGUI tiltInputValue;
    public TextMeshProUGUI yawInputValue;

    [Header("Tracker")]
    public GameObject CornerTrackers;
    public RectTransform TopLeftTracker;
    public RectTransform BottomLeftTracker;
    public RectTransform TopRightTracker;
    public RectTransform BottomRightTracker;
    
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
