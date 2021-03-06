﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Knowledge
{
    //outer dependencies : 
   // Nature.AddLifesource, Nature.SupportCreatedGrassland
   // Expedition.Dismiss
   // DockSystem.HandleShip

    public enum ResearchRoute : byte { Foundation = 0, CloudWhale, Engine, Pipes, Crystal, Monument, Blossom, Pollen }
    //dependence: 
    //uiController icons enum
    // labels in Localization - get challenge label

    private static Knowledge current;

    public readonly bool[] puzzlePins;
    public bool allRoutesUnblocked { get; private set; }
    public float[] routePoints { get; private set; }
    public byte[] puzzlePartsCount { get; private set; }
    public byte[] colorCodesArray{get; private set;}
    public float completeness { get; private set; }
    public int changesMarker { get; private set; }

    private byte[] routeBonusesMask = new byte[ROUTES_COUNT]; // учет полученных бонусов
    private KnowledgeTabUI observer;

    public const byte ROUTES_COUNT = 8, STEPS_COUNT = 7, PUZZLECOLORS_COUNT = 6,
       WHITECOLOR_CODE = 0, REDCOLOR_CODE = 1, GREENCOLOR_CODE = 2, BLUECOLOR_CODE = 3, CYANCOLOR_CODE = 4, BLACKCOLOR_CODE = 5, NOCOLOR_CODE = 6;
    public const int PUZZLEPARTS_COUNT = 64, PUZZLE_PINS_COUNT = 112; // 7 * 8 + 8 * 7

    public static readonly Color[] colors = new Color[6]
    {
        Color.white, // 0 - white
        new Color(0.8f, 0.3f, 0.3f, 1f), // 1 - red
        new Color(0.43f, 0.8f, 0.3f), // 2 - green
        new Color(0.25f, 0.52f, 0.86f, 1f), // 3 - blue
        new Color(0.1f, 0.92f, 0.9f, 1f), // 4 - cyan
        new Color(0.2f, 0.17f, 0.17f, 1f), // 5 - black      
    };
    public static readonly float[] STEPVALUES = new float[7] { 10f, 15f, 25f, 50f, 75f, 100f, 125f }; // 400 total
    public const float MAX_RESEARCH_PTS = 400f;
    public static readonly byte[,] routeButtonsIndexes = new byte[8, 7] {
        {36, 44, 43,51,52,60,59},
        {45, 53,46,54,62,55,63},
        {28,29,37,38,30,31,39 },
        {21,22, 13,14,6,15,7},
        {27,19,20,11,12,3,4 },
        {18, 10,17,9,1,8,0},
        {35,34,26,25,33,32,24 },
        {42,50,41,49,57,48,56 }
    };
    private static readonly byte[] blockedCells = new byte[8] { 2, 5, 16, 23, 40, 47, 58, 61 };

    #region boosting
    //foundation:
    public const float R_F_HAPPINESS_COND = 0.8f, R_E_ENERGY_STORED_COND = 10000f, R_E_GEARS_COND = 3.5f, R_P_FUEL_CONDITION = 1000f,
        R_C_MONEY_COND = 5000f, R_M_MONUMENTS_AFFECTION_CONDITION = Monument.MAX_AFFECTION_VALUE / 2f, R_B_GRASSLAND_RATIO_COND = 0.7f,
        R_P_ASCENSIOND_COND = 0.85f;
    public const int R_F_POPULATION_COND = 2500, R_F_IMMIGRANTS_CONDITION = 1000, R_F_QUEST_POPULATION_COND = 10000, R_CW_GRASSLAND_COUNT_COND = 6, R_CW_STREAMGENS_COUNT_COND = 8,
        R_CW_CREWS_COUNT_COND = 4, R_E_FACTORYCUBES_COUNT = 4, R_M_MONUMENTS_COUNT_COND = 2, R_M_SUCCESSFUL_EXPEDITIONS_COUNT_COND = 30;
    private const byte R_F_SETTLEMENT_LEVEL_COND = 6, R_CW_GRASSLAND_LEVEL_COND = 4, R_CW_CREW_LEVEL_COND = 3, 
        POINT_MASK_POSITION = 6, BUILDINGS_MASK = (1 << 4) + (1 << 5), R_P_ISLAND_SIZE_COND = 8;
    private const uint R_F_IMMIGRANTS_COUNT_COND = 1000;

    //order is important! 4 diff conds + 2 build conds + point cond + quest cond
    public enum FoundationRouteBoosters : byte {HappinessBoost, PopulationBoost, SettlementBoost, ImmigrantsBoost, HotelBoost, HousingMastBoost, PointBoost, QuestBoost }
    public enum CloudWhaleRouteBoosters: byte { GrasslandsBoost, StreamGensBoost, CrewsBoost, ArtifactBoost, XStationBoost, StabilityEnforcerBooster, PointBoost, QuestBoost}
    public enum EngineRouteBoosters : byte { EnergyBoost, CityMoveBoost,  GearsBoost, FactoryBoost, IslandEngineBoost, ControlCenterBoost, PointBoost, QuestBoost}
    public enum PipesRouteBoosters: byte { FarmsBoost, SizeBoost, FuelBoost, BiomesBoost, QETBoost, CapacitorMastBoost, PointBoost, QuestBoost}
    public enum CrystalRouteBoosters : byte { MoneyBoost, PinesBoost, GCubeBoost, BiomeBoost, CrystalliserBoost, CrystalMastBoost, PointsBoost, QuestBoost};
    public enum MonumentRouteBoosters : byte { MonumentAffectionBoost, LifesourceBoost, BiomeBoost, ExpeditionsBoost, MonumentConstructionBoost, AnchorMastBoost, PointBoost, QuestBoost}
    public enum BlossomRouteBoosters : byte { GrasslandsBoost, ArtifactBoost, BiomeBoost, Unknown, GardensBoost, HTowerBoost, PointBoost, QuestBoost}
    public enum PollenRouteBoosters: byte { FlowersBoost, AscensionBoost, CrewAccidentBoost, BiomeBoost, FilterBoost, ProtectorCoreBoost, PointBoost, QuestBoost}

    private void SetSubscriptions()
    {
        var colony = GameMaster.realMaster.colonyController;
        byte mask = routeBonusesMask[(int)ResearchRoute.Foundation];
        if (!BoostCounted(FoundationRouteBoosters.HappinessBoost)) colony.happinessUpdateEvent += HappinessCheck;
        if (!BoostCounted(FoundationRouteBoosters.PopulationBoost)) colony.populationUpdateEvent += PopulationCheck;
        //
        var gm = GameMaster.realMaster;      
        mask = routeBonusesMask[(int)ResearchRoute.CloudWhale];
        bool subscribeToEverydayUpdate = 
            !BoostCounted(CloudWhaleRouteBoosters.GrasslandsBoost)
            ||
            !BoostCounted(CloudWhaleRouteBoosters.CrewsBoost)
            ||
            !BoostCounted(EngineRouteBoosters.EnergyBoost)
            ||
            !BoostCounted(EngineRouteBoosters.GearsBoost)
            ||
            !BoostCounted(EngineRouteBoosters.EnergyBoost)
            ||
            !BoostCounted(PipesRouteBoosters.FuelBoost)
            ||
            !BoostCounted(PollenRouteBoosters.AscensionBoost)
            ;
        if (subscribeToEverydayUpdate) gm.everydayUpdate += EverydayUpdate;
        //
        mask = 255;
        foreach (var b in routeBonusesMask)
        {
            mask &= b;
        }
        if ((mask & (1 << POINT_MASK_POSITION)) == 0)  gm.globalMap.pointsExploringEvent += PointCheck;
        if ((mask & BUILDINGS_MASK) == 0 
            || !BoostCounted(CloudWhaleRouteBoosters.StreamGensBoost)
            || !BoostCounted(MonumentRouteBoosters.MonumentConstructionBoost)
            ) gm.eventTracker.buildingConstructionEvent += BuildingConstructionCheck;
        //
        if (!BoostCounted(FoundationRouteBoosters.SettlementBoost) || !BoostCounted(PipesRouteBoosters.FarmsBoost))
            gm.eventTracker.buildingUpgradeEvent += BuildingUpgradeCheck;
        //
        if (!BoostCounted(PipesRouteBoosters.SizeBoost) | !BoostCounted(CrystalRouteBoosters.GCubeBoost)) gm.mainChunk.ChunkUpdateEvent += BlockArrayCheck;
        //
        if (!BoostCounted(CrystalRouteBoosters.MoneyBoost)) colony.crystalsCountUpdateEvent += CrystalsCheck;
    }

    private void HappinessCheck(float h)
    {
        if (h >= R_F_HAPPINESS_COND)
        {
            var b = (byte)FoundationRouteBoosters.HappinessBoost;
            if (CountRouteBonus(ResearchRoute.Foundation, b))
            {
                GameMaster.realMaster.colonyController.happinessUpdateEvent -= HappinessCheck;
            }
        }
    }
    private void PopulationCheck(int p)
    {
        if (p >= R_F_POPULATION_COND)
        {
            var b = (byte)FoundationRouteBoosters.PopulationBoost;
            if (CountRouteBonus(ResearchRoute.Foundation, b))
            {
                GameMaster.realMaster.colonyController.populationUpdateEvent -= PopulationCheck;
            }
        }
    }
    public void ImmigrantsCheck(uint c)
    {
        if (c >= R_F_IMMIGRANTS_COUNT_COND) CountRouteBonus(FoundationRouteBoosters.ImmigrantsBoost);
    }
    private void PointCheck(MapPoint mp)
    {
        var poi = mp as PointOfInterest;
        if (poi != null) {
            switch (mp.type)
            {
                case MapMarkerType.Colony:
                    CountRouteBonus(ResearchRoute.Foundation, (byte)FoundationRouteBoosters.PointBoost);
                    break;
                case MapMarkerType.Wiseman:
                    {
                        switch (poi.path)
                        {
                            case Path.LifePath: CountRouteBonus(CloudWhaleRouteBoosters.PointBoost); break;
                            case Path.TechPath: CountRouteBonus(CrystalRouteBoosters.PointsBoost); break;
                            case Path.SecretPath: CountRouteBonus(PipesRouteBoosters.PointBoost); break;
                        }
                    break;
                    }
                case MapMarkerType.SOS:
                    {
                        switch (poi.path)
                        {
                            case Path.LifePath: CountRouteBonus(PollenRouteBoosters.PointBoost); break;
                            case Path.TechPath: 
                            case Path.SecretPath: CountRouteBonus(CloudWhaleRouteBoosters.PointBoost); break;
                        }
                        break;
                    }
                case MapMarkerType.Station:
                    {
                        switch (poi.path)
                        {
                            case Path.LifePath: CountRouteBonus(PollenRouteBoosters.PointBoost); break;
                            case Path.TechPath: CountRouteBonus(EngineRouteBoosters.PointBoost); break;
                            case Path.SecretPath: CountRouteBonus(MonumentRouteBoosters.PointBoost); break;
                        }
                        break;
                    }
                case MapMarkerType.Wreck:
                    {
                        switch (poi.path)
                        {
                            case Path.LifePath: CountRouteBonus(PollenRouteBoosters.PointBoost); break;
                            case Path.TechPath: CountRouteBonus(EngineRouteBoosters.PointBoost); break;
                            case Path.SecretPath: CountRouteBonus(MonumentRouteBoosters.PointBoost); break;
                        }
                        break;
                    }
                case MapMarkerType.Wonder:
                    {
                        switch (poi.path)
                        {
                            case Path.LifePath: CountRouteBonus(BlossomRouteBoosters.PointBoost); break;
                            case Path.TechPath: CountRouteBonus(EngineRouteBoosters.PointBoost); break;
                            case Path.SecretPath: CountRouteBonus(CrystalRouteBoosters.PointsBoost); break;
                        }
                        break;
                    }
                case MapMarkerType.Portal:
                    {
                        switch (poi.path)
                        {
                            case Path.LifePath: CountRouteBonus(BlossomRouteBoosters.PointBoost); break;
                            case Path.TechPath: 
                            case Path.SecretPath: CountRouteBonus(PipesRouteBoosters.PointBoost); break;
                        }
                        break;
                    }
                case MapMarkerType.Island:
                    switch (poi.path)
                    {
                        case Path.LifePath: CountRouteBonus(BlossomRouteBoosters.PointBoost); break;
                        case Path.TechPath: CountRouteBonus(MonumentRouteBoosters.PointBoost); break;
                        case Path.SecretPath: CountRouteBonus(CrystalRouteBoosters.PointsBoost); break;
                    }
                    break; 
            }
        }

        byte mask = 255;
        foreach (var b in routeBonusesMask)
        {
            mask &= b;
        }
        if ((mask & (1 << POINT_MASK_POSITION)) != 0) GameMaster.realMaster.globalMap.pointsExploringEvent -= PointCheck;

        // DEPENDENCY - Localization.FillQuest
    }
    private void BuildingConstructionCheck(Structure s)
    {
        switch (s.ID)
        {
            case Structure.HOTEL_BLOCK_6_ID:
                if (CountRouteBonus(ResearchRoute.Foundation, (byte)FoundationRouteBoosters.HotelBoost)) ;
                break;
            case Structure.HOUSING_MAST_6_ID:
                CountRouteBonus(ResearchRoute.Foundation, (byte)FoundationRouteBoosters.HousingMastBoost);
                break;
            case Structure.XSTATION_3_ID:
                CountRouteBonus(ResearchRoute.CloudWhale, (byte)CloudWhaleRouteBoosters.XStationBoost);
                break;
            case Structure.WIND_GENERATOR_1_ID:
                if (!BoostCounted(CloudWhaleRouteBoosters.StreamGensBoost))
                {
                    var pg = GameMaster.realMaster.colonyController.GetPowerGrid();
                    if (pg != null)
                    {
                        int count = 0;
                        foreach (var b in pg)
                        {
                            if (b.ID == Structure.WIND_GENERATOR_1_ID) count++;
                        }
                        if (count >= R_CW_STREAMGENS_COUNT_COND) CountRouteBonus(CloudWhaleRouteBoosters.StreamGensBoost);
                    }                    
                }
                break;
            case Structure.SMELTERY_BLOCK_ID:
                if (!BoostCounted(EngineRouteBoosters.FactoryBoost))
                {
                    var pg = GameMaster.realMaster.colonyController.GetPowerGrid();
                    if (pg != null)
                    {
                        int count = 0;
                        foreach (var b in pg)
                        {
                            if (b.ID == Structure.SMELTERY_BLOCK_ID) count++;
                        }
                        if (count >= R_E_FACTORYCUBES_COUNT) CountRouteBonus(EngineRouteBoosters.FactoryBoost);
                    }                    
                }
                break;
            case Structure.QUANTUM_ENERGY_TRANSMITTER_5_ID:
                CountRouteBonus(PipesRouteBoosters.QETBoost);
                break;
            case Structure.MONUMENT_ID:
                {
                    if (!BoostCounted(MonumentRouteBoosters.MonumentConstructionBoost))
                    {
                        CountRouteBonus(MonumentRouteBoosters.MonumentConstructionBoost);
                    }
                    if (!BoostCounted(MonumentRouteBoosters.MonumentAffectionBoost))
                    {
                        int count = 0;
                        var blist = GameMaster.realMaster.colonyController.powerGrid;
                        if (blist != null)
                        {
                            foreach (var b in blist)
                            {
                                if (b.ID == Structure.MONUMENT_ID)
                                {
                                    var m = b as Monument;
                                    if (m.affectionPath == Path.TechPath && m.affectionValue > R_M_MONUMENTS_AFFECTION_CONDITION) count++;
                                }
                            }
                            if (count > R_M_MONUMENTS_COUNT_COND) CountRouteBonus(MonumentRouteBoosters.MonumentAffectionBoost);
                        }
                    }
                    break;
                }
        }
        if (!BoostCounted(PipesRouteBoosters.FarmsBoost) && s is CoveredFarm) CoveredFarmsCheck();

        byte mask = 255;
        foreach (var b in routeBonusesMask)
        {
            mask &= b;
        }
        if ((mask & BUILDINGS_MASK) != 0)
        {
            if (BoostCounted(CloudWhaleRouteBoosters.StreamGensBoost) && BoostCounted(MonumentRouteBoosters.MonumentConstructionBoost))
            GameMaster.realMaster.eventTracker.buildingConstructionEvent -= BuildingConstructionCheck;
        }
    }
    private void BuildingUpgradeCheck(Building b)
    {
        switch (b.ID)
        {
            case Structure.SETTLEMENT_CENTER_ID:
                if (b.level == R_F_SETTLEMENT_LEVEL_COND)
                {
                    var x = (byte)FoundationRouteBoosters.SettlementBoost;
                    CountRouteBonus(ResearchRoute.Foundation, x);
                }
                break;
        }
        if (!BoostCounted(PipesRouteBoosters.FarmsBoost) && b is CoveredFarm) CoveredFarmsCheck();
    }
    private void CoveredFarmsCheck()
    {
        var pg = GameMaster.realMaster.colonyController.GetPowerGrid();
        if (pg != null)
        {
            int count = 0;
            foreach (var b in pg)
            {
                if (b is Farm) return;
                else
                {
                    if (b is CoveredFarm) count++;
                }
            }
            if (count >= 0) CountRouteBonus(PipesRouteBoosters.FarmsBoost);
        }
    }
    private void BlockArrayCheck()
    {
        var gmc = GameMaster.realMaster.mainChunk;
        var blocks = gmc?.blocks;
        if (blocks != null)
        {
            byte unsubscribeVotes = 0;
            if (!BoostCounted(PipesRouteBoosters.SizeBoost))
            {
                if (blocks.Count < R_P_ISLAND_SIZE_COND)
                {
                    CountRouteBonus(PipesRouteBoosters.SizeBoost);
                }
                else
                {
                    var csize = Chunk.chunkSize;
                    byte xmin = csize, xmax = 0, ymin = xmin, ymax = xmax, zmin = xmin, zmax = xmax;
                    ChunkPos cpos;
                    foreach (var b in blocks)
                    {
                        cpos = b.Value.pos;
                        if (cpos.x > xmax) xmax = cpos.x;
                        else
                        {
                            if (cpos.x < xmin) xmin = cpos.x;
                        }
                        if (cpos.y > ymax) ymax = cpos.y;
                        else
                        {
                            if (cpos.y < ymin) ymin = cpos.y;
                        }
                        if (cpos.z > zmax) zmax = cpos.z;
                        else
                        {
                            if (cpos.z < zmin) zmin = cpos.z;
                        }
                    }
                    int xsize = xmax - xmin, ysize = ymax - ymin, zsize = zmax - zmin;
                    byte cond = 0;
                    if (xsize <= R_P_ISLAND_SIZE_COND) cond++;
                    if (ysize <= R_P_ISLAND_SIZE_COND) cond++;
                    if (zsize <= R_P_ISLAND_SIZE_COND) cond++;
                    if (cond >= 2)
                    {
                        CountRouteBonus(PipesRouteBoosters.SizeBoost);
                    }
                }
            }
            else unsubscribeVotes++;
            if (!BoostCounted(CrystalRouteBoosters.GCubeBoost))
            {
                Block b;
                int mid = ResourceType.GRAPHONIUM_ID;
                foreach (var fb in blocks)
                {
                    b = fb.Value;
                    if (b.IsCube() && b.GetMaterialID() == mid)
                    {
                        CountRouteBonus(CrystalRouteBoosters.GCubeBoost);
                        break;
                    }
                }
            }
            else unsubscribeVotes++;
            //
            if (unsubscribeVotes == 2) gmc.ChunkUpdateEvent -= BlockArrayCheck;
        }
    }
    private void CrystalsCheck(float f)
    {
        if (f >= R_C_MONEY_COND)
        {
            CountRouteBonus(CrystalRouteBoosters.MoneyBoost);
            var c = GameMaster.realMaster.colonyController;
            if (c != null) c.crystalsCountUpdateEvent -= CrystalsCheck;
        }
    }
    public void ExpeditionsCheck(uint successfulCount)
    {
        if (successfulCount == R_M_SUCCESSFUL_EXPEDITIONS_COUNT_COND) CountRouteBonus(MonumentRouteBoosters.ExpeditionsBoost);
    }
    public void GrasslandsCheck(List<Grassland> glist)
    {
        if (glist == null) return;
       
        if (!BoostCounted(CloudWhaleRouteBoosters.GrasslandsBoost))
        {
            int count = 0;
            foreach (var g in glist)
            {
                if (g.level >= R_CW_GRASSLAND_LEVEL_COND) count++;
            }
            if (count >= R_CW_GRASSLAND_COUNT_COND) CountRouteBonus(CloudWhaleRouteBoosters.GrasslandsBoost);
        }
        if (!BoostCounted(BlossomRouteBoosters.GrasslandsBoost))
        {
            float scount = GameMaster.realMaster.mainChunk?.surfaces?.Length ?? 0;
            scount = (float)glist.Count / scount;
            if (scount >= R_B_GRASSLAND_RATIO_COND) CountRouteBonus(BlossomRouteBoosters.GrasslandsBoost);
        }
    }

    private void EverydayUpdate()
    {
        byte unsubscribeVotes = 0;        
        var gm = GameMaster.realMaster;
        var colony = gm.colonyController;
        int count = 0;

        #region cloud whale route + blossom - 3 positions       
        //crews
        if (!BoostCounted(CloudWhaleRouteBoosters.CrewsBoost))
        {
            var crewslist = Crew.crewsList;
            if (crewslist != null)
            {
                count = 0;
                foreach (var c in crewslist)
                {
                    if (c.level > R_CW_CREW_LEVEL_COND) count++;
                }
                if (count >= R_CW_CREWS_COUNT_COND) CountRouteBonus(CloudWhaleRouteBoosters.CrewsBoost);
            }
        }
        else unsubscribeVotes++;
        //artifacts
        if (!BoostCounted(CloudWhaleRouteBoosters.ArtifactBoost) || !BoostCounted(BlossomRouteBoosters.ArtifactBoost))
        {
            var alist = Artifact.artifactsList;
            if (alist != null)
            {
                foreach (var a in alist)
                {
                    if (a.affectionPath == Path.SecretPath )
                    {
                        if (a.status != Artifact.ArtifactStatus.Uncontrollable) CountRouteBonus(CloudWhaleRouteBoosters.ArtifactBoost);
                        break;
                    }
                    else
                    {
                        if (a.affectionPath == Path.TechPath && a.status != Artifact.ArtifactStatus.Uncontrollable)
                            CountRouteBonus(BlossomRouteBoosters.ArtifactBoost);
                    }
                }
            }
        }
        else unsubscribeVotes += 2;
        //
        #endregion

        #region engineRoute - 2 positions       
        if (!BoostCounted(EngineRouteBoosters.EnergyBoost))
        {
            if (colony.energyStored >= R_E_ENERGY_STORED_COND) CountRouteBonus(EngineRouteBoosters.EnergyBoost);
        }
        else unsubscribeVotes++;
        if (!BoostCounted(EngineRouteBoosters.GearsBoost))
        {
            if (colony.gears_coefficient >= R_E_GEARS_COND) CountRouteBonus(EngineRouteBoosters.GearsBoost);
        }
        else unsubscribeVotes++;
        #endregion

        #region pipes route - 1 position
        if (!BoostCounted(PipesRouteBoosters.FuelBoost))
        {
            if (colony.storage.standartResources[ResourceType.FUEL_ID] >= R_P_FUEL_CONDITION) CountRouteBonus(PipesRouteBoosters.FuelBoost);
        }
        #endregion
        
        //pollen route - 1 position
        if (!BoostCounted(PollenRouteBoosters.AscensionBoost)) {
            if (gm.globalMap.ascension >= R_P_ASCENSIOND_COND)
            {
                CountRouteBonus(PollenRouteBoosters.AscensionBoost);
            }
        }
        else unsubscribeVotes++;

        if (unsubscribeVotes == 7) GameMaster.realMaster.everydayUpdate -= EverydayUpdate;
    }
    
    private bool BoostCounted(CloudWhaleRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.CloudWhale] & (1 << (byte)type)) != 0;
    }
    private bool BoostCounted(FoundationRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.Foundation] & (1 << (byte)type)) != 0;
    }
    private bool BoostCounted(EngineRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.Engine] & (1 << (byte)type)) != 0;
    }
    private bool BoostCounted(PipesRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.Pipes] & (1 << (byte)type)) != 0;
    }
    private bool BoostCounted(CrystalRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.Crystal] & (1 << (byte)type)) != 0;
    }
    private bool BoostCounted(MonumentRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.Monument] & (1 << (byte)type)) != 0;
    }
    private bool BoostCounted(BlossomRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.Blossom] & (1 << (byte)type)) != 0;
    }
    private bool BoostCounted(PollenRouteBoosters type)
    {
        return (routeBonusesMask[(int)ResearchRoute.Pollen] & (1 << (byte)type)) != 0;
    }

    private bool CountRouteBonus(FoundationRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.Foundation, (byte)type);
    }
    private bool CountRouteBonus(CloudWhaleRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.CloudWhale, (byte)type);
    }
    private bool CountRouteBonus(EngineRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.Engine, (byte)type);
    }
    private bool CountRouteBonus(PipesRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.Pipes, (byte)type);
    }
    private bool CountRouteBonus(CrystalRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.Crystal, (byte)type);
    }
    public bool CountRouteBonus(MonumentRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.Monument, (byte)type);
    }
    private bool CountRouteBonus(BlossomRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.Blossom, (byte)type);
    }
    public bool CountRouteBonus(PollenRouteBoosters type)
    {
        return CountRouteBonus(ResearchRoute.Pollen, (byte)type);
    }
    private bool CountRouteBonus(ResearchRoute rr, byte boosterIndex)
    {
        byte mask = (byte)(1 << boosterIndex), routeIndex = (byte)rr;
        if ((routeBonusesMask[routeIndex] & mask) == 0)
        {
            routeBonusesMask[routeIndex] += mask;
            mask = routeBonusesMask[routeIndex];
            QuestUI.current?.FindAndCompleteQuest(rr, boosterIndex);
            byte bonusIndex = 0;
            if ((mask & 1) != 0) bonusIndex++;
            if ((mask & 2) != 0) bonusIndex++;
            if ((mask & 4) != 0) bonusIndex++;
            if ((mask & 8) != 0) bonusIndex++;
            if ((mask & 16) != 0) bonusIndex++;
            if ((mask & 32) != 0) bonusIndex++;
            if ((mask & 64) != 0) bonusIndex++;
            if ((mask & 128) != 0) bonusIndex++;
            if (bonusIndex < 4) AddResearchPoints(rr, STEPVALUES[bonusIndex]);
            else AddResearchPoints(rr, STEPVALUES[bonusIndex] / 2f);
            return true;
        }
        else return false;
    }

    //dependence : Localization.FillQuest
    #endregion   

    public static Knowledge GetCurrent()
    {
        if (current == null) current = new Knowledge();
        return current;
    }
    public static bool KnowledgePrepared() { return current != null; }

    private Knowledge()
    {
        puzzlePins = new bool[PUZZLE_PINS_COUNT]; // 7 * 8 + 8 * 7
        for (int i =0; i < puzzlePins.Length; i++)
        {
            puzzlePins[i] = Random.value > 0.55f ? true : false;
        }
        SYSTEM_FillBasicData();
    }    
    private Knowledge(bool[] pinsArray)
    {
        puzzlePins = pinsArray;
        SYSTEM_FillBasicData();
    }
    private void SYSTEM_FillBasicData()
    {
        routePoints = new float[ROUTES_COUNT];
        completeness = 0f;
        puzzlePartsCount = new byte[PUZZLECOLORS_COUNT];
        colorCodesArray = new byte[PUZZLEPARTS_COUNT]; //filed with 0 - whitecolor
        foreach (byte b in blockedCells)
        {
            colorCodesArray[b] = BLACKCOLOR_CODE;
        }
    }

    public byte GenerateCellColor(byte route, byte step)
    {
        float[] varieties; // r g b c
        switch ((ResearchRoute)route)
        {
            case ResearchRoute.Foundation:
                {
                    if (step == 1) varieties = new float[] { 3, 3, 3, 1 };
                    else
                    {
                        if (step == 6) varieties = new float[] { 0, 1, 3, 3 };
                        else varieties = new float[] { 1, 0.9f, 1, 0.1f };
                    }
                    break;
                }
            case ResearchRoute.CloudWhale:
                {
                    if (step < 4) varieties = new float[] { 1, 3, 6, 2 };
                    else varieties = new float[] { 0, 2, 6, 4 };
                    break;
                }
            case ResearchRoute.Engine:
                {
                    if (step < 3) varieties = new float[] { 0, 0, 1, 0 };
                    else varieties = new float[] { 1, 2, 12, 8 };
                    break;
                }
            case ResearchRoute.Pipes:
                {
                    if (step == 1) varieties = new float[] { 0, 0, 0, 1 };
                    else
                    {
                        if (step < 4) varieties = new float[] { 1, 1, 10, 3 };
                        else varieties = new float[] { 0, 0, 1, 1 };
                    }
                    break;
                }
            case ResearchRoute.Crystal:
                {
                    if (step == 1) varieties = new float[] { 0, 0, 0, 1 };
                    else
                    {
                        varieties = new float[] { 0.2f, 0.4f, 5, 5 }; ;
                    }
                    break;
                }
            case ResearchRoute.Monument:
                {
                    if (step < 5) varieties = new float[] { 1, 2, 5, 4 };
                    else varieties = new float[] { 0, 1, 5, 5 };
                    break;
                }
            case ResearchRoute.Blossom:
                {
                    varieties = new float[] { 1, 20, 5, 10 };
                    break;
                }
            case ResearchRoute.Pollen:
                {
                    if (step == 1) varieties = new float[] { 0, 10, 0, 1 };
                    else
                    {
                        varieties = new float[] { 1, 20, 10, 10 };
                    }
                    break;
                }
            default: varieties = new float[] { 1, 1, 1, 1 }; break;
        }

        float s = varieties[0] + varieties[1] + varieties[2] + varieties[3];
        float v = Random.value;
        byte col;
        if (v < (varieties[0] + varieties[1] + varieties[2]) / s)
        {
            if (v < varieties[0]) col = REDCOLOR_CODE; else col = GREENCOLOR_CODE;
        }
        else
        {
            if (v > varieties[3]) col = CYANCOLOR_CODE; else col = BLUECOLOR_CODE;
        }
        return col;
    }

    public void AddPuzzlePart(byte colorcode)
    {
        if (colorcode < puzzlePartsCount.Length && puzzlePartsCount[colorcode] < 255) puzzlePartsCount[colorcode]++;  
    }
    public void AddResearchPoints (ResearchRoute route, float pts)
    {
        byte routeIndex = (byte)route;
        float f = routePoints[routeIndex] + pts;
        float maxvalue = STEPVALUES[STEPS_COUNT - 1];
        if (f >= maxvalue)
        {
            routePoints[routeIndex] = maxvalue;
            for (byte step = 0; step < STEPS_COUNT; step++)
            {
                if (colorCodesArray[routeButtonsIndexes[routeIndex, step]] == WHITECOLOR_CODE)
                {
                    colorCodesArray[routeButtonsIndexes[routeIndex, step]] = GenerateCellColor(routeIndex, step);
                    changesMarker++;
                }                    
            }
        }
        else
        {
            byte step = 0;
            while (step < STEPS_COUNT && f >= STEPVALUES[step])
            {
                if ( colorCodesArray[routeButtonsIndexes[routeIndex, step]] == WHITECOLOR_CODE)
                {
                    colorCodesArray[routeButtonsIndexes[routeIndex, step]] = GenerateCellColor(routeIndex, step);
                    changesMarker++;
                }
                step++;
            }
            routePoints[routeIndex] = f;
        }
    }
    public bool UnblockButton(int i)
    {
        if (IsButtonUnblocked(i)) return true;
        var colorcode = colorCodesArray[i];
        if (puzzlePartsCount[colorcode] > 0)
        {
            puzzlePartsCount[colorcode]--;
            colorCodesArray[i] = NOCOLOR_CODE;
            changesMarker++;
            for (int a = 0; a < ROUTES_COUNT; a++)
            {
                int index = routeButtonsIndexes[a, STEPS_COUNT - 3];
                if (i != index)
                {
                    index = routeButtonsIndexes[a, STEPS_COUNT - 4];
                    if (i != index) index = -1;
                    else index = 0;
                }
                else index = 1;
                if (index != -1)
                {
                    if (observer.isActiveAndEnabled)
                    {
                        int x = GetBonusStructure((ResearchRoute)a, index);
                        if (x != -1) observer.UnblockAnnouncement(x);
                    }
                }                
            }
            return true;
        }
        else return false;
    }
    public bool IsButtonUnblocked(int i)
    {
        return colorCodesArray[i] == NOCOLOR_CODE;
    }

    public void AddUnblockedBuildings(byte face, ref List<int> bdlist)
    {
        bool surf = face == Block.UP_FACE_INDEX | face == Block.SURFACE_FACE_INDEX;
        bool side = !surf & (face != Block.DOWN_FACE_INDEX) & (face != Block.CEILING_FACE_INDEX);
        int index = routeButtonsIndexes[(byte)ResearchRoute.Foundation, STEPS_COUNT - 4];
        if (IsButtonUnblocked(index)) bdlist.Add(Structure.HOTEL_BLOCK_6_ID);
        index = routeButtonsIndexes[(byte)ResearchRoute.Foundation, STEPS_COUNT - 3];
        if (IsButtonUnblocked(index) && !side) bdlist.Add(Structure.HOUSING_MAST_6_ID);
        //
        index = routeButtonsIndexes[(byte)ResearchRoute.CloudWhale, STEPS_COUNT - 4];
        if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.XSTATION_3_ID);
        index = routeButtonsIndexes[(byte)ResearchRoute.CloudWhale, STEPS_COUNT - 3];
        if (IsButtonUnblocked(index) && !surf) bdlist.Add(Structure.STABILITY_ENFORCER_ID);
        //
        index = routeButtonsIndexes[(byte)ResearchRoute.Engine, STEPS_COUNT - 4];
        //if (IsButtonUnblocked(index) && !side) bdlist.Add(Structure.Engine);
        index = routeButtonsIndexes[(byte)ResearchRoute.Engine, STEPS_COUNT - 3];
        if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.CONNECT_TOWER_6_ID);
        //
        index = routeButtonsIndexes[(byte)ResearchRoute.Pipes, STEPS_COUNT - 4];
        if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.QUANTUM_ENERGY_TRANSMITTER_5_ID);
        index = routeButtonsIndexes[(byte)ResearchRoute.Pipes, STEPS_COUNT - 3];
        //if (IsButtonUnblocked(index) && !side && !surf) bdlist.Add(Structure.CapacitorMast);
        //
        index = routeButtonsIndexes[(byte)ResearchRoute.Crystal, STEPS_COUNT - 4];
        //if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.Crystalliser);
        index = routeButtonsIndexes[(byte)ResearchRoute.CloudWhale, STEPS_COUNT - 3];
        //if (IsButtonUnblocked(index) && !surf) bdlist.Add(Structure.CrystalLightingMast);
        //
        index = routeButtonsIndexes[(byte)ResearchRoute.Monument, STEPS_COUNT - 4];
        if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.MONUMENT_ID);
        index = routeButtonsIndexes[(byte)ResearchRoute.Monument, STEPS_COUNT - 3];
        //if (IsButtonUnblocked(index) && !surf &7 !side) bdlist.Add(Structure.Anchhormast);
        //
        index = routeButtonsIndexes[(byte)ResearchRoute.Blossom, STEPS_COUNT - 4];
        //if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.Gardens);
        index = routeButtonsIndexes[(byte)ResearchRoute.CloudWhale, STEPS_COUNT - 3];
        //if (IsButtonUnblocked(index) && !surf && !side) bdlist.Add(Structure.HANgingtowermast);
        //
        index = routeButtonsIndexes[(byte)ResearchRoute.Pollen, STEPS_COUNT - 4];
        //if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.ResourceFilter);
        index = routeButtonsIndexes[(byte)ResearchRoute.Pollen, STEPS_COUNT - 3];
        //if (IsButtonUnblocked(index) && surf) bdlist.Add(Structure.ProtectorCore);

        //connected with GetBonusStructure
    }
    private int GetBonusStructure(ResearchRoute r, int i)
    {
        if (i < 0) return -1;
        switch(r)
        {
            case ResearchRoute.Foundation:
                if (i == 0) return Structure.HOTEL_BLOCK_6_ID;
                else return Structure.HOUSING_MAST_6_ID;
            case ResearchRoute.CloudWhale:
                if (i == 0) return Structure.XSTATION_3_ID;
                else return Structure.STABILITY_ENFORCER_ID;
            case ResearchRoute.Engine:
                if (i == 0) return -1;//Structure.Engine;
                else return Structure.CONNECT_TOWER_6_ID;
            case ResearchRoute.Pipes:
                if (i == 0) return Structure.QUANTUM_ENERGY_TRANSMITTER_5_ID;
                else return -1;// Structure.CapacitorMast;
            case ResearchRoute.Crystal:
                if (i == 0) return -1; //Structure.Crystalliser;
                else return -1;// crystal lightning mast
            case ResearchRoute.Monument:
                if (i == 0) return Structure.MONUMENT_ID;
                else return -1; // anchormast
            case ResearchRoute.Blossom:
                if (i == 0) return -1; // gardens
                else return -1; //hanging tower mast
            case ResearchRoute.Pollen:
                if (i == 0) return -1; // resource filter
                else return -1; // protector core
            default: return -1;
        }
        //connected with AddUnblockedBuilding
    }

    public Quest GetHelpingQuest()
    {
        byte lvl = GameMaster.realMaster.colonyController.hq.level;
        var rlist = new List<ResearchRoute>();
        for (int i =0; i < ROUTES_COUNT; i++)
        {
            if (routeBonusesMask[i] < 64) rlist.Add((ResearchRoute)i);
        }
        if (rlist.Count > 0)
        {
            var n = Random.Range(0, rlist.Count);
            ResearchRoute rr = rlist[n];
            var mlist = new List<byte>();
            byte mask = routeBonusesMask[n];
            byte x;
            switch (rr)
            {
                case ResearchRoute.Foundation:
                    x = (byte)FoundationRouteBoosters.HappinessBoost;
                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                    x = (byte)FoundationRouteBoosters.PopulationBoost;
                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                    x = (byte)FoundationRouteBoosters.ImmigrantsBoost;
                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                    if (lvl >= 4)
                    {
                        x = (byte)FoundationRouteBoosters.PointBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 6)
                        {
                            x = (byte)FoundationRouteBoosters.SettlementBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)FoundationRouteBoosters.HotelBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)FoundationRouteBoosters.HousingMastBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        }
                    }
                    break;
                case ResearchRoute.CloudWhale:
                    x = (byte)CloudWhaleRouteBoosters.GrasslandsBoost;
                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                    x = (byte)CloudWhaleRouteBoosters.StreamGensBoost;
                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                    if (lvl >= 4)
                    {
                        x = (byte)CloudWhaleRouteBoosters.CrewsBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        x = (byte)CloudWhaleRouteBoosters.ArtifactBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        x = (byte)CloudWhaleRouteBoosters.PointBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 6)
                        {
                            x = (byte)CloudWhaleRouteBoosters.XStationBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)CloudWhaleRouteBoosters.StabilityEnforcerBooster;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        }
                    }
                    break;
                case ResearchRoute.Engine:
                    x = (byte)EngineRouteBoosters.EnergyBoost;
                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                    if (lvl >=2)
                    {
                        x = (byte)EngineRouteBoosters.GearsBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 4)
                        {
                            x = (byte)EngineRouteBoosters.PointBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            if (lvl >= 5)
                            {
                                x = (byte)EngineRouteBoosters.FactoryBoost;
                                if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                if (lvl >= 6)
                                {                                    
                                    x = (byte)EngineRouteBoosters.IslandEngineBoost;
                                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                    else
                                    {
                                        x = (byte)EngineRouteBoosters.CityMoveBoost;
                                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                    }
                                    x = (byte)EngineRouteBoosters.ControlCenterBoost;
                                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                }
                            }                            
                        }
                    }
                    break;
                case ResearchRoute.Pipes:                    
                    if (lvl >= 4)
                    {
                        x = (byte)PipesRouteBoosters.FuelBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 4)
                        {
                            x = (byte)PipesRouteBoosters.PointBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            if (lvl >= 5)
                            {
                                x = (byte)PipesRouteBoosters.FarmsBoost;
                                if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                if (lvl >= 6)
                                {
                                    x = (byte)PipesRouteBoosters.BiomesBoost;
                                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                    x = (byte)PipesRouteBoosters.QETBoost;
                                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                    x = (byte)PipesRouteBoosters.CapacitorMastBoost;
                                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                }
                            }
                        }
                    }
                    break;
                case ResearchRoute.Crystal:
                    x = (byte)CrystalRouteBoosters.MoneyBoost;
                    if ((mask & (1 >> x)) == 0) mlist.Add(x);
                    if (lvl >= 4)
                    {
                        x = (byte)CrystalRouteBoosters.PointsBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 6)
                        {
                            x = (byte)CrystalRouteBoosters.PinesBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)CrystalRouteBoosters.GCubeBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)CrystalRouteBoosters.BiomeBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)CrystalRouteBoosters.CrystalliserBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)CrystalRouteBoosters.CrystalMastBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        }
                    }
                    break;
                case ResearchRoute.Monument:
                    if (lvl >= 4)
                    {
                        x = (byte)MonumentRouteBoosters.ExpeditionsBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        x = (byte)MonumentRouteBoosters.PointBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 5)
                        {
                            x = (byte)MonumentRouteBoosters.MonumentAffectionBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)MonumentRouteBoosters.LifesourceBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            if (lvl >= 6)
                            {
                                x = (byte)MonumentRouteBoosters.BiomeBoost;
                                if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                x = (byte)MonumentRouteBoosters.MonumentConstructionBoost;
                                if ((mask & (1 >> x)) == 0) mlist.Add(x);
                                x = (byte)MonumentRouteBoosters.AnchorMastBoost;
                                if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            }
                        }
                    }
                    break;
                case ResearchRoute.Blossom:
                    if (lvl >= 4)
                    {
                        x = (byte)BlossomRouteBoosters.PointBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 6)
                        {
                            x = (byte)BlossomRouteBoosters.GrasslandsBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)BlossomRouteBoosters.ArtifactBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)BlossomRouteBoosters.BiomeBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)BlossomRouteBoosters.GardensBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)BlossomRouteBoosters.HTowerBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        }
                    }
                    break;
                case ResearchRoute.Pollen:
                    if (lvl >= 4)
                    {
                        x = (byte)PollenRouteBoosters.PointBoost;
                        if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        if (lvl >= 6)
                        {
                            x = (byte)PollenRouteBoosters.FlowersBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)PollenRouteBoosters.AscensionBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)PollenRouteBoosters.CrewAccidentBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)PollenRouteBoosters.BiomeBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)PollenRouteBoosters.FilterBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                            x = (byte)PollenRouteBoosters.ProtectorCoreBoost;
                            if ((mask & (1 >> x)) == 0) mlist.Add(x);
                        }
                    }
                    break;
            }
            if (mlist.Count > 0)
            {
                byte c = (byte)Random.Range(0, mlist.Count);
                if (QuestUI.current.FindQuest(rr, c) == null) return new Quest(rr, c);
                else return Quest.NoQuest;
            }
            else return Quest.NoQuest;
        }
        return Quest.NoQuest;
    }

    public void OpenResearchTab()
    {
        GameMaster.realMaster.environmentMaster.DisableDecorations();
        if (observer == null)
        {
            observer = GameObject.Instantiate(Resources.Load<GameObject>("UIPrefs/knowledgeTab")).GetComponent<KnowledgeTabUI>();
            observer.Prepare(this);            
        }
        if (!observer.gameObject.activeSelf) observer.gameObject.SetActive(true);
        observer.Redraw();
    }

    public (byte,byte) CellIndexToRouteAndStep(int buttonIndex)
    {
        for (byte ri = 0; ri < ROUTES_COUNT; ri++)
        {
            for (byte si = 0; si < STEPS_COUNT;si++)
            {
                if (routeButtonsIndexes[ri, si] == buttonIndex) return (ri,si);
            }
        }
        return (255,255);
    }

    #region save-load
    public void Save(System.IO.FileStream fs)
    {
        const byte truebyte = 1, falsebyte = 0;
        int i;
        for (i = 0; i< PUZZLE_PINS_COUNT; i++)
        {
            fs.WriteByte(puzzlePins[i] ? truebyte : falsebyte); 
        }
        //
        for (i = 0; i< ROUTES_COUNT; i++)
        {
            fs.Write(System.BitConverter.GetBytes(routePoints[i]),0,4);            
        }
        //
        for (i = 0; i < PUZZLECOLORS_COUNT; i++)
        {
            fs.WriteByte(puzzlePartsCount[i]);
        }
        //
        for (i = 0; i < PUZZLEPARTS_COUNT; i++)
        {
            fs.WriteByte(colorCodesArray[i]);
        }
        for (i = 0; i < ROUTES_COUNT; i++)
        {
            fs.WriteByte(routeBonusesMask[i]);
        }
    }
    public static void Load(System.IO.FileStream fs)
    {
        int i;
        var pins = new bool[PUZZLE_PINS_COUNT];
        for (i = 0; i < PUZZLE_PINS_COUNT; i++)
        {
            pins[i] = fs.ReadByte() == 1;
        }
        current = new Knowledge(pins);
        //
        current.routePoints = new float[ROUTES_COUNT];
        var data = new byte[4];
        for (i = 0;i < ROUTES_COUNT; i++)
        {
            fs.Read(data, 0, 4);
            current.routePoints[i] = System.BitConverter.ToSingle(data, 0);
        }
        //
        data = new byte[PUZZLECOLORS_COUNT];        
        fs.Read(data, 0, PUZZLECOLORS_COUNT);
        current.puzzlePartsCount = new byte[PUZZLECOLORS_COUNT];
        for (i = 0; i < PUZZLECOLORS_COUNT; i++)
        {
            current.puzzlePartsCount[i] = data[i];
        }
        //
        var cca = new byte[PUZZLEPARTS_COUNT];
        for (i = 0; i< PUZZLEPARTS_COUNT; i++)
        {
            cca[i] = (byte)fs.ReadByte();
        }
        current.colorCodesArray = cca;
        //
        current.routeBonusesMask = new byte[ROUTES_COUNT];
        for (i = 0; i < ROUTES_COUNT; i++)
        {
            current.routeBonusesMask[i] = (byte)fs.ReadByte();
        }
    }
    #endregion
}
