using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad()]
public class Startup
{
    static Startup()
    {
        EditorApplication.playModeStateChanged += PlayModeStateChanged;
    }

    private static void PlayModeStateChanged(PlayModeStateChange obj)
    {
        if (obj == PlayModeStateChange.EnteredPlayMode)
        {
            Init();
        }
    }

    private static void Init()
    {
        //find all classes that is IInitialize 
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IInitialize)));

        //find all object in scene that implement IInitialize
        var objects = Object.FindObjectsOfType<MonoBehaviour>().Where(o => o.GetType().GetInterfaces().Contains(typeof(IInitialize)));

        //find all prefabs that has a component that implement IInitialize from asset database
        var prefabsPath = AssetDatabase.FindAssets("t:GameObject").Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Where(path =>
        {
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return obj != null && obj.GetComponents<MonoBehaviour>().Any(c => c.GetType().GetInterfaces().Contains(typeof(IInitialize)));
        });

        //find all prefabs that has a component that implement IInitialize from asset database and load as IEnumerable<GameObject>
        var prefabs = prefabsPath.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path));

        //log
        Debug.Log("Found " + types.Count() + " types that implement IInitialize");
        Debug.Log("Found " + objects.Count() + " objects that implement IInitialize");
        Debug.Log("Found " + prefabs.Count() + " prefabs that has a component that implement IInitialize");

        //create new gameobject
        var go = new GameObject("Initialize");

        //if in scene missing any object that implement IInitialize, add it
        foreach (var type in types)
        {
            if (!objects.Any(o => o.GetType() == type))
            {
                //find prefab that has a component that implement IInitialize
                var prefab = prefabs.FirstOrDefault(o => o.GetComponents<MonoBehaviour>().Any(c => c.GetType() == type));

                //if is not null, instantiate it
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab);
                    instance.name = prefab.name;
                    instance.transform.SetParent(go.transform);
                    Debug.Log("Adding missing object of type " + type.Name);
                }
                else
                {
                    Debug.LogError("Missing prefab of type " + type.Name);
                    
                    //create new gameobject and add this component
                    var instance = new GameObject(type.Name);
                    instance.transform.SetParent(go.transform);
                    instance.AddComponent(type);

                    //save as prefab
                    CreatePrefab(instance);

                    Debug.Log("Adding missing object of type " + type.Name);
                }
            }
            else
            {
                //change this object parent
                var instance = objects.First(o => o.GetType() == type);
                instance.transform.SetParent(go.transform);

                Debug.Log("Found object of type " + type.Name);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //log
        Debug.Log("Initialize complete");
    }

    static string path = "Assets/Prefabs/Initialize/";

    static void CreatePrefab(GameObject instance)
    {
        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
        }
        
        string localPath = path + instance.name + ".prefab";

        // Make sure the file name is unique, in case an existing Prefab has the same name.
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        PrefabUtility.SaveAsPrefabAsset(instance, localPath, out bool prefabSuccess);
        if (prefabSuccess == true)
            Debug.Log("Prefab was saved successfully");
        else
            Debug.Log("Prefab failed to save" + prefabSuccess);
    }
}

/// <summary>
/// Interface for all classes that need to be initialized on startup.
/// </summary>
public interface IInitialize { }
