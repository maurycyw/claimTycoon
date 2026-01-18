using UnityEngine;
using System;
using System.Collections.Generic;
using ClaimTycoon.Controllers;

namespace ClaimTycoon.Systems.Units.Jobs
{
    public class JobManager : MonoBehaviour
    {
        public static JobManager Instance { get; private set; }

        // Track jobs and their assigned units
        public class JobEntry
        {
            public IJob Job;
            public UnitController Unit;
            public bool IsPaused;
            public string UnitName => Unit != null ? Unit.name : "Unassigned";

            public JobEntry(IJob job, UnitController unit)
            {
                Job = job;
                Unit = unit;
                IsPaused = false;
            }
        }

        private List<JobEntry> activeJobs = new List<JobEntry>();
        public List<JobEntry> ActiveJobs => activeJobs;

        // Events
        public event Action<JobEntry> OnJobAdded;
        public event Action<JobEntry> OnJobRemoved; // Completed or Cancelled
        public event Action<JobEntry> OnJobUpdated; // Paused/Resumed

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterJob(IJob job, UnitController unit)
        {
            if (job == null) return;
            
            // Avoid duplicates
            if (activeJobs.Exists(x => x.Job == job)) return;

            JobEntry entry = new JobEntry(job, unit);
            activeJobs.Add(entry);
            Debug.Log($"[JobManager] Job Registered: {job.Type} for {unit.name}");
            OnJobAdded?.Invoke(entry);
        }

        public void UnregisterJob(IJob job)
        {
            JobEntry entry = activeJobs.Find(x => x.Job == job);
            if (entry != null)
            {
                activeJobs.Remove(entry);
                Debug.Log($"[JobManager] Job Unregistered: {job.Type}");
                OnJobRemoved?.Invoke(entry);
            }
        }

        public void SetJobPaused(IJob job, bool paused)
        {
            JobEntry entry = activeJobs.Find(x => x.Job == job);
            if (entry != null)
            {
                entry.IsPaused = paused;
                Debug.Log($"[JobManager] Job {(paused ? "Paused" : "Resumed")}: {job.Type}");
                OnJobUpdated?.Invoke(entry);
            }
        }
        
        public void CancelJob(IJob job)
        {
            JobEntry entry = activeJobs.Find(x => x.Job == job);
            if (entry != null)
            {
                 // If the job is currently running on the unit, we might need to tell the unit to stop?
                 // But usually UnitController calls JobManager. 
                 // If this is called from UI, we need to tell UnitController.
                 
                 if (entry.Unit != null)
                 {
                     // If it's the active job, stop it.
                     // But we don't have direct access to Unit's internal state here easily without exposing more.
                     // Instead, let's assume UI calls UnitController.CancelJob, which calls this.
                     // OR, we expose a method on UnitController to ForceCancelJob(IJob).
                     
                     entry.Unit.CancelJob(job);
                 }
            }
        }

        public void ResumeJob(IJob job)
        {
             JobEntry entry = activeJobs.Find(x => x.Job == job);
             if (entry != null && entry.IsPaused)
             {
                 if (entry.Unit != null)
                 {
                     entry.Unit.ResumeJob(job);
                 }
             }
        }
    }
}
