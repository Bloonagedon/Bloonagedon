using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 
/// The AssetBundler does several useful things when preparing your mod:
/// 
/// 1. Modifies all non-Editor MonoScript files to reference ASSEMBLY_NAME rather than Assembly-CSharp.
///     - At runtime in the game, this new assembly will be used to resolve the script references.
/// 2. Builds your project as ASSEMBLY_NAME.dll rather than Assembly-CSharp.dll.
///     - Having a name distinct from "Assembly-CSharp.dll" is required in order to load the mod in the game.
/// 3. Copies any managed assemblies from Assets/Plugins to the output folder for inclusion alongside your bundle.
/// 4. Builds the AssetBundle and copies the relevant .bundle file to the final output folder.
/// 5. Restores MonoScript references to Assembly-CSharp so they can be found by the Unity Editor again.
/// 
/// </summary>
public class AssetBundler
{
    /// <summary>
    /// Temporary location for building AssetBundles
    /// </summary>
    static string TEMP_BUILD_FOLDER = "Temp/AssetBundles";

    /// <summary>
    /// List of managed assemblies to ignore in the build (because they already exist in KTaNE itself)
    /// </summary>
    static List<string> EXCLUDED_ASSEMBLIES = new List<string> {};

    /// <summary>
    /// Name of the bundle file produced. This relies on the AssetBundle tag used, which is set to mod.bundle by default.
    /// </summary>
    public static string BUNDLE_FILENAME = "mod.bundle";

    /// <summary>
    /// Folders which should not be included in the asset bundling process.
    /// </summary>
    public static string[] EXCLUDED_FOLDERS = new string[] { "Assets/Editor" };


    #region Internal bundler Variables

    /// <summary>
    /// Output folder for the final asset bundle file
    /// </summary>
    private string outputFolder;

    /// <summary>
    /// A variable for holding the current BuildTarget, for Mac compatibility.
    /// </summary>
    BuildTarget target = BuildTarget.StandaloneWindows;
    #endregion

    [MenuItem("Inscryption Multiplayer/Build AssetBundle _F6", priority = 10)]
    public static void BuildAllAssetBundles_WithEditorUtility()
    {
        BuildModBundle();
    }

    protected static void BuildModBundle()
    {
        Debug.LogFormat("Creating \"{0}\" AssetBundle...", BUNDLE_FILENAME);

        AssetBundler bundler = new AssetBundler();

        bundler.outputFolder = "build";
        if (Application.platform == RuntimePlatform.OSXEditor) bundler.target = BuildTarget.StandaloneOSX;

        bool success = false;

        try
        {
            bundler.WarnIfAssetsAreNotTagged();
            bundler.CheckForAssets();

            //Delete the cotnents of OUTPUT_FOLDER
            bundler.CleanBuildFolder();

            //Lastly, create the asset bundle itself and copy it to the output folder
            bundler.CreateAssetBundle();

            success = true;
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat("Failed to build AssetBundle: {0}\n{1}", e.Message, e.StackTrace);
        }

        if (success)
        {
            Debug.LogFormat("{0} Build complete! Output: {1}", System.DateTime.Now.ToLocalTime(), bundler.outputFolder);
        }
    }

    /// <summary>
    /// Delete and recreate the OUTPUT_FOLDER to ensure a clean build.
    /// </summary>
    protected void CleanBuildFolder()
    {
        Debug.LogFormat("Cleaning {0}...", outputFolder);

        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }

        Directory.CreateDirectory(outputFolder);
    }

    /// <summary>
    /// Build the AssetBundle itself and copy it to the OUTPUT_FOLDER.
    /// </summary>
    protected void CreateAssetBundle()
    {
        Debug.Log("Building AssetBundle...");

        //Build all AssetBundles to the TEMP_BUILD_FOLDER
        if (!Directory.Exists(TEMP_BUILD_FOLDER))
        {
            Directory.CreateDirectory(TEMP_BUILD_FOLDER);
        }

#pragma warning disable 618
        //Build the asset bundle with the CollectDependencies flag. This is necessary or else ScriptableObjects like Missions will
        //not be accessible within the asset bundle. Unity has deprecated this flag claiming it is now always active, but due to a bug
        //we must still include it (and ignore the warning).
        BuildPipeline.BuildAssetBundles(
            TEMP_BUILD_FOLDER,
            BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.CollectDependencies,
            target);
#pragma warning restore 618

        //We are only interested in the BUNDLE_FILENAME bundle (and not the extra AssetBundle or the manifest files
        //that Unity makes), so just copy that to the final output folder
        string srcPath = Path.Combine(TEMP_BUILD_FOLDER, BUNDLE_FILENAME);
        string destPath = Path.Combine(outputFolder, BUNDLE_FILENAME);
        File.Copy(srcPath, destPath, true);
    }


    /// <summary>
    /// Print a warning for all non-Example assets that are not currently tagged to be in this AssetBundle.
    /// </summary>
    protected void WarnIfAssetsAreNotTagged()
    {
        string[] assetGUIDs = AssetDatabase.FindAssets("t:prefab,t:audioclip");

        foreach (var assetGUID in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);

            if (!path.StartsWith("Assets/Examples") && IsIncludedAssetPath(path))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (!importer.assetBundleName.Equals(BUNDLE_FILENAME))
                {
                    Debug.LogWarningFormat("Asset \"{0}\" is not tagged for {1} and will not be included in the AssetBundle!", path, BUNDLE_FILENAME);
                }
            }
        }

    }

    /// <summary>
    /// Verify that there is at least one thing to be included in the asset bundle.
    /// </summary>
    protected void CheckForAssets()
    {
        string[] assetsInBundle = AssetDatabase.FindAssets(string.Format("t:prefab,t:audioclip,t:scriptableobject,b:", BUNDLE_FILENAME));
        if (assetsInBundle.Length == 0)
        {
            throw new Exception(string.Format("No assets have been tagged for inclusion in the {0} AssetBundle.", BUNDLE_FILENAME));
        }
    }

    /// <returns>true if the given path does not start with any of the paths in EXCLUDED_FOLDERS</returns>
    protected bool IsIncludedAssetPath(string path)
    {
        foreach (string excludedPath in EXCLUDED_FOLDERS)
        {
            if (path.StartsWith(excludedPath))
            {
                return false;
            }
        }
        return true;
    }
}
