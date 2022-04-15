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
        var objects = GameObject.FindObjectsOfType<MonoBehaviour>().Where(o => o.GetType().GetInterfaces().Contains(typeof(IInitialize)));

        //find all prefabs that has a component that implement IInitialize
        var prefabsWithComponent = Resources.FindObjectsOfTypeAll<GameObject>().Where(o => o.GetComponents<MonoBehaviour>().Any(c => c.GetType().GetInterfaces().Contains(typeof(IInitialize))));

        //find all prefabs that has a component that implement IInitialize from asset database
        var prefabsWithComponentFromAssetDatabase = AssetDatabase.FindAssets("t:GameObject").Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Where(path =>
        {
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return obj != null && obj.GetComponents<MonoBehaviour>().Any(c => c.GetType().GetInterfaces().Contains(typeof(IInitialize)));
        });

        //find all prefabs that has a component that implement IInitialize from asset database and load as IEnumerable<GameObject>
        var prefabsWithComponentFromAssetDatabaseAsEnumerable = prefabsWithComponentFromAssetDatabase.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path));

        //Combine all IEnumerable<GameObject> without duplicate members
        var prefabsWithComponentAsEnumerable = prefabsWithComponent.Concat(prefabsWithComponentFromAssetDatabaseAsEnumerable).Distinct();

        //log
        Debug.Log("Found " + types.Count() + " types that implement IInitialize");
        Debug.Log("Found " + objects.Count() + " objects that implement IInitialize");
        Debug.Log("Found " + prefabsWithComponentAsEnumerable.Count() + " prefabs that has a component that implement IInitialize");

        //if in scene missing any object that implement IInitialize, add it
        foreach (var type in types)
        {
            if (!objects.Any(o => o.GetType() == type))
            {
                //find prefab that has a component that implement IInitialize
                var prefab = prefabsWithComponentAsEnumerable.FirstOrDefault(o => o.GetComponents<MonoBehaviour>().Any(c => c.GetType() == type));

                //if is not null, instantiate it
                if (prefab != null)
                {
                    var instance = GameObject.Instantiate(prefab);
                    instance.name = prefab.name;
                    Debug.Log("Adding missing object of type " + type.Name);
                }
                else
                {
                    Debug.LogError("Missing prefab of type " + type.Name);
                }
            }
            else
            {
                Debug.Log("Found object of type " + type.Name);
            }
        }
    }
}

/// <summary>
/// Interface for all classes that need to be initialized on startup
/// <para>WARNING: Create prefab of this class and store in resources folder</para>
/// </summary>
public interface IInitialize
{

}