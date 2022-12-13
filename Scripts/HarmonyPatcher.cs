using HarmonyLib;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.ObjectBased.AlchemyMachine;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.FinishLegendarySubstanceMenu;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.SaveFileSystem;
using PotionCraft.SaveLoadSystem;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.TMPAtlasGenerationSystem;
using PotionCraftAlchemyMachineRecipes.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace PotionCraftAlchemyMachineRecipes.Scripts
{
    [HarmonyPatch(typeof(Icon), "GetByName")]
    public class test
    {
        static bool Prefix(ref Icon __result, string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                __result = Icon.allIcons.FirstOrDefault();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FinishLegendarySubstanceWindow), "UpdateSaveProductRecipeButton")]
    public class UpdateSaveProductRecipeButtonPatch
    {
        static void Postfix(SaveProductRecipeButton ___saveProductRecipeButton)
        {
            Ex.RunSafe(() => RecipeUIService.UpdateSaveProductRecipeButton(___saveProductRecipeButton));
        }
    }

    [HarmonyPatch(typeof(SaveProductRecipeButton), "OnButtonReleasedPointerInside")]
    public class ActuallySaveProductRecipeButtonPatch
    {
        static bool Prefix()
        {
            return Ex.RunSafe(() => RecipeService.ActuallySaveProductRecipeButton());
        }
    }

    [HarmonyPatch(typeof(AlchemyMachineObject), "WipeSlots")]
    public class StorePotionsBeforeWipePatch
    {
        static bool Prefix()
        {
            return Ex.RunSafe(() => RecipeService.StorePotionsBeforeWipe());
        }
    }

    [HarmonyPatch(typeof(Potion), "Clone")]
    public class StoreRecipeMarksFromRecipeBrewPatch
    {
        static void Postfix(ref Potion __result, Potion __instance)
        {
            RunSafe(__result, __instance);
        }

        private static void RunSafe(Potion __result, Potion __instance)
        {
            Ex.RunSafe(() => RecipeService.CopyImportantInfoToPotionInstance(__result, __instance));
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
            return Ex.RunSafe(() => RecipeUIService.FixLegendaryRecipeBookmarkIcon(__instance, index));
        }
    }

    [HarmonyPatch(typeof(PotionCraftPanel), "MakePotion")]
    public class SetCurrentlyMakingPotionPatch
    {
        static bool Prefix()
        {
            return Ex.RunSafe(() => RecipeService.SetCurrentlyMakingPotion(true));
        }
    }

    [HarmonyPatch(typeof(PotionCraftPanel), "MakePotion")]
    public class UnsetCurrentlyMakingPotionPatch
    {
        static void Postfix()
        {
            Ex.RunSafe(() => RecipeService.SetCurrentlyMakingPotion(false));
        }
    }



    [HarmonyPatch(typeof(SaveRecipeButton), "GenerateRecipe")]
    public class SetCurrentlyGeneratingRecipePatch
    {
        static bool Prefix()
        {
            return Ex.RunSafe(() => RecipeService.SetCurrentlyGeneratingRecipe(true));
        }
    }

    [HarmonyPatch(typeof(SaveRecipeButton), "GenerateRecipe")]
    public class UnsetCurrentlyGeneratingRecipePatch
    {
        static void Postfix()
        {
            Ex.RunSafe(() => RecipeService.SetCurrentlyGeneratingRecipe(false));
        }
    }

    [HarmonyPatch(typeof(PotionManager), "GeneratePotionFromCurrentPotion")]
    public class StoreRecipeMarksFromPotionBrewPatch
    {
        static void Postfix(ref Potion __result, PotionManager __instance)
        {
            RunSafe(__result);
        }

        private static void RunSafe(Potion __result)
        {
            Ex.RunSafe(() => RecipeService.StoreRecipeMarksFromPotionBrew(__result));
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdatePage")]
    public class HidePotionCustomizationPatch
    {
        static void Postfix(RecipeBookLeftPageContent __instance, SpriteRenderer ___potionSlotBackground)
        {
            Ex.RunSafe(() => RecipeUIService.HidePotionCustomization(__instance, ___potionSlotBackground));
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateIngredientsList")]
    public class AddLegendaryIngredientToListPatch
    {
        static bool Prefix(RecipeBookLeftPageContent __instance)
        {
            return Ex.RunSafe(() => RecipeService.AddLegendaryIngredientToList(__instance));
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateIngredientsList")]
    public class RemoveLegendaryIngredientFromListPatch
    {
        static void Postfix(RecipeBookLeftPageContent __instance)
        {
            Ex.RunSafe(() => RecipeService.RemoveLegendaryIngredientFromList(__instance));
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateIngredientsList")]
    public class ChangeAtlasNameForProductPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RecipeUIService.UpdateIngredientList(instructions);
        }
    }

    [HarmonyPatch(typeof(RecipeBookContinuePotionBrewingButton), "UpdateVisual")]
    public class DisableContinueFromHereButtonPatch
    {
        static void Postfix(RecipeBookContinuePotionBrewingButton __instance, RecipeBookRightPageContent ___rightPageContent)
        {
            Ex.RunSafe(() => RecipeUIService.DisableContinueFromHereButton(__instance, ___rightPageContent));
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
            return Ex.RunSafe(() => AlchemyMachineProductService.BrewPotion(__instance, ___rightPageContent), () => OnError(___rightPageContent));
        }

        private static bool OnError(RecipeBookRightPageContent rightPageContent)
        {
            try
            {
                if (rightPageContent.currentPotion == null) return true;
                if (!RecipeService.IsLegendaryRecipe(rightPageContent.currentPotion)) return true;
                return false;
            }
            catch(Exception ex)
            {
                Ex.LogException(ex);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateEffects")]
    public class HidePotionCustomizationEffectTextPrefixPatch
    {
        static bool Prefix(Potion ___currentPotion)
        {
            return Ex.RunSafe(() => RecipeService.RemoveRecipePotionEffects(___currentPotion));
        }
    }

    [HarmonyPatch(typeof(RecipeBookLeftPageContent), "UpdateEffects")]
    public class HidePotionCustomizationEffectTextPostfixPatch
    {
        static void Postfix(Potion ___currentPotion)
        {
            Ex.RunSafe(() => RecipeService.ReAddRecipePotionEffects(___currentPotion));
        }
    }

    [HarmonyPatch(typeof(File), "CreateNewFromState")]
    public class RemoveAlchemyMachineRecipesFromSavedStatePatch
    {
        static bool Prefix(SavePool savePool, SavedState savedState)
        {
            return Ex.RunSafe(() => SaveLoadService.RemoveAlchemyMachineRecipesFromSavedState(savePool, savedState));
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
            return Ex.RunSafe(() => SaveLoadService.RetrieveStateJsonString(__instance));
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadSelectedState")]
    public class RetreiveSavedAlchemyMachineRecipesFromSavedStatePatch
    {
        static bool Prefix(Type type)
        {
            return Ex.RunSafe(() => SaveLoadService.RetreiveSavedAlchemyMachineRecipesFromSavedState(type));
        }
    }

    [HarmonyPatch(typeof(Potion), "GetSerializedInventorySlot")]
    public class SavePotionSerializedDataPatch
    {
        static void Postfix(SerializedInventorySlot __result, Potion __instance)
        {
            Ex.RunSafe(() => SaveLoadService.SavePotionSerializedData(__result, __instance));
        }
    }
    
    [HarmonyPatch(typeof(Potion))]
    public class LoadPotionSerializedDataPatch
    {
        static MethodInfo TargetMethod()
        {
            return typeof(Potion).GetMethod("GetFromSerializedObject", new[] { typeof(SerializedInventorySlot) });
        }

        static void Postfix(Potion __result, SerializedInventorySlot serializedObject)
        {
            Ex.RunSafe(() => SaveLoadService.LoadPotionSerializedData(__result, serializedObject));
        }
    }
}
