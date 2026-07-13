using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Bolum5PlayerController : MonoBehaviour
{
    public static Bolum5PlayerController Instance;

    private NavMeshAgent agent;
    private Animator animator;

    public bool IsMoving
    {
        get
        {
            if (agent == null) return false;

            return agent.pathPending ||
                   agent.remainingDistance > agent.stoppingDistance;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.isStopped = false;
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    public void MoveTo(Vector3 point)
    {
        if (agent == null)
            return;

        agent.isStopped = false;
        agent.SetDestination(point);
    }

    public void StopMovement()
    {
        if (agent == null)
            return;

        agent.isStopped = true;
    }

    public void ResumeMovement()
    {
        if (agent == null)
            return;

        agent.isStopped = false;
    }
}