using UnityEngine;

public class BugAllignHandler : MonoBehaviour
{
    [SerializeField] private LayerMask terrainMask;
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private float raycastDistance = 2f;
    
    private RaycastHit hit;
    private Vector3 lastForward;

    void FixedUpdate()
    {
        AlignToSurface();
    }

    private void AlignToSurface()
    {
        if (Physics.Raycast(transform.position, -transform.up, out hit, raycastDistance, terrainMask))
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.parent.forward, hit.normal),
                hit.normal
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * alignmentSpeed
            );
        }
    }
}
