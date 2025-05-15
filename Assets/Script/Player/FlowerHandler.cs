using UnityEngine;

public class FlowerHandler : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer skinMeshRend;
    [SerializeField] Material BlossomMat;
    [SerializeField] private Color playerColor;

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
}
