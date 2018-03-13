using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DigSite : MonoBehaviour {
	public const int MAX_WORKERS = 96;
	public int workersCount {get;private set;}
	public float workflow;
	public bool dig = true;
	CubeBlock workObject;
	GameObject sign;
	ResourceType mainResource;

	void Awake () {
		workersCount = 0;
	}

	void Update () {
		if (GameMaster.gameSpeed == 0) return;
		if (workObject ==null || (workObject.volume == CubeBlock.MAX_VOLUME && dig == false) || (workObject.volume ==0 && dig == true)) {
			Destroy(this);
			return;
		}
		if (workersCount > 0) {
			workflow += GameMaster.CalculateWorkflow(workersCount);
		}
	}

	void EverydayUpdate() {
		if (workflow > 1) {
			int x = (int) workflow;
			float production = x;
			if (dig) {
				production = workObject.Dig(x, true);
				if (workObject.naturalFossils > 0) {
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
							m= GameMaster.metalC_abundance/2f * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
							GameMaster.colonyController.storage.AddResources(ResourceType.metal_C, m); production -= m;
						}
						if (GameMaster.metalP_abundance >= v) {
							m= GameMaster.metalP_abundance/2f * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
							GameMaster.colonyController.storage.AddResources(ResourceType.metal_P, m); production -= m;
						}
						if (GameMaster.mineralL_abundance >= v) {
							m= GameMaster.mineralL_abundance/2f * production * (Random.value + 1 + GameMaster.LUCK_COEFFICIENT);
							GameMaster.colonyController.storage.AddResources(ResourceType.mineral_L, m); production -= m;
						}
						if (production > 0) {
							GameMaster.colonyController.storage.AddResources(ResourceType.Dirt, production); 
						}
						break;
					default:
						GameMaster.colonyController.storage.AddResources(mainResource, production); 
						break;
					}
					workObject.naturalFossils -= production;
				}
				else { // no fossils
					GameMaster.colonyController.storage.AddResources(mainResource, production); 
				}
			}
			else {
				production = GameMaster.colonyController.storage.GetResources(mainResource, production);
				if (production != 0) {
					production = workObject.PourIn((int)production);
					if (production == 0) {Destroy(this);return;}
				}
			}
			workflow -= production;	
		}
	}

	public void Set(CubeBlock block, bool work_is_dig) {
		workObject = block;
		dig = work_is_dig;
		DigSite ds = gameObject.GetComponent<DigSite>();
		if (ds.dig == work_is_dig) {Destroy(this);return;} else Destroy(ds);
		if (dig) {
			sign = Instantiate(Resources.Load<GameObject> ("Prefs/DigSign")) as GameObject; 
			block.digStatus = -1;
		}
		else {
			sign = Instantiate(Resources.Load<GameObject>("Prefs/PourInSign"));
			block.digStatus = 1;
		}
		sign.transform.parent = workObject.transform;
		sign.transform.localPosition = Vector3.up * 0.5f;
		GameMaster.realMaster.everydayUpdateList.Add(this);
		GameMaster.colonyController.digSites.Add(this);
		mainResource = ResourceType.GetResourceTypeByMaterialId(workObject.material_id);
	}

	public void AddWorkers (int x) {
		if (x > 0) workersCount += x;
	}

	void OnDestroy() {
		GameMaster.colonyController.AddWorkers(workersCount);
		if (sign != null) Destroy(sign);
		if (workObject != null) {
			if (dig && workObject.digStatus == -1) workObject.digStatus = 0;
			else if (!dig && workObject.digStatus == 1) workObject.digStatus = 0;
		} 
	}
}
