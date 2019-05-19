﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recipe {
		public readonly ResourceType input;
		public readonly float inputValue;
		public readonly ResourceType output;
		public readonly float outputValue;
		public readonly float workflowToResult;
		public readonly int ID;

	public static readonly Recipe[] smelteryRecipes, oreRefiningRecipes, fuelFacilityRecipes, plasticFactoryRecipes, graphoniumEnricherRecipes;

	public static readonly Recipe NoRecipe;
	public static readonly Recipe StoneToConcrete;
	public static readonly Recipe LumberToPlastics, MineralLToPlastics;
	public static readonly Recipe MetalK_smelting, MetalE_smelting, MetalN_smelting, MetalM_smelting,MetalP_smelting, MetalS_smelting;
	public static readonly Recipe MetalK_refining, MetalE_refining,MetalN_refining,MetalM_refining,MetalP_refining,MetalS_refining;
	public static readonly Recipe MineralF_refining, MineralL_refining;
	public static readonly Recipe Fuel_fromNmetal, Fuel_fromNmetalOre, Fuel_fromMineralF, Fuel_fromGraphonium;
    public static readonly Recipe Graphonium_fromNmetal, Graphonium_fromNmetalOre;

	public const int STONE_TO_CONCRETE_ID = 1, LUMBER_TO_PLASTICS_ID = 2, MINERAL_L_TO_PLASTICS_ID = 3, METAL_K_SMELTING_ID = 4,
	METAL_E_SMELTING_ID = 5, METAL_N_SMELTING_ID = 6, METAL_M_SMELTING_ID = 7, METAL_P_SMELTING_ID = 8, METAL_S_SMELTING_ID = 9,
	METAL_K_REFINIG_ID = 10, METAL_E_REFINING_ID = 11, METAL_N_REFINING_ID = 12, METAL_M_REFINING_ID = 13, METAL_P_REFINING_ID = 14,
	METAL_S_REFINING_ID = 15, MINERAL_F_REFINING_ID = 16, MINERAL_L_REFINING_ID = 17, FUEL_FROM_NMETAL_ID = 18, FUEL_FROM_NMETAL_ORE_ID = 19,
	FUEL_FROM_MINERAL_F_ID = 20, GRAPHONIUM_FROM_NMETAL_ID = 21, GRAPHONIUM_FRON_NMETAL_ORE_ID = 22, FUEL_FROM_GRAPHONIUM = 23;

	static Recipe() {
		NoRecipe = new Recipe(ResourceType.Nothing, ResourceType.Nothing,0, 0,0,  0);

		List<Recipe> smelteryRecipesList = new List<Recipe>();
		smelteryRecipesList.Add(NoRecipe);
		StoneToConcrete = new Recipe(ResourceType.Stone, ResourceType.Concrete, STONE_TO_CONCRETE_ID, 3, 2,  8); smelteryRecipesList.Add(StoneToConcrete);
		LumberToPlastics = new Recipe(ResourceType.Lumber, ResourceType.Plastics, LUMBER_TO_PLASTICS_ID, 3, 1,  15);  smelteryRecipesList.Add(LumberToPlastics);
		MetalK_smelting = new Recipe(ResourceType.metal_K_ore, ResourceType.metal_K, METAL_K_SMELTING_ID, 1,1, 10); smelteryRecipesList.Add(MetalK_smelting);
		MetalE_smelting = new Recipe(ResourceType.metal_E_ore, ResourceType.metal_E, METAL_E_SMELTING_ID, 1,1, 10); smelteryRecipesList.Add(MetalE_smelting);
		MetalN_smelting = new Recipe(ResourceType.metal_N_ore, ResourceType.metal_N, METAL_N_SMELTING_ID, 1,1, 10); smelteryRecipesList.Add(MetalN_smelting);
		MetalM_smelting = new Recipe(ResourceType.metal_M_ore, ResourceType.metal_M, METAL_M_SMELTING_ID, 1,1, 10); smelteryRecipesList.Add(MetalM_smelting);
		MetalP_smelting = new Recipe(ResourceType.metal_P_ore, ResourceType.metal_P, METAL_P_SMELTING_ID, 1,1, 10); smelteryRecipesList.Add(MetalP_smelting);
		MetalS_smelting = new Recipe(ResourceType.metal_S_ore, ResourceType.metal_S, METAL_S_SMELTING_ID, 1,1, 10); smelteryRecipesList.Add(MetalS_smelting);
		smelteryRecipes = smelteryRecipesList.ToArray();

		oreRefiningRecipes = new Recipe[9];
		MetalK_refining= new Recipe(ResourceType.Stone, ResourceType.metal_K_ore, METAL_K_REFINIG_ID, 4,1, 10);
		MetalE_refining = new Recipe(ResourceType.Stone, ResourceType.metal_E_ore, METAL_E_REFINING_ID, 8,1,23); 
		MetalN_refining = new Recipe(ResourceType.Stone, ResourceType.metal_N_ore, METAL_N_REFINING_ID, 16,1,70); 
		MetalM_refining = new Recipe(ResourceType.Stone, ResourceType.metal_M_ore, METAL_M_REFINING_ID, 6,1, 15); 
		MetalP_refining = new Recipe(ResourceType.Stone, ResourceType.metal_P_ore, METAL_P_REFINING_ID, 4,1, 12); 
		MetalS_refining= new Recipe(ResourceType.Stone, ResourceType.metal_S_ore, METAL_S_REFINING_ID, 10,1, 35); 
		MineralF_refining = new Recipe(ResourceType.Dirt, ResourceType.mineral_F, MINERAL_F_REFINING_ID, 3, 1, 5);
		MineralL_refining = new Recipe(ResourceType.Dirt, ResourceType.mineral_L,  MINERAL_L_REFINING_ID, 3, 1, 7);
		oreRefiningRecipes[0] = NoRecipe;
		oreRefiningRecipes[1] = MetalK_refining; oreRefiningRecipes[2] = MetalE_refining;
		oreRefiningRecipes[3] = MetalN_refining; oreRefiningRecipes[4] = MetalM_refining;
		oreRefiningRecipes[5] = MetalP_refining; oreRefiningRecipes[6] = MetalS_refining;
		oreRefiningRecipes[7] = MineralF_refining; oreRefiningRecipes[8] = MineralL_refining;

		fuelFacilityRecipes = new Recipe[5];
		Fuel_fromNmetal = new Recipe(ResourceType.metal_N, ResourceType.Fuel,  FUEL_FROM_NMETAL_ID, 1, 10, 100);
		Fuel_fromNmetalOre = new Recipe(ResourceType.metal_N_ore, ResourceType.Fuel,  FUEL_FROM_NMETAL_ORE_ID, 1, 9, 300);
		Fuel_fromMineralF = new Recipe(ResourceType.mineral_F, ResourceType.Fuel,  FUEL_FROM_MINERAL_F_ID, 1, 0.33f, 270);
        Fuel_fromGraphonium = new Recipe(ResourceType.Graphonium, ResourceType.Fuel, FUEL_FROM_GRAPHONIUM, 1, 100, 600);
		fuelFacilityRecipes[0] = NoRecipe;
		fuelFacilityRecipes[1] = Fuel_fromNmetal;
		fuelFacilityRecipes[2] = Fuel_fromNmetalOre;
		fuelFacilityRecipes[3] = Fuel_fromMineralF;
        fuelFacilityRecipes[4] = Fuel_fromGraphonium;

		plasticFactoryRecipes = new Recipe[3];
		plasticFactoryRecipes[0] = NoRecipe;
		plasticFactoryRecipes[1] = LumberToPlastics;
		MineralLToPlastics = new Recipe(ResourceType.mineral_L, ResourceType.Plastics, MINERAL_L_TO_PLASTICS_ID, 1, 2, 8);
		plasticFactoryRecipes[2] = MineralLToPlastics;

        graphoniumEnricherRecipes = new Recipe[3];
        Graphonium_fromNmetal = new Recipe(ResourceType.metal_N, ResourceType.Graphonium, GRAPHONIUM_FROM_NMETAL_ID, 10, 1, 1000);
        Graphonium_fromNmetalOre = new Recipe(ResourceType.metal_N_ore, ResourceType.Graphonium, GRAPHONIUM_FRON_NMETAL_ORE_ID, 11, 1, 1100);
        graphoniumEnricherRecipes[0] = NoRecipe;
        graphoniumEnricherRecipes[1] = Graphonium_fromNmetal;
        graphoniumEnricherRecipes[2] = Graphonium_fromNmetalOre;
	}

	public Recipe (ResourceType res_input, ResourceType res_output,int f_id, float val_input, float val_output,  float workflowNeeded) {
		ID = f_id;
		input = res_input; output = res_output;
		inputValue = val_input; outputValue = val_output;
		workflowToResult = workflowNeeded;
	}

	public static Recipe GetRecipeByNumber (int n) {
		switch (n) {
		case STONE_TO_CONCRETE_ID:return StoneToConcrete;
		case LUMBER_TO_PLASTICS_ID: return LumberToPlastics; 
		case MINERAL_L_TO_PLASTICS_ID: return MineralLToPlastics;
		case METAL_K_SMELTING_ID:return MetalK_smelting;
		case METAL_E_SMELTING_ID: return MetalE_smelting;
		case METAL_N_SMELTING_ID: return MetalN_smelting;
		case METAL_M_SMELTING_ID: return MetalM_smelting; 
		case METAL_P_SMELTING_ID: return MetalP_smelting;
		case METAL_S_SMELTING_ID:return MetalS_smelting; 
		case METAL_K_REFINIG_ID: return MetalK_refining;
		case METAL_E_REFINING_ID:return MetalE_refining; 
		case METAL_N_REFINING_ID: return MetalN_refining;
		case METAL_M_REFINING_ID: return MetalM_refining;
		case METAL_P_REFINING_ID:return MetalP_refining; 
		case METAL_S_REFINING_ID: return MetalS_refining;
		case MINERAL_F_REFINING_ID:return MineralF_refining;
		case MINERAL_L_REFINING_ID: return MineralL_refining;
		case FUEL_FROM_NMETAL_ID: return Fuel_fromNmetal;
		case FUEL_FROM_NMETAL_ORE_ID: return Fuel_fromNmetalOre;
		case FUEL_FROM_MINERAL_F_ID: return Fuel_fromMineralF;
        case GRAPHONIUM_FROM_NMETAL_ID: return Graphonium_fromNmetal;
		default: return NoRecipe;
		}
	}
    
}