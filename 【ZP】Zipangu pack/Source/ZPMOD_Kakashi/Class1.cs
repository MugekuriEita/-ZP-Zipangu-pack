using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;         // Always needed
using Verse;               // RimWorld universal objects are here (like 'Building')
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')

using System.IO;

namespace ZPMOD_Kakashi
{
    [StaticConstructorOnStartup]
    public class Building_ZPMOD_Kakashi : Building
    {

        public const bool DEBUG = false;
        public const bool VALUE_WEIGHTING = false;

        public const String TXT_PATH = "debug.txt";
        private void AddDebug(string text)
        {
            if (!DEBUG)
                return;
            using (StreamWriter sw = new StreamWriter(TXT_PATH, true, Encoding.Default))
            {
                sw.WriteLine(text);
            }
        }

        //#region FuelManage

        //private CompRefuelable refuelableComp = new CompRefuelable();
        //private const int HeatPushInterval = 30;

        //private int m_Count = 0;

        //public override void Tick()
        //{
        //    base.Tick();
        //    if (!this.refuelableComp.HasFuel)
        //        return;

        //    if (m_Count % 60 == 0)
        //    {
        //        DoTickWork();
        //    }
        //    m_Count++;
        //    if (m_Count > 600000)
        //    {
        //        m_Count = 0;
        //    }
        //}

        //public void UsedThisTick()
        //{
        //    if (this.refuelableComp != null)
        //    {
        //        this.refuelableComp.Notify_UsedThisTick();
        //    }

        //    if (Find.TickManager.TicksGame % 30 == 4)
        //    {
        //        GenTemperature.PushHeat(this, this.def.building.heatPerTickWhileWorking * 30f);
        //    }
        //}

        //private void DoTickWork()
        //{
        //    List<IntVec3> cleaningRange = GetSquare(this.Position, DEF_CLEANING_RADIUS);
        //    List<Filth> cleaningFilth = new List<Filth>();

        //    foreach (Thing b in this.Map.listerThings.ThingsInGroup(ThingRequestGroup.Filth))
        //    {
        //        if (ContainsPos(b.Position, cleaningRange))
        //        {
        //            //destroyFilth = b as Filth;
        //            cleaningFilth.Add(b as Filth);
        //        }
        //    }
        //    foreach (Filth b in cleaningFilth)
        //    {
        //        if (b != null)
        //            b.ThinFilth();
        //    }
        //}
        //#endregion


        #region Variables
        Map map;
        private List<SaveData> m_Savedatas;

        public List<SaveData> Savedatas
        {
            get
            {
                if (m_Savedatas is null)
                    m_Savedatas = new List<SaveData>();
                return m_Savedatas;
            }
            set
            {
                m_Savedatas = value;
            }
        }

        public const int MOD_RANGE = 4;
        #endregion

        #region Setup Work
        // ==================================
        /// <summary>
        /// Do something after the object is spawned into the world
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.map = map;
            //AddDebug(String.Format("respawningAfterLoad = {0}", respawningAfterLoad));
            //if (!respawningAfterLoad)
            AddFertility(1f, MOD_RANGE);
        }
        #endregion

        #region Destroy
        // ==================================

