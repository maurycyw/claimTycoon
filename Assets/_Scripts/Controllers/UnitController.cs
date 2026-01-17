using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using ClaimTycoon.Systems.Units;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Buildings; // Added for SluiceBox
using ClaimTycoon.Managers;

namespace ClaimTycoon.Controllers
{
    public enum UnitState { Idle, Moving, Working }
    public enum JobType { None, Mine, DropDirt, FeedSluice, Build }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterStats))]
    public class UnitController : MonoBehaviour
    {
        private NavMeshAgent agent;
        private CharacterStats stats;
        
        [Header("State")]
        [SerializeField] private UnitState currentState = UnitState.Idle;
        public bool IsCarryingDirt { get; private set; } 

        private JobType currentJob = JobType.None;
        private Vector3Int jobTargetCoord;
        private SluiceBox targetSluice; 
        private ConstructionSite targetSite; // New Target

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stats = GetComponent<CharacterStats>();
            
            // Disable agent initially to prevent "Failed to create agent" if NavMesh isn't ready
            if (agent != null) agent.enabled = false;
        }

        private void Start()
        {
            // Wait for Terrain/NavMesh to build
            StartCoroutine(InitializeAgent());
        }

        private IEnumerator InitializeAgent()
        {
            // Wait 2 frames to ensure NavMeshSurface.BuildNavMesh() has finished
            yield return null;
            yield return null;

            if (agent != null)
            {
                agent.enabled = true;
                
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                
                Stat moveStat = stats.GetStat(StatType.MoveSpeed);
                if (moveStat != null)
                    agent.speed = moveStat.value;
                else
                    agent.speed = 3.5f; 

                agent.stoppingDistance = 0.2f; // Reduced from 1.0f
                Debug.Log("Unit Agent Initialized. Stopping Distance set to 0.2f.");
            }
        }

        public void MoveTo(Vector3 destination)
        {
            StopAllCoroutines();
            jobQueue.Clear(); // User command overrides all
            if (currentJob != JobType.None)
            {
                 Debug.LogWarning($"[UnitController] MoveTo called while Job {currentJob} was active! Cancelling Job. Trace: {System.Environment.StackTrace}");
            }
            
            currentJob = JobType.None;
            currentState = UnitState.Moving;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        // Job Queue System
        private struct JobData
        {
            public JobType jobType;
            public Vector3Int targetCoord;
            public Vector3 standPosition;
            public SluiceBox sluice;
            public ConstructionSite site;
        }

        private Queue<JobData> jobQueue = new Queue<JobData>();

        public void StartJob(JobType job, Vector3Int targetCoord, Vector3 standPosition, SluiceBox sluice = null, ConstructionSite site = null)
        {
            // Create Job Data
            JobData newJob = new JobData
            {
                jobType = job,
                targetCoord = targetCoord,
                standPosition = standPosition,
                sluice = sluice,
                site = site
            };

            // If completely Idle, start immediately. 
            // BUT if Moving to a job, or Working, we queue.
            // Be careful: 'Moving' could be a user command (MoveTo). 
            // If user explicitly moves, we cleared queue in MoveTo.
            
            if (currentState == UnitState.Idle && currentJob == JobType.None)
            {
                ExecuteJob(newJob);
            }
            else
            {
                jobQueue.Enqueue(newJob);
                Debug.Log($"[UnitController] Job Queued: {job}. Queue Size: {jobQueue.Count}");
            }
        }

        private void ExecuteJob(JobData job)
        {
            StopAllCoroutines();
            currentJob = job.jobType;
            jobTargetCoord = job.targetCoord;
            targetSluice = job.sluice;
            targetSite = job.site;
            
            Debug.Log($"Unit Starting Job: {currentJob} at {jobTargetCoord}. Moving to {job.standPosition}");

            // Move to position first
            currentState = UnitState.Moving;
            agent.isStopped = false;
            
            if (!agent.SetDestination(job.standPosition))
            {
                Debug.LogError("Agent SetDestination Failed! Is the target on the NavMesh?");
                currentState = UnitState.Idle;
                CheckQueue(); // Try next if this failed
            }
        }

        private void Update()
        {
            if (currentState == UnitState.Moving)
            {
                if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) {
                     return;
                }

                if (!agent.pathPending)
                {
                    // Check 1: Distance within tolerance
                    if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    {
                        ForceStopAndArrive();
                        return;
                    }

                    // Check 2: Agent has stopped moving (and is reasonably close)
                    if (agent.velocity.sqrMagnitude < 0.01f && agent.remainingDistance < 0.5f)
                    {
                         Debug.Log("Arrived via Velocity Check (Stopped near target).");
                         ForceStopAndArrive();
                         return;
                    }
                }
                
                // FALLBACK: Manual Geometric Distance Check
                if (Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance + 0.2f)
                {
                    Debug.Log("Arrived via Manual Distance Check.");
                    ForceStopAndArrive();
                }
            }
        }

        private void ForceStopAndArrive()
        {
            if (!agent.isStopped) 
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            Arrived();
        }

        private void Arrived()
        {
             // Reached destination
            if (currentJob != JobType.None)
            {
                Debug.Log($"[UnitController] Arrived at Job Site. Starting Job {currentJob}...");
                StartCoroutine(ExecuteJobRoutine());
            }
            else
            {
                Debug.Log("[UnitController] Arrived. State -> Idle");
                currentState = UnitState.Idle;
            }
        }

        private IEnumerator ExecuteJobRoutine()
        {
            currentState = UnitState.Working;
            // Face target?
            
            yield return new WaitForSeconds(1.0f); // Work duration

            CompleteJob();
        }

        private void CompleteJob()
        {
            if (currentJob == JobType.Mine)
            {
                // Verify tile still exists
                if (TerrainManager.Instance.TryGetTile(jobTargetCoord, out TileType type))
                {
                    Debug.Log($"[UnitController] Completing Mine Job on {type} at {jobTargetCoord}");
                    
                    // Dig DOWN
                    Vector3 targetPos = new Vector3(jobTargetCoord.x * TerrainManager.Instance.CellSize, 0, jobTargetCoord.z * TerrainManager.Instance.CellSize);
                    
                    TerrainManager.Instance.ModifyHeight(targetPos, -0.5f); // Dig down 0.5m
                    
                    // Pick up Dirt
                    IsCarryingDirt = true;
                    Debug.Log("[UnitController] Job Complete: Mined Dirt. Holding Dirt.");
                }
                else
                {
                    Debug.LogError("[UnitController] Job Failed: Tile no longer exists or is invalid!");
                }
            }
            else if (currentJob == JobType.DropDirt)
            {
                 // Drop Dirt on Terrain -> Raises Terrain
                float cellSize = TerrainManager.Instance.CellSize;
                Vector3 targetPos = new Vector3(jobTargetCoord.x * cellSize, 0, jobTargetCoord.z * cellSize);
                
                TerrainManager.Instance.ModifyHeight(targetPos, 0.5f); // Raise 0.5m
                
                IsCarryingDirt = false;
                Debug.Log("[UnitController] Job Complete: Dropped Dirt on Terrain.");
            }
            else if (currentJob == JobType.FeedSluice)
            {
                // Feed Sluice -> No Terrain Change
                if (targetSluice != null)
                {
                    targetSluice.AddDirt(0.5f); // Add dirt volume
                    IsCarryingDirt = false;
                    Debug.Log("[UnitController] Job Complete: Fed Sluice Box.");
                }
                else
                {
                    Debug.LogError("Target Sluice is null!");
                }
            }
            else if (currentJob == JobType.Build)
            {
                if (targetSite != null)
                {
                    targetSite.CompleteConstruction();
                    Debug.Log("[UnitController] Job Complete: Built Structure.");
                }
                else
                {
                    Debug.LogError("Target ConstructionSite is null!");
                }
            }

            currentJob = JobType.None;
            currentState = UnitState.Idle;
            
            CheckQueue();
        }

        private void CheckQueue()
        {
            if (jobQueue.Count > 0)
            {
                JobData nextJob = jobQueue.Dequeue();
                Debug.Log($"[UnitController] Dequeueing Job: {nextJob.jobType}. Remaining: {jobQueue.Count}");
                ExecuteJob(nextJob);
            }
        }
    }
}
