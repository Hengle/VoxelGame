﻿public sealed class LifeSource : MultiblockStructure {
    private int tick = 0, lifepowerPerTick = 500;
    const int MAXIMUM_TICKS = 1000;

    override public void SetBasement(SurfaceBlock b, PixelPosByte pos) {
		if (b == null) return;
		SetMultiblockStructureData(b,pos);
        if (!subscribedToUpdate)
        {
            GameMaster.realMaster.lifepowerUpdateEvent += LifepowerUpdate;
            subscribedToUpdate = true;
        }
	}

	public void LifepowerUpdate () {
        tick++;
        basement.myChunk.AddLifePower(lifepowerPerTick);
        if (tick == MAXIMUM_TICKS) Annihilate(false);
	}

    override public void Annihilate(bool forced)
    {
        if (destroyed) return;
        else destroyed = true;
        PrepareStructureForDestruction(forced);
        if (subscribedToUpdate)
        {
            GameMaster.realMaster.lifepowerUpdateEvent -= LifepowerUpdate;
            subscribedToUpdate = false;
        }
        Destroy(gameObject);
    }
}
