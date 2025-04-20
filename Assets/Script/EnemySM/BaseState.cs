using UnityEngine;

public abstract class BaseState
{
    protected EnemyStateMachine model;

    public BaseState(EnemyStateMachine stateMachine)
    {
        model = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
