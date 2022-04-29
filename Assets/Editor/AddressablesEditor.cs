using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using System.IO;
using System.Text;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Settings;

public class AddressablesEditor
{
    [MenuItem("Tools/Asset Management/Build Content Update")]
    public static string BuildContentUpdate()
    {
        return CheckAndUpdateContent();
    }

    public static string CheckAndUpdateContent(string activeProfileId = "Default")
    {
        bool buildPlayerContent = true;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null && settings.BuildRemoteCatalog)
        {
            var profileId = settings.profileSettings.GetProfileId(activeProfileId);
            settings.activeProfileId = profileId;
            string binPath = ContentUpdateScript.GetContentStateDataPath(false);
            if (File.Exists(binPath))
            {
                buildPlayerContent = false;


                // 检测是否有更新的资源
                var entries = ContentUpdateScript.GatherModifiedEntries(settings, binPath);
                if (entries.Count > 0)
                {
                    StringBuilder entryList = new StringBuilder();
                    foreach (var item in entries)
                    {
                        entryList.AppendLine(item.address);
                    }

                    // 为更新资源创建新的分组，并设置默认分组使用的模板设置
                    var groupName = string.Format("UpdateGroup_{0}", System.DateTime.Now.ToString("yyyyMMdd"));
                    ContentUpdateScript.CreateContentUpdateGroup(settings, entries, groupName);
                    var group = settings.FindGroup(groupName);
                    BundledAssetGroupSchema bagSchema = group.GetSchema<BundledAssetGroupSchema>();
                    if (bagSchema == null)
                    {
                        bagSchema = group.AddSchema<BundledAssetGroupSchema>();
                    }

                    //var defultBAGSchema = settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>();
                    //bagSchema.BuildPath.SetVariableByName(settings, defultBAGSchema.BuildPath.GetName(settings));
                    //bagSchema.LoadPath.SetVariableByName(settings, defultBAGSchema.LoadPath.GetName(settings));
                    bagSchema.BuildPath.SetVariableByName(settings, "RemoteBuildPath");
                    bagSchema.LoadPath.SetVariableByName(settings, "RemoteLoadPath");

                    Debug.Log($"Update content:{entryList}");
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.Refresh();
                }

                // 使用资源更新的打包方式
                ContentUpdateScript.BuildContentUpdate(settings, binPath);
            }
        }

        // 使用默认的资源打包方式
        if (buildPlayerContent)
        {
            AddressableAssetSettings.BuildPlayerContent();
        }

        AssetDatabase.Refresh();

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
        UpdatePlayerContent();
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
        AddressableAssetSettings.BuildPlayerContent();
        EditorUtility.SetDirty(settings);
        AssetDatabase.Refresh();
    }
}
