using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SnakeHead : MonoBehaviour
{
    public PlayerHandler PlayerHandler;

    public FlowerHandler FlowerHandler;
    public Transform PlayerTransform;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    public GameObject bodyPrefab;
    public int initialSize = 5;
    public float followDistance = 0.5f;

    private List<Transform> bodyParts = new List<Transform>();

    void Start()
    {
        // Add initial body parts
        for (int i = 0; i < initialSize; i++)
        {
            if(i+1 == initialSize)
            {
                AddBodySegment(true);
            }
            else
            {
                AddBodySegment(false);
            }
        }

        StartCoroutine(JumpOutOfWindow(2));
    }

    void Update()
    {
        if(PlayerHandler.canMove == false) { return; }

        Move();
    }

    public void HandleInput(float rotInput)
    {
        transform.Rotate(Vector3.up * rotInput * turnSpeed * Time.deltaTime);
    }

    void Move()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    public void AddBodySegment(bool isTail)
    {
        Vector3 newPos = bodyParts.Count == 0 ? transform.position - transform.forward * followDistance : bodyParts[bodyParts.Count - 1].position;
        GameObject newPart = Instantiate(bodyPrefab, newPos, Quaternion.identity, PlayerTransform);

        SnakeSegment ss = newPart.GetComponent<SnakeSegment>();
        if (isTail) { ss.SnakeTail.SetActive(true); ss.SnakeBody.SetActive(false);
        }
        else
        {
            ss.SnakeTail.SetActive(false);
            ss.SnakeBody.SetActive(true);
        }
        Transform newTransform = newPart.transform;

        SnakeSegment segmentScript = newPart.GetComponent<SnakeSegment>();
        segmentScript.target = bodyParts.Count == 0 ? this.transform : bodyParts[bodyParts.Count - 1];
        segmentScript.followDistance = followDistance;

        bodyParts.Add(newTransform);
    }

    IEnumerator JumpOutOfWindow(float duration)
    {

        Vector3 startPosition = PlayerHandler.transform.localPosition;
        Vector3 targetPosition = new Vector3(startPosition.x, 0,0);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            PlayerHandler.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final position is exactly the target
        PlayerHandler.transform.localPosition = targetPosition;

        PlayerHandler.canMove = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectable"))
        {
            Debug.Log("Collected Orb");
            PlayerHandler.CollectOrb();
            OrbManager.Instance.SpawnOrb();

            Destroy(other.gameObject);
        }

        if (other.CompareTag("Wall"))
        {
            Debug.Log("Hit Wall");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("SnakeBody"))
        {
            Debug.Log("Eaten by Snake");
            Destroy(PlayerHandler.gameObject);
        }
    }

}
