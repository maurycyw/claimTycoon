using UnityEngine;
using ClaimTycoon.Controllers;

namespace ClaimTycoon.Systems.Units.Jobs
{
    public interface IJob
    {
        JobType Type { get; }
        Vector3Int TargetCoord { get; }
        Vector3 StandPosition { get; }
        
        void OnEnter(UnitController unit);
        void Update(UnitController unit);
        void OnExit(UnitController unit);
        bool IsComplete();
    }
}
