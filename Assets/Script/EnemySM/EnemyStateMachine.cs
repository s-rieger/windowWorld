using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStateMachine : MonoBehaviour
{
    public BaseState _currentState;

    [SerializeField, TextArea] public string debugString;

    [Header("Agent")] 
    [HideInInspector] public NavMeshAgent agent;
    public Transform bug;
    
    [Header("Roaming State")]
    public float travelDistance = 100f;
    
    [Header("Attack State")]
    public GameObject target;
    public float attackRange;
    public float waitTime;
    public bool isWaiting = true;
    
    [Header("Dashing")] 
    public float dashSpeed;
    public float dashDuration;
    public bool isDashing;
    
    [Header("States")]
    public AttackState attackState;
    public RoamingState roamingState;
    public DeathState deathState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        InitializeStates();
    }

    void Start()
    {
        ChangeState(roamingState);
    }

    void Update()
    {
        _currentState?.Update();
        debugString = _currentState.ToString();
    }

    private void InitializeStates()
    {
        attackState = new AttackState(this);
        roamingState = new RoamingState(this);
        deathState = new DeathState(this);
    }

    public void ChangeState(BaseState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }
    
    public IEnumerator WaitBeforeNextMove()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
    }
}
