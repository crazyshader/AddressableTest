using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class AddressableTest : MonoBehaviour
{

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
        //if (!sceneRef.RuntimeKeyIsValid())
        //{
        //    return;
        //}

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
        if (!shpereMatRef.RuntimeKeyIsValid() || !cubeMatRef.RuntimeKeyIsValid())
        {
            return;
        }

        //Addressables.Release(sceneRef);
        //Addressables.Release(cubeMatRef);
        //Addressables.Release(shpereMatRef);
    }
}
