using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.TMP;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.FinishLegendarySubstanceMenu;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.Settings;
using PotionCraft.TMPAtlasGenerationSystem;
using PotionCraftAlchemyMachineRecipes.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using PotionCraft.ScriptableObjects;
using PotionCraft.ManagersSystem.Player;

namespace PotionCraftAlchemyMachineRecipes.Scripts.Services
{
    /// <summary>
    /// Responsible for modifying the recipe UI
    /// </summary>
    public class RecipeUIService
    {
        /// <summary>
        /// Postfix method for UpdateSaveProductRecipeButtonPatch.
        /// This method unlocks the save recipe button when finishing an alchemy machine product.
        /// </summary>
        public static void UpdateSaveProductRecipeButton(SaveProductRecipeButton saveProductRecipeButton)
        {
            //This may be null on startup
            if (saveProductRecipeButton == null) return;
            saveProductRecipeButton.Locked = false;
        }

        /// <summary>
        /// Prefix method for FixLegendaryRecipeTooltipContentPatch.
        /// This method overrides the tooltip used in the recipe book when hovering over a bookmark with the tooltip from the inventory for the corresponding alchemy machine product.
        /// </summary>
        public static bool FixLegendaryRecipeTooltipContent(Potion instance, ref TooltipContent result, int itemCount, bool anyModifierHeld)
        {
            TooltipContent productTooltip = null;
            var exResult = Ex.RunSafe(() =>
            {
            if (!RecipeService.IsLegendaryRecipe(instance)) return true;
            var product = AlchemyMachineProductService.GetAlchemyMachineProduct(instance);
                productTooltip = product.GetTooltipContent(itemCount, null, anyModifierHeld);
            return false;
            });
            if (exResult || productTooltip == null) return true;
            result = productTooltip;
            return false;
        }

        /// <summary>
        /// Prefix method for FixLegendaryRecipeBookmarkIconPatch.
        /// This method changes the bookmark icon to the corresponding alchemy machine product icon.
        /// </summary>
        public static bool FixLegendaryRecipeBookmarkIcon(RecipeBook instance, int index)
        {
            if (instance.savedRecipes.Count <= index) return true;
            var potion = instance.savedRecipes[index];
            if (potion == null) return true;
            if (!RecipeService.IsLegendaryRecipe(potion)) return true;
            var product = AlchemyMachineProductService.GetAlchemyMachineProduct(potion);
            if (product == null) return true;
            //Get icons from AlchemyMachineProduct
            var activeBookmarkIcon = product.GetActiveMarkerIcon();
            var inactiveBookmarkIcon = product.GetIdleMarkerIcon();
            //Update icons for this bookmark
            instance.bookmarkControllersGroupController.SetBookmarkContent(index, activeBookmarkIcon, inactiveBookmarkIcon);
            return false;
        }

