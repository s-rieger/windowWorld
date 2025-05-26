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
        transform.Rotate(Vector3.up * rotInput * turnSpeed * Time.deltaTime); // -1 so its initally reversed but more intuitional ?!?!?!
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

        Rigidbody snakebodyRB = newPart.GetComponent<Rigidbody>();
        PlayerHandler.SnakeRB.Add(snakebodyRB);

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
        PlayerHandler.canMove = false;

        Vector3 startPosition = PlayerHandler.transform.localPosition;
        Vector3 targetPosition = new Vector3(startPosition.x, 0,300);
        float elapsed = 0f;

        foreach (var item in PlayerHandler.SnakeRB)
        {
            item.isKinematic = false;
            item.transform.GetChild(0).gameObject.SetActive(false);
            item.position = startPosition;
            item.linearVelocity = Vector3.zero; // optional: reset velocity if needed
            item.angularVelocity = Vector3.zero; // optional: reset rotation momentum
            item.transform.GetChild(0).gameObject.SetActive(true);
            item.isKinematic = true;
        }


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
            //KillThisSnake();
            //PlayerHandler.transform.rotation = Quaternion.identity;
            //PlayerHandler.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            //PlayerHandler.transform.position = new Vector3(PlayerHandler.playerIndex * -300, 250, 400);
            //PlayerHandler.transform.localPosition = Vector3.zero;
            //PlayerHandler.transform.localEulerAngles = new Vector3(0, PlayerHandler.transform.localEulerAngles.y, PlayerHandler.transform.localEulerAngles.z);
            Vector3 newPosition = new Vector3(PlayerHandler.thisTransform.position.x, PlayerHandler.thisTransform.position.y + 300, 
                PlayerHandler.thisTransform.position.z);
            this.gameObject.transform.position = newPosition;
            this.gameObject.transform.rotation = PlayerHandler.thisTransform.rotation; 
            
            for(int i = 0; i <= bodyParts.Count; i++)
            {
                bodyParts[i].position = newPosition;
                bodyParts[i].rotation = PlayerHandler.thisTransform.rotation;
            }
            
            // StartCoroutine(JumpOutOfWindow(2));
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
            //KillThisSnake();

        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            PlayerHandler.transform.position = new Vector3(PlayerHandler.playerIndex * -300, 250, 400);
            StartCoroutine(JumpOutOfWindow(2));
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
