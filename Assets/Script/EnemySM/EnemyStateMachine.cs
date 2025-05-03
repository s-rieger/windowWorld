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

    [Header("Dashing")] 
    public float dashSpeed;
    public float dashDuration;
    public bool isDashing;
    public float waitTime;
    
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
    
    public void StartDash()
    {
        if (!isDashing)
        {
            StartCoroutine(Dash());
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        float elapsedTime = 0f;

        Vector3 dashDirection = (target.transform.position - transform.position).normalized;
        while (elapsedTime < dashDuration)
        {
            Debug.Log("Dashing...");
            float distanceToTarget = Vector3.Distance(bug.position, target.transform.position);

            if (distanceToTarget <= 10f)
            {
                Debug.LogWarning("Bug is within the buffer distance to the target.");
                //Add further logic here for wht to do when attacking the playeer
            }
  

            Vector3 moveVector = dashDirection * dashSpeed * Time.deltaTime;
            agent.transform.LookAt(target.transform.position);
            agent.Move(moveVector);

            elapsedTime += Time.deltaTime;

            if (elapsedTime >= dashDuration)
            {
                break;
            }

            yield return null;
        }

        isDashing = false;
    }
}
