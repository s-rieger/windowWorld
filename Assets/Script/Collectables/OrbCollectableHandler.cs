using TMPro;
using UnityEngine;

public class OrbCollectableHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI orbText;
    [SerializeField] private string orbTextOnEnter;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            orbText.enabled = true;
            orbText.text = orbTextOnEnter;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            orbText.enabled = false;
        }
    }
}
