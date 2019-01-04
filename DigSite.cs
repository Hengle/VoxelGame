using UnityEngine;
using System.Collections.Generic;

public class DigSite : Worksite {
	public bool dig = true;
	ResourceType mainResource;
	CubeBlock workObject;
    const int START_WORKERS_COUNT = 10;

    override public int GetMaxWorkers() { return 64; }

    override public void WorkUpdate () {
		if (workObject ==null || (workObject.volume == CubeBlock.MAX_VOLUME && dig == false) || (workObject.volume ==0 && dig == true)) {
            StopWork();
			return;
		}
		if (workersCount > 0) {
			workflow += workSpeed ;
			if (workflow >= 1) LabourResult();
		}
	}

	void LabourResult() {
			int x = (int) workflow;
			float production = x;
			if (dig) {
				production = workObject.Dig(x, true);
				GameMaster.geologyModule.CalculateOutput(production, workObject, GameMaster.realMaster.colonyController.storage);
			}
			else {
				production = GameMaster.realMaster.colonyController.storage.GetResources(mainResource, production);
				if (production != 0) {
					production = workObject.PourIn((int)production);
					if (production == 0) { StopWork(); return;}
				}
			}
			workflow -= production;
        if (dig)
        {
            actionLabel = Localization.GetActionLabel(LocalizationActionLabels.DigInProgress) + " (" + ((int)((1 - (float)workObject.volume / (float)CubeBlock.MAX_VOLUME) * 100)).ToString() + "%)";
        }
        else
        {
            actionLabel = Localization.GetActionLabel(LocalizationActionLabels.PouringInProgress) + " (" + ((int)(((float)workObject.volume / (float)CubeBlock.MAX_VOLUME) * 100)).ToString() + "%)";
        }
	}

	protected override void RecalculateWorkspeed() {
		workSpeed = GameMaster.realMaster.CalculateWorkspeed(workersCount,WorkType.Digging);
	}

	public void Set(CubeBlock block, bool work_is_dig) {
		workObject = block;
        workObject.SetWorksite(this);
		dig = work_is_dig;
		if (dig) {
			Block b = transform.root.GetComponent<Chunk>().GetBlock(block.pos.x, block.pos.y + 1, block.pos.z);
			if (b != null && (b.type == BlockType.Surface & b.worksite == null)) {
				CleanSite cs = b.gameObject.AddComponent<CleanSite>();
                TransferWorkers(this, cs);
                cs.Set(b as SurfaceBlock, true);
                if (showOnGUI)
                {
                    cs.ShowOnGUI();
                    showOnGUI = false;
                }
                StopWork();
                return;
			}
			sign = Instantiate(Resources.Load<GameObject> ("Prefs/DigSign")).GetComponent<WorksiteSign>(); 
		}
		else 	sign = Instantiate(Resources.Load<GameObject>("Prefs/PourInSign")).GetComponent<WorksiteSign>();
		sign.worksite = this;
		sign.transform.position = workObject.transform.position + Vector3.up * Block.QUAD_SIZE;
        //FollowingCamera.main.cameraChangedEvent += SignCameraUpdate;

        mainResource = ResourceType.GetResourceTypeById(workObject.material_id);
		if (workersCount < START_WORKERS_COUNT) GameMaster.realMaster.colonyController.SendWorkers(START_WORKERS_COUNT, this);
        if (!worksitesList.Contains(this)) worksitesList.Add(this);
        if (!subscribedToUpdate)
        {
            GameMaster.realMaster.labourUpdateEvent += WorkUpdate;
            subscribedToUpdate = true;
        }
    }

  //  public void SignCameraUpdate()
   // {
   //     sign.transform.LookAt(FollowingCamera.camPos);
   // }

    override public void StopWork()
    {
        if (destroyed) return;
        else destroyed = true;
        if (workersCount > 0)
        {
            GameMaster.realMaster.colonyController.AddWorkers(workersCount);
            workersCount = 0;
        }
        if (sign != null)
        {
           // FollowingCamera.main.cameraChangedEvent -= SignCameraUpdate;
            Destroy(sign.gameObject);
        }
        if (workObject != null)
        {            
            if ( workObject.excavatingStatus == 0) workObject.myChunk.AddBlock(new ChunkPos(workObject.pos.x, workObject.pos.y + 1, workObject.pos.z), BlockType.Surface, workObject.material_id, false);
            if (workObject.worksite == this) workObject.ResetWorksite();
        }
        if (worksitesList.Contains(this)) worksitesList.Remove(this);
        if (subscribedToUpdate)
        {
            GameMaster.realMaster.labourUpdateEvent -= WorkUpdate;
            subscribedToUpdate = false;
        }
        if (showOnGUI)
        {
            observer.SelfShutOff();
            showOnGUI = false;
            if (workObject != null) UIController.current.ChangeChosenObject(workObject);
            else UIController.current.ChangeChosenObject(ChosenObjectType.None);
        }
        Destroy(this);
    }

    #region save-load system
    override protected List<byte> Save() {
		if (workObject == null) {
            StopWork();
			return null;
		}
        var data = new List<byte>() { (byte)WorksiteType.DigSite };
        data.Add(workObject.pos.x);
        data.Add(workObject.pos.y);
        data.Add(workObject.pos.z);
        data.Add(dig ? (byte)1 : (byte)0);
        data.AddRange(SerializeWorksite());
		return data;
	}
	override protected void Load(System.IO.FileStream fs, ChunkPos pos) {
		Set(transform.root.GetComponent<Chunk>().GetBlock(pos) as CubeBlock, fs.ReadByte() == 1);
        LoadWorksiteData(fs);
	}
	#endregion
}
