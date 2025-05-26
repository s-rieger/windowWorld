using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIRunner : MonoBehaviour
{
    [Header("Agent")] 
    private NavMeshAgent agent;
    
    [Header("Agent Settings")]
    public float roamingDistance = 50f;
    public float escapeDistance = 20f;
    private Vector3 targetPosition;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    
    void Start()
    {
        SetRandomDestination();
    }

    private void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                RunAwayFromClosestTarget();
            }
        }
    }

    private void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamingDistance;
        randomDirection += transform.position;
        
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, roamingDistance, NavMesh.AllAreas))
        {
            Debug.Log("Setting new destination...");
            targetPosition = hit.position;
            agent.gameObject.transform.LookAt(targetPosition);
            agent.SetDestination(targetPosition);
        }
    }

    private void RunAwayFromClosestTarget()
    {
        GameObject closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject target in GameManager.instance.targets)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        if (closestTarget != null && closestDistance <= escapeDistance)
        {
            Vector3 directionAway = (transform.position - closestTarget.transform.position).normalized;
            Vector3 escapePosition = transform.position + directionAway * escapeDistance;

            if (NavMesh.SamplePosition(escapePosition, out NavMeshHit hit, roamingDistance, NavMesh.AllAreas))
            {
                targetPosition = hit.position;
                agent.SetDestination(targetPosition);
            }
        }
        else
        {
            SetRandomDestination();
        }
    }
}
