using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    public GameObject SnakeBody;
    public GameObject SnakeTail;

    public Transform target;
    public float followSpeed = 10f;
    public float followDistance = 0.5f;

    void FixedUpdate()
    {
        if (target == null) return;

        // Desired position is a point behind the target based on distance
        Vector3 targetPosition = target.position - target.forward * followDistance;

        // Smoothly move to that position
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // Face the direction of movement
        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
        }
    }
}
