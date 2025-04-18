using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PieChartHandler : MonoBehaviour
{    
    [SerializeField] private Image pieChart;
    public Color pieChartColor;
    public TextMeshProUGUI infoText;
    
    void Start()
    {
        pieChart.fillAmount = 0f;
        pieChart.color = pieChartColor;
        infoText.text = "";
    }

    public void FillScanProgress(int fillValue)
    {
        pieChart.fillAmount = fillValue / 100f;
    }
}
