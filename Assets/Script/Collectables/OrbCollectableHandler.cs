using UnityEngine;

public class OrbCollectableHandler : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name);
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player collected the orb!");
            Destroy(gameObject);
        }
    }
}
