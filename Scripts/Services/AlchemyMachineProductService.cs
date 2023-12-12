using DarkScreenSystem;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Game;
using PotionCraft.ObjectBased.AlchemyMachine;
using PotionCraft.ObjectBased.UIElements.Books.GoalsBook;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.ConfirmationWindow;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.Settings;
using PotionCraft.SoundSystem;
using PotionCraft.SoundSystem.SoundPresets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PotionCraftAlchemyMachineRecipes.Scripts.Services
{
    /// <summary>
    /// Responsible for interacting with the PotionCraft alchemy machine product system.
    /// </summary>
    public static class AlchemyMachineProductService
    {
        /// <summary>
        /// Prefix method for BrewPotionPatch.
        /// This method is responsible for creating the alchemy machine product and removing ingredients as specified in the recipe.
        /// </summary>
        public static bool BrewPotion(RecipeBookBrewPotionButton instance, RecipeBookRightPageContent rightPageContent)
        {
            if (rightPageContent.pageContentPotion == null) return true;
            if (!RecipeService.IsLegendaryRecipe(rightPageContent.pageContentPotion)) return true;
            MakeProduct(instance, rightPageContent);
            return false;
        }

        private static void MakeProduct(RecipeBookBrewPotionButton instance, RecipeBookRightPageContent rightPageContent)
        {
            var resultPotionsCount = RecipeBook.GetAvailableResultPotionsCount(rightPageContent.currentState, rightPageContent.pageContentPotion);

            if (resultPotionsCount == 0 || rightPageContent.pageContentPotion == null) return;

            var count = instance.GetType().GetMethod("GetPotionCountForBrew", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(instance, null) as int?;
            if (count == null || count == 0) return;
            if (count > 1 && count == resultPotionsCount)
            {
                //Ensure recipe is not overcrafted in situations where more than one is being crafted on the same frame
                DarkScreen.DeactivateAll(DarkScreenDeactivationType.Other, Managers.Potion.recipeBook.layer, false);
                var confirmTitleKey = new Key("#max_potion_brewing_confirmation_title", new List<string>() { count.ToString() });
                ConfirmationWindow.Show(DarkScreenLayer.Middle, confirmTitleKey, new Key("#max_potion_brewing_confirmation_description"),
                                        Settings<GameManagerSettings>.Asset.confirmationWindowPosition, new Action(BrewProduct), null);
            }
            else
            {
                BrewProduct();
            }

            void BrewProduct()
            {
                var potion = rightPageContent.pageContentPotion;
                var productName = potion.potionFromPanel.collectedPotionEffects.LastOrDefault();
                if (string.IsNullOrEmpty(productName)) return;
                //Create clone of default product
                var product = AlchemyMachineProduct.GetByName(productName, warning: false).Clone();
                //Copy custom title and description to product
                if (potion.isTitleCustom && !string.IsNullOrWhiteSpace(potion.customTitle) && !potion.customTitle.Equals(product.name))
                    product.customTitle = potion.customTitle;
                if (!string.IsNullOrWhiteSpace(potion.customDescription))
                    product.customDescription = potion.customDescription;

                //Remove ingredients in accordance with the recipe
                RecipeBook.DecreaseIngredientsAmountOnPotionBrewing(rightPageContent.pageContentPotion, count.Value);
                DecreaseLegendaryIngredientsAmmountOnBrewing(rightPageContent, count.Value);
                Managers.Player.inventory.onItemChanged.Invoke(false);

                //Add the item to the inventory
                if (product is LegendarySaltPile inventoryItem)
                {
                    Managers.Player.inventory.AddItem(inventoryItem.convertToSaltOnPickup, inventoryItem.amountOfSalt);
                }
                else 
                {
                    Managers.Player.inventory.AddItem(product, count.Value);
                }

                //Update stats and goals
                var potionsRequiredForRecipe = GetPotionCountForAlchemyMachineProduct(product);
                var potionBrewedFromRecipeGoal = GoalsLoader.GetGoalByName("CreatePotionFromRecipeBook");
                //I am not sure if this goal tracks potions brewed but just incase it does we should increment it by the number of required potions to simulate brewing potions manually for the recipe
                Enumerable.Range(0, potionsRequiredForRecipe).ToList().ForEach(i => potionBrewedFromRecipeGoal.ProgressIncrement());
                GoalsLoader.GetGoalByName("CreateLegendarySubstance" + productName, false)?.ProgressIncrement();
                Sound.Play(Settings<SoundPresetInterface>.Asset.potionFinishing);
                //Update the potions brewed counter by the number of potions that would have been required to make this alchemy machine product
                Managers.Potion.PotionsBrewed += count.Value * potionsRequiredForRecipe;
                Managers.Ingredient.alchemyMachine.LegendarySubstancesBrewedAmount += count.Value;
            }
        }

        private static void DecreaseLegendaryIngredientsAmmountOnBrewing(RecipeBookRightPageContent rightPageContent, int countToRemove)
        {
            var requiredProductInstance = GetRequiredAlchemyMachineProduct(rightPageContent.pageContentPotion);
            if (requiredProductInstance == null) return;
            var totalItemCount = RecipeService.GetAvailableProductCount(requiredProductInstance);
            //This check should be unnesessary...
            if (totalItemCount >= countToRemove)
            {
                //Get all matching product stacks from inventory
                var items = Managers.Player.inventory.items.Where(i => i.Key is AlchemyMachineProduct && i.Key.name == requiredProductInstance.name).ToList();
                //Start from the beginning and remove from each stack until we have satisfied the countToRemove
                while (countToRemove > 0)
                {
                    if (items.Count == 0)
                    {
                        //This should never happen but lets make sure we don't crash if it does
                        return;
                    }
                    var item = items.First();
                    items.Remove(item);
                    var curItemCount = item.Value;
                    if (curItemCount <= countToRemove)
                        Managers.Player.inventory.items.Remove(item);
                    else
                        Managers.Player.inventory.items[item.Key] -= countToRemove;
                    countToRemove -= Mathf.Min(countToRemove, curItemCount);
                }

            }
        }

        /// <summary>
        /// Returns the AlchemyMachineProduct this recipe makes.
        /// </summary>
        public static AlchemyMachineProduct GetAlchemyMachineProduct(Potion recipe)
        {
            var productName = recipe.potionFromPanel.collectedPotionEffects.LastOrDefault();
            if (string.IsNullOrEmpty(productName)) return null;
            return AlchemyMachineProduct.GetByName(productName, warning: false);
        }

        /// <summary>
        /// Returns the AlchemyMachineProduct this recipe requires if it requires any.
        /// </summary>
        /// <returns>null if no product is required for this recipe or the product if a product is required for the recipe.</returns>
        public static AlchemyMachineProduct GetRequiredAlchemyMachineProduct(Potion recipe)
        {
            var modifiedList = recipe.potionFromPanel.collectedPotionEffects;
            var requiredProduct = modifiedList[^2];
            if (string.IsNullOrEmpty(requiredProduct)) return null;
            return AlchemyMachineProduct.GetByName(requiredProduct, warning: false);
        }

        public static int GetPotionCountForAlchemyMachineProduct(AlchemyMachineProduct product)
        {
            var legendaryRecipe = GetLegendaryRecipe(product);
            //This should never happen but the minimum possible ammount of potions used is 5 so lets return that in this error case
            if (legendaryRecipe == null) return 5;
            var machineSlots = Enum.GetValues(typeof(Slot)).OfType<Slot>().Except(new[] { Slot.RightFurnace, Slot.LeftFurnace }).ToList();
            return machineSlots.Where(s => legendaryRecipe.DesiredItemPerSlot(s) != null).Count();
        }

        public static LegendaryRecipe GetLegendaryRecipe(AlchemyMachineProduct product)
        {
            return Managers.SaveLoad.SelectedProgressState
                                    .unlockedLegendaryRecipes
                                    .Select(r => LegendaryRecipe.GetByName(r))
                                    .FirstOrDefault(lr => lr.resultItem.name == product.name);

        }
    }
}
