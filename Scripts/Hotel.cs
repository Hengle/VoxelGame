﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Hotel : Building
{
    public byte lodgersCount { get; private set; }
    private const float RENT = 1f, NEGATIVE_EFFECT_TIMER = 25f;
    private const byte MAX_LODGERS_COUNT = 150;
    private static List<Hotel> hotels;

    public static void DistributeLodgers(int x)
    {
        if (hotels != null )
        {
            int count = hotels.Count;
            if (count == 1)
            {
                var h = hotels[0];
                if (h.lodgersCount + x > MAX_LODGERS_COUNT) h.lodgersCount = MAX_LODGERS_COUNT;
                else h.lodgersCount += (byte)x;
            }
            else
            {
                int i = Random.Range(0, count);
                var h = hotels[i];
                if ( h.lodgersCount + x <= MAX_LODGERS_COUNT) h.lodgersCount += (byte)x;
                else
                {
                    x -= MAX_LODGERS_COUNT - h.lodgersCount;
                    h.lodgersCount = MAX_LODGERS_COUNT;
                    i += 1;
                    if (i == count) i = 0;
                    h = hotels[i];
                    if (h.lodgersCount + x <= MAX_LODGERS_COUNT) h.lodgersCount += (byte)x;
                    else h.lodgersCount = MAX_LODGERS_COUNT;
                }
            }
        }
    }

    override public void Prepare() {
        PrepareBuilding();
        lodgersCount = 0;
    }

    override public void SetBasement(SurfaceBlock b, PixelPosByte pos)
    {
        if (b == null) return;
        SetBuildingData(b, pos);
        if (!subscribedToUpdate)
        {
            GameMaster.realMaster.everydayUpdate += EverydayUpdate;
            subscribedToUpdate = true;
            if (hotels != null) hotels.Add(this);
            else hotels = new List<Hotel>() { this };
        }
    }

    override public void SetActivationStatus(bool x, bool recalculateAfter)
    {
        isActive = x;
        if (connectedToPowerGrid & recalculateAfter)
        {
            GameMaster.realMaster.colonyController.RecalculatePowerGrid();
        }
        Switch(isActive & isEnergySupplied);        
    }
    override public void SetEnergySupply(bool x, bool recalculateAfter)
    {
        isEnergySupplied = x;
        if (connectedToPowerGrid & recalculateAfter) GameMaster.realMaster.colonyController.RecalculatePowerGrid();
        Switch(isActive & isEnergySupplied);
    }
    private void Switch(bool x)
    {
        if (x == false)
        {            
            if (subscribedToUpdate)
            {
                var gm = GameMaster.realMaster;
                gm.everydayUpdate -= EverydayUpdate;
                subscribedToUpdate = false;
                var cc = gm.colonyController;
                cc.AddHappinessAffect(lodgersCount * 2f / (float)cc.citizenCount, NEGATIVE_EFFECT_TIMER);
            }
            lodgersCount = 0;
        }
        else
        {
            if (!subscribedToUpdate)
            {
                GameMaster.realMaster.everydayUpdate += EverydayUpdate;
                subscribedToUpdate = true;
            }
        }
        ChangeRenderersView(x);
    }

    private void EverydayUpdate()
    {
        if (lodgersCount > 0)
        {            
            var c = GameMaster.realMaster.colonyController;            
            c.AddEnergyCrystals(lodgersCount * RENT * c.happiness_coefficient);
            if (Random.value > c.happiness_coefficient)
            {
                if (lodgersCount == 1) lodgersCount = 0;
                else
                {
                    lodgersCount -= (byte)(Random.value * 0.5f * lodgersCount);
                }
            }
        }
    }

    override public void Annihilate(bool clearFromSurface, bool returnResources, bool leaveRuins)
    {
        if (destroyed) return;
        else destroyed = true;
        if (subscribedToUpdate)
        {
            GameMaster.realMaster.everydayUpdate -= EverydayUpdate;
            subscribedToUpdate = false;
        }
        PrepareBuildingForDestruction(clearFromSurface, returnResources, leaveRuins);
        hotels.Remove(this);
        if (lodgersCount > 0) DistributeLodgers(lodgersCount);
        Destroy(gameObject);
    }

    #region save-load system
    override public List<byte> Save()
    {
        var data = base.Save();
        data.Add(lodgersCount);
        return data;
    }

    override public void Load(System.IO.FileStream fs, SurfaceBlock sblock)
    {
        base.Load(fs, sblock);
        lodgersCount = (byte)fs.ReadByte();
    }
    #endregion
}
