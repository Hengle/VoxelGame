﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Dock : WorkBuilding {
	bool correctLocation = false;
	bool gui_tradeGoodTabActive = false, gui_sellResourcesTabActive = false, gui_addTransactionMenu = false;
	public static bool?[] isForSale{get; private set;}
	public static int[] minValueForTrading{get; private set;}
	int preparingResourceIndex = 0;
	ColonyController colony;
	public static int immigrationPlan {get; private set;} 
	public static bool immigrationEnabled{get; private set;}
	public bool maintainingShip{get; private set;}
	Ship loadingShip;
	const float LOADING_TIME = 10;
	float loadingTimer = 0, shipArrivingTimer = 0;
	const float SHIP_ARRIVING_TIME = 300;

	override public void Prepare() {
		PrepareWorkbuilding();
		type = StructureType.MainStructure;
		borderOnlyConstruction = true;
		if (isForSale == null) {
			isForSale = new bool?[ResourceType.RTYPES_COUNT];
			minValueForTrading= new int[ResourceType.RTYPES_COUNT];
			immigrationEnabled = true;
			immigrationPlan = 0;
		}
	}

	override public void SetBasement(SurfaceBlock b, PixelPosByte pos) {
		if (b == null) return;
		SetBuildingData(b, pos);
		Transform meshTransform = transform.GetChild(0);
		if (basement.pos.z == 0) {
			meshTransform.transform.localRotation = Quaternion.Euler(0, 180,0); correctLocation = true;
		}
		else {
			if (basement.pos.z == Chunk.CHUNK_SIZE - 1) {
				correctLocation = true;
			}
			else {
				if (basement.pos.x == 0) {
					meshTransform.transform.localRotation = Quaternion.Euler(0, -90,0); correctLocation = true;
				}
				else {
					if (basement.pos.x == Chunk.CHUNK_SIZE - 1) {
						meshTransform.transform.localRotation = Quaternion.Euler(0, 90,0); correctLocation = true;
					}
				}
			}
		}
		if (correctLocation) 
		{	
			basement.ReplaceMaterial(ResourceType.CONCRETE_ID);
			colony = GameMaster.colonyController;
			colony.AddDock(this);
			shipArrivingTimer = SHIP_ARRIVING_TIME * GameMaster.tradeVesselsTrafficCoefficient * (1 - (colony.docksLevel * 2 / 100f)) /2f ;
		}
	}

	void Update() {
		if (GameMaster.gameSpeed == 0) return;
		if ( maintainingShip ) {
			if (loadingTimer > 0) {
					loadingTimer -= Time.deltaTime * GameMaster.gameSpeed;
					if (loadingTimer <= 0) {
						if (loadingShip != null) ShipLoading(loadingShip);
						loadingTimer = 0;
					}
			}
		}
		else {
			// ship arriving
			if (shipArrivingTimer > 0) { 
				shipArrivingTimer -= Time.deltaTime * GameMaster.gameSpeed;
				if (shipArrivingTimer <= 0 ) {
					bool sendImmigrants = false, sendGoods = false;
					if ( immigrationPlan > 0  && immigrationEnabled ) {
						if (Random.value < 0.3f || colony.totalLivespace > colony.citizenCount) sendImmigrants = true;
					}
					int transitionsCount = 0;
					for (int x = 0; x < Dock.isForSale.Length; x++) {
						if (Dock.isForSale[x] != null) transitionsCount++;
					}
					if (transitionsCount > 0) sendGoods = true;
					ShipType stype = ShipType.Cargo;
					if (sendImmigrants) {
						if (sendGoods) {
							if (Random.value > 0.55f ) stype = ShipType.Passenger;
						}
						else {
							if (Random.value < 0.05f) stype = ShipType.Private;
							else stype = ShipType.Passenger;
						}
					}
					else {
						if (sendGoods) {
							if (Random.value <= GameMaster.warProximity) stype = ShipType.Military;
							else stype = ShipType.Cargo;
						}
						else {
							if (Random.value > 0.5f) {
								if (Random.value > 0.1f) stype = ShipType.Passenger;
								else stype = ShipType.Private;
							}
							else {
								if (Random.value > GameMaster.warProximity) stype = ShipType.Cargo;
								else stype = ShipType.Military;
							}
						}
					}
					Ship s = PoolMaster.current.GetShip( level, stype );
					if ( s!= null ) {
						maintainingShip = true;
						s.SetDestination( this );
					}
					else print ("error:no ship given");
				}
			}
		}
	}

	public void ShipLoading(Ship s) {
		if (loadingShip == null) {
			loadingTimer = LOADING_TIME;
			loadingShip = s;
			return;
		}
		int peopleBefore = immigrationPlan;
		switch (s.type) {
		case ShipType.Passenger:
			if (immigrationPlan > 0) {
				if (s.volume > immigrationPlan) {GameMaster.colonyController.AddCitizens(immigrationPlan); immigrationPlan = 0;}
				else {GameMaster.colonyController.AddCitizens(s.volume); immigrationPlan -= s.volume;}
			}
			if (isForSale[ResourceType.FOOD_ID] != null) {
				if (isForSale[ResourceType.FOOD_ID] == true) SellResource(ResourceType.Food, s.volume * 0.1f);
				else BuyResource(ResourceType.Food, s.volume * 0.1f);
			}
			break;
		case ShipType.Cargo:
			float totalDemand= 0;
			List<int> buyPositions = new List<int>();
			for (int i = 0; i < ResourceType.RTYPES_COUNT; i ++) {
				if (isForSale[i] == null) continue;
				if (isForSale[i] == true) {
					totalDemand += ResourceType.demand[i];
				}
				else {
					if ( colony.storage.standartResources[i] <= minValueForTrading[i])	buyPositions.Add(i);
				}
			}
			if (totalDemand > 0) {
				float demandPiece = 1 / totalDemand;
				for (int i = 0; i < ResourceType.RTYPES_COUNT; i ++) {
					if (isForSale[i] == true) SellResource(ResourceType.resourceTypesArray[i], ResourceType.demand[i] * demandPiece * s.volume);
				}
			}
			if (buyPositions.Count > 0) {
				float v = s.volume;
				while (v > 0 && buyPositions.Count > 0) {
					int buyIndex = (int)(Random.value * buyPositions.Count - 1); // index in index arrays
					int i = buyPositions[buyIndex]; // real index
					float buyVolume = minValueForTrading[i] - colony.storage.standartResources[i]; 
					if (buyVolume < 0) {buyVolume = 0; print ("error : negative buy volume");}
					if (v < buyVolume) buyVolume = v;
					BuyResource(ResourceType.resourceTypesArray[i], buyVolume);
					v -= buyVolume;
					buyPositions.RemoveAt(buyIndex);
				}
			}
			break;
		case ShipType.Military:
			if (GameMaster.warProximity < 0.5f && Random.value < 0.1f && immigrationPlan > 0) {
				int veterans =(int)( s.volume * 0.02f );
				if (veterans > immigrationPlan) veterans = immigrationPlan;
				colony.AddCitizens(veterans);
			}
			if ( isForSale[ResourceType.FUEL_ID] == true) SellResource(ResourceType.Fuel, s.volume * 0.5f * (Random.value * 0.5f + 0.5f));
			if (GameMaster.warProximity > 0.5f) {
				if (isForSale[ResourceType.METAL_S_ID] == true) SellResource(ResourceType.metal_S, s.volume * 0.1f);
				if (isForSale[ResourceType.METAL_K_ID] == true) SellResource(ResourceType.metal_K, s.volume * 0.05f);
				if (isForSale[ResourceType.METAL_M_ID] == true) SellResource(ResourceType.metal_M, s.volume * 0.1f);
			}
			break;
		case ShipType.Private:
			if ( isForSale[ResourceType.FUEL_ID] == true) SellResource(ResourceType.Fuel, s.volume * 0.8f);
			if ( isForSale[ResourceType.FOOD_ID] == true) SellResource(ResourceType.Fuel, s.volume * 0.15f);
			break;
		}
		loadingShip = null;
		maintainingShip = false;
		s.Undock();

		shipArrivingTimer = SHIP_ARRIVING_TIME * GameMaster.tradeVesselsTrafficCoefficient * (1 - (colony.docksLevel * 2 / 100f))  ;
		float f = 1;
		if (colony.docks.Count != 0) f /= (float)colony.docks.Count;
		if ( f < 0.1f ) f = 0.1f;
		shipArrivingTimer /= f;

		int newPeople = peopleBefore - immigrationPlan;
		if (newPeople > 0) GameMaster.realMaster.AddAnnouncement(Localization.announcement_peopleArrived + " (" + newPeople.ToString() + ')');
	}

	void SellResource(ResourceType rt, float volume) {
		float vol = colony.storage.GetResources(rt, volume);
		colony.AddEnergyCrystals(vol * ResourceType.prices[rt.ID] * GameMaster.sellPriceCoefficient);
	}
	void BuyResource(ResourceType rt, float volume) {
		volume = colony.GetEnergyCrystals(volume * ResourceType.prices[rt.ID]) / ResourceType.prices[rt.ID];
		colony.storage.AddResource(rt, volume);
	}

	public static void MakeLot( int index, bool forSale, int minValue ) {
		isForSale[index] = forSale;
		minValueForTrading[index] = minValue;
	}

	public static void SetImmigrationStatus ( bool x, int count) {
		immigrationEnabled = x;
		immigrationPlan = count;
	}

	//---------------------                   SAVING       SYSTEM-------------------------------
	public override string Save() {
		return SaveStructureData() + SaveBuildingData() + SaveWorkBuildingData() + SaveDockData();
	}

	protected string SaveDockData() {
		string s = "";
		if (maintainingShip ) s+='1'; else s+='0';
		if ( loadingShip != null ) {
			switch ( loadingShip.type ) {
			case ShipType.Cargo : s += '1';break;
			case ShipType.Military: s += '2'; break;
			case ShipType.Passenger: s+= '3';break;
			case ShipType.Private: s+='4';break;
			}
			s += loadingShip.level.ToString();
			if (loadingTimer == LOADING_TIME) loadingTimer = LOADING_TIME * 0.99f;
			s += string.Format("{0:d2}", ((int) (loadingTimer / LOADING_TIME * 100 )).ToString());
			s += loadingShip.Save();
		}
		s += string.Format("{0:000}", shipArrivingTimer);
		return s;
	}

	public override void Load(string s_data, Chunk c, SurfaceBlock surface) {
		byte x = byte.Parse(s_data.Substring(0,2));
		byte z = byte.Parse(s_data.Substring(2,2));
		Prepare();
		SetBasement(surface, new PixelPosByte(x,z));
		//workbuilding class part
		workflow = int.Parse(s_data.Substring(12,3)) / 100f;
		AddWorkers(int.Parse(s_data.Substring(15,3)));
		//building class part
		SetActivationStatus(s_data[11] == '1');     
		//dock class part
		if ( s_data[18] == '1' ) maintainingShip = true;
		if ( s_data.Length < 20 || s_data[19] == ';') return;
		switch (s_data[19]) {
		case '1': loadingShip = PoolMaster.current.GetShip( (byte)int.Parse(s_data.Substring(20,1)), ShipType.Cargo ); break;
		case '2': loadingShip = PoolMaster.current.GetShip( (byte)int.Parse(s_data.Substring(20,1)), ShipType.Military ); break;
		case '3': loadingShip = PoolMaster.current.GetShip( (byte)int.Parse(s_data.Substring(20,1)), ShipType.Passenger ); break;
		case '4': loadingShip = PoolMaster.current.GetShip( (byte)int.Parse(s_data.Substring(20,1)), ShipType.Private ); break;
		default: loadingShip = null; break;
		}
		if ( loadingShip != null ) {
			loadingShip.SetDestination(this);
			loadingShip.Load(s_data);
		}
		if (s_data.Length > 36) shipArrivingTimer = float.Parse(s_data.Substring(36, s_data.Length - 36));
		//--
		transform.localRotation = Quaternion.Euler(0, 45 * int.Parse(s_data[7].ToString()), 0);
		hp = int.Parse(s_data.Substring(8,3)) / 100f * maxHp;
	}
	//---------------------------------------------------------------------------------------------------	

	void OnDestroy() {
		GameMaster.colonyController.RemoveDock(this);
		PrepareWorkbuildingForDestruction();
	}

	void OnGUI() {
		if (!showOnGUI) return;
		GUI.skin = GameMaster.mainGUISkin;
		float k =GameMaster.guiPiece;
		Rect r = new Rect(UI.current.rightPanelBox.x, gui_ypos, UI.current.rightPanelBox.width, GameMaster.guiPiece);
		//trade
		if ( !gui_tradeGoodTabActive ) GUI.DrawTexture(new Rect(r.x, r.y, r.width / 2f, r.height), PoolMaster.orangeSquare_tx, ScaleMode.StretchToFill);
		else GUI.DrawTexture(new Rect(r.x + r.width/2f, r.y, r.width/2f, r.height), PoolMaster.orangeSquare_tx, ScaleMode.StretchToFill);
		if (GUI.Button(new Rect(r.x, r.y, r.width / 2f, r.height), Localization.ui_immigration, PoolMaster.GUIStyle_BorderlessButton)) {gui_tradeGoodTabActive = false;gui_addTransactionMenu = false;}
		if (GUI.Button(new Rect(r.x + r.width/2f, r.y, r.width/2f, r.height), Localization.ui_trading, PoolMaster.GUIStyle_BorderlessButton)) gui_tradeGoodTabActive = true;
		GUI.DrawTexture(r, PoolMaster.twoButtonsDivider_tx, ScaleMode.StretchToFill);
		r.y += r.height;
		if (gui_tradeGoodTabActive) {
			if (!gui_sellResourcesTabActive) GUI.DrawTexture(new Rect(r.x, r.y, r.width / 2f, r.height), PoolMaster.orangeSquare_tx, ScaleMode.StretchToFill);
			else GUI.DrawTexture(new Rect(r.x + r.width/2f, r.y, r.width / 2f, r.height), PoolMaster.orangeSquare_tx, ScaleMode.StretchToFill);
			if (GUI.Button(new Rect(r.x, r.y, r.width / 2f, r.height), Localization.ui_buy, PoolMaster.GUIStyle_BorderlessButton)) gui_sellResourcesTabActive = false;
			if (GUI.Button(new Rect(r.x + r.width/2f, r.y, r.width/2f, r.height), Localization.ui_sell, PoolMaster.GUIStyle_BorderlessButton)) gui_sellResourcesTabActive = true;
			GUI.DrawTexture(r, PoolMaster.twoButtonsDivider_tx, ScaleMode.StretchToFill);
			r.y += r.height;
			if (GUI.Button(r, '<' + Localization.ui_add_transaction + '>')) {
				gui_addTransactionMenu = !gui_addTransactionMenu;
				UI.current.touchscreenTemporarilyBlocked = gui_addTransactionMenu;
			}
			r.y += r.height;
			if (gui_addTransactionMenu) {  // Настройка торговой операции
				GUI.Box(new Rect(2 *k, 2*k, Screen.width - 2 *k - UI.current.rightPanelBox.width,Screen.height - 4 *k), GUIContent.none);
				float resQuad_k = 10 * k / ResourceType.RTYPE_ARRAY_ROWS;
				float resQuad_leftBorder = 2 * k ;
				Rect resrect = new Rect(  resQuad_leftBorder, 2 *k , resQuad_k, resQuad_k);
				int index = 1; resrect.x += resrect.width;
				for (int i = 0; i < ResourceType.RTYPE_ARRAY_ROWS; i++) {
					for (int j =0; j < ResourceType.RTYPE_ARRAY_COLUMNS; j++) {
						if (ResourceType.resourceTypesArray[index] == null) {
							index++;
							resrect.x += resrect.width;
							continue;
						}
						if (index == preparingResourceIndex) GUI.DrawTexture(resrect, PoolMaster.orangeSquare_tx, ScaleMode.StretchToFill);
						if (GUI.Button(resrect, ResourceType.resourceTypesArray[index].icon)) {
							preparingResourceIndex = index;
						}
						GUI.Label(new Rect(resrect.x, resrect.y, resrect.width, resrect.height/2f), ResourceType.resourceTypesArray[index].name, PoolMaster.GUIStyle_CenterOrientedLabel);
						GUI.Label(new Rect(resrect.x, resrect.yMax -  resrect.height/2f, resrect.width, resrect.height/2f), ((int)(colony.storage.standartResources[index] *100f)/100f ).ToString(), PoolMaster.GUIStyle_RightBottomLabel);
						if ( isForSale [index] != null) {
							if (isForSale [index] == true) GUI.DrawTexture( new Rect(resrect.x, resrect.y + resrect.height / 2f, resrect.height / 2f, resrect.height/2f), PoolMaster.redArrow_tx, ScaleMode.StretchToFill);
							else GUI.DrawTexture(new Rect(resrect.x, resrect.y + resrect.height / 2f, resrect.height / 2f, resrect.height/2f), PoolMaster.greenArrow_tx, ScaleMode.StretchToFill);
						}
						index ++;
						resrect.x += resrect.width;
						if (index >= ResourceType.resourceTypesArray.Length) break;
					}
					resrect.x = resQuad_leftBorder;
					resrect.y += resrect.height;
					if (index >= ResourceType.resourceTypesArray.Length) break;
				}

				int fb = PoolMaster.GUIStyle_CenterOrientedLabel.fontSize, fb_b = GUI.skin.button.fontSize;
				PoolMaster.GUIStyle_CenterOrientedLabel.fontSize = fb * 2; GUI.skin.button.fontSize = fb_b * 2;
				float xpos = 2 *k, ypos = Screen.height/2f, qsize = 2 *k;
				GUI.Label(new Rect(xpos, ypos, 4 * qsize, qsize), Localization.min_value_to_trade, PoolMaster.GUIStyle_CenterOrientedLabel); 
				GUI.Label(new Rect(Screen.width/2f - 4 * resQuad_k, k, 5 * resQuad_k, k ), Localization.ui_selectResource, PoolMaster.GUIStyle_CenterOrientedLabel);
				int val = minValueForTrading[preparingResourceIndex];
				if (GUI.Button(new Rect(xpos + 4 * qsize, ypos , qsize, qsize ), "-10")) val -= 10;
				if (GUI.Button(new Rect(xpos + 5 * qsize, ypos, qsize, qsize), "-1")) val--;
				if (val < 0) val = 0; minValueForTrading[preparingResourceIndex] = val;
				GUI.Label (new Rect(xpos + 6 * qsize, ypos, qsize * 2, qsize), minValueForTrading[preparingResourceIndex].ToString(), PoolMaster.GUIStyle_CenterOrientedLabel);
				if (GUI.Button(new Rect(xpos + 8 * qsize, ypos, qsize, qsize), "+1")) minValueForTrading[preparingResourceIndex]++;
				if (GUI.Button(new Rect(xpos + 9 * qsize, ypos, qsize, qsize), "+10")) minValueForTrading[preparingResourceIndex] += 10; 
				ypos += qsize;
				GUI.Label(new Rect(xpos, ypos, 2 * qsize, qsize), '+' + (ResourceType.prices[preparingResourceIndex] * GameMaster.sellPriceCoefficient).ToString(), PoolMaster.GUIStyle_CenterOrientedLabel);
				if (isForSale [preparingResourceIndex] == true ) GUI.DrawTexture(new Rect(xpos + 2 *qsize, ypos, 3 * qsize, qsize) , PoolMaster.orangeSquare_tx, ScaleMode.StretchToFill);
				if (GUI.Button (new Rect(xpos + 2 *qsize, ypos, 3 * qsize, qsize) , Localization.ui_sell)) {isForSale [preparingResourceIndex] = true; gui_sellResourcesTabActive = true;}
				if ( isForSale [preparingResourceIndex] == false ) GUI.DrawTexture(new Rect(xpos + 6 *qsize, ypos, 3 * qsize, qsize) , PoolMaster.orangeSquare_tx, ScaleMode.StretchToFill);
				if (GUI.Button (new Rect(xpos + 6 *qsize, ypos, 3 * qsize, qsize) , Localization.ui_buy)) {isForSale [preparingResourceIndex] = false; gui_sellResourcesTabActive = false;}
				GUI.DrawTexture(new Rect(xpos + 2 * qsize, ypos, 7 * qsize, qsize) , PoolMaster.twoButtonsDivider_tx, ScaleMode.StretchToFill);
				GUI.Label(new Rect(xpos + 9 * qsize, ypos, 2 * qsize, qsize), '-' + ResourceType.prices[preparingResourceIndex] .ToString(), PoolMaster.GUIStyle_CenterOrientedLabel);
				ypos += 2 * qsize;
				if (GUI.Button(new Rect(xpos + 3 * qsize, ypos, 2 * qsize, qsize), Localization.ui_close)) {
					gui_addTransactionMenu = false;
					UI.current.touchscreenTemporarilyBlocked = false;
				}
				if (GUI.Button(new Rect(xpos + 6 * qsize, ypos, 2 * qsize, qsize), Localization.ui_reset)) {
					minValueForTrading[preparingResourceIndex] = 0;
					isForSale [preparingResourceIndex] = null;
					gui_addTransactionMenu = false;
					UI.current.touchscreenTemporarilyBlocked = false;
				}
				PoolMaster.GUIStyle_CenterOrientedLabel.fontSize = fb;
				GUI.skin.button.fontSize = fb_b;
			}
			// list
			for (int i = 1 ; i < minValueForTrading.Length; i ++) {
				if (ResourceType.resourceTypesArray[i] == null || isForSale[i] == null) continue;
				bool b = (isForSale[i] == true);
				if (b != gui_sellResourcesTabActive) continue;
				GUI.DrawTexture(new Rect(r.x, r.y, r.height, r.height), ResourceType.resourceTypesArray[i].icon);
				int val = minValueForTrading[i];
				if (GUI.Button(new Rect(r.x + r.height, r.y, r.height, r.height), "-10")) val -= 10;
				if (GUI.Button(new Rect(r.x + 2 *r.height, r.y, r.height, r.height), "-1")) val--;
				if (val < 0) val = 0; minValueForTrading[i] = val;
				GUI.Label(new Rect(r.x + 3 * r.height, r.y, r.height * 2, r.height ), minValueForTrading[i].ToString(), PoolMaster.GUIStyle_CenterOrientedLabel);
				if (GUI.Button(new Rect(r.x + 5 * r.height, r.y, r.height, r.height), "+1")) minValueForTrading[i]++;
				if (GUI.Button(new Rect(r.x + 6 * r.height, r.y, r.height, r.height), "+10")) minValueForTrading[i] += 10;
				if (minValueForTrading[i] >= 10000) minValueForTrading[i] = 9999;
				if (GUI.Button(new Rect(r.x + 7 * r.height, r.y, r.height, r.height), PoolMaster.minusButton_tx)) {isForSale [i] = null; minValueForTrading[i] = 0;}
					r.y += r.height;
				}
		}
		else // immigration tab
		{
			immigrationEnabled = GUI.Toggle(r, immigrationEnabled, Localization.ui_immigrationEnabled);
			r.y += r.height;
			if ( !immigrationEnabled ) {
				GUI.Label(new Rect(r.x + 3 * r.height, r.y, r.height * 3, r.height ), Localization.ui_immigrationDisabled , PoolMaster.GUIStyle_CenterOrientedLabel);
			}
			else {
				GUI.Label(new Rect(r.x + 1 * r.height, r.y, r.height * 7, r.height ), Localization.ui_immigrationPlaces + " :", PoolMaster.GUIStyle_CenterOrientedLabel);
				r.y += r.height;
				if (GUI.Button(new Rect(r.x + r.height, r.y, r.height, r.height), "-10")) immigrationPlan=10;
				if (GUI.Button(new Rect(r.x + 2 *r.height, r.y, r.height, r.height), "-1")) immigrationPlan--;
				GUI.Label(new Rect(r.x + 3 * r.height, r.y, r.height * 3, r.height ), Localization.ui_immigrationPlaces + " : " + immigrationPlan.ToString(),  PoolMaster.GUIStyle_CenterOrientedLabel);
				if (GUI.Button(new Rect(r.x + 6 * r.height, r.y, r.height, r.height), "+1")) immigrationPlan++;
				if (GUI.Button(new Rect(r.x + 7 * r.height, r.y, r.height, r.height), "+10")) immigrationPlan += 10;
				if (immigrationPlan >= 1000) immigrationPlan = 999;
			}
		}
	}
}
