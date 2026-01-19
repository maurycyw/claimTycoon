using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI; // Needed for NavMeshObstacle

namespace ClaimTycoon.Systems.Terrain
{
    public enum NatureType
    {
        Tree,
        Rock,
        Bush,
        Grass,
        Mushroom,
        Branch
    }

    [RequireComponent(typeof(NavMeshObstacle))]
    public class NatureItem : MonoBehaviour
    {
        [SerializeField] private NatureType type;
        public NatureType Type => type;

        [SerializeField] private bool isObstacle = true;
        public bool IsObstacle => isObstacle;

        private NavMeshObstacle obs;

        private void Awake()
        {
            obs = GetComponent<NavMeshObstacle>();
            UpdateObstacle();
        }

        private void OnValidate()
        {
            if (obs == null) obs = GetComponent<NavMeshObstacle>();
            UpdateObstacle();
        }

        private void UpdateObstacle()
        {
            if (obs == null) return;

            if (isObstacle)
            {
                obs.enabled = true;
                obs.carving = true; 
                obs.carveOnlyStationary = true; // Better performance for static trees
                
                // Adjust shape based on Type if needed, or rely on collider
                // Generally Box or Capsule.
            }
            else
            {
                obs.enabled = false;
            }
        }

        public void SetType(NatureType newType)
        {
            type = newType;
            // Trees and Rocks are obstacles. Bushes usually not.
            if (type == NatureType.Tree || type == NatureType.Rock) SetObstacle(true);
            else SetObstacle(false);
        }

        public void SetObstacle(bool obstacle)
        {
            isObstacle = obstacle;
            UpdateObstacle();
        }
    }
}
