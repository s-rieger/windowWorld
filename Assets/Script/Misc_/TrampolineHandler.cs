using UnityEngine;

public class TrampolineHandler : MonoBehaviour
{
    [SerializeField] private float shootForce = 20f;
    [SerializeField] private float glideAcceleration = -2f; 
    [SerializeField] private float glideThreshold = -5f;
    [SerializeField] private bool isGliding = false;
    private Transform playerTransform;
    private Vector3 velocity;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            isGliding = true;
            velocity = new Vector3(0, shootForce, 0);
        }
    }

    private void Update()
    {
        if (isGliding)
        {
            velocity.y += glideAcceleration * Time.deltaTime;

            playerTransform.localPosition += velocity * Time.deltaTime;

            if (playerTransform.localPosition.y <= glideThreshold)
            {
                isGliding = false;
                velocity = Vector3.zero;
            }
        }
    }
}
