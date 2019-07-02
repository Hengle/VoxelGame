﻿using System.Collections.Generic;
public sealed class PsychokineticGenerator : WorkBuilding
{
    public const float ENERGY_MULTIPLIER = 1f;

    override public void SetBasement(SurfaceBlock b, PixelPosByte pos)
    {
        if (b == null) return;
        SetBuildingData(b, pos);
    }

    override public void RecalculateWorkspeed()
    {
        energySurplus = workersCount * ENERGY_MULTIPLIER;
        colony.powerGridChanged = true;
    }

    override public void Annihilate(bool clearFromSurface, bool returnResources, bool leaveRuins)
    {
        if (destroyed) return;
        else destroyed = true;
        PrepareWorkbuildingForDestruction(clearFromSurface, returnResources, leaveRuins);
        Destroy(gameObject);
    }

    #region save-load system
    override public List<byte> Save()
    {
        var data = SaveStructureData();
        data.AddRange(SaveBuildingData());
        data.AddRange(System.BitConverter.GetBytes(workersCount));
        return data;
    }
    override public void Load(System.IO.FileStream fs, SurfaceBlock sblock)
    {
        LoadStructureData(fs, sblock);
        LoadBuildingData(fs);
        var data = new byte[4];
        fs.Read(data, 0, 4);
        workersCount = System.BitConverter.ToInt32(data, 0);
        RecalculateWorkspeed();
    }
    #endregion
}
