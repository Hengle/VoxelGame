﻿using System.Collections.Generic;
using UnityEngine;

public class GatherSite : Worksite
{
    private float destructionTimer;
    private const int START_WORKERS_COUNT = 5;
    // public const int MAX_WORKERS = 32

    public GatherSite(Plane i_plane) : base(i_plane)
    {
        sign = Object.Instantiate(Resources.Load<GameObject>("Prefs/GatherSign")).GetComponent<WorksiteSign>();
        sign.worksite = this;
        sign.transform.position = workplace.GetCenterPosition() + workplace.GetLookVector()  * 0.01f;
        actionLabel = Localization.GetActionLabel(LocalizationActionLabels.GatherInProgress);
        colony.SendWorkers(START_WORKERS_COUNT, this);
        destructionTimer = 10;
        gearsDamage = GameConstants.GEARS_DAMAGE_COEFFICIENT * 0.33f;
    }

    override public void WorkUpdate()
    {
        if (workplace == null)
        {
            StopWork(true);
        }
        if (workersCount > 0)
        {
            workSpeed = colony.workspeed * workersCount * GameConstants.GATHERING_SPEED;
            workflow += workSpeed;
            colony.gears_coefficient -= gearsDamage * workSpeed;
            if (workflow >= 1f)
            {
                int i = 0;
                bool resourcesFound = false;
                List<Structure> strs = workplace.GetStructuresList();
                if (strs != null)
                {
                    while (i < strs.Count & workflow > 0)
                    {
                        switch (strs[i].ID)
                        {
                            case Structure.PLANT_ID:
                                Plant p = strs[i] as Plant;
                                if (p != null)
                                {
                                    p.Harvest(false);
                                    resourcesFound = true;
                                    workflow--;
                                }
                                break;
                            case Structure.CONTAINER_ID:
                                HarvestableResource hr = strs[i] as HarvestableResource;
                                if (hr != null)
                                {
                                    hr.Harvest();
                                    resourcesFound = true;
                                    workflow--;
                                }
                                break;
                            case Structure.RESOURCE_STICK_ID:
                                ScalableHarvestableResource shr = strs[i] as ScalableHarvestableResource;
                                if (shr != null)
                                {
                                    shr.Harvest();
                                    resourcesFound = true;
                                    workflow--;
                                }
                                break;
                        }
                        i++;
                    }
                    if (resourcesFound) destructionTimer = GameMaster.LABOUR_TICK * 10;
                }
            }
        }
        else workSpeed = 0f;
        destructionTimer -= GameMaster.LABOUR_TICK;
        if (destructionTimer <= 0) StopWork(true);
    }
    

    #region save-load system
    override public void Save(System.IO.FileStream fs)
    {
        if (workplace == null)
        {
            StopWork(true);
            return;
        }
        else
        {
            var pos = workplace.pos;
            fs.WriteByte((byte)WorksiteType.GatherSite);
            fs.WriteByte(pos.x);
            fs.WriteByte(pos.y);
            fs.WriteByte(pos.z);
            fs.WriteByte(workplace.faceIndex);
            fs.Write(System.BitConverter.GetBytes(destructionTimer),0,4);
            SerializeWorksite(fs);
        }
    }
    public static GatherSite Load(System.IO.FileStream fs, Chunk chunk)
    {
        var data = new byte[8];
        fs.Read(data, 0, data.Length);
        Plane plane = null;
        if (chunk.GetBlock(data[0], data[1], data[2])?.TryGetPlane(data[3], out plane) == true)
        {
            var cs = new GatherSite(plane);
            cs.destructionTimer = System.BitConverter.ToSingle(data, 4);
            cs.LoadWorksiteData(fs);
            return cs;
        }
        else
        {
            Debug.Log("gather site load error");
            return null;
        }
    }

    #endregion
}
