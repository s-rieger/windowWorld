using UnityEngine;
using UnityEngine.UI;

public class SliderHandler : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float speed;

    private float targetValue;

    void Start()
    {
        slider = GetComponent<Slider>();
        targetValue = Random.Range(-1f, 1f);
    }

    void Update()
    {
        slider.value = Mathf.MoveTowards(slider.value, targetValue, speed * Time.deltaTime);

        if (Mathf.Approximately(slider.value, targetValue))
        {
            targetValue = Random.Range(-1f, 1f);
        }
    }
    
    // If you are reading this, idk if this is what you meant by return value but
    // calling would get you slider vale
    public float GetSliderValue()
    {
        return slider.value;
    }
}
