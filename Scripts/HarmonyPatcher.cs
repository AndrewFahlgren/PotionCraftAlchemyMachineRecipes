using HarmonyLib;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.ObjectBased.AlchemyMachine;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.FinishLegendarySubstanceMenu;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.SaveFileSystem;
using PotionCraft.SaveLoadSystem;
using PotionCraft.ScriptableObjects;
using PotionCraftAlchemyMachineRecipes.Scripts.Services;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PotionCraftAlchemyMachineRecipes.Scripts
{
    [HarmonyPatch(typeof(FinishLegendarySubstanceWindow), "UpdateSaveProductRecipeButton")]
    public class UpdateSaveProductRecipeButtonPatch
    {
        static void Postfix(SaveProductRecipeButton ___saveProductRecipeButton)
        {
            RecipeUIService.UpdateSaveProductRecipeButton(___saveProductRecipeButton);
        }
    }

    [HarmonyPatch(typeof(SaveProductRecipeButton), "OnButtonReleasedPointerInside")]
    public class ActuallySaveProductRecipeButtonPatch
    {
        static bool Prefix()
        {
            return RecipeService.ActuallySaveProductRecipeButton();
        }
    }

    [HarmonyPatch(typeof(AlchemyMachineObject), "WipeSlots")]
    public class StorePotionsBeforeWipePatch
    {
        static bool Prefix()
        {
            return RecipeService.StorePotionsBeforeWipe();
        }
    }

    [HarmonyPatch(typeof(Potion), "Clone")]
    public class StoreRecipeMarksFromRecipeBrewPatch
    {
        static void Postfix(ref Potion __result, Potion __instance)
        {
            RecipeService.CopyImportantInfoToPotionInstance(__result, __instance);
        }
    }

    [HarmonyPatch(typeof(Potion), "GetTooltipContent")]
    public class FixLegendaryRecipeTooltipContentPatch
    {
        static bool Prefix(Potion __instance, ref TooltipContent __result, int itemCount, bool anyModifierHeld)
        {
            return RecipeUIService.FixLegendaryRecipeTooltipContent(__instance, ref __result, itemCount, anyModifierHeld);
        }
    }

    [HarmonyPatch(typeof(RecipeBook), "UpdateBookmarkIcon")]
    public class FixLegendaryRecipeBookmarkIconPatch
    {
        static bool Prefix(RecipeBook __instance, int index)
        {
            return RecipeUIService.FixLegendaryRecipeBookmarkIcon(__instance, index);
        }
    }


    [HarmonyPatch(typeof(PotionManager), "GeneratePotionFromCurrentPotion")]
    public class StoreRecipeMarksFromPotionBrewPatch
    {
        static void Postfix(ref Potion __result, PotionManager __instance)
        {
            RecipeService.StoreRecipeMarksFromPotionBrew(__result, __instance);
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdatePage")]
    public class HidePotionCustomizationPatch
    {
        static void Postfix(RecipeBookLeftPageContent __instance, SpriteRenderer ___potionSlotBackground)
        {
            RecipeUIService.HidePotionCustomization(__instance, ___potionSlotBackground);
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateIngredientsList")]
    public class AddLegendaryIngredientToListPatch
    {
        static bool Prefix(RecipeBookLeftPageContent __instance)
        {
            return RecipeService.AddLegendaryIngredientToList(__instance);
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateIngredientsList")]
    public class RemoveLegendaryIngredientFromListPatch
    {
        static void Postfix(RecipeBookLeftPageContent __instance)
        {
            RecipeService.RemoveLegendaryIngredientFromList(__instance);
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateIngredientsList")]
    public class ChangeAtlasNameForProductPatch
    {
        static bool Prefix(RecipeBookLeftPageContent __instance, TextMeshPro ___ingredientsListTitle,
                                                Color ___emptyPageTextColor, TextMeshPro ___ingredientsText, Color ___filledPageTextColor,
                                                int ____ColorMask, Color ___missingIngredientsTextColor, Color ___missingIngredientsSpriteColor,
                                                List<IngredientTooltipObject> ___instantiatedIngredientObjects, Transform ___ingredientTooltipObjectsContainer)
        {
            return RecipeUIService.UpdateIngredientList(__instance, ___ingredientsListTitle, ___emptyPageTextColor,
                                                       ___ingredientsText, ___filledPageTextColor, ____ColorMask, 
                                                       ___missingIngredientsTextColor, ___missingIngredientsSpriteColor, 
                                                       ___instantiatedIngredientObjects, ___ingredientTooltipObjectsContainer);
        }
    }

    [HarmonyPatch(typeof(RecipeBookContinuePotionBrewingButton), "UpdateVisual")]
    public class DisableContinueFromHereButtonPatch
    {
        static void Postfix(RecipeBookContinuePotionBrewingButton __instance, RecipeBookRightPageContent ___rightPageContent)
        {
            RecipeUIService.DisableContinueFromHereButton(__instance, ___rightPageContent);
        }
    }

    [HarmonyPatch(typeof(RecipeBookRightPageContent), "GetAvailableResultPotionsCount")]
    public class ChangeBrewPotionButtonCountPatch
    {
        static void Postfix(ref int __result, RecipeBookRightPageContent __instance)
        {
            RecipeUIService.ChangeBrewPotionButtonCount(ref __result, __instance);
        }
    }

    [HarmonyPatch(typeof(RecipeBookBrewPotionButton), "BrewPotion")]
    public class BrewPotionPatch
    {
        static bool Prefix(RecipeBookBrewPotionButton __instance, RecipeBookRightPageContent ___rightPageContent)
        {
            return AlchemyMachineProductService.BrewPotion(__instance, ___rightPageContent);
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateEffects")]
    public class HidePotionCustomizationEffectTextPrefixPatch
    {
        static bool Prefix(Potion ___currentPotion)
        {
            return RecipeService.RemoveRecipePotionEffects(___currentPotion);
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateEffects")]
    public class HidePotionCustomizationEffectTextPostfixPatch
    {
        static void Postfix(Potion ___currentPotion)
        {
            RecipeService.ReAddRecipePotionEffects(___currentPotion);
        }
    }

    [HarmonyPatch(typeof(File), "CreateNewFromState")]
    public class RemoveAlchemyMachineRecipesFromSavedStatePatch
    {
        static bool Prefix(SavePool savePool, SavedState savedState)
        {
            return SaveLoadService.RemoveAlchemyMachineRecipesFromSavedState(savePool, savedState);
        }
    }

    [HarmonyPatch(typeof(SavedState), "ToJson")]
    public class InjectSavedRecipesPatch
    {
        static void Postfix(ref string __result)
        {
            SaveLoadService.InjectSavedRecipes(ref __result);
        }
    }

    [HarmonyPatch(typeof(File), "Load")]
    public class RetrieveStateJsonStringPatch
    {
        static bool Prefix(File __instance)
        {
            return SaveLoadService.RetrieveStateJsonString(__instance);
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadSelectedState")]
    public class RetreiveSavedAlchemyMachineRecipesFromSavedStatePatch
    {
        static bool Prefix(Type type)
        {
            return SaveLoadService.RetreiveSavedAlchemyMachineRecipesFromSavedState(type);
        }
    }
}
