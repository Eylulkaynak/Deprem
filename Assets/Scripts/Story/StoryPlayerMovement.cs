using System;
using UnityEngine;
using UnityEngine.AI;

namespace Deprem.Story
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class StoryPlayerMovement : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float arrivalPadding = 0.08f;
        [Header("Reachable Target Resolution")]
        [SerializeField, Min(0.25f)] private float directSampleRadius = 1.8f;
        [SerializeField, Min(1f)] private float fallbackSearchRadius = 6f;
        [SerializeField, Range(2, 6)] private int fallbackSearchRings = 5;
        [SerializeField, Range(8, 24)] private int fallbackSamplesPerRing = 16;

        private NavMeshAgent agent;
        private Action onArrived;
        private bool navigationEnabled = true;
        private bool destinationPending;
        private float destinationRequestedAt;
        private int speedHash;
        private NavMeshPath reusablePath;
        private bool faceOnArrival;
        private Vector3 arrivalFacing;
        private bool movementConstrained;
        private Vector3 movementConstraintCenter;
        private float movementConstraintRadius;
        private bool storyInputLocked;
        private Vector3 activeDestination;

        public bool NavigationEnabled => navigationEnabled;
        public bool StoryInputLocked => storyInputLocked;
        public bool IsMoving => agent != null && agent.isOnNavMesh && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator ??= GetComponentInChildren<Animator>();
            speedHash = Animator.StringToHash(speedParameter);
            reusablePath = new NavMeshPath();
            agent.updateRotation = false;
        }

        private void Start()
        {
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);

            ApplyAgentLockState();
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            if (storyInputLocked)
            {
                if (!agent.isStopped)
                    agent.isStopped = true;
                if (agent.hasPath || destinationPending || agent.velocity.sqrMagnitude > 0.0001f)
                    Stop();
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

            if (destinationPending && !agent.pathPending &&
                HasActuallyReachedDestination())
            {
                agent.ResetPath();
                destinationPending = false;
                Action callback = onArrived;
                onArrived = null;
                if (faceOnArrival)
                {
                    Vector3 facing = arrivalFacing;
                    facing.y = 0f;
                    if (facing.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
                }
                faceOnArrival = false;
                callback?.Invoke();
            }
        }

        public bool TrySetDestination(Vector3 worldPosition)
        {
            return TrySetDestination(worldPosition, null);
        }

        public bool TrySetDestination(Vector3 worldPosition, Action arrivedCallback)
        {
            return TrySetDestinationInternal(worldPosition, arrivedCallback, false, Vector3.zero);
        }

        private bool TrySetDestinationInternal(Vector3 worldPosition, Action arrivedCallback, bool shouldFaceOnArrival, Vector3 facing)
        {
            if (storyInputLocked || !navigationEnabled || agent == null || !agent.isOnNavMesh)
                return false;

            if (!TryResolveReachableDestination(worldPosition, out Vector3 resolvedDestination))
                return false;

            reusablePath ??= new NavMeshPath();
            if (!agent.CalculatePath(resolvedDestination, reusablePath) || reusablePath.status != NavMeshPathStatus.PathComplete)
                return false;

            onArrived = arrivedCallback;
            faceOnArrival = shouldFaceOnArrival;
            arrivalFacing = facing;
            if (agent.isStopped)
                agent.isStopped = false;
            destinationPending = agent.SetPath(reusablePath);
            destinationRequestedAt = Time.time;
            activeDestination = resolvedDestination;
            if (!destinationPending)
            {
                onArrived = null;
                faceOnArrival = false;
            }
            return destinationPending;
        }

        private bool HasActuallyReachedDestination()
        {
            float threshold = agent.stoppingDistance + arrivalPadding;
            if (!float.IsInfinity(agent.remainingDistance) && !float.IsNaN(agent.remainingDistance) &&
                agent.remainingDistance <= threshold)
                return true;

            if (agent.hasPath)
                return false;

            Vector3 offset = activeDestination - agent.nextPosition;
            offset.y = 0f;
            return offset.sqrMagnitude <= (threshold + 0.2f) * (threshold + 0.2f);
        }

        /// <summary>
        /// Mobilya, duvar ya da NavMesh dışındaki bir noktaya dokunulduğunda hedefi reddetmek
        /// yerine, dokunulan yere en yakın ve oyuncunun gerçekten ulaşabildiği zemin noktasını bulur.
        /// Bu yalnızca dokunma/etkileşim anında çalışır; frame başına arama veya allocation üretmez.
        /// </summary>
        public bool TryResolveReachableDestination(Vector3 requestedPosition, out Vector3 resolvedPosition)
        {
            resolvedPosition = default;
            if (!navigationEnabled || agent == null || !agent.isOnNavMesh)
                return false;

            reusablePath ??= new NavMeshPath();
            Vector3 flattenedRequest = requestedPosition;
            flattenedRequest.y = agent.nextPosition.y;
            if (TryReachableSample(flattenedRequest, directSampleRadius, out resolvedPosition))
                return true;

            Vector3 searchCenter = flattenedRequest;
            float bestScore = float.PositiveInfinity;
            bool found = false;
            int rings = Mathf.Max(2, fallbackSearchRings);
            int samples = Mathf.Max(8, fallbackSamplesPerRing);

            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = fallbackSearchRadius * ring / rings;
                float sampleRadius = Mathf.Max(0.45f, radius / rings);
                for (int sample = 0; sample < samples; sample++)
                {
                    float angle = sample * Mathf.PI * 2f / samples;
                    Vector3 candidate = searchCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (!TryReachableSample(candidate, sampleRadius, out Vector3 reachable))
                        continue;

                    Vector3 delta = reachable - flattenedRequest;
                    delta.y = 0f;
                    float score = delta.sqrMagnitude;
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    resolvedPosition = reachable;
                    found = true;
                }
            }

            if (found)
                return true;

            // Çok uzaktaki bir duvara ya da duvarın arkasına dokunulduğunda, ışın çarpma noktası
            // NavMesh'ten tamamen kopuk olabilir. Oyuncudan dokunulan X/Z yönüne doğru geri tarayıp
            // erişilebilen en uzak noktayı seçmek, dokunmayı boşa çıkarmadan engelin önünde durdurur.
            Vector3 start = agent.nextPosition;
            start.y = flattenedRequest.y;
            for (int step = 9; step >= 1; step--)
            {
                Vector3 candidate = Vector3.Lerp(start, flattenedRequest, step / 10f);
                if (TryReachableSample(candidate, directSampleRadius, out resolvedPosition))
                    return true;
            }

            return false;
        }

        private bool TryReachableSample(Vector3 requestedPosition, float radius, out Vector3 reachablePosition)
        {
            reachablePosition = default;
            if (!NavMesh.SamplePosition(requestedPosition, out NavMeshHit sampled, radius, agent.areaMask))
                return false;
            if (!IsInsideMovementConstraint(sampled.position))
                return false;
            if (!agent.CalculatePath(sampled.position, reusablePath) || reusablePath.status != NavMeshPathStatus.PathComplete)
                return false;

            reachablePosition = sampled.position;
            return true;
        }

        private bool IsInsideMovementConstraint(Vector3 position)
        {
            if (!movementConstrained)
                return true;

            Vector3 offset = position - movementConstraintCenter;
            offset.y = 0f;
            return offset.sqrMagnitude <= movementConstraintRadius * movementConstraintRadius;
        }

        public bool MoveTo(Transform destination, Action arrivedCallback)
        {
            if (destination == null)
                return false;

            return TrySetDestinationInternal(destination.position, arrivedCallback, true, destination.forward);
        }

        public bool MoveTo(Transform destination, Vector3 lookAtPosition, Action arrivedCallback)
        {
            if (destination == null)
                return false;

            Vector3 facing = lookAtPosition - destination.position;
            return TrySetDestinationInternal(destination.position, arrivedCallback, true, facing);
        }

        public void FaceTowards(Vector3 worldPosition)
        {
            Vector3 facing = worldPosition - transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        }

        public void SetMovementConstraint(Vector3 center, float radius)
        {
            movementConstraintCenter = center;
            movementConstraintRadius = Mathf.Max(0.5f, radius);
            movementConstrained = true;
        }

        public void ClearMovementConstraint()
        {
            movementConstrained = false;
        }

        public void SetNavigationEnabled(bool enabled)
        {
            navigationEnabled = enabled;
            if (!enabled)
                Stop();
        }

        public void SetStoryInputLocked(bool locked)
        {
            if (storyInputLocked == locked)
            {
                ApplyAgentLockState();
                return;
            }

            storyInputLocked = locked;
            if (locked)
                Stop();
            ApplyAgentLockState();
        }

        public void Stop()
        {
            onArrived = null;
            destinationPending = false;
            faceOnArrival = false;
            if (agent != null && agent.isOnNavMesh)
                agent.ResetPath();
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetFloat(speedHash, 0f);
        }

        public void Warp(Vector3 worldPosition)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.Warp(worldPosition);
            else
                transform.position = worldPosition;
        }

        private void ApplyAgentLockState()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            agent.isStopped = storyInputLocked;
            if (!storyInputLocked)
                return;

            if (agent.hasPath)
                agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }
}