        /// <summary>
        /// Clean up when this is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            RemoveFertility(1f, MOD_RANGE);
            base.Destroy(mode);
        }
        #endregion

        private void AddFertility(float fertility, int range)
        {
            //AddDebug("ここまでOK AddFertility-1");

            List<IntVec3> list = GetSquare(this.Position, range);
            //AddDebug(String.Format("ここまでOK AddFertility-2 | list.Count = {0}", list.Count));

            foreach (IntVec3 pos in list)
            {
                TerrainDef terrain = GetTerrainDef(pos);
                bool modified = terrain is TerrainDefEx;
                if (modified)
                {
                    bool breakFlg = false;
                    foreach (Building b in this.Map.listerBuildings.allBuildingsColonist)
                    {
                        if (b is Building_ZPMOD_Kakashi)
                        {
                            Building_ZPMOD_Kakashi myth = (Building_ZPMOD_Kakashi)b;
                            foreach (SaveData data in myth.Savedatas)
                            {
                                if ((pos.x == data.Position.x) && (pos.y == data.Position.y) && (pos.z == data.Position.z))
                                {
                                    Savedatas.Add(new SaveData() { OriginalTerrainDef = data.OriginalTerrainDef, Position = pos });
                                    breakFlg = true;
                                    break;
                                }
                            }
                        }
                        if (breakFlg)
                            break;
                    }
                }
                else
                {
                    if (terrain.fertility <= 0f)
                        continue;
                    //AddDebug("ここまでOK AddFertility-3)");
                    Savedatas.Add(new SaveData() { OriginalTerrainDef = terrain, Position = pos });
                }

                //AddDebug("ここまでOK AddFertility-4)");
                IncrementsTerrainDef(pos, terrain, modified);
            }
        }

        private void RemoveFertility(float fertility, int range)
        {
            foreach (SaveData data in Savedatas)
            {
                IntVec3 pos = data.Position;
                TerrainDef terrain = GetTerrainDef(pos);
                TerrainDef originalTerrain = data.OriginalTerrainDef;
                DecrementsTerrainDef(pos, terrain, originalTerrain);
            }
        }

        private void IncrementsTerrainDef(IntVec3 pos, TerrainDef terrain, bool modified)
        {
            TerrainGrid terrainGrid = this.Map.terrainGrid;
            if (terrainGrid == null)
                return;

            if (modified)
            {
                TerrainDefEx terrainEx = (TerrainDefEx)terrain;
                TerrainDefEx newTerrain = new TerrainDef1(terrain, terrainEx.BuffCount);
                terrainGrid.SetTerrain(pos, newTerrain);
                AddDebug(String.Format("ここまでOK IncrementsTerrainDef-1 | BuffCount = {0}", newTerrain.BuffCount));
            }
            else
            {
                terrainGrid.SetTerrain(pos, new TerrainDef1(terrain, 0));
            }
        }

        private void DecrementsTerrainDef(IntVec3 pos, TerrainDef terrain, TerrainDef originalTerrain)
        {
            TerrainGrid terrainGrid = this.Map.terrainGrid;
            if (terrainGrid == null)
                return;

            bool ex = (terrain is TerrainDefEx);
            if (!ex)
                return;
            TerrainDefEx terrainEx = (TerrainDefEx)terrain;
            if (terrainEx.BuffCount <= 1)
            {
                terrainGrid.SetTerrain(pos, originalTerrain);
            }
            else
            {

                terrainGrid.SetTerrain(pos, new TerrainDef1(terrain, terrainEx.BuffCount, false));
            }
        }

        private TerrainDef GetTerrainDef(IntVec3 pos)
        {
            TerrainGrid terrainGrid = this.Map.terrainGrid;
            if (terrainGrid == null)
                return null;
            TerrainDef terrain = terrainGrid.TerrainAt(pos);
            if (terrain == null)
                return null;
            return terrain;
        }

        private List<IntVec3> GetSquare(IntVec3 pos, int range)
        {
            //AddDebug("ここまでOK GetSquare-1");
            int x = pos.x;
            //AddDebug(String.Format("ここまでOK GetSquare-2 | x = {0}", x));
            int y = pos.y;
            //AddDebug(String.Format("ここまでOK GetSquare-3 | y = {0}", y));
            int z = pos.z;
            //AddDebug(String.Format("ここまでOK GetSquare-4 | z = {0}", z));
            List<IntVec3> posList = new List<IntVec3>();
            for (int i = -range; i <= range; i++)
            {
                for (int j = -range; j <= range; j++)
                {
                    int newX = x + i;
                    int newZ = z + j;
                    if ((newX >= 0) && (newZ >= 0))
                        posList.Add(new IntVec3(newX, y, newZ));
                }
            }
            return posList;
        }

        /// <summary>
        /// To save and load actual values (savegame-data)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Collections.Look<SaveData>(ref m_Savedatas, "m_Savedatas");
            //AddDebug(String.Format("m_Savedatas.Count = {0}", m_Savedatas.Count));
        }


    }

    //public class SaveData : IExposable
    public class SaveData
    {
        private IntVec3 m_Position;
        private TerrainDef m_OriginalTerrainDef;

        public IntVec3 Position
        {
            get
            {
                return m_Position;
            }
            set
            {
                m_Position = value;
            }
        }

        public TerrainDef OriginalTerrainDef
        {
            get
            {
                return m_OriginalTerrainDef;
            }
            set
            {
                m_OriginalTerrainDef = value;
            }
        }

        //public void ExposeData()
        //{
        //    Scribe_Values.Look<IntVec3>(ref m_Position, "m_Position");
        //    Scribe_Defs.Look<TerrainDef>(ref m_OriginalTerrainDef, "m_OriginalTerrainDef");
        //}
    }

    //public class TerrainDefEx : TerrainDef, IExposable
    public class TerrainDefEx : TerrainDef
    {

        private int m_BuffCount = 0;
        public int BuffCount
        {
            get
            {
                return m_BuffCount;
            }
            set
            {
                m_BuffCount = value;
            }
        }

        public TerrainDefEx(TerrainDef terrain)
        {
            CopyImplements(terrain);
        }

        public void CopyImplements(TerrainDef terrain)
        {
            this.acceptFilth = terrain.acceptFilth;
            this.acceptTerrainSourceFilth = terrain.acceptTerrainSourceFilth;
            this.affordances = terrain.affordances;
            this.altitudeLayer = terrain.altitudeLayer;
            this.avoidWander = terrain.avoidWander;
            this.blueprintDef = terrain.blueprintDef;
            this.buildingPrerequisites = terrain.buildingPrerequisites;
            this.burnedDef = terrain.burnedDef;
            this.changeable = terrain.changeable;
            this.color = terrain.color;
            this.constructEffect = terrain.constructEffect;
            this.costList = terrain.costList;
            this.costStuffCount = terrain.costStuffCount;
            this.debugRandomId = terrain.debugRandomId;
            this.defaultPlacingRot = terrain.defaultPlacingRot;
            this.defName = terrain.defName;
            this.description = terrain.description;
            this.designationCategory = terrain.designationCategory;
            this.designationHotKey = terrain.designationHotKey;
            this.driesTo = terrain.driesTo;
            this.edgeType = terrain.edgeType;
            this.fertility = terrain.fertility;
            this.frameDef = terrain.frameDef;
            this.graphic = terrain.graphic;
            this.holdSnow = terrain.holdSnow;
            this.index = terrain.index;
            this.installBlueprintDef = terrain.installBlueprintDef;
            this.label = terrain.label;
            this.layerable = terrain.layerable;
            this.menuHidden = terrain.menuHidden;
            this.modExtensions = terrain.modExtensions;
            this.passability = terrain.passability;
            this.pathCost = terrain.pathCost;
            this.pathCostIgnoreRepeat = terrain.pathCostIgnoreRepeat;
            this.placeWorkers = terrain.placeWorkers;
            this.placingDraggableDimensions = terrain.placingDraggableDimensions;
            this.renderPrecedence = terrain.renderPrecedence;
            this.repairEffect = terrain.repairEffect;
            this.researchPrerequisites = terrain.researchPrerequisites;
            this.resourcesFractionWhenDeconstructed = terrain.resourcesFractionWhenDeconstructed;
            this.scatterType = terrain.scatterType;
            this.shortHash = terrain.shortHash;
            this.smoothedTerrain = terrain.smoothedTerrain;
            this.specialDisplayRadius = terrain.specialDisplayRadius;
            this.statBases = terrain.statBases;
            this.stuffCategories = terrain.stuffCategories;
            this.tags = terrain.tags;
            this.takeFootprints = terrain.takeFootprints;
            this.takeSplashes = terrain.takeSplashes;
            this.terrainAffordanceNeeded = terrain.terrainAffordanceNeeded;
            this.terrainFilthDef = terrain.terrainFilthDef;
            this.texturePath = terrain.texturePath;
            this.uiIcon = terrain.uiIcon;
            this.uiIconPath = terrain.uiIconPath;
            this.waterDepthMaterial = terrain.waterDepthMaterial;
            this.waterDepthShader = terrain.waterDepthShader;
            this.waterDepthShaderParameters = terrain.waterDepthShaderParameters;
        }

        //public void ExposeData()
        //{
        //    Scribe_Values.Look<int>(ref m_BuffCount, "m_BuffCount");
        //}
    }
    public class TerrainDef1 : TerrainDefEx
    {
        public TerrainDef1(TerrainDef terrain, int bufCount, bool add = true) : base(terrain)
        {
            const float FERTALITY_GAIN_VALUE = 0.3f;

            if (Building_ZPMOD_Kakashi.VALUE_WEIGHTING)
            {
                if (add)
                    this.fertility += FERTALITY_GAIN_VALUE;
                else
                    this.fertility -= FERTALITY_GAIN_VALUE;
            }
            else
            {
                if (bufCount == 0)
                    this.fertility += FERTALITY_GAIN_VALUE;
            }
            if (add)
                BuffCount = bufCount + 1;
            else
                BuffCount = bufCount - 1;

        }
    }

    public class PlaceWorker_Kakashi : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            List<IntVec3> ghostRanges = GetSquare(center, Building_ZPMOD_Kakashi.MOD_RANGE);
            GenDraw.DrawFieldEdges(ghostRanges, Color.green);
        }

        private List<IntVec3> GetSquare(IntVec3 pos, int range)
        {
            int x = pos.x;
            int y = pos.y;
            int z = pos.z;
            List<IntVec3> posList = new List<IntVec3>();
            for (int i = -range; i <= range; i++)
            {
                for (int j = -range; j <= range; j++)
                {
                    int newX = x + i;
                    int newZ = z + j;
                    if ((newX >= 0) && (newZ >= 0))
                        posList.Add(new IntVec3(newX, y, newZ));
                }
            }
            return posList;
        }
    }

}
