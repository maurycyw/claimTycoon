using UnityEngine;
using System;

namespace ClaimTycoon.Systems.TimeSystem
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Time Settings")]
        [SerializeField] private float timeScale = 60.0f; // Multiplier: 1 real sec = 1 game minute
        [SerializeField] private float startHour = 6.0f; // Start at 6 AM (Sunrise)

        public int Day { get; private set; } = 1;
        public int Hour { get; private set; }
        public int Minute { get; private set; }

        public float NormalizedTime => _currentTime / SECONDS_PER_DAY; // 0.0 to 1.0

        private float _currentTime; // Current time in seconds within the day (0 to 86400)
        private const float SECONDS_PER_DAY = 86400f;

        public event Action OnTimeChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Initialize time
            _currentTime = startHour * 3600f;
            CalculateTime();
        }

        private void Update()
        {
            // Advance time
            // _currentTime += Time.deltaTime * timeScale * 60f; // 1 real sec = 1 game minute at scale 1? 
                                                              // Let's standardise: timeScale of 1 means 1 real second = 1 game second.
                                                              // Usually games are faster. Let's say 1 real sec = 1 game minute -> scale = 60.
                                                              // Adjust logic based on desired default speed. 
                                                              // For now: timeScale is a multiplier on deltaTime. 
            
            // Let's assume user wants configurable speed. 
            // If timeScale = 60, then 1 real second adds 60 game seconds.
             _currentTime += Time.deltaTime * timeScale;

            if (_currentTime >= SECONDS_PER_DAY)
            {
                _currentTime -= SECONDS_PER_DAY;
                Day++;
            }

            CalculateTime();
            OnTimeChanged?.Invoke();
        }

        private void CalculateTime()
        {
            Hour = Mathf.FloorToInt(_currentTime / 3600f);
            Minute = Mathf.FloorToInt((_currentTime % 3600f) / 60f);
        }

        public string GetTimeString()
        {
            return $"{Hour:00}:{Minute:00}";
        }
    }
}
