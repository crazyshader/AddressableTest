using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.AddressableAssets.Addressables;

public class AddressableManager : MonoBehaviour
{
    public enum UpdateState
    {
        NoNeedUpdate,
        UpdateFailed,
        UpdateSuccessed,
    }

    private WaitForEndOfFrame waitForEnd = new WaitForEndOfFrame();

    private void Start()
    {
        DontDestroyOnLoad(this);
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
        //StartCoroutine(ClearAllAsset());
    }

    private string InternalIdTransformFunc(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation location)
    {
        return string.Empty;
    }

    public IEnumerator CheckAndDownLoad(string catalogFilePath, Action<UpdateState> finishedAction, Action<float> updateAction)
    {
        Addressables.ClearResourceLocators();
        string catalogPath = @"C:/Unity/AddressableTest/ServerData/StandaloneWindows64/catalog.json";
        AsyncOperationHandle<IResourceLocator> handle = Addressables.LoadContentCatalogAsync(catalogPath, true);
        yield return handle;

        // 1. 检查更新清单
        AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;
        List<string> updateList = new List<string>();
        if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            updateList = checkHandle.Result;
            if (updateList == null || updateList.Count == 0)
            {
                Addressables.Release(checkHandle);
                finishedAction?.Invoke(UpdateState.NoNeedUpdate);
                yield break;
            }
        }
        else
        {
            Addressables.Release(checkHandle);
            finishedAction?.Invoke(UpdateState.UpdateFailed);
            yield break;
        }

        // 2.开始更新清单
        AsyncOperationHandle<List<IResourceLocator>> updateHandler = Addressables.UpdateCatalogs(updateList, false);
        yield return updateHandler;

        //3.获取需要更新资源的key
        List<string> updateKeyList = new List<string>();
        foreach (IResourceLocator locator in updateHandler.Result)
        {
            if (locator is ResourceLocationMap map)
            {
                foreach (var item in map.Locations)
                {
                    if (item.Value.Count == 0) continue;
                    string key = item.Key.ToString();
                    if (int.TryParse(key, out int resKey)) continue;
                    if (!updateKeyList.Contains(key))
                        updateKeyList.Add(key);
                }
            }
        }
        if (updateKeyList == null || updateKeyList.Count == 0)
        {
            Addressables.Release(updateHandler);
            Addressables.Release(checkHandle);
            finishedAction?.Invoke(UpdateState.NoNeedUpdate);
            yield break;
        }

        // 4.获得下载资源总大小
        AsyncOperationHandle<long> downLoadSizeHandle = Addressables.GetDownloadSizeAsync(updateKeyList);
        yield return downLoadSizeHandle;
        long totalDownloadSize = downLoadSizeHandle.Result;
        if (totalDownloadSize == 0)
        {
            Addressables.Release(downLoadSizeHandle);
            Addressables.Release(updateHandler);
            Addressables.Release(checkHandle);
            finishedAction?.Invoke(UpdateState.NoNeedUpdate);
            yield break;
        }

        // 5.开始下载需要更新的资源
        AsyncOperationHandle downLoadHandle = Addressables.DownloadDependenciesAsync(updateKeyList, MergeMode.Union);
        while (!downLoadHandle.IsDone)
        {
            float downloadPercent = downLoadHandle.PercentComplete;
            updateAction?.Invoke(downloadPercent);
            Debug.Log($"{downLoadHandle.Result} = percent {(int)(totalDownloadSize * downloadPercent)}/{totalDownloadSize}");
            yield return waitForEnd;
        }

        Addressables.Release(downLoadHandle);
        Addressables.Release(downLoadSizeHandle);
        Addressables.Release(updateHandler);
        Addressables.Release(checkHandle);
        finishedAction?.Invoke(UpdateState.UpdateSuccessed);
    }

    public void ClearCache()
    {
        StartCoroutine(ClearAllAsset());
    }

    IEnumerator ClearAllAsset()
    {
        foreach (var locats in Addressables.ResourceLocators)
        {
            var async = Addressables.ClearDependencyCacheAsync(locats.Keys, false);
            yield return async;
            Addressables.Release(async);
        }
        Caching.ClearCache();
    }
}
