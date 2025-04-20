using UnityEngine;

public class AttackState : BaseState
{
    public AttackState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
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
                model.StartDash();
                Debug.Log("Attacking target");
            }
            else
            {
                Debug.Log("Target out of range switching to roaming state");
                model.ChangeState(model.roamingState);
            }
        }
        else
        {
            Debug.Log("No target found switching to roaming state");
            model.ChangeState(model.roamingState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        model.target = null;
    }
    
    
}