        /// <summary>
        /// Postfix method for HidePotionCustomizationPatch.
        /// This method hides most of the potion customization options which have no purpose for alchemy machine recipes.
        /// This method is also responsible for adding the alchemy machine product image in place of where the potion image would be.
        /// </summary>
        public static void HidePotionCustomization(RecipeBookLeftPageContent instance, GameObject potionSlotBackgroundGameObject)
        {
            const float spriteScale = 1.5f;

            var potionSlotBackground = potionSlotBackgroundGameObject.GetComponent<SpriteRenderer>();

            if (StaticStorage.PotionBackgroundSprite == null)
            {
                StaticStorage.PotionBackgroundSprite = potionSlotBackground.sprite;
                StaticStorage.PotionBackgroundColor = potionSlotBackground.color;
                StaticStorage.PotionBackgroundSize = potionSlotBackground.size;
                StaticStorage.PotionBackgroundIsActive = potionSlotBackgroundGameObject.activeSelf;
            }
            if (instance.pageContentPotion == null)
            {
                RestorePotionSlotBackground(potionSlotBackground);
                return;
            }
            var isLegendaryRecipe = RecipeService.IsLegendaryRecipe(instance.pageContentPotion);
            ShowHide(instance.potionCustomizationPanel.customizeBottleButton.gameObject, !isLegendaryRecipe);
            ShowHide(instance.potionCustomizationPanel.customizeIconButton.gameObject, !isLegendaryRecipe);
            ShowHide(instance.potionCustomizationPanel.customizeStickerButton.gameObject, !isLegendaryRecipe);
            ShowHide(instance.potionCustomizationPanel.customizeColorButton1.gameObject, !isLegendaryRecipe);
            ShowHide(instance.potionCustomizationPanel.customizeColorButton2.gameObject, !isLegendaryRecipe);
            ShowHide(instance.potionCustomizationPanel.customizeColorButton3.gameObject, !isLegendaryRecipe);
            ShowHide(instance.potionCustomizationPanel.customizeColorButton4.gameObject, !isLegendaryRecipe);
            ShowHide(instance.potionCustomizationPanel.resetAllCustomizationButton.gameObject, !isLegendaryRecipe);
            ShowHide(instance.GetPotionVisualObject().gameObject, !isLegendaryRecipe);

            if (isLegendaryRecipe)
            {
                var product = AlchemyMachineProductService.GetAlchemyMachineProduct(instance.pageContentPotion);
                if (product == null) return;
                var sprite = product.GetRecipeIcon();
                potionSlotBackground.color = new Color(1,1,1,1);
                potionSlotBackground.sprite = sprite;
                potionSlotBackground.color = new Color(potionSlotBackground.color.r, potionSlotBackground.color.g, potionSlotBackground.color.b, 1.0f);
                potionSlotBackground.size *= spriteScale;
                potionSlotBackgroundGameObject.SetActive(true);
                var recipePageInventory = instance.transform.Find("PotionBottle").Find("InventoryPanel").GetComponent<RecipeBookPanelInventoryPanel>().Inventory;
                recipePageInventory.RemoveItem(recipePageInventory.items.First().Key);
                var tooltip = AccessTools.Field(instance.GetType(), "placeholderForPotionTooltip").GetValue(instance) as RecipeBookPotionPlaceholderForTooltip;
                tooltip.isInteractable = false;
            }
            else
            {
                RestorePotionSlotBackground(potionSlotBackground);
            }
        }

        private static void RestorePotionSlotBackground(SpriteRenderer potionSlotBackground)
        {
            potionSlotBackground.color = StaticStorage.PotionBackgroundColor;
            potionSlotBackground.sprite = StaticStorage.PotionBackgroundSprite;
            potionSlotBackground.color = StaticStorage.PotionBackgroundColor;
            potionSlotBackground.size = StaticStorage.PotionBackgroundSize;
            if (potionSlotBackground.gameObject.activeSelf != StaticStorage.PotionBackgroundIsActive)
                potionSlotBackground.gameObject.SetActive(StaticStorage.PotionBackgroundIsActive);
        }

        private static void ShowHide(GameObject gameObject, bool show)
        {
            gameObject.SetActive(show);
        }

        public static MethodInfo CrucibleAtlasNameMethod;

        /// <summary>
        /// Transpiler method for ChangeAtlasNameForProductPatch.
        /// This is a method which overrides the existing UpdateIngredientsList method.
        /// The purpose of this is to allow legendary stones to have icons in the ingredient list.
        /// </summary>
        public static IEnumerable<CodeInstruction> UpdateIngredientList(IEnumerable<CodeInstruction> instructions)
        {
            var getIconAtlasNameIfUsedComponentIsProduct = AccessTools.Method(typeof(RecipeUIService), nameof(GetIconAtlasNameIfUsedComponentIsProduct));
            var getAtlasSpriteNameIfUsedComponentIsProduct = AccessTools.Method(typeof(RecipeUIService), nameof(GetAtlasSpriteNameIfUsedComponentIsProduct));
            var getItemCountIfUsedComponentIsProduct = AccessTools.Method(typeof(RecipeUIService), nameof(GetItemCountIfUsedComponentIsProduct));
            var returnInstructions = new List<CodeInstruction>();

            var modifyingGetItemCount = false;
            foreach (var instruction in instructions)
            {
                //Find the first call where we start to call Inventory.GetItemCount and skip it
                if (instruction.operand is MethodInfo getPlayer
                    && getPlayer == AccessTools.Method(typeof(Managers), $"get_{nameof(Managers.Player)}"))
                {
                    modifyingGetItemCount = true;
                    continue;
                }

                if (modifyingGetItemCount)
                {
                    //Skip the call to get the Inventory from the player manager
                    if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo fieldInfo
                        && fieldInfo == AccessTools.Field(typeof(PlayerManager), nameof(PlayerManager.inventory)))
                    {
                        continue;
                    }

                    //Skip all operands if we have found the initial call to get the Player manager until we find the call to Inventory.GetItemCount and then return our item count method instead
                    if (instruction.opcode == OpCodes.Callvirt)
                    {
                        modifyingGetItemCount = false;
                        //Replace the Inventory.GetItemCount call with our method
                        yield return new CodeInstruction(OpCodes.Call, getItemCountIfUsedComponentIsProduct);
                        continue;
                    }
                    yield return instruction;
                    continue;
                }

                //Find every instance where ingredientsAtlasName is accessed
                if (instruction.opcode == OpCodes.Ldloc_1)
                {
                    //Replace the Ldloc_0 call with our method effectivly filtering ingredientsAtlasName through our method whenever it is accessed
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6); // usedComponent
                    yield return new CodeInstruction(OpCodes.Ldloc_1); // ingredientsAtlasName
                    yield return new CodeInstruction(OpCodes.Call, getIconAtlasNameIfUsedComponentIsProduct);
                    continue;
                }

