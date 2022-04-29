using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.AddressableAssets.Addressables;

public class AddressableManager : MonoBehaviour
{
    private WaitForEndOfFrame waitEnd = new WaitForEndOfFrame();

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
        StartCoroutine(ClearAllAsset());
    }

    private string InternalIdTransformFunc(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation location)
    {
        return string.Empty;
    }

    public void CheckAndUpdate()
    {
        StartCoroutine(CheckAndDownLoad());
    }

    private IEnumerator CheckAndDownLoad()
    {
        // 1. 检查更新清单
        AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;
        List<string> updateList = new List<string>();
        if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            updateList = checkHandle.Result;
            if (updateList == null || updateList.Count == 0)
            {
                Debug.Log("No need update catalog.");
                Addressables.Release(checkHandle);
                yield break;
            }
        }
        else
        {
            Debug.Log("Check catalog failed.");
            Addressables.Release(checkHandle);
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
            Debug.Log("No Need update assets");
            Addressables.Release(updateHandler);
            Addressables.Release(checkHandle);
            yield break;
        }

        // 4.获得下载资源总大小
        AsyncOperationHandle<long> downLoadSizeHandle = Addressables.GetDownloadSizeAsync(updateKeyList);
        yield return downLoadSizeHandle;
        long totalDownloadSize = downLoadSizeHandle.Result;
        if (totalDownloadSize == 0)
        {
            Debug.Log("Update size is zero");
            Addressables.Release(downLoadSizeHandle);
            Addressables.Release(updateHandler);
            Addressables.Release(checkHandle);
            yield break;
        }

        // 5.开始下载需要更新的资源
        AsyncOperationHandle downLoadHandle = Addressables.DownloadDependenciesAsync(updateKeyList, MergeMode.None);
        while (!downLoadHandle.IsDone)
        {
            float downloadPercent = downLoadHandle.PercentComplete;
            Debug.Log($"{downLoadHandle.Result} = percent {(int)(totalDownloadSize * downloadPercent)}/{totalDownloadSize}");
            yield return waitEnd;
        }

        Debug.Log("Download content finished");

        Addressables.Release(downLoadHandle);
        Addressables.Release(downLoadSizeHandle);
        Addressables.Release(updateHandler);
        Addressables.Release(checkHandle);
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
