using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using System.IO;
using System.Text;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Pipeline.Utilities;

public class AddressablesEditor
{
    [MenuItem("Tools/Asset Management/Clean Build")]
    public static void CleanBuild()
    {
        AddressableAssetSettings.CleanPlayerContent(null);
        BuildCache.PurgeCache(true);
    }

    [MenuItem("Tools/Asset Management/Build Content Update")]
    public static string BuildContentUpdate()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            return CheckAndUpdateContent();
        }

        return string.Empty;
    }

    public static string CheckAndUpdateContent(string activeProfileId = "Default")
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null && settings.BuildRemoteCatalog)
        {
            var profileId = settings.profileSettings.GetProfileId(activeProfileId);
            settings.activeProfileId = profileId;
            string binPath = ContentUpdateScript.GetContentStateDataPath(false);
            if (File.Exists(binPath))
            {
                // 检测是否有更新的资源
                var entries = ContentUpdateScript.GatherModifiedEntries(settings, binPath);
                Debug.Log($"GatherModifiedEntries Count:{entries.Count}");
                if (entries.Count > 0)
                {
                    StringBuilder entryList = new StringBuilder();
                    foreach (var item in entries)
                    {
                        entryList.AppendLine(item.address);
                    }

                    // 为更新资源创建新的分组，并设置默认分组使用的模板设置
                    var groupName = string.Format("UpdateGroup_{0}", System.DateTime.Now.ToString("yyyyMMddHHmmss"));
                    ContentUpdateScript.CreateContentUpdateGroup(settings, entries, groupName);
                    var group = settings.FindGroup(groupName);
                    BundledAssetGroupSchema bagSchema = group.GetSchema<BundledAssetGroupSchema>();
                    if (bagSchema == null)
                    {
                        bagSchema = group.AddSchema<BundledAssetGroupSchema>();
                    }

                    bagSchema.BuildPath.SetVariableByName(settings, "RemoteBuildPath");
                    bagSchema.LoadPath.SetVariableByName(settings, "RemoteLoadPath");

                    ContentUpdateGroupSchema cugScheam = group.GetSchema<ContentUpdateGroupSchema>();
                    if (cugScheam == null)
                    {
                        cugScheam = group.AddSchema<ContentUpdateGroupSchema>();
                    }

                    cugScheam.StaticContent = false;

                    Debug.Log($"Update content:{entryList}");
                }

                // 设置默认分组为远程路径，确保更新资源引用的引擎内置Shader可以打包到远程
                var defultBAGSchema = settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>();
                defultBAGSchema.BuildPath.SetVariableByName(settings, "RemoteBuildPath");
                defultBAGSchema.LoadPath.SetVariableByName(settings, "RemoteLoadPath");

                ContentUpdateGroupSchema defaultCUGSchema = settings.DefaultGroup.GetSchema<ContentUpdateGroupSchema>();
                if (defaultCUGSchema == null)
                {
                    defaultCUGSchema = settings.DefaultGroup.AddSchema<ContentUpdateGroupSchema>();
                }
                defaultCUGSchema.StaticContent = false;

                // 使用资源更新的打包方式
                EditorUtility.SetDirty(settings);
                AssetDatabase.Refresh();
                ContentUpdateScript.BuildContentUpdate(settings, binPath);
            }
            else
            {
                Debug.LogError("ContentStateDataPath is not exist.");
            }
        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        if (settings != null && settings.BuildRemoteCatalog)
        {
            var buildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
            Debug.Log($"RemoteBuildPath:{buildPath}");
            return buildPath;
        }

        return string.Empty;
    }

    [MenuItem("Tools/Asset Management/Build Player Content")]
    public static void BuildPlayerContent()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            UpdatePlayerContent();
        }
    }

    public static void UpdatePlayerContent(string activeProfileId = "Default")
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var profileId = settings.profileSettings.GetProfileId(activeProfileId);
            settings.activeProfileId = profileId;

            AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        }

        // 设置默认分组为本地路径，确保全量打包资源引用的引擎内置Shader可以打包到本地
        var defultBAGSchema = settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>();
        defultBAGSchema.BuildPath.SetVariableByName(settings, "LocalBuildPath");
        defultBAGSchema.LoadPath.SetVariableByName(settings, "LocalLoadPath");

        ContentUpdateGroupSchema defaultCUGSchema = settings.DefaultGroup.GetSchema<ContentUpdateGroupSchema>();
        if (defaultCUGSchema == null)
        {
            defaultCUGSchema = settings.DefaultGroup.AddSchema<ContentUpdateGroupSchema>();
        }
        defaultCUGSchema.StaticContent = true;

        AddressableAssetSettings.BuildPlayerContent();
        EditorUtility.SetDirty(settings);
        AssetDatabase.Refresh();
    }
}
