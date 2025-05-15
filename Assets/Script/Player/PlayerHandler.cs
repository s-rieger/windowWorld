using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandler : MonoBehaviour
{
    public bool canMove = false;

    [Header("Player Stats")]
    public int playerIndex;
    public Color PlayerColor;

    [Header("Player Movement Stats")]
    [SerializeField] private float playerRotSpeed;
    [SerializeField] private float playerMoveSpeed;
    [SerializeField] private float playerStretchSpeed;

    [Header("Flower Stuff")]
    public GameObject flower;
    public FlowerHandler fh;


    [Header("Snake Stuff")]
    public GameObject SnakeHead;
    public Vector3 SnakeSpawnLocation;
    public SnakeHead sh;
    public Coroutine snakeSpawnCoro;
    

    [Header("References")]
    public Transform thisTransform;
    public float rotInput;

    public ScreenDetector.PlayerInput thisPlayerInput;

    
    private void Awake()
    {
        GameManager.instance.targets.Add(gameObject);
    }

    private void FixedUpdate()
    {
        if(canMove == false) { return; }
        if (sh != null) { sh.HandleInput(thisPlayerInput.rotInput); }


        //// Flower COntrol
        //// Calculate rotation amount for this frame
        //float rotationAmount = thisPlayerInput.rotInput * playerRotSpeed * Time.fixedDeltaTime;

        //// Apply rotation around z-axis
        //transform.Rotate(0f, rotationAmount, 0f, Space.Self);

        //transform.localPosition += transform.forward * thisPlayerInput.tiltUpDownInput * playerMoveSpeed * Time.fixedDeltaTime; 


        #region Use Vec2 for rotattion
        //// Calculate the angle from the input
        //float targetAngle = Mathf.Atan2(rotInput.y, rotInput.x) * Mathf.Rad2Deg;

        //// Create a Quaternion representing the target rotation
        //Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        //// Smoothly rotate towards the target
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, playerRotSpeed * Time.fixedDeltaTime);
        #endregion
    }

    public void CollectOrb()
    {
        StartCoroutine(GrowFlower(Vector3.one * 0.1f, 0.3f)); 
    }


    private IEnumerator GrowFlower(Vector3 growthAmount, float duration)
    {
        Vector3 initialScale = flower.transform.localScale;
        Vector3 targetScale = initialScale + growthAmount;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            flower.transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the final scale is exact
        flower.transform.localScale = targetScale;
    }


    public void SpawnSnake()
    {
        Debug.Log("Spawn Snake");
        this.transform.localPosition = SnakeSpawnLocation;
        this.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        this.transform.localEulerAngles = new Vector3(0, this.transform.localEulerAngles.y, this.transform.localEulerAngles.z);
        GameObject newSnake = Instantiate(SnakeHead, thisTransform);
        //newSnake.transform.localPosition = SnakeSpawnLocation;
        sh = newSnake.GetComponent<SnakeHead>();
        sh.PlayerHandler = this;
        sh.PlayerTransform = this.transform;
        sh.FlowerHandler.PlayerColor = PlayerColor;
        flower = sh.FlowerHandler.gameObject;
    }

    public IEnumerator SpawnSnakeCoro()
    {
        Debug.Log("Start new SnakeSpawn Coro");
        yield return new WaitForSeconds(1);
        Debug.Log("Waited Start new SnakeSpawn Coro");
        SpawnSnake();
    }
}
