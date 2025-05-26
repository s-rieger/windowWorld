using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using static UnityEditor.Progress;
using UnityEngine.UIElements;

public class SnakeHead : MonoBehaviour
{
    public PlayerHandler PlayerHandler;

    public Rigidbody rb;

    public FlowerHandler FlowerHandler;
    public Transform PlayerTransform;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    public GameObject bodyPrefab;
    public int initialSize = 5;
    public float followDistance = 0.5f;
    public float lifeTime = 0;
    public float initialProtection = 2;
    public float jumnpOutOfWindowTime = 1;
    public float snakeEjectionForce = 500;

    private List<Transform> bodyParts = new List<Transform>();

    void Start()
    {
        if(rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        rb.isKinematic = true;  // disables physics on this object

        // Add initial body parts
        for (int i = 0; i < initialSize; i++)
        {
            if(i+1 == initialSize)
            {
                AddBodySegment(true, i);
            }
            else
            {
                AddBodySegment(false, i);
            }
        }

        StartCoroutine(JumpOutOfWindow(jumnpOutOfWindowTime));
    }

    void Update()
    {

        Debug.Log("Rotation: " + this.transform.eulerAngles);
        lifeTime += Time.deltaTime;
        if(PlayerHandler.canMove == false) { return; }


        Move();

        if(this.transform.position.y < -3)
        {
            Debug.Log("Snake Fell through Ground");
            StartCoroutine(JumpOutOfWindow(jumnpOutOfWindowTime));
        }
    }

    public void HandleInput(float rotInput)
    {
        transform.Rotate(Vector3.up * rotInput * turnSpeed * Time.deltaTime); // -1 so its initally reversed but more intuitional ?!?!?!
    }

    void Move()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        //this.transform.rotation = Quaternion.Euler(this.transform.rotation.x, this.transform.rotation.y, 0);

    }

    public void AddBodySegment(bool isTail, int index)
    {
        Vector3 newPos = bodyParts.Count == 0 ? -transform.forward * index : bodyParts[bodyParts.Count - 1].position + (-transform.forward * index);

        GameObject newPart = Instantiate(bodyPrefab, newPos, Quaternion.identity, PlayerTransform);
        newPart.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        newPart.transform.localEulerAngles = new Vector3(0, newPart.transform.localEulerAngles.y, newPart.transform.localEulerAngles.z);

        Rigidbody newRB = newPart.GetComponent<Rigidbody>();
        newRB.isKinematic = true;
        newRB.transform.GetChild(0).gameObject.SetActive(false);
        newPart.transform.position = newPos;
        newRB.isKinematic = false;


        Rigidbody snakebodyRB = newPart.GetComponent<Rigidbody>();
        PlayerHandler.SnakeRB.Add(snakebodyRB);

        SnakeSegment ss = newPart.GetComponent<SnakeSegment>();
        ss.SnakeHead = this;
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

    public IEnumerator JumpOutOfWindow(float duration)
    {
        PlayerHandler.canMove = false;
        yield return new WaitForSeconds(.1f);

        rb.isKinematic = true;

        this.transform.position = PlayerHandler.transform.position - Vector3.forward * 100;
        //this.transform.localRotation = Quaternion.LookRotation(PlayerHandler.transform.forward, Vector3.up);
        //this.transform.transform.eulerAngles = Vector3.zero;
        this.transform.transform.localEulerAngles = Vector3.zero;

        Vector3 startPosition = PlayerHandler.transform.position;
        for (int i = 0; i < PlayerHandler.SnakeRB.Count; i++)
        {
            PlayerHandler.SnakeRB[i].transform.GetChild(0).gameObject.SetActive(false);
            PlayerHandler.SnakeRB[i].isKinematic = true;
            PlayerHandler.SnakeRB[i].useGravity = false;
            PlayerHandler.SnakeRB[i].rotation = PlayerHandler.transform.rotation;
            //PlayerHandler.SnakeRB[i].transform.eulerAngles = Vector3.zero;
            PlayerHandler.SnakeRB[i].transform.position = this.transform.position + this.transform.forward * i * 50 + transform.up * i * 50;
            PlayerHandler.SnakeRB[i].AddForce(-transform.forward * snakeEjectionForce);
            PlayerHandler.SnakeRB[i].transform.GetChild(0).gameObject.SetActive(true);
            PlayerHandler.SnakeRB[i].isKinematic = false;
        }
        PlayerHandler.canMove = true;

        yield return new WaitForSeconds(1);

        for (int i = 0; i < PlayerHandler.SnakeRB.Count; i++)
        {
            PlayerHandler.SnakeRB[i].useGravity = true;
        }
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
            StartCoroutine(JumpOutOfWindow(2));
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
            StartCoroutine(JumpOutOfWindow(jumnpOutOfWindowTime));
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
        //PlayerHandler.SpawnSnake();

        Destroy(this.gameObject);
    }

}
