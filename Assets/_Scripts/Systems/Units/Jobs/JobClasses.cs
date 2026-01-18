using UnityEngine;
using ClaimTycoon.Controllers;
using ClaimTycoon.Systems.Terrain;
using ClaimTycoon.Systems.Buildings;

namespace ClaimTycoon.Systems.Units.Jobs
{
    public abstract class BaseJob : IJob
    {
        public abstract JobType Type { get; }
        public Vector3Int TargetCoord { get; protected set; }
        public Vector3 StandPosition { get; protected set; }
        
        protected bool isComplete = false;
        protected float workTimer = 0f;
        protected float workDuration = 1.0f; // Default 1s

        public BaseJob(Vector3Int target, Vector3 standPos)
        {
            TargetCoord = target;
            StandPosition = standPos;
        }

        public virtual void OnEnter(UnitController unit)
        {
            workTimer = 0f;
        }

        public virtual void Update(UnitController unit)
        {
            workTimer += Time.deltaTime;
            if (workTimer >= workDuration)
            {
                OnWorkComplete(unit);
                isComplete = true;
            }
        }

        protected abstract void OnWorkComplete(UnitController unit);

        public virtual void OnExit(UnitController unit) { }

        public bool IsComplete() => isComplete;
    }

    public class MineJob : BaseJob
    {
        public override JobType Type => JobType.Mine;

        public MineJob(Vector3Int target, Vector3 standPos) : base(target, standPos) { }

        protected override void OnWorkComplete(UnitController unit)
        {
            if (TerrainManager.Instance.TryGetTile(TargetCoord, out TileType type))
            {
                 Vector3 targetPos = new Vector3(TargetCoord.x * TerrainManager.Instance.CellSize, 0, TargetCoord.z * TerrainManager.Instance.CellSize);
                 try
                 {
                     DirtData data = TerrainManager.Instance.ModifyHeight(targetPos, -0.5f);
                     
                     float total = data.Total;
                     float richness = 0f;
                     
                     if (total > 0.001f)
                     {
                         // Richness Logic
                         float topSoilRichness = 0.1f;
                         float payLayerRichness = 1.0f;
                         richness = ((data.TopSoil * topSoilRichness) + (data.PayLayer * payLayerRichness)) / total;
                     }
                     
                     // We actually mined 'total' amount.
                     // But previously we just assumed we are carrying "a load".
                     // Ideally we carry what we dug. But if we dug 0.2 because of bedrock?
                     // Let's assume we carry what we dug.
                     
                     unit.SetCarryingDirt(total, richness);
                     Debug.Log($"MineJob Complete. Mined {total} dirt (Richness: {richness}) at {TargetCoord}");
                 }
                 catch (System.Exception e)
                 {
                     Debug.LogError($"MineJob Failed: {e.Message}");
                 }
            }
        }
    }

    public class DropDirtJob : BaseJob
    {
        public override JobType Type => JobType.DropDirt;

        public DropDirtJob(Vector3Int target, Vector3 standPos) : base(target, standPos) { }

        protected override void OnWorkComplete(UnitController unit)
        {
            float cellSize = TerrainManager.Instance.CellSize;
            Vector3 targetPos = new Vector3(TargetCoord.x * cellSize, 0, TargetCoord.z * cellSize);
            TerrainManager.Instance.ModifyHeight(targetPos, 0.5f);
            unit.SetCarryingDirt(0, 0);
        }
    }

    public class FeedSluiceJob : BaseJob
    {
        public override JobType Type => JobType.FeedSluice;
        private SluiceBox sluice;

        public FeedSluiceJob(Vector3Int target, Vector3 standPos, SluiceBox sluice) : base(target, standPos) 
        {
            this.sluice = sluice;
        }

        protected override void OnWorkComplete(UnitController unit)
        {
            if (sluice != null)
            {
                sluice.AddDirt(unit.CarriedDirt.Amount, unit.CarriedDirt.GoldRichness);
                unit.SetCarryingDirt(0, 0);
            }
        }
    }

    public class BuildJob : BaseJob
    {
        public override JobType Type => JobType.Build;
        private ConstructionSite site;

        public BuildJob(Vector3Int target, Vector3 standPos, ConstructionSite site) : base(target, standPos)
        {
            this.site = site;
        }

        protected override void OnWorkComplete(UnitController unit)
        {
            if (site != null)
            {
                site.CompleteConstruction();
            }
        }
    }

    public class CleanSluiceJob : BaseJob
    {
        public override JobType Type => JobType.CleanSluice;
        private SluiceBox sluice;

        public CleanSluiceJob(Vector3Int target, Vector3 standPos, SluiceBox sluice) : base(target, standPos)
        {
            this.sluice = sluice;
            workDuration = 2.0f; // Cleaning takes longer
        }

        protected override void OnWorkComplete(UnitController unit)
        {
            if (sluice != null)
            {
                sluice.CleanSluice();
            }
        }
    }
}
