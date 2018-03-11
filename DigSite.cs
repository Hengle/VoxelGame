using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DigSite : MonoBehaviour {
	public int workersCount {get;private set;}
	float workflow;
	public bool dig = true;
	CubeBlock workObject;
	GameObject sign;

	void Update () {
		if (GameMaster.gameSpeed == 0) return;
		if (workObject ==null || (workObject.volume == CubeBlock.maxVolume && dig == false) || (workObject.volume ==0 && dig == true)) {
			Destroy(this);
		}
		workflow += GameMaster.CalculateWorkflow(workersCount);
	}

	void EverydayUpdate() {
		if (workflow > 1) {
			int x = (int) workflow;
			if (dig) workObject.Dig(x, true);
			else workObject.PourIn(x);
			workflow -= x;

			if (workObject.IsNatural() == false) return;
			float production = x;
			float v = Random.value - GameMaster.LUCK_COEFFICIENT; 
			float m = 0;

			switch (workObject.material_id) {
			case PoolMaster.STONE_ID :
				if (GameMaster.metalC_abundance >= v) {
					m= GameMaster.metalC_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_C, m); production -= m;
				}
				if (GameMaster.metalM_abundance >= v) {
					m= GameMaster.metalM_abundance * production * (Random.value +1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_M, m); production -= m;
				}
				if (GameMaster.metalE_abundance >= v) {
					m= GameMaster.metalE_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_E, m); production -= m;
				}
				if (GameMaster.metalN_abundance >= v) {
					m= GameMaster.metalN_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_N, m); production -= m;
				}
				if (GameMaster.metalP_abundance >= v) {
					m= GameMaster.metalP_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_P, m); production -= m;
				}
				if (GameMaster.metalS_abundance >= v) {
					m= GameMaster.metalS_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_S, m); production -= m;
				}
				if (GameMaster.mineralF_abundance >= v) {
					m= GameMaster.mineralF_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.mineral_F, m); production -= m;
				}
				if (GameMaster.mineralL_abundance >= v) {
					m= GameMaster.mineralL_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.mineral_L, m); production -= m;
				}
				if (production > 0) {
					GameMaster.colonyController.storage.AddResources(ResourceType.Stone, production); 
				}
				break;
			case PoolMaster.DIRT_ID:
				if (GameMaster.metalC_abundance >= v) {
					m= GameMaster.metalC_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_C, m); production -= m;
				}
				if (GameMaster.metalP_abundance >= v) {
					m= GameMaster.metalP_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.metal_P, m); production -= m;
				}
				if (GameMaster.mineralL_abundance >= v) {
					m= GameMaster.mineralL_abundance * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
					GameMaster.colonyController.storage.AddResources(ResourceType.mineral_L, m); production -= m;
				}
				if (production > 0) {
					GameMaster.colonyController.storage.AddResources(ResourceType.Dirt, production); 
				}
				break;
			default:
				GameMaster.colonyController.storage.AddResources(ResourceType.GetResourceTypeByMaterialId(workObject.material_id), production); 
				break;
			}
		}
	}

	public void Set(CubeBlock block, bool work_is_dig) {
		workObject = block;
		dig = work_is_dig;
		if (dig) {
			sign = Instantiate(Resources.Load<GameObject> ("Prefs/DigSign")) as GameObject; 
			block.digWorks = true;
		}
		else sign = Instantiate(Resources.Load<GameObject>("Prefs/PourInSign"));
		sign.transform.parent = workObject.transform;
		sign.transform.localPosition = Vector3.up * 0.5f;
		GameMaster.realMaster.everydayUpdateList.Add(this);
	}

	void OnDestroy() {
		GameMaster.colonyController.AddWorkers(workersCount);
		if (sign != null) Destroy(sign);
		if (workObject != null) workObject.digWorks = false;
	}
}
