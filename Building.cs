﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : Structure {
	public int upgradedIndex = -1;
	public bool canBePowerSwitched = true;
	public bool isActive {get;protected set;}
	public bool energySupplied {get;protected set;} // подключение, контролирующееся Colony Controller'ом
	[SerializeField]
	public int resourcesContainIndex = 0;
	public float energySurplus = 0, energyCapacity = 0;
	public  bool connectedToPowerGrid {get; protected set;}// подключение, контролирующееся игроком
	public int requiredBasementMaterialId = -1;
	[SerializeField]
	protected Renderer[] myRenderers;
	protected static ResourceContainer[] requiredResources;

	void Awake() {PrepareBuilding();}

	protected void	PrepareBuilding() {
		PrepareStructure();
		isActive = false;
		energySupplied = false;
		borderOnlyConstruction = false;
		connectedToPowerGrid = false;
		switch (id) {
		case LANDED_ZEPPELIN_ID: upgradedIndex = HQ_2_ID; break;
		case STORAGE_0_ID: upgradedIndex = STORAGE_1_ID;break;
		case FARM_1_ID: upgradedIndex = FARM_2_ID;break;
		case HQ_2_ID : upgradedIndex = HQ_3_ID;break;
		case LUMBERMILL_1_ID: upgradedIndex = LUMBERMILL_2_ID;break;
		case SMELTERY_1_ID : upgradedIndex = SMELTERY_2_ID;break;
		case FOOD_FACTORY_4_ID: upgradedIndex = FOOD_FACTORY_5_ID;break;
		case STORAGE_2_ID: upgradedIndex = STORAGE_3_ID;break; 
		case HOUSE_2_ID: upgradedIndex = HOUSE_3_ID;break;
		case ENERGY_CAPACITOR_2_ID: upgradedIndex = ENERGY_CAPACITOR_3_ID;break;
		case FARM_2_ID : upgradedIndex = FARM_3_ID;break;
		case FARM_3_ID : upgradedIndex = FARM_4_ID;break;
		case FARM_4_ID: upgradedIndex = FARM_5_ID;break;
		case LUMBERMILL_2_ID : upgradedIndex = LUMBERMILL_3_ID;break;
		case LUMBERMILL_3_ID : upgradedIndex = LUMBERMILL_4_ID;break;
		case LUMBERMILL_4_ID: upgradedIndex = LUMBERMILL_5_ID;break;
		case SMELTERY_2_ID: upgradedIndex = SMELTERY_3_ID;break;
		case SMELTERY_3_ID: upgradedIndex = SMELTERY_4_ID;break;
		case SMELTERY_4_ID: upgradedIndex = SMELTERY_5_ID;break;
		case HQ_3_ID: upgradedIndex = HQ_4_ID;break;
		}
	}


	override public void SetBasement(SurfaceBlock b, PixelPosByte pos) {
		if (b == null) return;
		SetBuildingData(b, pos);
	}

	protected void SetBuildingData(SurfaceBlock b, PixelPosByte pos) {
		SetStructureData(b,pos);
		isActive = true;
		if (energySurplus != 0 || energyCapacity >  0) {
			GameMaster.colonyController.AddToPowerGrid(this);
			connectedToPowerGrid = true;
		}
	}
	virtual public void SetActivationStatus(bool x) {
		isActive = x;
		if (connectedToPowerGrid) {
			GameMaster.colonyController.RecalculatePowerGrid();
		}
		ChangeRenderersView(x);
	}
	public void SetEnergySupply(bool x) {
		energySupplied = x;
		ChangeRenderersView(x);
	}

	protected void ChangeRenderersView(bool setOnline) {
		if (setOnline == false) {
			if (myRenderers != null) {
				for (int i = 0; i < myRenderers.Length; i++) {
						Material m= myRenderers[i].sharedMaterial;
						bool replacing = false;
						if (m == PoolMaster.glass_material) {m = PoolMaster.glass_offline_material; replacing = true;}
						else {
							if (m == PoolMaster.colored_material) {m = PoolMaster.colored_offline_material; replacing = true;}
							else {
									if (m == PoolMaster.energy_material ) {m = PoolMaster.energy_offline_material; replacing = true;}
									}
						}
					if (replacing) myRenderers[i].sharedMaterial = m;
				}
			}
			if (myRenderer != null) {
				Material[] allMaterials = myRenderer.sharedMaterials;
				int j =0;
				while (j < allMaterials.Length) {
					if (allMaterials[j] == PoolMaster.glass_material) allMaterials[j] = PoolMaster.glass_offline_material;
					else {
						if (allMaterials[j] == PoolMaster.colored_material) allMaterials[j] = PoolMaster.colored_offline_material;
						else {
							if (allMaterials[j].name == PoolMaster.energy_material.name ) {
								allMaterials[j] = PoolMaster.energy_offline_material;
							}
						}
					}
					j++;
				}
				myRenderer.sharedMaterials = allMaterials;
			}
		}
		else {
			if (myRenderers != null) {
				for (int i = 0; i < myRenderers.Length; i++) {
						Material m = myRenderers[i].sharedMaterial;
						bool replacing = false;
						if (m == PoolMaster.glass_offline_material) { m = PoolMaster.glass_material; replacing = true;}
						else {
							if (m == PoolMaster.colored_offline_material) {m = PoolMaster.colored_material;replacing = true;}
							else {
								if (m == PoolMaster.energy_offline_material) { m = PoolMaster.energy_material;replacing = true;}
									}
						}
						if (replacing) myRenderers[i].sharedMaterial = m;
				}
			}
			if (myRenderer != null) {
				int j = 0;
				Material[] allMaterials = myRenderer.sharedMaterials;
				while (j < allMaterials.Length) {
					if (allMaterials[j] == PoolMaster.glass_offline_material) allMaterials[j] = PoolMaster.glass_material;
					else {
						if (allMaterials[j] == PoolMaster.colored_offline_material) allMaterials[j] = PoolMaster.colored_material;
						else {
							if (allMaterials[j] == PoolMaster.energy_offline_material) allMaterials[j] = PoolMaster.energy_material;
						}
					}
					j++;
				}
				myRenderer.sharedMaterials = allMaterials;
			}
		}
	}

	override public void SetVisibility( bool x) {
		if (x == visible) return;
		else {
			visible = x;
			if (myRenderers != null) {
				foreach (Renderer r in myRenderers) {
					r.enabled = x;
					if (r is SpriteRenderer) {
						if (r.GetComponent<MastSpriter>() != null) r.GetComponent<MastSpriter>().SetVisibility(x);
					}
				}
				if (isBasement) {
					BlockRendererController brc = gameObject.GetComponent<BlockRendererController>();
					if (brc != null) brc.SetVisibility(x);
				}
			}
			if (myRenderer != null) {
				myRenderer.enabled = x;
				if (myRenderer is SpriteRenderer) {
					if (myRenderer.GetComponent<MastSpriter>() != null) myRenderer.GetComponent<MastSpriter>().SetVisibility(x);
				}
			}
		}
	}
		
	protected void PrepareBuildingForDestruction() {
		if (basement != null) {
			basement.RemoveStructure(new SurfaceObject(innerPosition, this));
			basement.artificialStructures --;
		}
		if (connectedToPowerGrid) GameMaster.colonyController.DisconnectFromPowerGrid(this);
	}

	public void Demolish() {
		if (resourcesContainIndex != 0 && GameMaster.demolitionLossesPercent != 1) {
			ResourceContainer[] rleft = ResourcesCost.GetCost(id);
			for (int i = 0 ; i < rleft.Length; i++) {
				rleft[i] = new ResourceContainer(rleft[i].type, rleft[i].volume * (1 - GameMaster.demolitionLossesPercent));
			}
		}
		Destroy(gameObject);
	}

	void OnDestroy() {
		PrepareBuildingForDestruction();
	}

	override public void SetGUIVisible (bool x) {
		if (x != showOnGUI) {
			showOnGUI = x;
			if ( showOnGUI) {
				requiredResources = ResourcesCost.GetCost(id);
				if (requiredResources.Length > 0) {
					for (int i = 0; i < requiredResources.Length; i++) {
						requiredResources[i] = new ResourceContainer(requiredResources[i].type, requiredResources[i].volume * GameMaster.upgradeDiscount);
					}
				}
			}
		}
	}

	void OnGUI() {
		//sync with hospital.cs, rollingShop.cs
		if ( !showOnGUI ) return;
		Rect rr = new Rect(UI.current.rightPanelBox.x, gui_ypos, UI.current.rightPanelBox.width, GameMaster.guiPiece);
		if (canBeUpgraded && level < GameMaster.colonyController.hq.level) {
			rr.y = GUI_UpgradeButton(rr);
		}
	}

	virtual protected float GUI_UpgradeButton( Rect rr) {
			GUI.DrawTexture(new Rect( rr.x, rr.y, rr.height, rr.height), PoolMaster.greenArrow_tx, ScaleMode.StretchToFill);
			if ( GUI.Button(new Rect (rr.x + rr.height, rr.y, rr.height * 4, rr.height), "Level up") ) {
				if ( GameMaster.colonyController.storage.CheckBuildPossibilityAndCollectIfPossible( requiredResources ) )
				{
				Building upgraded = Structure.LoadStructure(id, (byte)(level + 1)) as Building;
					upgraded.Awake();
					PixelPosByte setPos = new PixelPosByte(innerPosition.x, innerPosition.z);
					byte bzero = (byte)0;
				if (upgraded.innerPosition.x_size == 16) setPos = new PixelPosByte(bzero, innerPosition.z);
				if (upgraded.innerPosition.z_size == 16) setPos = new PixelPosByte(setPos.x, bzero);
					Quaternion originalRotation = transform.rotation;
					upgraded.SetBasement(basement, setPos);
				upgraded.transform.localRotation = originalRotation;
				}
				else UI.current.ChangeSystemInfoString(Localization.announcement_notEnoughResources);
			}
		if ( requiredResources.Length > 0) {
			rr.y += rr.height;
			for (int i = 0; i < requiredResources.Length; i++) {
				GUI.DrawTexture(new Rect(rr.x, rr.y, rr.height, rr.height), requiredResources[i].type.icon, ScaleMode.StretchToFill);
				GUI.Label(new Rect(rr.x +rr.height, rr.y, rr.height * 5, rr.height), requiredResources[i].type.name);
				GUI.Label(new Rect(rr.xMax - rr.height * 3, rr.y, rr.height * 3, rr.height), (requiredResources[i].volume * (1 - GameMaster.upgradeDiscount)).ToString(), PoolMaster.GUIStyle_RightOrientedLabel);
				rr.y += rr.height;
			}
		}
		return rr.y;
		}
}