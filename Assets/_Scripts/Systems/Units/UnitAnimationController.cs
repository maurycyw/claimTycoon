using UnityEngine;
using UnityEngine.AI;

namespace ClaimTycoon.Systems.Units
{
    [RequireComponent(typeof(Animator))]
    public class UnitAnimationController : MonoBehaviour
    {
        private Animator animator;
        private NavMeshAgent agent;

        // Animator Parameter Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsWorkingHash = Animator.StringToHash("IsWorking");
        private static readonly int WorkTypeHash = Animator.StringToHash("WorkType");
        private static readonly int IsCarryingHash = Animator.StringToHash("IsCarrying");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (agent != null && animator != null)
            {
                // Normalize speed based on agent's max speed (approximate)
                float speed = agent.velocity.magnitude / agent.speed;
                animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
            }
        }

        public void SetWorking(bool isWorking, int workType = 0)
        {
            if (animator == null) return;
            
            animator.SetBool(IsWorkingHash, isWorking);
            if (isWorking)
            {
                animator.SetInteger(WorkTypeHash, workType);
            }
            else
            {
                animator.SetInteger(WorkTypeHash, 0);
            }
        }

        public void SetCarrying(bool isCarrying)
        {
            if (animator == null) return;
            // Debug.Log($"[UnitAnimationController] Setting IsCarrying: {isCarrying}");
            animator.SetBool(IsCarryingHash, isCarrying);
        }

        [ContextMenu("Toggle Carrying")]
        public void ToggleCarryingDebug()
        {
            bool current = animator.GetBool(IsCarryingHash);
            SetCarrying(!current);
            Debug.Log($"[Debug] Toggled IsCarrying to {!current}");
        }
        
        // Helper to visualize the state in editor
        private void OnValidate()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
        }
    }
}
