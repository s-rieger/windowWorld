using UnityEngine;
using UnityEngine.UI;

public class PieChartHandler : MonoBehaviour
{
    [SerializeField] private ScreenDetector screenDetector;
    [SerializeField] private Image pieChart;
    
    void Start()
    {
        pieChart.fillAmount = 0f;
    }

    void Update()
    {
        pieChart.fillAmount = screenDetector.scanCompleteValue / 100f;
        pieChart.color = screenDetector.colorList[screenDetector.currentPlayers];
    }
}
