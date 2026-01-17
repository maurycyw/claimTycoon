using UnityEngine;
using ClaimTycoon.Managers;
using ClaimTycoon.Controllers;

namespace ClaimTycoon.Systems.Buildings
{
    public class ConstructionSite : MonoBehaviour
    {
        [SerializeField] private GameObject prefabToBuild;
        // [SerializeField] private float buildTime = 2.0f; // Unused for now
        
        // Could track progress here if needed
        private bool isCompleted = false;

        public void SetResultPrefab(GameObject prefab)
        {
            prefabToBuild = prefab;
        }

        public void CompleteConstruction()
        {
            if (isCompleted) return;
            isCompleted = true;

            if (prefabToBuild != null)
            {
                // Instantiate the real building at same position/rotation
                GameObject builtObject = Instantiate(prefabToBuild, transform.position, transform.rotation);
                
                // Register with TerrainManager so it's "placed"
                // Note: BuildingManager.Place already registered the SITE. We might need to update the registry.
                // However, TerrainManager just tracks "occupied tiles". Removing this site and adding new one handling is automatic if colliders work,
                // BUT TerrainManager.RegisterBuilding sets persistence data.
                
                // Better approach: BuildingManager registers the Site as a placeholder? 
                // Or we update the tracking here. 
                // For Phase 1 prototype: Just spawning is enough visually.
                // Logic: Destroy site, spawn building.
                
                Debug.Log("Construction Complete!");
            }

            Destroy(gameObject);
        }
    }
}
