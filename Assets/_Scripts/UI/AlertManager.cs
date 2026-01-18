using UnityEngine;
using TMPro;
using System.Collections;

namespace ClaimTycoon.UI
{
    public class AlertManager : MonoBehaviour
    {
        public static AlertManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject alertPanel;
        [SerializeField] private TextMeshProUGUI alertText;
        [SerializeField] private float defaultDuration = 3f;

        private Coroutine currentAlertRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (alertPanel != null) alertPanel.SetActive(false);
        }

        public void ShowAlert(string message, float duration = -1f)
        {
            if (duration < 0) duration = defaultDuration;

            if (currentAlertRoutine != null) StopCoroutine(currentAlertRoutine);
            currentAlertRoutine = StartCoroutine(ShowAlertRoutine(message, duration));
        }

        private IEnumerator ShowAlertRoutine(string message, float duration)
        {
            if (alertPanel != null)
            {
                alertPanel.SetActive(true);
                if (alertText != null) alertText.text = message;
            }

            yield return new WaitForSeconds(duration);

            if (alertPanel != null)
            {
                alertPanel.SetActive(false);
            }
            currentAlertRoutine = null;
        }
    }
}
