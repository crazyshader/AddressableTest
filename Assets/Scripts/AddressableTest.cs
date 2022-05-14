using EasyProgressBar;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using XLua;

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

    private LuaEnv luaenv;
    private AsyncOperationHandle<IList<TextAsset>> luaHandle;
    private Dictionary<string, byte[]> luaScripts = new Dictionary<string, byte[]>();

    private void Start()
    {
        luaenv = new LuaEnv();
        luaenv.AddLoader(LuaScriptLoader);
    }

    private void Update()
    {
        if (luaenv != null)
        {
            luaenv.Tick();
        }
    }

    private void OnDestroy()
    {
        if (luaenv != null)
        {
            luaenv.Dispose();
        }

        if (luaHandle.IsValid())
        {
            Addressables.Release(luaHandle);
        }
        if (sceneRef.IsValid())
        {
            Addressables.Release(sceneRef);
        }
        if (cubeMatRef.IsValid())
        {
            Addressables.Release(cubeMatRef);
        }
        if (shpereMatRef.IsValid())
        {
            Addressables.Release(shpereMatRef);
        }

    }

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
        string catalogPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6),
            @"ServerData/StandaloneWindows64/catalog.json");
        StartCoroutine(addressableManager.CheckAndDownLoad(catalogPath, OnCheckAndUpdate, OnUpdateProgress));
    }

    private void OnCheckAndUpdate(AddressableManager.UpdateState state)
    {
        if (state == AddressableManager.UpdateState.UpdateFailed)
        {
            Debug.LogError("Update Failed");
        }
        else
        {
            if (state == AddressableManager.UpdateState.UpdateSuccessed)
            {
                Debug.Log("Update Successes");
            }
            else
            {
                Debug.Log("No Need Update");
            }

            StartCoroutine(LoadAllLuaCode());
        }
    }

    public IEnumerator LoadAllLuaCode()
    {
        var luaObjList = new List<TextAsset>();
        var keys = new List<object>() { "LuaScripts" };
        luaHandle = Addressables.LoadAssetsAsync<TextAsset>((IEnumerable)keys, luaObjList.Add, Addressables.MergeMode.Union);
        luaHandle.WaitForCompletion();
        if (luaHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Lua scripts load failed.");
            Addressables.Release(luaHandle);
            yield break;
        }

        luaScripts.Clear();
        foreach (var luaObj in luaObjList)
        {
            if (!luaScripts.ContainsKey(luaObj.name))
            {
                luaScripts.Add(luaObj.name, luaObj.bytes);
            }
        }

        TestLua();
    }

    private void TestLua()
    {
        if (luaenv != null)
        {
            luaenv.DoString("require 'Test'");
        }
    }

    public byte[] GetLuaCodeBytes(string fileName)
    {
        if (luaScripts.ContainsKey(fileName))
        {
            return luaScripts[fileName];
        }

        return null;
    }

    private byte[] LuaScriptLoader(ref string filePath)
    {
        string fileName = Path.GetFileName(filePath + ".lua");
        byte[] buff = GetLuaCodeBytes(fileName);
        return buff;
    }

    private void OnUpdateProgress(float progress)
    {
        progressBar.FillAmount = progress;
    }

    public void ChangeColor()
    {
        TestLua();

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
}
