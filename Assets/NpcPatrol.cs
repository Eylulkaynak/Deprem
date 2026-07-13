using UnityEngine;
using UnityEngine.AI;

public class NPCPatrol : MonoBehaviour
{
    public Transform[] waypoints;

    private NavMeshAgent agent;
    private int currentIndex;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[0].position);
        }
    }

    void Update()
    {
        if (waypoints.Length == 0)
            return;

        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            currentIndex++;

            if (currentIndex >= waypoints.Length)
                currentIndex = 0;

            agent.SetDestination(waypoints[currentIndex].position);
        }
    }
}