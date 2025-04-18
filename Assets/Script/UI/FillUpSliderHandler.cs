using UnityEngine;
using UnityEngine.UI;

public class FillUpSliderHandler : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float fillSpeed = 0.5f;

    
    void Start()
    {           
        slider = GetComponent<Slider>();
        slider.value = 0f;
    }

    void Update()
    {
        if (slider.value < slider.maxValue)
        {
            slider.value += fillSpeed * Time.deltaTime;
        }
    }
}
