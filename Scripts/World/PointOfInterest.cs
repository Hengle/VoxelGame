﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PointOfInterest : MapPoint
{
    public float richness { get; protected set; }
    public float danger { get; protected set; }
    public float mysteria { get; protected set; }
    public float friendliness { get; protected set; }
    public float difficulty { get; protected set; }
    private float _exploredPart;
    public float exploredPart
    {
        get { return _exploredPart; }
        private set
        {
            if (value > 1) _exploredPart = 1f;
            else
            {
                if (value < 0) _exploredPart = 0f;
                else _exploredPart = value;
            }
        }
    }
    public Mission[] availableMissions;
    public Expedition sentExpedition; // не больше одной экспедиции на точку  

    public PointOfInterest(int i_id) : base(i_id) { }
    public PointOfInterest(float i_angle, float i_height, MapMarkerType mtype) : base(i_angle, i_height, mtype)
    {
        float locationDifficulty = 0f;
        switch (mtype)
        {
            case MapMarkerType.Unknown:
                richness = Random.value;
                danger = Random.value;
                mysteria = 1f;
                friendliness = Random.value;
                locationDifficulty = Random.value;
                break;
            case MapMarkerType.MyCity:
                richness = 0.05f;
                danger = 0f;
                mysteria = 0.01f;
                friendliness = GameMaster.realMaster.environmentMaster.environmentalConditions;
                locationDifficulty = 0f;
                break;
            case MapMarkerType.Star:
                richness = 0.1f;
                danger = 1f;
                mysteria = 0.1f + Random.value * 0.2f;
                friendliness = Random.value;
                locationDifficulty = 1f;
                break;
            case MapMarkerType.Station:
                richness = Random.value * 0.8f + 0.2f;
                danger = Random.value * 0.2f;
                mysteria = Random.value * 0.5f;
                friendliness = Random.value;
                locationDifficulty = 0.2f + Random.value * 0.2f;
                break;
            case MapMarkerType.Wreck:
                richness = 0.3f + Random.value * 0.5f;
                danger = 0.3f + Random.value * 0.2f;
                mysteria = 0.1f * Random.value;
                friendliness = Random.value * 0.8f;
                locationDifficulty = 0.7f + Random.value * 0.2f;
                break;
            case MapMarkerType.Shuttle:
                richness = 0f;
                danger = 0f;
                mysteria = 0f;
                friendliness = 1f;
                locationDifficulty = 0f;
                break; // flyingExpedition.expedition.sectorCollapsingTest
            case MapMarkerType.Island:
                richness = 0.2f + Random.value * 0.7f;
                danger = Random.value * 0.5f;
                mysteria = 0.2f + Random.value * 0.5f;
                friendliness = Random.value * 0.7f + 0.3f;
                locationDifficulty = Random.value * 0.4f + 0.4f * danger;
                break;
            case MapMarkerType.SOS:
                richness = 0f;
                danger = 0.2f + 0.8f * Random.value;
                mysteria = Random.value * 0.3f;
                friendliness = Random.value * 0.5f + 0.5f;
                locationDifficulty = Random.value * 0.5f + 0.25f;
                break;
            case MapMarkerType.Portal:
                richness = 0.1f;
                danger = 0.3f + Random.value;
                mysteria = 0.3f + Random.value * 0.7f;
                friendliness = Random.value;
                locationDifficulty = Random.value * 0.6f + 0.4f;
                break;
            case MapMarkerType.QuestMark:
                // устанавливается квестом                
                break;
            case MapMarkerType.Colony:
                richness = 0.5f + Random.value * 0.5f;
                danger = 0.1f * Random.value;
                mysteria = 0.1f * Random.value;
                friendliness = Random.value * 0.7f + 0.3f;
                locationDifficulty = 0.3f * Random.value;
                break;
            case MapMarkerType.Wiseman:
                richness = 0.1f;
                danger = 0.1f * Random.value;
                mysteria = Random.value;
                friendliness = Random.value * 0.3f + 0.7f;
                locationDifficulty = Random.value * (1 - friendliness);
                break;
            case MapMarkerType.Wonder:
                richness = 0.3f + Random.value * 0.7f;
                danger = 0.1f * Random.value;
                mysteria = 0.5f + Random.value * 0.5f;
                friendliness = Random.value;
                locationDifficulty = Random.value;
                break;
            case MapMarkerType.Resources:
                richness = 0.5f + Random.value * 0.5f;
                danger = Random.value * 0.3f;
                mysteria = 0.1f * Random.value;
                friendliness = Random.value * 0.6f;
                locationDifficulty = 0.25f + 0.25f * Random.value;
                break;
        }
        difficulty =  ((danger + mysteria + locationDifficulty - friendliness) * 0.5f + Random.value * 0.5f) * (1f + GameMaster.realMaster.GetDifficultyCoefficient()) * 0.5f;
        if (difficulty < 0f) difficulty = 0f; // ну мало ли
        exploredPart = 0f;
    }
    
    public List<Dropdown.OptionData> GetAvailableMissionsDropdownData()
    {
        var l = new List<Dropdown.OptionData>();
        if (availableMissions != null)
        {
            foreach (Mission m in availableMissions)
            {
                l.Add(new Dropdown.OptionData(Localization.GetMissionStandartName(m.type)));
            }
        }        
        return l;
    }
    public Mission GetMission(int index)
    {
        if (index < 0 | index >= availableMissions.Length) return Mission.NoMission;
        else return availableMissions[index];
    }
    public void Explore(float k)
    {
        exploredPart += GameConstants.POINT_EXPLORE_SPEED * k;
    }

    protected Artifact GetArtifact()
    {
        float a = mysteria * friendliness * 0.8f + 0.2f * Random.value;
        var atype = Artifact.AffectionType.NoAffection;
        bool researched = false;
        switch (type)
        {
            case MapMarkerType.Unknown:
                atype = Artifact.AffectionType.SpaceAffection;
                break;
            case MapMarkerType.Station:
                if (Random.value > 0.5f) researched = true;
                atype = Random.value > 0.5f ? Artifact.AffectionType.LifepowerAffection : Artifact.AffectionType.StabilityAffection;
                break;
            case MapMarkerType.Wreck:
                if (Random.value < 0.1f) researched = true;
                break;
            case MapMarkerType.Island:
                atype = Random.value > 0.4f ? Artifact.AffectionType.StabilityAffection : Artifact.AffectionType.LifepowerAffection;
                break;
            case MapMarkerType.Portal:
                if (Random.value > 0.5f) atype = Artifact.AffectionType.StabilityAffection;
                break;
            case MapMarkerType.Colony:
                if (Random.value > 0.6f) atype = Artifact.AffectionType.LifepowerAffection; else atype = Artifact.AffectionType.StabilityAffection;
                if (Random.value > 0.3f) researched = true;
                break;
            case MapMarkerType.Wiseman:
                {
                    float f = Random.value;
                    if (f <= 0.33f) atype = Artifact.AffectionType.SpaceAffection;
                    else
                    {
                        if (f >= 0.66f) atype = Artifact.AffectionType.StabilityAffection;
                        else atype = Artifact.AffectionType.LifepowerAffection;
                    }
                    researched = true;
                    break;
                }
            case MapMarkerType.Wonder:
                atype = Artifact.AffectionType.StabilityAffection;
                break;
        }

        var art = new Artifact(
            (friendliness * 0.5f + richness * 0.5f) * 0.6f * Random.value + 0.4f,
            (richness * 0.7f + 0.3f * Random.value) * 0.6f + 0.4f * Random.value,
            (danger * richness * 0.55f + 0.45f * Random.value) * 0.5f + 0.5f * Random.value,
           atype
            );
        art.SetResearchStatus(researched);
        return art;
    }

    public void TakeTreasure(Crew c)
    {
        if (c.artifact == null)
        {
            if (Random.value * c.perception * c.perception * c.luck > Random.value * mysteria)
            {
                c.SetArtifact(GetArtifact());
                GameLogUI.MakeAnnouncement(Localization.GetPhrase(LocalizedPhrase.CrewFoundArtifact));
                return;
            }
        }
        GainResources(c);
    }    
    public void GainResources(Crew c)
    {
        List<ResourceType> typesList = null;
        switch (type)
        {
            case MapMarkerType.Station:
                typesList = new List<ResourceType>() { ResourceType.metal_K, ResourceType.metal_M, ResourceType.Plastics, ResourceType.Fuel };
                if (Random.value < 0.4f) c.AddExperience(Expedition.ONE_STEP_XP);
                break;
            case MapMarkerType.Wreck:
                typesList = new List<ResourceType>() { ResourceType.metal_S, ResourceType.Fuel, ResourceType.metal_M, ResourceType.Graphonium };
                break;
            case MapMarkerType.Island:
                typesList = new List<ResourceType>() { ResourceType.metal_M_ore, ResourceType.metal_E_ore, ResourceType.metal_N_ore, ResourceType.metal_P_ore };
                break;
            case MapMarkerType.SOS:
                typesList = new List<ResourceType>() { ResourceType.metal_S, ResourceType.Fuel, ResourceType.Supplies };
                if (Random.value < 0.3f) c.AddExperience(Expedition.ONE_STEP_XP);
                break;
            case MapMarkerType.Portal:
                typesList = new List<ResourceType>() { ResourceType.metal_N, ResourceType.metal_N_ore, ResourceType.Graphonium };
                break;
            case MapMarkerType.QuestMark:
                // ?
                break;
            case MapMarkerType.Colony:
                typesList = new List<ResourceType>() { ResourceType.Food, ResourceType.Supplies, ResourceType.metal_K };
                break;
            case MapMarkerType.Wiseman:
                if (Random.value > 0.3f) c.AddExperience(Expedition.ONE_STEP_XP * 10f);
                else c.ImproveNativeParameters();
                break;
            case MapMarkerType.Wonder:
                if (Random.value > 0.5f) typesList = new List<ResourceType>() { ResourceType.metal_N, ResourceType.Graphonium };
                else c.AddExperience(Expedition.ONE_STEP_XP * 2f);
                break;
            case MapMarkerType.Resources: // or changing!
                typesList = new List<ResourceType>()
                {
                    ResourceType.metal_K_ore, ResourceType.metal_M_ore, ResourceType.metal_N_ore, ResourceType.metal_E_ore, ResourceType.metal_P_ore,
                    ResourceType.metal_S_ore, ResourceType.mineral_F, ResourceType.mineral_L
                };
                break;
        }
        if (typesList != null && typesList.Count > 0)
        {
            GameMaster.realMaster.colonyController.storage.AddResource(typesList[Random.Range(0,typesList.Count - 1)], 50f * c.membersCount / (float)Crew.MAX_MEMBER_COUNT * c.persistence);
        }
    }

    #region save-load
    override public List<byte> Save()
    {
        var data = base.Save();
        data.AddRange(System.BitConverter.GetBytes(richness)); // 0 - 3
        data.AddRange(System.BitConverter.GetBytes(danger)); // 4 - 7
        data.AddRange(System.BitConverter.GetBytes(mysteria)); // 8 - 11
        data.AddRange(System.BitConverter.GetBytes(friendliness)); // 12 - 15
        data.AddRange(System.BitConverter.GetBytes(difficulty)); // 16 - 19
        data.AddRange(System.BitConverter.GetBytes(exploredPart)); // 20 - 23
        // 24 :
        byte n = 0;        
        if (availableMissions != null)
        {
            n = (byte)availableMissions.Length;
            if (n > 0)
            {
                foreach (Mission m in availableMissions) data.AddRange(m.Save());
            }
            data.Add(n);
        }
        else  data.Add(n);
        return data;
    }
    public void Load(System.IO.FileStream fs)
    {
        int LENGTH = 25;
        var data = new byte[LENGTH];
        fs.Read(data, 0, LENGTH);
        richness = System.BitConverter.ToSingle(data, 0);
        danger = System.BitConverter.ToSingle(data, 4);
        mysteria = System.BitConverter.ToSingle(data, 8);
        friendliness = System.BitConverter.ToSingle(data, 12);
        difficulty = System.BitConverter.ToSingle(data, 16);
        exploredPart = System.BitConverter.ToSingle(data, 20);
        byte n = data[24];
        if (n > 0)
        {
            availableMissions = new Mission[n];
            Mission.StaticLoad(fs, n, this);
        }
    } 
    #endregion
}