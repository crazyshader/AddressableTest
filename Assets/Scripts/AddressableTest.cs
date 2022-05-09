using EasyProgressBar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class AddressableTest : MonoBehaviour
{
    public ProgressBar progressBar;
    public AddressableManager addressableManager;

    public AssetReference sceneRef;
    public AssetReference shpereMatRef;
    public AssetReference cubeMatRef;

    public MeshRenderer sphereRenderer;
    public MeshRenderer cubeRenderer;

    private AsyncOperationHandle<SceneInstance> sceneHandle;
    private AsyncOperationHandle<Material> sphereHandle;
    private AsyncOperationHandle<Material> cubeHandle;


    public void OpenScene()
    {
        sceneHandle = Addressables.LoadSceneAsync("Demo");
        sceneHandle.Completed += (op) =>
        {
            if (sceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Open demo scene failed");
                return;
            }

            Debug.Log("Open demo scene succeeded");
        };
    }

    public void CheckAndUpdate()
    {
        StartCoroutine(addressableManager.CheckAndDownLoad(OnCheckAndUpdate, OnUpdateProgress));
    }

    private void OnCheckAndUpdate(bool result)
    {
        if (result)
        {
            Debug.Log("Update Successes");
        }
        else
        {
            Debug.LogError("Update Failed");
        }
    }

    private void OnUpdateProgress(float progress)
    {
        progressBar.FillAmount = progress;
    }

    public void ChangeColor()
    {
        if (!shpereMatRef.RuntimeKeyIsValid() || !cubeMatRef.RuntimeKeyIsValid())
        {
            return;
        }

        sphereHandle = Addressables.LoadAssetAsync<Material>(shpereMatRef);
        sphereHandle.Completed += (op) =>
        {
            if (sphereHandle.Result != null)
            {
                sphereRenderer.material = sphereHandle.Result as Material;
            }
        };

        cubeHandle = Addressables.LoadAssetAsync<Material>(cubeMatRef);
        cubeHandle.Completed += (op) =>
        {
            if (cubeHandle.Result != null)
            {
                cubeRenderer.material = cubeHandle.Result as Material;
            }
        };
    }

    private void OnDestroy()
    {
        Addressables.Release(sceneRef);
        Addressables.Release(cubeMatRef);
        Addressables.Release(shpereMatRef);
    }
}
