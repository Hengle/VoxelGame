using UnityEngine;
using System.Collections.Generic;

public class DigSite : Worksite {
	public bool dig = true;
	BlockExtension workObject;
    const int START_WORKERS_COUNT = 10;

    override public int GetMaxWorkers() { return 64; }

    public DigSite(Plane i_plane, bool work_is_dig) : this(i_plane, work_is_dig, START_WORKERS_COUNT) { }
    public DigSite(Plane i_plane, bool work_is_dig, int startWorkers) : base (i_plane)
    {
        workObject = workplace.GetBlock().GetExtension();
        if (workObject == null)
        {
            StopWork(true);
            return;
        }
        dig = work_is_dig;
        if (i_plane.faceIndex == Block.SURFACE_FACE_INDEX | i_plane.faceIndex == Block.UP_FACE_INDEX)
        {
            if (dig)
            {
                sign = Object.Instantiate(Resources.Load<GameObject>("Prefs/DigSign")).GetComponent<WorksiteSign>();
            }
            else sign = Object.Instantiate(Resources.Load<GameObject>("Prefs/PourInSign")).GetComponent<WorksiteSign>();
            sign.transform.position = workplace.GetCenterPosition() + workplace.GetLookVector() * Block.QUAD_SIZE * 0.5f;
        }
        else
        {
            sign = Object.Instantiate(Resources.Load<GameObject>("Prefs/tunnelBuildingSign")).GetComponent<WorksiteSign>();
            sign.transform.position = workplace.GetCenterPosition();
            sign.transform.rotation = Quaternion.Euler(workplace.GetEulerRotationForQuad());
        }
		sign.worksite = this;     

        int wom = workObject.materialID;
        if (workplace.materialID != wom) workplace.ChangeMaterial(wom, true);
		if (startWorkers != 0) colony.SendWorkers(startWorkers, this);
    }

    override public void WorkUpdate () {
		if (workersCount > 0) {
			workflow += workSpeed ;
            colony.gears_coefficient -= gearsDamage;
			if (workflow >= 1f) LabourResult();
		}
	}

    void LabourResult()
    {
        int x = (int)workflow;
        float production = x;
        if (dig)
        {
            production = workObject.Dig(x, true, workplace.faceIndex);
            if (production == 0f)
            {
                StopWork(true);
                return;
            }
        }
        else
        {
            production = workObject.Dig(x, true, workplace.faceIndex);
            if (production != 0)
            {
                production = workObject.PourIn((int)production, workplace.faceIndex);
                if (production == 0) { StopWork(true); return; }
            }
        }
        workflow -= production;
        if (dig)
        {
            actionLabel = Localization.GetActionLabel(LocalizationActionLabels.DigInProgress) + " (" + ((int)((1 - workObject.GetVolumePercent()) * 100f)).ToString() + "%)";
        }
        else
        {
            actionLabel = Localization.GetActionLabel(LocalizationActionLabels.PouringInProgress) + " (" + ((int)(workObject.GetVolumePercent() * 100f)).ToString() + "%)";
        }
    }

    protected override void RecalculateWorkspeed() {
        workSpeed = colony.labourCoefficient * workersCount * GameConstants.DIGGING_SPEED;
        gearsDamage = GameConstants.WORKSITES_GEARS_DAMAGE_COEFFICIENT * workSpeed;
	}

    #region save-load system
    override public void Save(System.IO.FileStream fs) {
        if (workObject == null) {
            StopWork(true);
            return;
        }
        else {
            var pos = workplace.pos;
            fs.WriteByte((byte)WorksiteType.DigSite);
            fs.WriteByte(pos.x);
            fs.WriteByte(pos.y);
            fs.WriteByte(pos.z);
            fs.WriteByte(workplace.faceIndex);
            fs.WriteByte(dig ? (byte)1 : (byte)0);
            SerializeWorksite(fs);
        }
	}
	public static DigSite Load(System.IO.FileStream fs, Chunk chunk)
    {
        var data = new byte[5];
        fs.Read(data, 0, data.Length);
        Plane plane = null;
        if (chunk.GetBlock(data[0], data[1], data[2])?.TryGetPlane(data[3], out plane) == true)
        {
            var cs = new DigSite(plane, data[4] == 1);
            cs.LoadWorksiteData(fs);
            return cs;
        }
        else
        {
            Debug.Log("digsite load error");
            return null;
        }
    }
	#endregion
}
