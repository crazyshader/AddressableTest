using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableTest : MonoBehaviour
{
    public AssetReference shpereMatRef;
    public AssetReference cubeMatRef;

    public MeshRenderer sphereRenderer;
    public MeshRenderer cubeRenderer;

    private AsyncOperationHandle<Material> sphereHandle;
    private AsyncOperationHandle<Material> cubeHandle;


    private void Start()
    {
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

        //Addressables.Release(materialRef);
    }
}
