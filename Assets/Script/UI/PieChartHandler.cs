using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PieChartHandler : MonoBehaviour
{    
    [SerializeField] private Image pieChart;
    public Color pieChartColor;
    public TextMeshProUGUI infoText;
    public GameObject InputDebug;
    public TextMeshProUGUI rotInputValue;
    public TextMeshProUGUI tiltInputValue;
    public TextMeshProUGUI yawInputValue;
    
    void Start()
    {
        pieChart.fillAmount = 0f;
        pieChart.color = pieChartColor;
        infoText.text = "";
        InputDebug.SetActive(false);
    }

    public void FillScanProgress(float fillValue)
    {
        pieChart.fillAmount = fillValue;
    }
}
