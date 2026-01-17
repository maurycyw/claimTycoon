using UnityEngine;
using System;

namespace ClaimTycoon.Systems.Units
{
    public enum StatType
    {
        Mining,
        MoveSpeed,
        Negotiation,
        Repair
    }

    [System.Serializable]
    public class Stat
    {
        public StatType type;
        public int level = 1;
        public float currentXP;
        public float xpToNext = 100f;
        public float value; // e.g. Speed 3.5, MiningTime 1.0

        public void AddXP(float amount)
        {
            currentXP += amount;
            if (currentXP >= xpToNext)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            currentXP -= xpToNext;
            level++;
            xpToNext *= 1.2f;

            // Simple scaling for prototype
            if (type == StatType.Mining) value *= 0.9f; // Faster
            if (type == StatType.MoveSpeed) value *= 1.05f; // Faster

            Debug.Log($"{type} Leveled Up to {level}!");
        }
    }

    public class CharacterStats : MonoBehaviour
    {
        [Header("RPG Attributes")]
        [SerializeField] private System.Collections.Generic.List<Stat> stats = new System.Collections.Generic.List<Stat>();

        public System.Collections.Generic.List<Stat> AllStats => stats;

        // Events
        public event Action<Stat> OnStatChanged;

        public void LoadStats(System.Collections.Generic.List<Stat> loadedStats)
        {
            stats = loadedStats;
            // Notify listeners?
            foreach(var s in stats) OnStatChanged?.Invoke(s);
        }

        private void Awake()
        {
            // Initialize defaults if empty
            if (stats.Count == 0)
            {
                stats.Add(new Stat { type = StatType.Mining, value = 1.0f });
                stats.Add(new Stat { type = StatType.MoveSpeed, value = 3.5f });
            }
        }

        public Stat GetStat(StatType type)
        {
            return stats.Find(s => s.type == type);
        }

        public void GainXP(StatType type, float amount)
        {
            Stat s = GetStat(type);
            if (s != null)
            {
                s.AddXP(amount);
                OnStatChanged?.Invoke(s);
            }
        }
    }
}
