using PotionCraft.ManagersSystem;
using PotionCraft.SaveFileSystem;
using PotionCraft.SaveLoadSystem;
using PotionCraftAlchemyMachineRecipes.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PotionCraftAlchemyMachineRecipes.Scripts.Services
{
    /// <summary>
    /// Responsible for saving and loading alchemy machine recipes.
    /// </summary>
    public class SaveLoadService
    {
        /// <summary>
        /// Prefix method for RemoveAlchemyMachineRecipesFromSavedStatePatch.
        /// This method is responsible for storing the alchemy machine recipes and wiping them out of the normal save data.
        /// This is important for if the user uninstalls this mod. If we did not store the recipes seperatly they might crash the game when launching without the mod.
        /// </summary>
        public static bool RemoveAlchemyMachineRecipesFromSavedState(SavePool savePool, SavedState savedState)
        {
            if (savePool != SavePool.Global && savePool != SavePool.Undefined && savedState is ProgressState progressState)
            {
                var indexes = new List<int>();
                var recipeList = Managers.Potion.recipeBook.savedRecipes;
                //Find all of the page indexes of created alchemy machine recipes
                for (var i = 0; i < recipeList.Count; i++)
                {
                    var curRecipe = recipeList[i];
                    if (curRecipe != null && RecipeService.IsLegendaryRecipe(curRecipe)) indexes.Add(i);
                }
                //Retrieve the serialized recipe and remove it from the main list so that it does not persist in the save file if this mod is uninstalled
                var recipiesToSave = new List<SavedRecipe>();
                for (var i = 0; i < progressState.savedRecipes.Count; i++)
                {
                    if (!indexes.Contains(i)) continue;
                    recipiesToSave.Add(new SavedRecipe(i, progressState.savedRecipes[i]));
                    progressState.savedRecipes[i] = null;
                }
                //Store recipes in static storage for later saving
                StaticStorage.RecipesToSave = recipiesToSave;
            }
            else
            {
                StaticStorage.RecipesToSave = null;
            }
            return true;
        }

        /// <summary>
        /// Postfix method for InjectSavedRecipesPatch.
        /// This method is responsible for actually inserting the alchemy machine recipies into the save file before it is converted to base64.
        /// Alchemy machine recipies are stored at the end of the file in a custom field.
        /// </summary>
        public static void InjectSavedRecipes(ref string result)
        {
            string modifiedResult = null;
            var savedStateJson = result;
            Ex.RunSafe(() =>
            {
                if (StaticStorage.RecipesToSave == null) return;
                //Serialize recipies to json
                var sbRecipes = new StringBuilder();
                sbRecipes.Append('[');
                var firstRecipe = true;
                StaticStorage.RecipesToSave.ForEach(recipe =>
                {
                    if (!firstRecipe)
                    {
                        sbRecipes.Append(',');
                    }
                    firstRecipe = false;
                    sbRecipes.Append(JsonUtility.ToJson(recipe, false));
                });
                sbRecipes.Append(']');
                var serializedRecipesToSave = $",\"{StaticStorage.AlchemyMachineRecipesJsonSaveName}\":{sbRecipes}";
                //Insert custom field at the end of the save file at the top level
                var insertIndex = savedStateJson.LastIndexOf('}');
                modifiedResult = savedStateJson.Insert(insertIndex, serializedRecipesToSave);
                StaticStorage.RecipesToSave = null;
            });
            if (!string.IsNullOrEmpty(modifiedResult))
            {
                result = modifiedResult;
            }
        }

        /// <summary>
        /// Prefix method for RetrieveStateJsonStringPatch.
        /// This method retrieves the raw json string and stores it in static storage for later use.
        /// The StateJsonString is inaccessible later on when we need it so this method is necessary to provide access to it.
        /// </summary>
        public static bool RetrieveStateJsonString(File instance)
        {
            StaticStorage.StateJsonString = instance.StateJsonString;
            return true;
        }

        /// <summary>
        /// Prefix method for RetreiveSavedAlchemyMachineRecipesFromSavedStatePatch.
        /// This method is responsible for loading saved recipes from the save file.
        /// </summary>
        public static bool RetreiveSavedAlchemyMachineRecipesFromSavedState(Type type)
        {
            if (type != typeof(ProgressState)) return true;
            var progressState = Managers.SaveLoad.SelectedProgressState;
            var stateJsonString = StaticStorage.StateJsonString;
            StaticStorage.StateJsonString = null;
            if (string.IsNullOrEmpty(stateJsonString))
            {
                Plugin.PluginLogger.LogInfo("Error: stateJsonString is empty. Cannot load alchemy machine recipes");
                return true;
            }
            //Check if there are any existing recipes in save file
            var keyIndex = stateJsonString.IndexOf(StaticStorage.AlchemyMachineRecipesJsonSaveName);
            if (keyIndex == -1)
            {
                Plugin.PluginLogger.LogInfo("No existing alchemy machine recipes found during load");
                return true;
            }
            //Determine the start of the list object containing the recipes
            var startSavedRecipesIndex = stateJsonString.IndexOf('[', keyIndex);
            if (startSavedRecipesIndex == -1)
            {
                Plugin.PluginLogger.LogInfo("Error: alchemy machine recipes are not formed correctly. No alchemy machine recipes will be loaded (bad start index).");
                return true;
            }

            //Find the closing bracket of the list
            var endSavedRecipesIndex = GetEndJsonIndex(stateJsonString, startSavedRecipesIndex, true);
            if (endSavedRecipesIndex >= stateJsonString.Length)
            {
                Plugin.PluginLogger.LogInfo("Error: alchemy machine recipes are not formed correctly. No alchemy machine recipes will be loaded (bad end index).");
                return true;
            }

            //Read through the list parsing each recipe manually since list deserialization is not supported in unity
            var savedRecipesJson = stateJsonString.Substring(startSavedRecipesIndex, endSavedRecipesIndex - startSavedRecipesIndex);
            if (savedRecipesJson.Length <=2)
            {
                Plugin.PluginLogger.LogInfo("No existing alchemy machine recipes found during load");
                return true;
            }
            var savedRecipes = new List<SavedRecipe>();
            var objectEndIndex = 0;
            //Continue parsing list until we find a non-comma character after the parsed object
            while (objectEndIndex == 0 || savedRecipesJson[objectEndIndex] == ',')
            {
                var objectStartIndex = objectEndIndex + 1;
                objectEndIndex = GetEndJsonIndex(savedRecipesJson, objectStartIndex, false);
                var curRecipeJson = savedRecipesJson.Substring(objectStartIndex, objectEndIndex - objectStartIndex);
                var savedRecipe = JsonUtility.FromJson<SavedRecipe>(curRecipeJson);
                if (savedRecipe == null)
                {
                    Plugin.PluginLogger.LogInfo("Error: alchemy machine recipes are not in sync with save file. Some alchemy machine recipes may have been loaded (failed to deserialize recipe).");
                    break;
                }

                savedRecipes.Add(savedRecipe);
            }

            //Load each recipe into the save state so it can be fully deserialized into an actual saved recipe
            foreach (var savedRecipe in savedRecipes)
            {
                if (progressState.savedRecipes.Count <= savedRecipe.Index)
                {
                    Plugin.PluginLogger.LogInfo($"Error: alchemy machine recipes are not in sync with save file. Some alchemy machine recipes may have been loaded (index out of bounds: {savedRecipe.Index}).");
                    return true;
                }
                if (!string.IsNullOrEmpty(progressState.savedRecipes[savedRecipe.Index].data))
                {
                    Plugin.PluginLogger.LogInfo($"Error: alchemy machine recipes are not in sync with save file. Some alchemy machine recipes may have been loaded (indexed recipe not null  {savedRecipe.Index}).");
                    return true;
                }
                Plugin.PluginLogger.LogInfo($"Loaded recipe at index {savedRecipe.Index}");
                //The recipe is stored in the same page index it was taken from to preserve bookmark locations
                progressState.savedRecipes[savedRecipe.Index] = savedRecipe.Recipe;
            }
            return true;
        }

        /// <summary>
        /// Manually parses the json to find the closing bracket for this json object.
        /// </summary>
        /// <param name="input">the json string to parse.</param>
        /// <param name="startIndex">the openning bracket of this object/list.</param>
        /// <param name="useBrackets">if true this code will look for closing brackets and if false this code will look for curly braces.</param>
        /// <returns></returns>
        private static int GetEndJsonIndex(string input, int startIndex, bool useBrackets)
        {
            var endIndex = startIndex + 1;
            var unclosedCount = 1;
            var openChar = useBrackets ? '[' : '{';
            var closeChar = useBrackets ? ']' : '}';
            while (unclosedCount > 0 && endIndex < input.Length - 1)
            {
                var nextOpenIndex = input.IndexOf(openChar, endIndex);
                var nextCloseIndex = input.IndexOf(closeChar, endIndex);
                if (nextOpenIndex == -1 && nextCloseIndex == -1)
                {
                    break;
                }
                if (nextOpenIndex == -1) nextOpenIndex = int.MaxValue;
                if (nextCloseIndex == -1) nextCloseIndex = int.MaxValue;
                if (nextOpenIndex < nextCloseIndex)
                {
                    endIndex = nextOpenIndex + 1;
                    unclosedCount++;
                }
                else if (nextCloseIndex < nextOpenIndex)
                {
                    endIndex = nextCloseIndex + 1;
                    unclosedCount--;
                }
            }
            return endIndex;
        }
    }
}
