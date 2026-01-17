using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using ClaimTycoon.Systems.Units;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Buildings;
using ClaimTycoon.Managers;
using ClaimTycoon.Systems.Units.Jobs;

namespace ClaimTycoon.Controllers
{
    public enum UnitState { Idle, Moving, Working, AutoMining }
    public enum JobType { None, Mine, DropDirt, FeedSluice, Build, CleanSluice }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(AutoMiner))]
    public class UnitController : MonoBehaviour
    {
        private NavMeshAgent agent;
        private CharacterStats stats;
        private AutoMiner autoMiner;
        
        [Header("State")]
        [SerializeField] private UnitState currentState = UnitState.Idle;
        public bool IsCarryingDirt { get; private set; } 

        // New Job System
        private IJob activeJob = null;

        // Stuck Detection
        private float stuckTimer = 0f;
        private Vector3 lastPosition;
        private const float STUCK_TIMEOUT = 3.0f;
        private const float MOVE_THRESHOLD = 0.1f;

        // Job Queue
        private Queue<IJob> jobQueue = new Queue<IJob>();

        public bool IsProcessingJob => currentState == UnitState.Working || currentState == UnitState.Moving;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stats = GetComponent<CharacterStats>();
            autoMiner = GetComponent<AutoMiner>();
            
            if (autoMiner == null) autoMiner = gameObject.AddComponent<AutoMiner>();

            if (agent != null) agent.enabled = false;
        }

        private void Start()
        {
            StartCoroutine(InitializeAgent());
        }

        private IEnumerator InitializeAgent()
        {
            yield return null;
            yield return null;

            if (agent != null)
            {
                agent.enabled = true;
                
                Vector3Int center = new Vector3Int(TerrainManager.Instance.GridSize.x / 2, 0, TerrainManager.Instance.GridSize.y / 2);
                Vector3? spawnPos = FindSafeSpawnPoint(center);

                if (spawnPos.HasValue)
                {
                    agent.Warp(spawnPos.Value);
                    Debug.Log($"[UnitController] Player spawned at closest safe point: {spawnPos.Value}");
                }
                else if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.LogWarning("[UnitController] Could not find safe center spawn. Warped to closest NavMesh point.");
                }
                
                Stat moveStat = stats.GetStat(StatType.MoveSpeed);
                if (moveStat != null)
                    agent.speed = moveStat.value;
                else
                    agent.speed = 3.5f; 

                agent.stoppingDistance = 0.2f;
                Debug.Log("Unit Agent Initialized. Stopping Distance set to 0.2f.");
                
                CameraController cam = FindFirstObjectByType<CameraController>();
                if (cam != null)
                {
                    cam.FocusOn(agent.transform.position);
                }
            }
        }

        private Vector3? FindSafeSpawnPoint(Vector3Int center)
        {
            int maxRadius = Mathf.Max(TerrainManager.Instance.GridSize.x, TerrainManager.Instance.GridSize.y) / 2;
            
            for (int r = 0; r <= maxRadius; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    for (int z = -r; z <= r; z++)
                    {
                        if (Mathf.Abs(x) != r && Mathf.Abs(z) != r) continue;

                        Vector3Int checkPos = center + new Vector3Int(x, 0, z);
                        
                        if (TerrainManager.Instance.TryGetTile(checkPos, out TileType type))
                        {
                            float waterDepth = 0f;
                            if (WaterManager.Instance != null)
                            {
                                waterDepth = WaterManager.Instance.GetWaterDepth(checkPos.x, checkPos.z);
                            }

                            if (waterDepth < 0.1f) 
                            {
                                float height = TerrainManager.Instance.GetHeight(checkPos.x, checkPos.z);
                                float cellSize = TerrainManager.Instance.CellSize;
                                Vector3 worldPos = new Vector3(checkPos.x * cellSize + cellSize/2, height, checkPos.z * cellSize + cellSize/2);
                                
                                if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                                {
                                    return hit.position;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public void MoveTo(Vector3 destination)
        {
            StopAutoMining();
            StopAllCoroutines();
            jobQueue.Clear(); 
            
            activeJob = null;
            currentState = UnitState.Moving;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        public void StartAutoMining(Vector3Int center, SluiceBox sluice)
        {
            StopAutoMining(); 
            currentState = UnitState.AutoMining;
            autoMiner.StartMining(center, sluice);
        }

        public void StopAutoMining()
        {
            if (currentState == UnitState.AutoMining)
            {
                autoMiner.StopMining();
                currentState = UnitState.Idle;
                Debug.Log("[UnitController] Auto Mining Stopped.");
            }
        }

        public void SetCarryingDirt(bool val)
        {
            IsCarryingDirt = val;
        }

        // --- BACKWARD COMPATIBILITY WRAPPER ---
        public void StartJob(JobType jobType, Vector3Int targetCoord, Vector3 standPosition, SluiceBox sluice = null, ConstructionSite site = null)
        {
            IJob job = null;
            switch(jobType)
            {
                case JobType.Mine: job = new MineJob(targetCoord, standPosition); break;
                case JobType.DropDirt: job = new DropDirtJob(targetCoord, standPosition); break;
                case JobType.FeedSluice: job = new FeedSluiceJob(targetCoord, standPosition, sluice); break;
                case JobType.Build: job = new BuildJob(targetCoord, standPosition, site); break;
                case JobType.CleanSluice: job = new CleanSluiceJob(targetCoord, standPosition, sluice); break;
            }

            if (job != null) StartJob(job);
        }

        // --- NEW JOB SYSTEM ---
        public void StartJob(IJob job)
        {
            if ((currentState == UnitState.Idle || currentState == UnitState.AutoMining) && activeJob == null)
            {
                ExecuteJob(job);
            }
            else
            {
                jobQueue.Enqueue(job);
                Debug.Log($"[UnitController] Job Queued: {job.GetType().Name}. Queue Size: {jobQueue.Count}");
            }
        }

        private void ExecuteJob(IJob job)
        {
            StopAllCoroutines();
            activeJob = job;
            
            Debug.Log($"Unit Starting Job: {activeJob.Type} at {activeJob.TargetCoord}. Moving to {activeJob.StandPosition}");

            currentState = UnitState.Moving;
            stuckTimer = 0f;
            lastPosition = transform.position;
            agent.isStopped = false;
            
            bool pathSet = agent.SetDestination(activeJob.StandPosition);

            if (!pathSet)
            {
                Debug.LogError("Agent SetDestination Failed! Is the target on the NavMesh?");
                currentState = UnitState.Idle;
                activeJob = null;
                CheckQueue(); 
            }
            else
            {
                activeJob.OnEnter(this);
            }
        }

        private void Update()
        {
            if (currentState == UnitState.AutoMining)
            {
                autoMiner.DecideNextAction();
            }

            if (currentState == UnitState.Moving)
            {
                if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)  return;

                if (!agent.pathPending)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    {
                        ForceStopAndArrive();
                        return;
                    }

                    if (agent.velocity.sqrMagnitude < 0.01f && agent.remainingDistance < 0.5f)
                    {
                         ForceStopAndArrive();
                         return;
                    }
                }
                
                if (Vector3.Distance(transform.position, lastPosition) < MOVE_THRESHOLD)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > STUCK_TIMEOUT)
                    {
                        Debug.LogWarning("[UnitController] Unit STUCK! Resetting state.");
                        ForceStopAndArrive(true); 
                        return;
                    }
                }
                else
                {
                     stuckTimer = 0f;
                     lastPosition = transform.position;
                }
                
                if (!agent.pathPending && (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid))
                {
                      if (agent.remainingDistance > 2.0f)
                     {
                         Debug.LogError("[UnitController] Path Invalid or Partial AND far from target. Aborting Job.");
                         ForceStopAndArrive(true);
                         return;
                     }
                }
                
                if (Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance + 0.2f)
                {
                    ForceStopAndArrive();
                }
            }
            else if (currentState == UnitState.Working && activeJob != null)
            {
                activeJob.Update(this);
                if (activeJob.IsComplete())
                {
                    CompleteJob();
                }
            }
        }

        private void ForceStopAndArrive(bool failure = false)
        {
            if (!agent.isStopped) 
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            
            if (failure)
            {
                Debug.Log("[UnitController] Movement Failed/Stuck. Cancelling Job.");
                activeJob = null;
                currentState = currentState == UnitState.AutoMining ? UnitState.AutoMining : UnitState.Idle;
                
                 if (currentState == UnitState.AutoMining)
                {
                    autoMiner.StopMining(); // Or retry logic
                }
            }
            else
            {
                Arrived();
            }
        }

        private void Arrived()
        {
            if (activeJob != null)
            {
                currentState = UnitState.Working;
                // Job continues in Update loop -> calls activeJob.Update()
            }
            else
            {
                currentState = UnitState.Idle;
                CheckQueue();
            }
        }

        private void CompleteJob()
        {
            if (activeJob != null)
            {
                activeJob.OnExit(this);
                Debug.Log($"[UnitController] Completed Job: {activeJob.Type}");
                activeJob = null;
            }

            // Fix: Check if Auto Miner is active to resume loop
            if (autoMiner != null && autoMiner.IsActive)
            {
                currentState = UnitState.AutoMining;
            }
            else
            {
                currentState = UnitState.Idle;
            }
             
            // If AutoMining, next Update will trigger autoMiner.DecideNextAction() because local activeJob is null
            // Check Queue first (Manual commands override auto mining usually, but here we queue strictly)
            CheckQueue();
        }

        private void CheckQueue()
        {
            if (jobQueue.Count > 0)
            {
                IJob nextJob = jobQueue.Dequeue();
                ExecuteJob(nextJob);
            }
        }
    }
}
