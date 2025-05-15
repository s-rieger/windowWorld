using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandler : MonoBehaviour
{
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
    public SnakeHead sh;
    

    [Header("References")]
    public Transform thisTransform;
    public float rotInput;

    public ScreenDetector.PlayerInput thisPlayerInput;

    
    private void Awake()
    {
        GameManager.instance.targets.Add(gameObject);

        sh.playerTransform = this.transform;
    }

    private void Start()
    {
        fh.PlayerColor = PlayerColor;
    }

    private void FixedUpdate()
    {
        sh.HandleInput(thisPlayerInput.rotInput);


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
        flower.transform.localScale += Vector3.one * 0.1f;

    }
}