                //Find every instance where GetAtlasSpriteName is called
                if (instruction.operand is MethodInfo getAtlasSpriteName 
                    && getAtlasSpriteName == AccessTools.Method(typeof(IngredientsAtlasGenerator), nameof(IngredientsAtlasGenerator.GetAtlasSpriteName)))
                {
                    //Replace the GetAtlasSpriteName call with our method
                    yield return new CodeInstruction(OpCodes.Call, getAtlasSpriteNameIfUsedComponentIsProduct);
                    continue;
                }
                yield return instruction;
            }
        }

        private static int GetItemCountIfUsedComponentIsProduct(InventoryItem item)
        {
            if (item is not AlchemyMachineProduct product) return Managers.Player.inventory.GetItemCount(item);
            return RecipeService.GetAvailableProductCount(product);
        }

        public static string GetIconAtlasNameIfUsedComponentIsProduct(PotionUsedComponent usedComponent, string oldIngredientsAtlasName)
        {
            if (usedComponent.componentObject is not AlchemyMachineProduct) return oldIngredientsAtlasName;
            Plugin.PluginLogger.LogMessage($"{(usedComponent.componentObject as AlchemyMachineProduct).name} - ");
            return Settings<TMPManagerSettings>.Asset.IconsAtlasName;
        }

        public static string GetAtlasSpriteNameIfUsedComponentIsProduct(ScriptableObject componentObject)
        {
            if (componentObject is not AlchemyMachineProduct) return IngredientsAtlasGenerator.GetAtlasSpriteName(componentObject);
            if (componentObject.name == "PhilosophersStone")
            {
                return "Philosopher'sStone";
            }
            return componentObject.name;
        }


        /// <summary>
        /// Postfix method for DisableContinueFromHereButtonPatch.
        /// This method disables the continue brewing from here button on the recipe page since pressing that would cause a lot of exceptions.
        /// </summary>
        public static void DisableContinueFromHereButton(RecipeBookContinuePotionBrewingButton instance, RecipeBookRightPageContent rightPageContent)
        {
            if (rightPageContent.pageContentPotion == null) return;
            if (!RecipeService.IsLegendaryRecipe(rightPageContent.pageContentPotion)) return;
            instance.Locked = true;
        }

        /// <summary>
        /// Postfix method for ChangeBrewPotionButtonCountPatch.
        /// This method updates the ammount of available brews to the actual number.
        /// By default this calculation is only done with the normal ingredients and ignores required legendary stones.
        /// </summary>
        public static void ChangeBrewPotionButtonCount(ref int result, Potion currentPotion)
        {
            try
            {
            if (currentPotion == null) return;
            if (!RecipeService.IsLegendaryRecipe(currentPotion)) return;
            var requiredProductInstance = AlchemyMachineProductService.GetRequiredAlchemyMachineProduct(currentPotion);
            if (requiredProductInstance != null)
            {
                //Get available count of the required legendary stone if it is required for the recipe
                var availableProductCount = RecipeService.GetAvailableProductCount(requiredProductInstance);
                //Update the count to take this into consideration
                if (availableProductCount < result)
                {
                    result = availableProductCount;
                }
                }
            }
            catch (Exception ex)
            {
                Ex.LogException(ex);
            }
            
        }

        public static bool FixPotionIconException(ref Icon result, string iconName)
        {
            Icon tempResult = null;
            var returnValue = Ex.RunSafe(() =>
            {

                if (string.IsNullOrEmpty(iconName))
                {
                    tempResult = Icon.allIcons.FirstOrDefault();
                    return false;
                }
                return true;
            });
            result = tempResult;
            return returnValue;
        }
    }
}
