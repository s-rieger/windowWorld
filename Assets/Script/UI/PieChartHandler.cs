using UnityEngine;
using UnityEngine.UI;

public class PieChartHandler : MonoBehaviour
{
    [TextArea] public string debugString;
    
    [SerializeField] private ScreenDetector screenDetector;
    [SerializeField] private Image pieChart;
    [SerializeField] private Transform pieChartParent;

    private Vector3 position;
    
    void Start()
    {
        pieChart.fillAmount = 0f;
    }

    void Update()
    {
        pieChart.fillAmount = screenDetector.scanCompleteValue / 100f;
        pieChart.color = screenDetector.colorList[screenDetector.currentPlayers];
        
        position = pieChartParent.transform.position;
        position.x = -screenDetector.xOff;
        pieChartParent.transform.position = position;
        Debug.Log(position.x);
    }
}
