using System;
using UnityEngine;

public class PlayerHandler : MonoBehaviour
{
    [Header("Player Movement Stats")]
    [SerializeField] private float playerRotSpeed;

    [Header("Player Visuals")]
    [SerializeField] SkinnedMeshRenderer skinMeshRend;
    [SerializeField] Material  BlossomMat;
    [SerializeField] private Color playerColor;
    //[SerializeField] MeshRenderer meshRend;
    
    public Color PlayerColor
    {
        get
        {
            return playerColor;
        }
        set
        {

            playerColor = value;
            //meshRend.material.color = playerColor;

            skinMeshRend.material = new Material(BlossomMat);
            skinMeshRend.material.color = playerColor;
        }
    }

    [Header("References")]
    public Transform thisTransform;
    //public Vector2 rotInput;
    public float rotInput;

    
    private void Awake()
    {
        GameManager.instance.targets.Add(gameObject);
    }
    
    private void FixedUpdate()
    {
        // Calculate rotation amount for this frame
        float rotationAmount = rotInput * playerRotSpeed * Time.fixedDeltaTime;

        // Apply rotation around z-axis
        transform.Rotate(0f, rotationAmount, 0f, Space.Self);


        #region Use Vec2 for rotattion
        //// Calculate the angle from the input
        //float targetAngle = Mathf.Atan2(rotInput.y, rotInput.x) * Mathf.Rad2Deg;

        //// Create a Quaternion representing the target rotation
        //Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        //// Smoothly rotate towards the target
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, playerRotSpeed * Time.fixedDeltaTime);
        #endregion
    }
}
