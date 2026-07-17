using UnityEngine;
using UnityEngine.AI;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class StorySiblingFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField, Min(0.5f)] private float followDistance = 1.05f;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.22f;
        [SerializeField, Min(0.01f)] private float repathDistance = 0.28f;
        [SerializeField, Min(1f)] private float turnSpeed = 11f;

        private NavMeshAgent agent;
        private int speedHash;
        private float nextRepathAt;
        private Vector3 lastTargetPosition;
        private bool following;
        private StoryPlayerMovement targetMovement;

        public Transform Target => target;
        public bool IsFollowing => following;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator ??= GetComponentInChildren<Animator>();
            targetMovement = target != null ? target.GetComponent<StoryPlayerMovement>() : null;
            speedHash = Animator.StringToHash(speedParameter);
            agent.updateRotation = false;
            agent.stoppingDistance = followDistance;
        }

        private void Start()
        {
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            targetMovement ??= target != null ? target.GetComponent<StoryPlayerMovement>() : null;
            if (targetMovement != null && targetMovement.StoryInputLocked)
            {
                StopFollowingMotion();
                return;
            }

            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetFloat(speedHash, velocity.magnitude, 0.12f, Time.deltaTime);

            if (!following || target == null)
                return;

            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= followDistance * followDistance)
            {
                if (agent.hasPath)
                    agent.ResetPath();
                return;
            }

            if (Time.time < nextRepathAt && (target.position - lastTargetPosition).sqrMagnitude < repathDistance * repathDistance)
                return;

            nextRepathAt = Time.time + repathInterval;
            lastTargetPosition = target.position;
            if (NavMesh.SamplePosition(target.position, out NavMeshHit sampled, 1.25f, agent.areaMask))
                agent.SetDestination(sampled.position);
        }

        public void SetFollowing(bool enabled)
        {
            following = enabled;
            nextRepathAt = 0f;
            if (!enabled)
                StopFollowingMotion();
        }

        private void StopFollowingMotion()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetFloat(speedHash, 0f);
        }
    }
}
