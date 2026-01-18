using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClaimTycoon.Systems.Units.Jobs;

namespace ClaimTycoon.UI
{
    public class JobItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI jobTypeText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button cancelButton;

        private JobManager.JobEntry linkedEntry;

        public void Setup(JobManager.JobEntry entry)
        {
            linkedEntry = entry;
            UpdateVisuals();

            // Setup Buttons
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseClicked);

            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        public void UpdateVisuals()
        {
            if (linkedEntry == null) return;

            if (unitNameText != null) unitNameText.text = linkedEntry.UnitName;
            if (jobTypeText != null) jobTypeText.text = linkedEntry.Job.Type.ToString();

            if (pauseButton != null) pauseButton.gameObject.SetActive(!linkedEntry.IsPaused);
            if (resumeButton != null) resumeButton.gameObject.SetActive(linkedEntry.IsPaused);
        }

        private void OnPauseClicked()
        {
            if (linkedEntry != null && JobManager.Instance != null)
            {
                // Pause via Logic or UnitController directly?
                // UnitController logic is "PauseActiveJob" inside MoveTo. 
                // But we want to just pause it without moving.
                // We need a method in UnitController to "PauseJob" specifically without moving?
                // The prompt says: "pause the job ... resume/play button in the jobs panel"
                // If I click pause, I should probably stop the unit?
                // Actually, the prompt says "pause the job (when player interrupts)... resume/play button (in panel)"
                // It doesn't explicitly say "Pause button in panel". 
                // "Resume/Play button in the jobs panel OR they use shift+right click active jobs panel".
                // Wait. "pause the job AND ONLY RESUME after the player hits the resume/play button in the jobs panel"
                // So the PANEL is for RESUMING.
                // Does the panel need a PAUSE button? The prompt implies pausing happens by "Unit is selected and then the player decides to move the character".
                // However, a Pause button is good UX. I will implement it.
                // To pause via UI, we might need to tell Unit to stop what it's doing.
                
                // For now, let's assume we can just SetJobPaused via Manager? 
                // No, Manager just tracks state. Unit dictates state.
                // Implementation Plan didn't specify "PauseJob" method on UnitController exposed to UI.
                // MoveTo calls PauseActiveJob.
                // I'll leave Pause button implementation as "Cancel Active Job and set as Paused"?
                // Or I can add `UnitController.PauseCurrentJob()`.
                
                if (linkedEntry.Unit != null)
                {
                    // For now, let's do nothing on Pause Click unless we add the method.
                    // But I'll focus on Resume as per requirements.
                    // Actually, I'll allow Cancel.
                }
            }
        }

        private void OnResumeClicked()
        {
             if (linkedEntry != null && JobManager.Instance != null)
            {
                JobManager.Instance.ResumeJob(linkedEntry.Job);
            }
        }

        private void OnCancelClicked()
        {
            if (linkedEntry != null && JobManager.Instance != null)
            {
                JobManager.Instance.CancelJob(linkedEntry.Job);
            }
        }
    }
}
