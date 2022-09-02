using PotionCraft.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace PotionCraftAlchemyMachineRecipes.Scripts.Storage
{
    public static class StaticStorage
    {
        public const string AlchemyMachineRecipesJsonSaveName = "FahlgorithmAlchemyMachineRecipes";

        public static bool CurrentlyMakingPotion;
        public static bool CurrentlyGeneratingRecipe;

        public static List<InventoryItem> Ingredients;
        public static PotionEffect[] SavedPotionEffects;
        public static Sprite PotionBackgroundSprite;
        public static Color PotionBackgroundColor;
        public static Vector2 PotionBackgroundSize;
        public static List<SavedRecipe> RecipesToSave;
        public static string StateJsonString;
    }
}
