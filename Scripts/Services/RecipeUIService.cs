using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.TMP;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.FinishLegendarySubstanceMenu;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.Settings;
using PotionCraft.TMPAtlasGenerationSystem;
using PotionCraftAlchemyMachineRecipes.Scripts.Storage;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static PotionCraft.ScriptableObjects.Potion;

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
                productTooltip = product.GetTooltipContent(itemCount, anyModifierHeld);
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
        public static void HidePotionCustomization(RecipeBookLeftPageContent instance, SpriteRenderer potionSlotBackground)
        {
            const float spriteScale = 1.5f;

            if (instance.currentPotion == null) return;
            var isLegendaryRecipe = RecipeService.IsLegendaryRecipe(instance.currentPotion);
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
                var product = AlchemyMachineProductService.GetAlchemyMachineProduct(instance.currentPotion);
                if (product == null) return;
                var sprite = product.GetRecipeIcon();
                if (StaticStorage.PotionBackgroundSprite == null)
                {
                    StaticStorage.PotionBackgroundSprite = potionSlotBackground.sprite;
                    StaticStorage.PotionBackgroundColor = potionSlotBackground.color;
                    StaticStorage.PotionBackgroundSize = potionSlotBackground.size;
                }
                potionSlotBackground.color = new Color(1,1,1,1);
                potionSlotBackground.sprite = sprite;
                potionSlotBackground.color = new Color(potionSlotBackground.color.r, potionSlotBackground.color.g, potionSlotBackground.color.b, 1.0f);
                potionSlotBackground.size *= spriteScale;
            }
            else
            {
                potionSlotBackground.color = StaticStorage.PotionBackgroundColor;
                potionSlotBackground.sprite = StaticStorage.PotionBackgroundSprite;
                potionSlotBackground.color = StaticStorage.PotionBackgroundColor;
                potionSlotBackground.size = StaticStorage.PotionBackgroundSize;
            }
        }

        private static void ShowHide(GameObject gameObject, bool show)
        {
            gameObject.SetActive(show);
        }


        /// <summary>
        /// Prefix method for ChangeAtlasNameForProductPatch.
        /// This is a method which overrides the existing UpdateIngredientsList method.
        /// The purpose of this is to allow legendary stones to have icons in the ingredient list.
        /// The only thing this changes from the original method is the atlas name for the icon set and the sprite name to use the existing icons for legendary stones.
        /// This method could probably be delted and replaced with a Transpiler but I was not able to pull that off on my first attempt.
        /// </summary>
        public static bool UpdateIngredientList(RecipeBookLeftPageContent instance, TextMeshPro ingredientsListTitle, Color emptyPageTextColor, TextMeshPro ingredientsText, Color filledPageTextColor, int _ColorMask, Color missingIngredientsTextColor, Color missingIngredientsSpriteColor, List<IngredientTooltipObject> instantiatedIngredientObjects, Transform ingredientTooltipObjectsContainer)
        {
            if (instance.currentState == PageContent.State.Empty)
            {
                ingredientsListTitle.color = emptyPageTextColor;
                ingredientsText.text = string.Empty;
            }
            else
            {
                ingredientsListTitle.color = filledPageTextColor;
                var ingredientsAtlasName = Settings<TMPManagerSettings>.Asset.IngredientsAtlasName;
                var iconsAtlasName = Settings<TMPManagerSettings>.Asset.IconsAtlasName;
                var str1 = string.Empty;
                var str2 = "#" + ColorUtility.ToHtmlStringRGBA(missingIngredientsTextColor);
                var str3 = "#" + ColorUtility.ToHtmlStringRGBA(missingIngredientsSpriteColor);
                for (int index = 0; index < instance.currentPotion.usedComponents.Count; ++index)
                {
                    var usedComponent = instance.currentPotion.usedComponents[index];
                    var isLegendaryIngredient = usedComponent.componentObject is AlchemyMachineProduct;
                    var atlasName = isLegendaryIngredient ? iconsAtlasName : ingredientsAtlasName;
                    var atlasSpriteName = isLegendaryIngredient ? usedComponent.componentObject.name : IngredientsAtlasGenerator.GetAtlasSpriteName(usedComponent.componentObject);

                    var itemCount = isLegendaryIngredient
                                        ? RecipeService.GetAvailableProductCount(usedComponent.componentObject as AlchemyMachineProduct)
                                        : usedComponent.componentObject is InventoryItem item
                                            ? Managers.Player.inventory.GetItemCount(item)
                                            : 1;
                    if (usedComponent.componentType == UsedComponent.ComponentType.InventoryItem && itemCount < usedComponent.amount)
                        str1 = str1 + "<sprite=\"" + atlasName + "\" color=" + str3 + " name=\"" + atlasSpriteName + "\"><color=" + str2 + ">"
                                + (usedComponent.amount == 0 ? "" : string.Format("<sub><voffset=-0.13em>{0}</voffset></sub>", usedComponent.amount)) + "</color> ";
                    else
                        str1 = str1 + "<sprite=\"" + atlasName + "\" name=\"" + atlasSpriteName + "\">"
                                + (usedComponent.amount == 0 ? "" : string.Format("<sub><voffset=-0.13em>{0}</voffset></sub>", usedComponent.amount)) + " ";
                }
                ingredientsText.text = str1;
            }
            ingredientsText.ForceMeshUpdate(true, true);
            foreach (var componentsInChild in ingredientsText.GetComponentsInChildren<MeshRenderer>())
            {
                var material = componentsInChild.material;
                if (material.HasProperty(_ColorMask))
                    material.SetFloat(_ColorMask, 14f);
            }
            IngredientTooltipObject.RespawnIngredientTooltipObjects(instantiatedIngredientObjects,
                                                                    ingredientsText,
                                                                    instance.ingredientTooltipRaycastRange,
                                                                    ingredientTooltipObjectsContainer,
                                                                    instance.currentPotion?.usedComponents);
            return false;
        }

        /// <summary>
        /// Postfix method for DisableContinueFromHereButtonPatch.
        /// This method disables the continue brewing from here button on the recipe page since pressing that would cause a lot of exceptions.
        /// </summary>
        public static void DisableContinueFromHereButton(RecipeBookContinuePotionBrewingButton instance, RecipeBookRightPageContent rightPageContent)
        {
            if (rightPageContent.currentPotion == null) return;
            if (!RecipeService.IsLegendaryRecipe(rightPageContent.currentPotion)) return;
            instance.Locked = true;
        }

        /// <summary>
        /// Postfix method for ChangeBrewPotionButtonCountPatch.
        /// This method updates the ammount of available brews to the actual number.
        /// By default this calculation is only done with the normal ingredients and ignores required legendary stones.
        /// </summary>
        public static void ChangeBrewPotionButtonCount(ref int result, RecipeBookRightPageContent instance)
        {
            try
            {
            if (instance.currentPotion == null) return;
            if (!RecipeService.IsLegendaryRecipe(instance.currentPotion)) return;
            var requiredProductInstance = AlchemyMachineProductService.GetRequiredAlchemyMachineProduct(instance.currentPotion);
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
    }
}
