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
    public float lifeTime = 0;
    public float initialProtection = 2;

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
        lifeTime += Time.deltaTime;
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
        Vector3 newPos = bodyParts.Count == 0 ? transform.position - transform.forward * followDistance*2 : bodyParts[bodyParts.Count - 1].position - (transform.forward * followDistance*2);
        GameObject newPart = Instantiate(bodyPrefab, newPos, Quaternion.identity, PlayerTransform);
        newPart.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        newPart.transform.localEulerAngles = new Vector3(0, newPart.transform.localEulerAngles.y, newPart.transform.localEulerAngles.z);

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
        Vector3 targetPosition = new Vector3(startPosition.x, 0,300);
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
            KillThisSnake();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("SnakeBody"))
        {
            if(lifeTime < initialProtection) { return; }

            if (bodyParts.Contains(collision.gameObject.transform))
            {
                Debug.Log("Hit Own Body");
            }
            else
            {
                Debug.Log("Hit Someones Body");
            }
            KillThisSnake();

        }
    }

    void KillThisSnake()
    {
        for (int i = 0; i < bodyParts.Count; i++)
        {
            Destroy(bodyParts[i].gameObject);
        }
        bodyParts.Clear();

        if (PlayerHandler.snakeSpawnCoro != null) {
            PlayerHandler.StopCoroutine(PlayerHandler.snakeSpawnCoro); 
            PlayerHandler.snakeSpawnCoro = null; 
        };
        PlayerHandler.snakeSpawnCoro = PlayerHandler.StartCoroutine(PlayerHandler.SpawnSnakeCoro()); 

        Destroy(this.gameObject);
    }

}
