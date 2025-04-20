using UnityEngine;
using UnityEngine.AI;

public class RoamingState : BaseState
{
    private NavMeshAgent agent;
    private Vector3 targetPosition;
    
    
    public RoamingState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        agent = model.agent;
        
        agent.updateRotation = true;
        SetRandomDestination();
    }
    public override void Update()
    {
        base.Update();
        
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            CheckDistancesToTargets();
            SetRandomDestination();
        }
        
    }
    
    public override void Exit()
    {
        base.Exit();
    }
    
    private void SetRandomDestination()
    {
        Vector3 randomDirection = Random.onUnitSphere * model.travelDistance;
        randomDirection += model.transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, model.travelDistance, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            agent.SetDestination(targetPosition);
        }
    }
    
    private void CheckDistancesToTargets()
    {
        Debug.Log("Checking distances to targets...");
        
        foreach (GameObject target in GameManager.instance.targets)
        {
            if (target != null)
            {
                int randomIndex = Random.Range(0, GameManager.instance.targets.Count);
                model.target = GameManager.instance.targets[randomIndex];    
                Debug.Log("Attack State");
                model.ChangeState(model.attackState);
            }
        }
    }
    

}
