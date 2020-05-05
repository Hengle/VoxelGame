﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AdvancedFactory : Factory
{
    public float inputResourcesBuffer2 { get; private set; }
    new private AdvancedRecipe recipe;
    new public const int FACTORY_SERIALIZER_LENGTH = Factory.FACTORY_SERIALIZER_LENGTH + 4;

    override public void Prepare()
    {
        PrepareWorkbuilding();
        recipe = AdvancedRecipe.NoRecipe;
        switch (ID)
        {
            case SUPPLIES_FACTORY_4_ID:
            case SUPPLIES_FACTORY_5_ID:
                specialization = FactorySpecialization.SuppliesFactory;
                break;
            default:
                specialization = FactorySpecialization.Unspecialized;
                break;
        }
        inputResourcesBuffer = 0;
    }

    override public void LabourUpdate()
    {
        if (recipe == AdvancedRecipe.NoRecipe) return;
        Storage storage = colony.storage;
        if (outputResourcesBuffer > 0)
        {
            outputResourcesBuffer = storage.AddResource(recipe.output, outputResourcesBuffer);
        }
        if (outputResourcesBuffer <= BUFFER_LIMIT)
        {
            if (isActive)
            {
                if (isEnergySupplied)
                {
                    workSpeed = colony.workspeed * workersCount * GameConstants.FACTORY_SPEED * level * level;
                    if (productionMode == FactoryProductionMode.Limit)
                    {
                        if (!workPaused)
                        {
                            workflow += workSpeed;
                            colony.gears_coefficient -= gearsDamage * workSpeed;
                            if (workflow >= workflowToProcess) LabourResult();
                        }
                        else
                        {
                            if (storage.standartResources[recipe.output.ID] < productionModeValue)
                            {
                                workPaused = false;
                                workflow += workSpeed;
                                colony.gears_coefficient -= gearsDamage * workSpeed;
                                if (workflow >= workflowToProcess) LabourResult();
                            }
                        }
                    }
                    else
                    {
                        workflow += workSpeed;
                        colony.gears_coefficient -= gearsDamage * workSpeed;
                        if (workflow >= workflowToProcess) LabourResult();
                    }
                }
            }
        }
    }
    override protected void LabourResult()
    {
        int iterations = (int)(workflow / workflowToProcess);
        workflow = 0;
        Storage storage = colony.storage;
        float i1 = storage.standartResources[recipe.input.ID] + inputResourcesBuffer,
            i2 = storage.standartResources[recipe.input2.ID] + inputResourcesBuffer2;
        if (i1 >= recipe.inputValue && i2 >= recipe.inputValue2)
        {
            int it = (int)(i1 / recipe.inputValue);
            if (it > iterations) it = iterations;
            int it2 = (int)(i2 / recipe.inputValue2);
            if (it2 > iterations) it = iterations;
            if (it2 < it) it = it2;
            if (it > 0)
            {
                inputResourcesBuffer += storage.GetResources(recipe.input, recipe.inputValue * it - inputResourcesBuffer);
                inputResourcesBuffer2 += storage.GetResources(recipe.input2, recipe.inputValue2 * it - inputResourcesBuffer2);
                iterations = Mathf.FloorToInt(inputResourcesBuffer / recipe.inputValue);
                switch (productionMode)
                {
                    case FactoryProductionMode.Limit:
                        {
                            float stVal = storage.standartResources[recipe.output.ID];
                            if (stVal + recipe.outputValue * iterations > productionModeValue)
                            {
                                iterations = (int)((productionModeValue - stVal) / recipe.outputValue);
                            }
                            break;
                        }
                    case FactoryProductionMode.Iterations:
                        {
                            if (productionModeValue < iterations) iterations = productionModeValue;
                            break;
                        }
                }
                if (iterations > 0)
                {
                    inputResourcesBuffer -= iterations * recipe.inputValue;
                    inputResourcesBuffer2 -= iterations * recipe.inputValue2;
                    outputResourcesBuffer += iterations * recipe.outputValue;
                    outputResourcesBuffer = storage.AddResource(recipe.output, outputResourcesBuffer);
                }
            }
        }
        switch (productionMode)
        {
            case FactoryProductionMode.Limit:
                workPaused = (storage.standartResources[recipe.output.ID] >= productionModeValue);
                break;
            case FactoryProductionMode.Iterations:
                productionModeValue -= iterations;
                if (productionModeValue <= 0)
                {
                    productionModeValue = 0;
                    SetActivationStatus(false, true);
                }
                break;
        }
    }

    new public AdvancedRecipe GetRecipe()
    {
        return recipe;
    }
    override protected void SetRecipe(Recipe br)
    {
        AdvancedRecipe ar = br as AdvancedRecipe;
        if (ar == null)
        {
            recipe = AdvancedRecipe.NoRecipe;
        }
        else recipe = ar;
        if (recipe != AdvancedRecipe.NoRecipe)
        {
            if (inputResourcesBuffer > 0f)
            {
                colony.storage.AddResource(recipe.input, recipe.inputValue);
                inputResourcesBuffer = 0f;
            }
            if (inputResourcesBuffer2 > 0f)
            {
                colony.storage.AddResource(recipe.input2, recipe.inputValue2);
                inputResourcesBuffer2 = 0f;
            }
            if (outputResourcesBuffer > 0f)
            {
                colony.storage.AddResource(recipe.output, recipe.outputValue);
                outputResourcesBuffer = 0f;
            }
        }
        workflow = 0;
        recipe = ar;
        productionModeValue = 0;
        workflowToProcess = ar.workflowToResult;
        workPaused = (productionMode == FactoryProductionMode.Limit) & colony.storage.standartResources[ar.output.ID] >= productionModeValue;
    }

    #region save-load system
    new private List<byte> SerializeFactory()
    {
        var data = new List<byte>() { (byte)productionMode };
        data.AddRange(System.BitConverter.GetBytes(recipe.ID));
        data.AddRange(System.BitConverter.GetBytes(inputResourcesBuffer));
        data.AddRange(System.BitConverter.GetBytes(inputResourcesBuffer2));
        data.AddRange(System.BitConverter.GetBytes(outputResourcesBuffer));
        data.AddRange(System.BitConverter.GetBytes(productionModeValue));
        //SERIALIZER_LENGTH = 21;
        return data;
    }

    new private int LoadFactoryData(byte[] data, int startIndex)
    {
        SetRecipe(Recipe.GetRecipeByNumber(System.BitConverter.ToInt32(data, startIndex + 1)));
        inputResourcesBuffer = System.BitConverter.ToSingle(data, startIndex + 5);
        inputResourcesBuffer2 = System.BitConverter.ToSingle(data, startIndex + 9);
        outputResourcesBuffer = System.BitConverter.ToSingle(data, startIndex + 13);
        productionMode = (FactoryProductionMode)data[startIndex];
        productionModeValue = System.BitConverter.ToInt32(data, startIndex + 17);
        return startIndex + 21;
    }
    #endregion
}