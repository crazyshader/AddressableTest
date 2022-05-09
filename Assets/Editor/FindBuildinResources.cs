using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class FindbuiltinResources : EditorWindow
{
    [MenuItem("Tools/Check builtin asset")]
    public static void FindResource()
    {
        var window = GetWindow<FindbuiltinResources>();
        window.minSize = new Vector2(820, 650);
        window.Show();
        GetbuiltinResource();
    }

    private Vector3 scrollPos = Vector3.zero;
    private static Dictionary<UnityEngine.Object, Node> res = new Dictionary<UnityEngine.Object, Node>();
    private const string shader = "shader";
    private const string texture = "texture";
    private const string material = "material";
    private const string sprite = "sprite";
    private const string prefab = "prefab";
    private const string renderer = "renderer";
    private const string image = "image";
    private const string builtin = "builtin";

    /// <summary>
    /// 加载场景 builtin资源
    /// </summary>
    private static void GetSceneBuiltinResource()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorUtility.DisplayDialog("提示", "请先保存当前修改的场景", "确认");
            return;
        }

        res.Clear();
        var scene = EditorSceneManager.GetActiveScene();
        GameObject[] gameObjs = scene.GetRootGameObjects();
        foreach (var gameObj in gameObjs)
        {
            if (gameObj)
            {
                // 找到prefab里的 builtin shader & material & texture
                Renderer[] renders = gameObj.GetComponentsInChildren<Renderer>(true);
                foreach (var render in renders)
                {
                    foreach (var mat in render.sharedMaterials)
                    {
                        if (!mat) continue;
                        //判断材质是不是用的builtin的
                        if (AssetDatabase.GetAssetPath(mat).Contains(builtin))
                        {
                            Node node;
                            if (res.Keys.Contains(gameObj)) node = res[gameObj];
                            else
                            {
                                node = new Node(gameObj, prefab);
                                res.Add(gameObj, node);
                            }
                            node.Add(render, renderer).Add(mat, material);
                        }
                        //判断shader是不是builtin的
                        if (AssetDatabase.GetAssetPath(mat.shader).Contains(builtin))
                        {
                            Node node;
                            if (res.Keys.Contains(gameObj)) node = res[gameObj];
                            else
                            {
                                node = new Node(gameObj, prefab);
                                res.Add(gameObj, node);
                            }
                            node.Add(render, renderer).Add(mat, material).Add(mat.shader, shader);
                        }
                        //判断shader用的贴图是不是用的builtin的
                        for (int i = 0; i < ShaderUtil.GetPropertyCount(mat.shader); i++)
                        {
                            if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                string propertyname = ShaderUtil.GetPropertyName(mat.shader, i);
                                Texture t = mat.GetTexture(propertyname);
                                if (t && AssetDatabase.GetAssetPath(t).Contains(builtin))
                                {
                                    Node node;
                                    if (res.Keys.Contains(gameObj)) node = res[gameObj];
                                    else
                                    {
                                        node = new Node(gameObj, prefab);
                                        res.Add(gameObj, node);
                                    }
                                    node.Add(render, renderer).Add(mat, material).Add(t, texture);
                                }
                            }
                        }
                    }
                }
                // 找到prefab里的 builtin Sprite
                Image[] images = gameObj.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (AssetDatabase.GetAssetPath(img.sprite).Contains(builtin))
                    {
                        Node node;
                        if (res.Keys.Contains(gameObj)) node = res[gameObj];
                        else
                        {
                            node = new Node(gameObj, prefab);
                            res.Add(gameObj, node);
                        }
                        node.Add(img, "image").Add(img.sprite, sprite);
                    }
                }

                // 找到prefab 里的Texture
                RawImage[] rawimgs = gameObj.GetComponentsInChildren<RawImage>(true);
                foreach (var rawimg in rawimgs)
                {
                    if (rawimg.texture && AssetDatabase.GetAssetPath(rawimg.texture).Contains(builtin))
                    {
                        Node node;
                        if (res.Keys.Contains(gameObj)) node = res[gameObj];
                        else
                        {
                            node = new Node(gameObj, prefab);
                            res.Add(gameObj, node);
                        }
                        node.Add(rawimg, "rawimage").Add(rawimg.texture, texture);
                    }
                }
            }
        }

        ReplaceDefaultMaterial();
    }

    /// <summary>
    /// 加载 builtin资源
    /// </summary>
    private static void GetbuiltinResource()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorUtility.DisplayDialog("提示", "请先保存当前修改的场景", "确认");
            return;
        }

        res.Clear();
        string path = "Assets/";
        string lastScenePath = EditorSceneManager.GetActiveScene().path;
        var allfiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(
            s => s.EndsWith("mat")
            || s.EndsWith("prefab")
            ).ToArray();
        foreach (var item in allfiles)
        {
            if (item.EndsWith("prefab"))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(item);
                if (go)
                {
                    // 找到prefab里的 builtin shader & material & texture
                    Renderer[] renders = go.GetComponentsInChildren<Renderer>(true);
                    foreach (var render in renders)
                    {
                        foreach (var mat in render.sharedMaterials)
                        {
                            if (!mat) continue;
                            //判断材质是不是用的builtin的
                            if (AssetDatabase.GetAssetPath(mat).Contains(builtin))
                            {
                                Node node;
                                if (res.Keys.Contains(go)) node = res[go];
                                else
                                {
                                    node = new Node(go, prefab);
                                    res.Add(go, node);
                                }
                                node.Add(render, renderer).Add(mat, material);
                            }
                            //判断shader是不是builtin的
                            if (AssetDatabase.GetAssetPath(mat.shader).Contains(builtin))
                            {
                                Node node;
                                if (res.Keys.Contains(go)) node = res[go];
                                else
                                {
                                    node = new Node(go, prefab);
                                    res.Add(go, node);
                                }
                                node.Add(render, renderer).Add(mat, material).Add(mat.shader, shader);
                            }
                            //判断shader用的贴图是不是用的builtin的
                            for (int i = 0; i < ShaderUtil.GetPropertyCount(mat.shader); i++)
                            {
                                if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                                {
                                    string propertyname = ShaderUtil.GetPropertyName(mat.shader, i);
                                    Texture t = mat.GetTexture(propertyname);
                                    if (t && AssetDatabase.GetAssetPath(t).Contains(builtin))
                                    {
                                        Node node;
                                        if (res.Keys.Contains(go)) node = res[go];
                                        else
                                        {
                                            node = new Node(go, prefab);
                                            res.Add(go, node);
                                        }
                                        node.Add(render, renderer).Add(mat, material).Add(t, texture);
                                    }
                                }
                            }
                        }
                    }
                    // 找到prefab里的 builtin Sprite
                    Image[] images = go.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (AssetDatabase.GetAssetPath(img.sprite).Contains(builtin))
                        {
                            Node node;
                            if (res.Keys.Contains(go)) node = res[go];
                            else
                            {
                                node = new Node(go, prefab);
                                res.Add(go, node);
                            }
                            node.Add(img, "image").Add(img.sprite, sprite);
                        }
                    }

                    // 找到prefab 里的Texture
                    RawImage[] rawimgs = go.GetComponentsInChildren<RawImage>(true);
                    foreach (var rawimg in rawimgs)
                    {
                        if (rawimg.texture && AssetDatabase.GetAssetPath(rawimg.texture).Contains(builtin))
                        {
                            Node node;
                            if (res.Keys.Contains(go)) node = res[go];
                            else
                            {
                                node = new Node(go, prefab);
                                res.Add(go, node);
                            }
                            node.Add(rawimg, "rawimage").Add(rawimg.texture, texture);
                        }
                    }
                }
            }
            else if (item.EndsWith("mat"))
            {
                // 找到material里的 shader
                Material mt = AssetDatabase.LoadAssetAtPath<Material>(item);
                if (!mt) continue;
                if (AssetDatabase.GetAssetPath(mt.shader).Contains(builtin))
                {
                    Node node;
                    if (res.Keys.Contains(mt)) node = res[mt];
                    else
                    {
                        node = new Node(mt, material);
                        res.Add(mt, node);
                    }
                    node.Add(mt.shader, shader);
                }
                // 找到material里的 texutre
                for (int i = 0; i < ShaderUtil.GetPropertyCount(mt.shader); i++)
                {
                    if (ShaderUtil.GetPropertyType(mt.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyname = ShaderUtil.GetPropertyName(mt.shader, i);
                        Texture t = mt.GetTexture(propertyname);
                        if (t && AssetDatabase.GetAssetPath(t).Contains(builtin))
                        {
                            Node node;
                            if (res.Keys.Contains(mt)) node = res[mt];
                            else
                            {
                                node = new Node(mt, material);
                                res.Add(mt, node);
                            }
                            node.Add(t, sprite);
                        }
                    }
                }
            }
        }

        if (lastScenePath != EditorSceneManager.GetActiveScene().path)
        {
            EditorSceneManager.OpenScene(lastScenePath);
        }

        Debug.Log("Process Finished.");
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// 将standard 替换成Mobile Diffuse
    /// </summary>
    private static void ReplaceStandardToDiffuse()
    {
        Shader sd = Shader.Find("Standard");
        Shader diffuse_sd = Shader.Find("Mobile/Diffuse");
        int count = 0;
        foreach (var item in res.Values)
        {
            TransforNode(item, (s) =>
            {
                if (s.des == material)
                {
                    Material mt = s.content as Material;
                    if (mt && mt.shader == sd)
                    {
                        mt.shader = diffuse_sd;
                        count++;
                        Debug.Log($"Replace Standard Shader:{AssetDatabase.GetAssetPath(s.content)}");
                    }
                }
            });
        }

        EditorUtility.DisplayDialog("Result", "Replace " + count + " Standard shader", "OK");
        if (count != 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GetbuiltinResource();
        }
    }
    /// <summary>
    /// 将builtin shader 替换成本地shader
    /// </summary>
    private void ReplacebuiltinToLocal()
    {
        int count = 0;
        foreach (var item in res.Values)
        {
            TransforNode(item, (s) =>
            {
                if (s.des == material)
                {
                    Material mt = s.content as Material;
                    Shader shader = Shader.Find(mt.shader.name); 
                    if (mt && shader != mt.shader)
                    {
                        mt.shader = shader;
                        count++;
                        Debug.Log($"Replace Builtin Shader:{AssetDatabase.GetAssetPath(s.content)}");
                    }
                }
            });
        }
        EditorUtility.DisplayDialog("Result", "Replace " + count + " builtin shader", "OK");
        if (count != 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GetbuiltinResource();
        }
    }
    /// <summary>
    /// 替换默认材质
    /// </summary>
    private static void ReplaceDefaultMaterial()
    {
        string defaultDiff = "Default-Diffuse";
        string defaultMat = "Default-Material";
        string[] x = Directory.GetFiles("Assets/", defaultMat + ".mat", SearchOption.AllDirectories);
        if (x.Length == 0)
        {
            EditorUtility.DisplayDialog("Tip", "No" + defaultMat + "!!!", "OK");
            return;
        }
        Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(x[0]);
        int count = 0;
        foreach (var item in res.Values)
        {
            TransforNode(item, (s) =>
            {
                if (s.des == renderer)
                {
                    Renderer render = s.content as Renderer;
                    if (render)
                    {
                        Material[] mats = render.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (mats[i].name == defaultMat || mats[i].name == defaultDiff)
                            {
                                Material mt = defaultMaterial as Material;
                                if (mt && mats[i] != mt)
                                {
                                    mats[i] = mt;
                                    count++;
                                    Debug.Log($"Replace Default Material:{AssetDatabase.GetAssetPath(mats[i])}");
                                }
                            }
                        }
                        render.sharedMaterials = mats;
                    }
                }
            });
        }
        EditorUtility.DisplayDialog("Tip", $"Replace {count} {defaultMat}", "OK");
        if (count != 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GetbuiltinResource();
        }
    }
    /// <summary>
    /// 移除使用默认材质的ParticleSystem组件
    /// </summary>
    private void RemoveParticleSystemWithDefaultParticle()
    {
        int count = 0;
        foreach (var item in res.Values)
        {
            TransforNode(item, (s) =>
            {
                if (s.des == renderer)
                {
                    Renderer render = s.content as Renderer;
                    if (render)
                    {
                        Material[] mats = render.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (mats[i].name == "Default-Particle")
                            {
                                ParticleSystem ps = render.GetComponent<ParticleSystem>();
                                if (ps)
                                {
                                    render.materials = new Material[] { };
                                    DestroyImmediate(ps, true);
                                    EditorUtility.SetDirty(render.gameObject);
                                    count++;
                                    Debug.Log($"Remove Default Particle System:{AssetDatabase.GetAssetPath(ps)}");
                                    break;
                                }
                            }
                        }
                    }
                }
            });
        }
        EditorUtility.DisplayDialog("Tip", "Remove" + count + " ParticleSystem Component", "OK");
        if (count != 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GetbuiltinResource();
        }
    }

    private void ClearGatherData()
    {
        res.Clear();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清理收集数据数据", GUILayout.Width(400), GUILayout.Height(50))) ClearGatherData();
        if (GUILayout.Button("收集引擎内置资源", GUILayout.Width(400), GUILayout.Height(50))) GetbuiltinResource();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("将所有Standard shader 替换成MobileDiffuse  ", GUILayout.Width(400), GUILayout.Height(50))) ReplaceStandardToDiffuse();
        if (GUILayout.Button("将所有builtin shader 替换成本地shader  ", GUILayout.Width(400), GUILayout.Height(50))) ReplacebuiltinToLocal();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("替换所有Default Material", GUILayout.Width(400), GUILayout.Height(50))) ReplaceDefaultMaterial();
        if (GUILayout.Button("移除所有带Default Particle的ParticleSystem组件", GUILayout.Width(400), GUILayout.Height(50))) RemoveParticleSystemWithDefaultParticle();
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("收集场景引用的引擎内置资源", GUILayout.Width(800), GUILayout.Height(50))) GetSceneBuiltinResource();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Width(850));
        EditorGUILayout.BeginVertical();
        foreach (var item in res.Keys)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(item, item.GetType(), true, GUILayout.Width(200));
            TransforNode(res[item]);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

    }


    /// <summary>
    /// 遍历显示
    /// </summary>
    /// <param name="n"></param>
    private static void TransforNode(Node n)
    {
        EditorGUILayout.BeginVertical();
        foreach (var item in n.next.Values)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(item.content, item.content.GetType(), true, GUILayout.Width(200));
            TransforNode(item);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
    /// <summary>
    /// 遍历 操作
    /// </summary>
    /// <param name="n"></param>
    /// <param name="a"></param>
    private static void TransforNode(Node n, Action<Node> a)
    {
        a(n);
        foreach (var item in n.next.Values)
        {
            a(item);
            TransforNode(item, a);
        }
    }


}
public class Node
{
    public UnityEngine.Object content;
    public string des;
    public Dictionary<UnityEngine.Object, Node> next;
    public Node Add(UnityEngine.Object obj, string type)
    {
        if (!next.Keys.Contains(obj))
        {
            Node no = new Node(obj, type);
            next.Add(obj, no);
            return no;
        }
        return next[obj];
    }
    public Node(UnityEngine.Object content, string des)
    {
        this.content = content;
        this.des = des;
        next = new Dictionary<UnityEngine.Object, Node>();
    }
    public void TransforNode(Action<Node> action)
    {
        action(this);
        foreach (var item in next.Values)
        {
            action(item);
            TransforNode(action);
        }
    }
}

