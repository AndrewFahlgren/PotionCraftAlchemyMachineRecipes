using PotionCraft.SaveLoadSystem;
using System;
using UnityEngine;

namespace PotionCraftAlchemyMachineRecipes.Scripts.Storage
{
    [Serializable]
    public class SavedRecipe
    {
        [SerializeField] public int Index;
        [SerializeField] public SerializedPotionRecipe Recipe;

        public SavedRecipe(int index, SerializedPotionRecipe recipe)
        {
            Index = index;
            Recipe = recipe;
        }
    }
}
