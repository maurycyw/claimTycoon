using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Systems.Buildings
{
    using ClaimTycoon.Systems.Terrain;
    using ClaimTycoon.Controllers;

    public class SluiceBox : MonoBehaviour
    {
        [Header("Production Settings")]
        [SerializeField] private float goldPerDirt = 0.5f;
        [SerializeField] private float storedDirt = 0f;
        [SerializeField] private float maxDirtCapacity = 5.0f;
        [SerializeField] private float accumulatedGold = 0f;

        public bool IsFull => storedDirt >= maxDirtCapacity;
        
        [Header("Visualization")]
        [SerializeField] private GameObject warningBubble; // Assign a red question mark/exclamation bubble here
        [SerializeField] private float minWaterDepth = 0.1f;

        private bool isValidPlacement = false;

        private void Start()
        {
            if (warningBubble != null) warningBubble.SetActive(false);
            InvokeRepeating(nameof(CheckStatus), 1.0f, 1.0f); // Check every second
        }

        private void CheckStatus()
        {
            if (TerrainManager.Instance == null) return;

            // Check if we are in water
            float cellSize = TerrainManager.Instance.CellSize;
            Vector3 worldPos = transform.position;
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int z = Mathf.RoundToInt(worldPos.z / cellSize);

            float terrainHeight = TerrainManager.Instance.GetHeight(x, z);
            // Ideally call WaterManager.Instance.GetWaterDepth(x, z);
            // I'll assume valid if terrain height is significantly below 1.8 (water level)
            float waterSurfaceLevel = 1.8f; 
            bool hasWater = false;
            
            if ((waterSurfaceLevel - terrainHeight) >= minWaterDepth)
            {
                 hasWater = true;
            }

            isValidPlacement = hasWater;

            if (warningBubble != null)
                warningBubble.SetActive(!isValidPlacement);
        }

        public void AddDirt(float amount)
        {
            if (!isValidPlacement)
            {
                Debug.LogWarning("SluiceBox: Cannot add dirt, not in water!");
                return;
            }

            if (IsFull)
            {
                Debug.LogWarning("SluiceBox: Full! Cannot add more dirt.");
                return;
            }
            
            storedDirt += amount;
            Debug.Log($"SluiceBox: Added {amount} Dirt. Total: {storedDirt}");
            
            // Process immediately or over time? 
            // "cleanup which takes time but is when the gold is finally tallied"
            // So we just store dirt for now? Or convert to accumulated gold?
            // "remove automatic pay... based off cubic meters fed"
            // "cleanup... takes time... when gold is tallied"
            
            // Let's verify: Dirt -> [Processing...] -> Accumulated Gold -> [Cleanup] -> Wallet
            // For simplicity: Feeding it instantly converts to "Potential Gold" (Accumulated)
            // But you can't get it until Cleanup.
            
            ProcessDirt(amount);
        }

        private void ProcessDirt(float amount)
        {
             accumulatedGold += amount * goldPerDirt;
        }

        public void Interact() // Called by player tap
        {
            if (accumulatedGold > 0)
            {
                StartCoroutine(CleanupRoutine());
            }
            else
            {
                Debug.Log("SluiceBox: Nothing to clean up.");
            }
        }

        public void CleanSluice()
        {
            if (accumulatedGold > 0)
            {
                StartCoroutine(CleanupRoutine());
            }
        }

        private IEnumerator CleanupRoutine()
        {
            Debug.Log("SluiceBox: Cleanup started...");
            // Show loading bar or animation?
            yield return new WaitForSeconds(2.0f); // Cleaning time

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddGold(accumulatedGold);
                Debug.Log($"SluiceBox: Cleanup Complete. Collected {accumulatedGold} Gold.");
                accumulatedGold = 0;
                storedDirt = 0; // Reset dirt after cleaning
            }
        }
        public Vector3 GetInteractionPosition()
        {
            float cellSize = TerrainManager.Instance.CellSize;
            Vector3 center = transform.position;
            int cx = Mathf.RoundToInt(center.x / cellSize);
            int cz = Mathf.RoundToInt(center.z / cellSize);

            UnitController playerUnit = null;
            if (Managers.SelectionManager.Instance != null) 
                playerUnit = Managers.SelectionManager.Instance.SelectedUnit;
            if (playerUnit == null) 
                playerUnit = FindFirstObjectByType<UnitController>();

            Vector3 referencePos = playerUnit != null ? playerUnit.transform.position : center;

            Vector3 bestPos = center;
            float minDist = float.MaxValue;
            bool foundValid = false;

            // Spiral/Area Search Radius 2
            int radius = 2;
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (x == 0 && z == 0) continue; 

                    Vector3Int checkCoord = new Vector3Int(cx + x, 0, cz + z);
                    
                    if (TerrainManager.Instance.TryGetTile(checkCoord, out TileType type))
                    {
                        bool isUnderwater = false;
                        if (WaterManager.Instance != null)
                        {
                            if (WaterManager.Instance.GetWaterDepth(checkCoord.x, checkCoord.z) > 0.1f)
                                isUnderwater = true;
                        }

                        if ((type == TileType.Dirt || type == TileType.Bedrock) && !isUnderwater)
                        {
                            float h = TerrainManager.Instance.GetHeight(checkCoord.x, checkCoord.z);
                            Vector3 potentialPos = new Vector3(checkCoord.x * cellSize + cellSize / 2, h, checkCoord.z * cellSize + cellSize / 2);
                            
                            // Check reachability on NavMesh
                            if (NavMesh.SamplePosition(potentialPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                            {
                                float dist = Vector3.SqrMagnitude(hit.position - referencePos);
                                // Debug.Log($"[SluiceBox] Candidate at {checkCoord}: Valid. Dist: {dist}");
                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    bestPos = hit.position;
                                    foundValid = true;
                                }
                            }
                            else
                            {
                                 Debug.Log($"[SluiceBox] Candidate at {checkCoord} rejected: Not on NavMesh.");
                            }
                        }
                    }
                }
            }
            
            if (foundValid) 
            {
                Debug.Log($"[SluiceBox] GetInteractionPosition found valid spot: {bestPos}");
                return bestPos;
            }

            // Fallback - Try to return center projected to NavMesh
            if (NavMesh.SamplePosition(center, out NavMeshHit centerHit, 5.0f, NavMesh.AllAreas))
            {
                 Debug.LogWarning($"[SluiceBox] Search Failed. Defaulting to center NavMesh: {centerHit.position}");
                 return centerHit.position;
            }

            Debug.LogError($"[SluiceBox] GetInteractionPosition FAILED completely. Returning transform.position: {transform.position}");
            return transform.position;
        }
    }
}
