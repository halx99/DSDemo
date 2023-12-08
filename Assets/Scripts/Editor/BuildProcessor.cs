using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Template.Multiplayer.NGO.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    ///<summary>
    ///Performs additional operations before/after the build is done
    ///</summary>
    public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static readonly string[] k_BuildOnlySymbols = new string[]
        {
            //"LIVE", //this is an example, add your own symbols instead
        };

        static readonly string[] k_EditorOnlySymbols = new string[]
        {
            //"DEV", //this is an example, add your own symbols instead
        };

        /// <summary>
        /// CallbackOrder of the preprocessing and postprocessing calls.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Called at the beginning of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            AssetDatabase.SaveAssets();
            ApplyChangesToMetagameApplication();

            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_BuildOnlySymbols.Except(allDefines));
            }
            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_EditorOnlySymbols.Contains(def));
            }
            Debug.Log($"Symbols used for build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.ToArray()));
        }
        /// <summary>
        /// Called at the end of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            RevertChangesToMetagameApplication();
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();

            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_BuildOnlySymbols.Contains(def));
            }
            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_EditorOnlySymbols.Except(allDefines));
            }
            Debug.Log($"Symbols restored after build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.ToArray()));
            AssetDatabase.SaveAssets();
#if !CLOUD_BUILD_WINDOWS && !CLOUD_BUILD_LINUX && !CLOUD_BUILD_MAX
            Debug.Log($"Manually Doing PostExport: {report.summary.outputPath}");
            bool isServerBuild = EditorUserBuildSettings.standaloneBuildSubtarget != StandaloneBuildSubtarget.Player; // Non-Player must be DedicatedServer
            CloudBuildHelpers.PostExport(report.summary.outputPath, isServerBuild);
#endif
        }

        void ApplyChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();
            //add your code to apply changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to alter MetagameApplication before building");
            }
            Debug.Log("Updated MetagameApp before build");
        }

        void RevertChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();
            //add your code to revert changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to restore MetagameApplication after building");
            }
            Debug.Log("Updated MetagameApp after build");
        }

        MetagameApplication FindMetagameAppInProject()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Prefabs/Metagame" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
                if (root.GetComponent<MetagameApplication>())
                {
                    return root.GetComponent<MetagameApplication>();
                }
            }
            return null;
        }

        [MenuItem("Multiplayer/Builds/All")]
        public static void MakeServerAndClientBuilds()
        {
            PerformServerStandaloneLinux64();
            PerformServerStandaloneWindows64();
            PerformStandaloneWindows64();
        }

        [MenuItem("Multiplayer/Builds/Server_StandaloneWindows")]
        public static void PerformServerStandaloneWindows64()
        {
            Debug.Log("Building server windows");
            DeleteOutputFolder("ServerWindows/");
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneWindows64);
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = "Builds/ServerWindows/GameServer.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
            });
        }

        [MenuItem("Multiplayer/Builds/Server_StandaloneLinux")]
        public static void PerformServerStandaloneLinux64()
        {
            Debug.Log("Building server linux");
            DeleteOutputFolder("ServerLinux/");
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneLinux64);
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = "Builds/ServerLinux/GameServer",
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
            });
        }

        [MenuItem("Multiplayer/Builds/Client_StandaloneWindows")]
        public static void PerformStandaloneWindows64()
        {
            Debug.Log("Building client");
            DeleteOutputFolder("Client/");

            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Standalone, BuildTarget.StandaloneWindows64);
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = "Builds/Client/Game.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Player,
            });
        }

        static void DeleteOutputFolder(string pathFromBuildsFolder)
        {
            string projectPath = Path.Combine(Application.dataPath, "..", "Builds", pathFromBuildsFolder);
            DirectoryInfo directoryInfo = new FileInfo(projectPath).Directory;
            if (directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }
        }

        static string[] GetScenePaths()
        {
            var scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }
            return scenes;
        }
    }
}
