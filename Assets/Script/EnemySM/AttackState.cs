using UnityEngine;

public class AttackState : BaseState
{
    float randomDistance;
    
    public AttackState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        model.agent.speed += model.dashSpeed;
        randomDistance = Random.Range(1f, 15f);

        model.debugString = "Attacking";
    }

    public override void Update()
    {
        base.Update();
        
        if(model.target != null)
        {
            float distanceToTarget = Vector3.Distance(model.transform.position, model.target.transform.position);

            if (distanceToTarget <= model.attackRange)
            {
                if (!model.isWaiting)
                {
                    Vector3 directionToTarget = (model.target.transform.position - model.transform.position).normalized;
                    Vector3 dashPosition = model.target.transform.position - directionToTarget * model.agent.stoppingDistance;

                    model.agent.SetDestination(dashPosition);
                }
                else
                {
                    Debug.Log(randomDistance + " " + model.attackRange);
                    Vector3 directionToTarget = (model.target.transform.position - model.transform.position).normalized;

                    Vector3 randomPosition = model.transform.position + directionToTarget * randomDistance;

                    model.agent.SetDestination(randomPosition);

                    if (!model.agent.pathPending && model.agent.remainingDistance <= model.agent.stoppingDistance + 6f)
                    {
                        Debug.Log("Agent has reached the random position.");
                        model.StartCoroutine(model.WaitBeforeNextMove());
                    }
                }
            }
        }
        else
        {
            //Debug.Log("No target found switching to roaming state");
            model.ChangeState(model.roamingState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        model.agent.speed -= model.dashSpeed;
    }
    
    
}

