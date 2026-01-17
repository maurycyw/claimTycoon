using UnityEngine;
using UnityEngine.AI;
using ClaimTycoon.Controllers;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Buildings;
using ClaimTycoon.Systems.Units.Jobs;

namespace ClaimTycoon.Systems.Units
{
    public class AutoMiner : MonoBehaviour
    {
        private UnitController unit;
        private Vector3Int mineCenter;
        private SluiceBox assignedSluice;
        private bool loopActive = false;

        public bool IsActive => loopActive;

        private void Awake()
        {
            unit = GetComponent<UnitController>();
        }

        public void StartMining(Vector3Int center, SluiceBox sluice)
        {
            mineCenter = center;
            assignedSluice = sluice;
            loopActive = true;
            Debug.Log($"[AutoMiner] Started loop at {center}");
            DecideNextAction();
        }

        public void StopMining()
        {
            loopActive = false;
        }

        public void DecideNextAction()
        {
            if (!loopActive) return;
            if (unit.IsProcessingJob) return; // Wait for current job

            if (unit.IsCarryingDirt)
            {
                // Go to Sluice
                if (assignedSluice.IsFull)
                {
                    unit.StartJob(new CleanSluiceJob(Vector3Int.zero, assignedSluice.GetInteractionPosition(), assignedSluice));
                }
                else
                {
                    unit.StartJob(new FeedSluiceJob(Vector3Int.zero, assignedSluice.GetInteractionPosition(), assignedSluice));
                }
            }
            else
            {
                // Dig
                if (TryFindNextDigTarget(out Vector3Int targetGrid, out Vector3 targetPos))
                {
                    unit.StartJob(new MineJob(targetGrid, targetPos));
                }
                else
                {
                    Debug.LogWarning("[AutoMiner] No dirt found. Stopping.");
                    StopMining();
                    unit.StopAutoMining(); // Notify main controller
                }
            }
        }

        private bool TryFindNextDigTarget(out Vector3Int gridPos, out Vector3 worldPos)
        {
            gridPos = Vector3Int.zero;
            worldPos = Vector3.zero;

            int maxRadius = 10;
            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();

            for (int r = 0; r <= maxRadius; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    for (int z = -r; z <= r; z++)
                    {
                        if (Mathf.Abs(x) != r && Mathf.Abs(z) != r) continue;

                        Vector3Int checkPos = mineCenter + new Vector3Int(x, 0, z);
                        
                        if (TerrainManager.Instance.TryGetTile(checkPos, out TileType type))
                        {
                            if (type == TileType.Dirt)
                            {
                                float y = TerrainManager.Instance.GetHeight(checkPos.x, checkPos.z);
                                Vector3 rawWorldPos = new Vector3(checkPos.x * TerrainManager.Instance.CellSize + TerrainManager.Instance.CellSize/2, y, checkPos.z * TerrainManager.Instance.CellSize + TerrainManager.Instance.CellSize/2);
                                
                                if (NavMesh.SamplePosition(rawWorldPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                                {
                                    NavMeshPath path = new NavMeshPath();
                                    if (agent.CalculatePath(hit.position, path))
                                    {
                                        if (path.status == NavMeshPathStatus.PathComplete)
                                        {
                                            gridPos = checkPos;
                                            worldPos = hit.position;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
