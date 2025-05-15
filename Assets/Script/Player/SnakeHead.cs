using UnityEngine;
using System.Collections.Generic;

public class SnakeHead : MonoBehaviour
{
    public FlowerHandler FlowerHandler;
    public Transform playerTransform;
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
    }

    void Update()
    {
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
        GameObject newPart = Instantiate(bodyPrefab, newPos, Quaternion.identity, playerTransform);

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
}
