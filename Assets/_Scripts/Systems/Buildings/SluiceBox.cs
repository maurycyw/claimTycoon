using System.Collections;
using UnityEngine;
using ClaimTycoon.Managers;

namespace ClaimTycoon.Systems.Buildings
{
    public class SluiceBox : MonoBehaviour
    {
        [Header("Production Settings")]
        [SerializeField] private float goldAmount = 0.2f;
        [SerializeField] private float interval = 2.0f; // Seconds

        private void Start()
        {
            StartCoroutine(ProduceGoldRoutine());
        }

        private IEnumerator ProduceGoldRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);

                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddGold(goldAmount);
                    // Optional: Visual effect or pop-up here
                }
            }
        }
    }
}
