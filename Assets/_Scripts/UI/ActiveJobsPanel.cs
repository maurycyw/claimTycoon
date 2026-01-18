using UnityEngine;
using System.Collections.Generic;
using ClaimTycoon.Systems.Units.Jobs;

namespace ClaimTycoon.UI
{
    public class ActiveJobsPanel : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameObject jobItemPrefab;
        [SerializeField] private Transform jobListContainer;

        private Dictionary<JobManager.JobEntry, JobItemUI> entryToUI = new Dictionary<JobManager.JobEntry, JobItemUI>();

        private void Start()
        {
            // Wait for JobManager
            if (JobManager.Instance != null)
            {
                SubscribeToManager();
            }
            else
            {
                // Fallback catch-up in update or coroutine? 
                // Usually Managers spawn in Awake.
                SubscribeToManager(); // Try anyway, might be null but we'll check in Update if needed?
                // Actually safer to assume Managers exist.
            }
        }

        private void SubscribeToManager()
        {
            if (JobManager.Instance != null)
            {
                JobManager.Instance.OnJobAdded += HandleJobAdded;
                JobManager.Instance.OnJobRemoved += HandleJobRemoved;
                JobManager.Instance.OnJobUpdated += HandleJobUpdated;

                // Load existing
                foreach (var entry in JobManager.Instance.ActiveJobs)
                {
                    HandleJobAdded(entry);
                }
            }
        }

        private void OnDestroy()
        {
            if (JobManager.Instance != null)
            {
                JobManager.Instance.OnJobAdded -= HandleJobAdded;
                JobManager.Instance.OnJobRemoved -= HandleJobRemoved;
                JobManager.Instance.OnJobUpdated -= HandleJobUpdated;
            }
        }

        private void HandleJobAdded(JobManager.JobEntry entry)
        {
            if (jobItemPrefab == null || jobListContainer == null) return;

            GameObject go = Instantiate(jobItemPrefab, jobListContainer);
            JobItemUI ui = go.GetComponent<JobItemUI>();
            if (ui != null)
            {
                ui.Setup(entry);
                entryToUI[entry] = ui;
            }
        }

        private void HandleJobRemoved(JobManager.JobEntry entry)
        {
            if (entryToUI.ContainsKey(entry))
            {
                if (entryToUI[entry] != null)
                {
                    Destroy(entryToUI[entry].gameObject);
                }
                entryToUI.Remove(entry);
            }
        }

        private void HandleJobUpdated(JobManager.JobEntry entry)
        {
             if (entryToUI.ContainsKey(entry))
            {
                if (entryToUI[entry] != null)
                {
                    entryToUI[entry].UpdateVisuals();
                }
            }
        }
    }
}
