using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ObjectBased.AlchemyMachine;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraftAlchemyMachineRecipes.Scripts.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static PotionCraft.SaveLoadSystem.ProgressState;
using static PotionCraft.ScriptableObjects.Potion;

namespace PotionCraftAlchemyMachineRecipes.Scripts.Services
{
    /// <summary>
    /// Responsible for creating and managing the alchemy machine recipe.
    /// </summary>
    public class RecipeService
    {

        /// <summary>
        /// Prefix method for ActuallySaveProductRecipeButtonPatch.
        /// This method is responsible for generating the alchemy machine recipe.
        /// </summary>
        public static bool ActuallySaveProductRecipeButton()
        {
            //Generate alchemy machine recipe
            var recipe = GenerateRecipe();
            if (recipe == null) return true;
            var recipeBook = Managers.Potion.recipeBook;
            var potionCraftPanel = Managers.Potion.potionCraftPanel;
            //Add recipe to book
            recipeBook.AddRecipe(recipe);
            //Update values in the same way that a normal recipe would to avoid any unintended consequences
            potionCraftPanel.previousPotionRecipe = null;
            potionCraftPanel.potionChangedAfterSavingRecipe = false;
            Managers.Potion.potionCraftPanel.UpdateSaveRecipeButton(false);
            Managers.Ingredient.alchemyMachine.finishLegendarySubstanceWindow.UpdateSaveProductRecipeButton();
            return true;
        }

        private static Potion GenerateRecipe()
        {
            if (StaticStorage.Ingredients == null)
            {
                Plugin.PluginLogger.LogInfo("Error: No ingredients were stored");
                return null;
            }

            //Determine type of legendary substance this recipe is for
            var spawnedItem = Managers.Ingredient.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedItem;
            var potions = StaticStorage.Ingredients.Where(i => i is Potion).OfType<Potion>().ToList();
            var legendaryIngredient = StaticStorage.Ingredients.FirstOrDefault(i => i is LegendarySubstance);

            //Initialize recipe with basic fields
            var firstPotion = potions.First();
            var potionBase = firstPotion.potionBase;
            var recipe = Object.Instantiate(PotionCraft.Settings.Settings<PotionManagerSettings>.Asset.defaultPotion);
            recipe.customTitle = spawnedItem.inventoryItem.GetLocalizedTitle();
            recipe.isTitleCustom = true;
            recipe.potionBase = potionBase;

            //Setup usedComponents for each potion base used by all potions for the recipe
            var usedPotionBases = potions.Select(p => p.potionBase).GroupBy(p => p.name).Select(pg => pg.First()).ToList();
            usedPotionBases.ForEach(potionBase =>
            {
                var component = UsedComponent.GetComponent(recipe.usedComponents, potionBase);
            });

            potions.ForEach(potion =>
            {
                //Aggregate potion recipe marks
                if (potion.potionFromPanel.recipeMarks.Count == 0)
                {
                    //If this is a potion which was brewed pre-mod installation it will not have any recipe marks
                    //In this case try and match this potion to a saved recipe so we can use those marks instead
                    var correspondingRecipe = Managers.Potion.recipeBook.savedRecipes.FirstOrDefault(p => RecipeMatchesPotion(potion, p));
                    if (correspondingRecipe != null)
                    {
                        recipe.potionFromPanel.recipeMarks.AddRange(correspondingRecipe.potionFromPanel.recipeMarks.Select(m => m.Clone()));
                    }
                    else
                    {
                        //If the pre-mod potion was brewed without a recipe and that recipe was not saved then the best we can do is store the potion base recipe mark
                        recipe.potionFromPanel.recipeMarks.Add(SerializedRecipeMark.Generate(SerializedRecipeMark.Type.PotionBase, potionBase.name));
                    }
                }
                else
                {
                    recipe.potionFromPanel.recipeMarks.AddRange(potion.potionFromPanel.recipeMarks.Select(m => m.Clone()));
                }
                //Aggregate potion usedComponents
                potion.usedComponents.ToList().ForEach(ingredient =>
                {
                    if (ingredient.componentObject is PotionBase) return;
                    var component = UsedComponent.GetComponent(recipe.usedComponents, ingredient.componentObject);
                    if (ingredient.componentObject is InventoryItem)
                        component.amount += ingredient.amount;
                });
            });

            //Update serializable potion's usedComponents as well
            recipe.usedComponents.ForEach(component => recipe.potionFromPanel.potionUsedComponents.Add(new SerializedUsedComponent()
            {
                componentName = component.componentObject.name,
                componentAmount = component.amount,
                componentType = component.componentType.ToString()
            }));


            //These will not be shown but go ahead and populate them to prevent exceptions
            recipe.coloredIcon = firstPotion.coloredIcon;
            recipe.bottle = firstPotion.bottle;
            recipe.sticker = firstPotion.sticker;
            recipe.soundPreset = firstPotion.soundPreset;
            recipe.stickerAngle = firstPotion.stickerAngle;
            recipe.Effects = firstPotion.Effects;
            recipe.potionFromPanel.serializedPath = firstPotion.potionFromPanel.serializedPath;
            recipe.potionFromPanel.collectedPotionEffects = firstPotion.potionFromPanel.collectedPotionEffects;
            recipe.potionFromPanel.potionSkinSettings = firstPotion.potionFromPanel.potionSkinSettings;

            //Store the type of legendary product and the type of legendary product needed to craft this recipe inside of collectedPotionEffects
            if (recipe.potionFromPanel.collectedPotionEffects.Count > 3)
            {
                recipe.potionFromPanel.collectedPotionEffects.RemoveRange(3, recipe.potionFromPanel.collectedPotionEffects.Count - 3);
            }
            recipe.potionFromPanel.collectedPotionEffects.Add(legendaryIngredient?.name);
            recipe.potionFromPanel.collectedPotionEffects.Add(spawnedItem.name);

            return recipe;
        }

