using System.Collections;
using UnityEngine;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Systems.Buildings
{
    using ClaimTycoon.Systems.Terrain;

    public class SluiceBox : MonoBehaviour
    {
        [Header("Production Settings")]
        [SerializeField] private float goldPerDirt = 0.5f;
        [SerializeField] private float storedDirt = 0f;
        [SerializeField] private float accumulatedGold = 0f;
        
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
            }
        }
    }
}
