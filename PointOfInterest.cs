﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PointOfInterest : MapPoint
{
    public bool explored { get; protected set; }
    public Expedition sentExpedition { get; protected set; }
    private List<Mission> availableMissions;

    public PointOfInterest(float i_angle, float i_height, byte ring, MapMarkerType mtype) : base(i_angle, i_height, ring, mtype)
    {
        explored = false;
        availableMissions = new List<Mission>() { new Mission(Mission.EXPLORE_MISSION_ID, true) };
    }

    public void SendExpedition(Expedition e)
    {
        if (sentExpedition == null)
        {
            sentExpedition = e;
            e.Launch(this);
        }
        else
        {
            if (sentExpedition.stage == Expedition.ExpeditionStage.Preparation)
            {
                int d_id = sentExpedition.ID;
                sentExpedition = null;
                Expedition.DismissExpedition(d_id);
                sentExpedition = e;
            }
        }        
    }
    public void ReturnExpedition()
    {

    }
    
    public List<Dropdown.OptionData> GetAvailableMissionsDropdownData()
    {
        var l = new List<Dropdown.OptionData>();
        foreach (Mission m in availableMissions)
        {
            l.Add(new Dropdown.OptionData(m.codename));
        }
        return l;
    }
    public Mission GetMission(int index)
    {
        if (index < 0 | index >= availableMissions.Count) return Mission.NoMission;
        else return availableMissions[index];
    }

    public override bool DestroyRequest()
    {
        if (sentExpedition != null && (sentExpedition.stage == Expedition.ExpeditionStage.WayIn | sentExpedition.stage == Expedition.ExpeditionStage.OnMission)) return false;
        else return true;
    }
}