        private static bool RecipeMatchesPotion(Potion potion, Potion recipe)
        {
            if (recipe == null) return false;
            if (!recipe.GetLocalizedTitle().Equals(potion.GetLocalizedTitle())) return false;
            if (recipe.Effects.Count() != potion.Effects.Count()) return false;
            if (!SequencesMatch(recipe.Effects.Select(e => e.name).ToList(), potion.Effects.Select(e => e.name).ToList())) return false;
            if (recipe.usedComponents.Count() != potion.usedComponents.Count()) return false;
            var recipeUsedComps = recipe.usedComponents.Select(e => new { e.componentObject.name, e.amount }).ToList();
            var potionUsedComps = potion.usedComponents.Select(e => new { e.componentObject.name, e.amount }).ToList();
            if (!SequencesMatch(recipeUsedComps, potionUsedComps)) return false;
            return true;
        }

        private static bool SequencesMatch<T>(IList<T> seq1, IList<T> seq2)
        {
            if (seq1.Count != seq2.Count) return false;
            for (var i = 0; i < seq1.Count; i++)
            {
                if (!seq1[i].Equals(seq2[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Prefix method for StorePotionsBeforeWipePatch.
        /// Potions stored in the alchemy machine are wiped just before we are ready to save the recipe.
        /// This method stores those potions before they are wiped so we can generate the recipe from them.
        /// </summary>
        public static bool StorePotionsBeforeWipe()
        {
            var alchMachine = Resources.FindObjectsOfTypeAll<AlchemyMachineObject>()?.FirstOrDefault();
            if (alchMachine == null)
            {
                Plugin.PluginLogger.LogInfo("Error: Could not find AlchemyMachineObject GameObject instance");
                return true;
            }

            StaticStorage.Ingredients = alchMachine.slots.Select(s => s.ContainedInventoryItem).Where(i => i != null).ToList();
            return true;
        }

        /// <summary>
        /// Determines whether or not this recipe is a legendary recipe.
        /// </summary>
        /// <param name="recipe">The recipe to check.</param>
        /// <returns>True if this is a legendary recipe or False if this is not a legendary recipe.</returns>
        public static bool IsLegendaryRecipe(Potion recipe)
        {
            return recipe.potionFromPanel.recipeMarks.Count(m => m.type == SerializedRecipeMark.Type.PotionBase) > 1;
        }

        /// <summary>
        /// Postfix method for StoreRecipeMarksFromPotionBrewPatch.
        /// When a potion is brewed all recipe marks are lost.
        /// This method will ensure that recipe marks are copied over to the brewed potion so the recipe generation code can access them.
        /// </summary>
        public static void StoreRecipeMarksFromPotionBrew(Potion copyTo, PotionManager instance)
        {
            var copyFrom = instance.potionCraftPanel.GetCurrentPotion();
            CopyImportantInfoToPotionInstance(copyTo, copyFrom);
        }

        /// <summary>
        /// Postfix method for StoreRecipeMarksFromRecipeBrewPatch.
        /// When a potion is brewed all recipe marks are lost.
        /// This method will ensure that recipe marks are copied over to the brewed potion so the recipe generation code can access them.
        /// </summary>
        public static void CopyImportantInfoToPotionInstance(Potion copyTo, Potion copyFrom)
        {
            var recipeMarks = copyTo.potionFromPanel.recipeMarks;
            copyFrom.potionFromPanel.recipeMarks.ForEach(m => recipeMarks.Add(m.Clone()));
            copyTo.potionFromPanel.collectedPotionEffects = copyFrom.potionFromPanel.collectedPotionEffects;
        }

        /// <summary>
        /// Gets the number of matching alchemy machine products in the player's inventory.
        /// </summary>
        public static int GetAvailableProductCount(AlchemyMachineProduct requiredProductInstance)
        {
            return Managers.Player.inventory.items.Where(i => i.Key.name == requiredProductInstance.name).Sum(i => i.Value);
        }

        /// <summary>
        /// Prefix method for AddLegendaryIngredientToListPatch.
        /// Legendary products are not stored in the usedComponent's list to prevent potential issues.
        /// Instead this method is responsible for adding them just before the ingredient list is generated for the recipe.
        /// </summary>
        public static bool AddLegendaryIngredientToList(RecipeBookLeftPageContent instance)
        {
            if (instance.currentPotion == null) return true;
            var requiredLegendaryProduct = AlchemyMachineProductService.GetRequiredAlchemyMachineProduct(instance.currentPotion);
            if (requiredLegendaryProduct != null)
            {
                var component = UsedComponent.GetComponent(instance.currentPotion.usedComponents, requiredLegendaryProduct);
                ++component.amount;
            }
            return true;
        }

        /// <summary>
        /// Postfix method for RemoveLegendaryIngredientFromListPatch.
        /// Legendary products are not stored in the usedComponent's list to prevent potential issues.
        /// This method is responsible for clearing out any added legendary products which were added to generate the ingredient list.
        /// </summary>
        public static void RemoveLegendaryIngredientFromList(RecipeBookLeftPageContent instance)
        {
            if (instance.currentPotion == null) return;
            if (instance.currentPotion.usedComponents.Count == 0) return;
            if (instance.currentPotion.usedComponents.Last().componentObject is not AlchemyMachineProduct) return;
            instance.currentPotion.usedComponents.RemoveAt(instance.currentPotion.usedComponents.Count - 1);
        }

        /// <summary>
        /// Prefix method for HidePotionCustomizationEffectTextPrefixPatch.
        /// Even though alchemy machine recipes have no potion effects to prevent potential issues we use the effects of the first potion used in the recipe.
        /// This method removes those effects before they would be displayed.
        /// </summary>
        public static bool RemoveRecipePotionEffects(Potion currentPotion)
        {
            if (currentPotion == null || !IsLegendaryRecipe(currentPotion)) return true;
            StaticStorage.SavedPotionEffects = currentPotion.Effects;
            var prop = currentPotion.GetType().GetField("effects", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(currentPotion, new PotionEffect[0]);
            return true;
        }

        /// <summary>
        /// Postfix method for HidePotionCustomizationEffectTextPostfixPatch.
        /// Even though alchemy machine recipes have no potion effects to prevent potential issues we use the effects of the first potion used in the recipe.
        /// This method adds back in the effects after we have removed them in the prefix counterpart to this method.
        /// </summary>
        public static void ReAddRecipePotionEffects(Potion currentPotion)
        {
            if (currentPotion == null || !IsLegendaryRecipe(currentPotion)) return;
            var prop = currentPotion.GetType().GetField("effects", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(currentPotion, StaticStorage.SavedPotionEffects);
        }
    }
}
