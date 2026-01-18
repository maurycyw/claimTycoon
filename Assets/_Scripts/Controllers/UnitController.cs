using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using ClaimTycoon.Systems.Units;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Buildings;
using ClaimTycoon.Systems.Units.Jobs;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Controllers
{
    public enum UnitState { Idle, Moving, Working, AutoMining }
    public enum JobType { None, Mine, DropDirt, FeedSluice, Build, CleanSluice }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(AutoMiner))]
    [RequireComponent(typeof(UnitAnimationController))]
    [RequireComponent(typeof(UnitThoughtController))]
    public class UnitController : MonoBehaviour
    {
        private NavMeshAgent agent;
        private CharacterStats stats;
        private AutoMiner autoMiner;

        private UnitAnimationController animController;
        private UnitThoughtController thoughtController;
        
        [Header("State")]
        [SerializeField] private UnitState currentState = UnitState.Idle;
        public bool IsCarryingDirt { get; private set; } 

        // New Job System
        private IJob activeJob = null;
        private IJob pausedJob = null;

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

            animController = GetComponent<UnitAnimationController>();
            if (animController == null) animController = gameObject.AddComponent<UnitAnimationController>();

            thoughtController = GetComponent<UnitThoughtController>();
            if (thoughtController == null) thoughtController = gameObject.AddComponent<UnitThoughtController>();

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
            // Do NOT StopAutoMining here. Moving pauses the active job, and we want to resume logic later.
            // StopAutoMining(); 
            StopAllCoroutines();
            jobQueue.Clear(); 
            
            // If we have an active job, we are interrupting it -> Pause it
            if (activeJob != null)
            {
                PauseActiveJob();
            }

            // Ensure paused job updates if we move again while paused (optional, but good for safety)
            if (pausedJob != null)
            {
                 // Keep it paused.
            }

            // Don't set activeJob to null here if we just paused it? 
            // Actually, for the logic of Move:
            // The unit IS now Moving (statE), so it is NOT Working.
            // activeJob reference generally means "currently executing job".
            // If we pause it, we should probably clear activeJob but keep it in pausedJob.
            
            activeJob = null; 
            currentState = UnitState.Moving;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        private void PauseActiveJob()
        {
            if (activeJob != null)
            {
                pausedJob = activeJob;
                if (JobManager.Instance != null)
                {
                    JobManager.Instance.SetJobPaused(pausedJob, true);
                }
                Debug.Log($"[UnitController] Paused Job: {pausedJob.Type}");
                // We do NOT unregister it, just pause it.
            }
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
                animController.SetWorking(false);
                Debug.Log("[UnitController] Auto Mining Stopped.");
            }
        }

        public void SetCarryingDirt(bool val)
        {
            IsCarryingDirt = val;
            animController.SetCarrying(val);
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
                // If we start a NEW job, we should clear any paused job (unless this IS the resumption, handled nicely if we call this with pausedJob)
                if (pausedJob != null && pausedJob != job)
                {
                     // User started a DIFFERENT job. Correct logic: Cancel the paused one?
                     // Prompt says: "or they use shift+right click again and setup the job from anew"
                     // This implies old paused job is discarded.
                     if (JobManager.Instance != null) JobManager.Instance.UnregisterJob(pausedJob);
                     pausedJob = null;
                }

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
            animController.SetWorking(false);
            
            // Register with JobManager (Safe to call even if already registered)
            if (JobManager.Instance != null)
            {
                JobManager.Instance.RegisterJob(activeJob, this);
                JobManager.Instance.SetJobPaused(activeJob, false); // Ensure not paused
            }

            Debug.Log($"Unit Starting Job: {activeJob.Type} at {activeJob.TargetCoord}. Moving to {activeJob.StandPosition}");
            thoughtController.ShowThought($"Doing {activeJob.Type}...");

            currentState = UnitState.Moving;
            stuckTimer = 0f;
            lastPosition = transform.position;
            agent.isStopped = false;
            
            bool pathSet = agent.SetDestination(activeJob.StandPosition);

            if (!pathSet)
            {
                Debug.LogError("Agent SetDestination Failed! Is the target on the NavMesh?");
                currentState = UnitState.Idle;
                // If failed, unregister
                if (JobManager.Instance != null) JobManager.Instance.UnregisterJob(activeJob);
                activeJob = null;
                CheckQueue(); 
            }
            else
            {
                activeJob.OnEnter(this);
            }
        }

        public void CancelJob(IJob jobToCancel)
        {
            if (activeJob == jobToCancel)
            {
                Debug.Log($"[UnitController] Cancelling active job: {activeJob.Type}");
                StopAllCoroutines();
                agent.isStopped = true;
                agent.ResetPath();
                activeJob.OnExit(this); // Allow job to clean up
                if (JobManager.Instance != null) JobManager.Instance.UnregisterJob(activeJob);
                activeJob = null;
                currentState = UnitState.Idle; // Will become AutoMining if not stopped?
                StopAutoMining(); // Ensure we stop the loop if canceling active
                animController.SetWorking(false);
                CheckQueue();
            }
            else if (pausedJob == jobToCancel)
            {
                Debug.Log($"[UnitController] Cancelling paused job: {pausedJob.Type}");
                if (JobManager.Instance != null) JobManager.Instance.UnregisterJob(pausedJob);
                pausedJob = null;
                StopAutoMining(); // If we cancel, we assume user wants to stop the whole auto process
            }
            else if (jobQueue.Contains(jobToCancel))
            {
                Debug.Log($"[UnitController] Cancelling queued job: {jobToCancel.Type}");
                // Rebuild queue without the job to cancel
                Queue<IJob> newQueue = new Queue<IJob>();
                while (jobQueue.Count > 0)
                {
                    IJob job = jobQueue.Dequeue();
                    if (job != jobToCancel)
                    {
                        newQueue.Enqueue(job);
                    }
                    else
                    {
                        if (JobManager.Instance != null) JobManager.Instance.UnregisterJob(job);
                    }
                }
                jobQueue = newQueue;
            }
            else
            {
                Debug.LogWarning($"[UnitController] Attempted to cancel job not found: {jobToCancel.Type}");
            }
        }

        public void ResumeJob()
        {
            if (pausedJob != null)
            {
                Debug.Log($"[UnitController] Resuming paused job: {pausedJob.Type}");
                IJob jobToResume = pausedJob;
                pausedJob = null; // Clear paused job reference
                ExecuteJob(jobToResume); // Start executing it again
            }
            else
            {
                Debug.LogWarning("[UnitController] No job to resume.");
            }
        }

        public void ResumeJob(IJob job)
        {
            if (pausedJob == job)
            {
                ResumeJob();
            }
            else if (pausedJob == null)
            {
                // This might happen if state got desynced or loaded from save without setting pausedJob?
                Debug.LogWarning($"[UnitController] Requested to resume {job.Type} but no job is paused locally. Force executing.");
                ExecuteJob(job);
            }
            else
            {
                Debug.LogError($"[UnitController] Mismatch! Requested to resume {job.Type} but local paused job is {pausedJob.Type}. Ignoring.");
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
                if (activeJob != null)
                {
                    activeJob.OnExit(this); // Allow job to clean up
                    if (JobManager.Instance != null) JobManager.Instance.UnregisterJob(activeJob);
                }
                activeJob = null;
                currentState = currentState == UnitState.AutoMining ? UnitState.AutoMining : UnitState.Idle;
                
                 if (currentState == UnitState.AutoMining)
                {
                    autoMiner.StopMining(); // Or retry logic
                }
                CheckQueue(); // Check for next job if current one failed
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
                animController.SetWorking(true, GetWorkType(activeJob.Type));
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
                thoughtController.ShowThought("Done!");
                
                if (JobManager.Instance != null) JobManager.Instance.UnregisterJob(activeJob);

                activeJob = null;
                animController.SetWorking(false);
            }

            // Fix: Check if Auto Miner is active to resume loop
            if (autoMiner != null && autoMiner.IsActive)
            {
                currentState = UnitState.AutoMining;
            }
            else
            {
                currentState = UnitState.Idle;
                animController.SetWorking(false);
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

        private int GetWorkType(JobType jobType)
        {
            switch (jobType)
            {
                case JobType.Mine: return 1;
                case JobType.DropDirt: return 2;
                case JobType.FeedSluice: return 3;
                case JobType.Build: return 4;
                case JobType.CleanSluice: return 5;
                default: return 0;
            }
        }
    }
}
