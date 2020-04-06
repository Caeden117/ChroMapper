using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * This goal of this class is to automatically locate and install Action map interfaces from the new Unity Input System.
 * This cuts down a bit of slack from the other files and allows some cleaner code on their end.
 * It also limits the amount of CMInput objects being created at runtime, instead creating it once and using it wherever its needed.
 * 
 * This class heavily relies on the fact that Unity generated code creates an interface for every Action map struct, with the only
 * difference being the prefix "I" to differentiate a interface from the struct.
 */
public class CMInputCallbackInstaller : MonoBehaviour
{
    private Dictionary<string, Type> interfaceNameToType = new Dictionary<string, Type>(); //Interface names to action map types
    private Dictionary<string, object> interfaceNameToReference = new Dictionary<string, object>(); //Interface names to action map references

    private CMInput input; //Singular CMInput object that will be shared to every class that requires it.
    private BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

    // Subscribe to events here.
    private void OnEnable()
    {
        input = new CMInput();
        input.Enable();
        SceneManager.sceneLoaded += SceneLoaded;
        Application.wantsToQuit += WantsToQuit;
    }

    /*
     * This Start method looks a little messy.
     * Essentially, we create our dictionaries for Interface names to Action Map types and object references.
     * The first loop grabs our interface types, the second loop grabs action map types, and the third loop grabs references.
     */
    private void Start()
    {
        foreach (Type childClass in typeof(CMInput).GetNestedTypes())
        {
            if (childClass.IsInterface)
            {
                interfaceNameToType.Add(childClass.Name, null); //Default them to null, we'll fill these up in the second loop.
            }
        }
        foreach (string interfaceName in new List<string>(interfaceNameToType.Keys))
        {
            Type actionType = typeof(CMInput).GetNestedType(interfaceName.Substring(1));
            if (actionType != null)
            {
                interfaceNameToType[interfaceName] = actionType;
            }
        }
        foreach (PropertyInfo prop in typeof(CMInput).GetProperties())
        {
            if (interfaceNameToType.ContainsKey($"I{prop.PropertyType.Name}"))
            {
                interfaceNameToReference.Add($"I{prop.PropertyType.Name}", prop.GetValue(input));
            }
        }
    }

    // Here we find our GameObjects that need our input map, install it, and then enable the input map.
    // Then we wait to re-enable our input map.
    // GameObjects that require our Input Map should be present when the scene loads, not created dynamically.
    private void SceneLoaded(Scene scene, LoadSceneMode sceneMode)
    {
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            FindAndInstallCallbacksRecursive(obj.transform);
        }
        StartCoroutine(WaitThenReenableInputs());
    }

    // Wait for the Scene Transition Manager to fade out, then enable our input.
    private IEnumerator WaitThenReenableInputs()
    {
        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        input.Enable();
        SceneTransitionManager.Instance?.AddLoadRoutine(DisableInputs());
    }

    // Automatically disables our Input Map when we change scenes. This is to prevent MissingReferenceExceptions.
    private IEnumerator DisableInputs()
    {
        yield return new WaitForEndOfFrame();
        input.Disable();
    }

    // Set all callbacks to null, then disable our input when we try to exit the application. Prevents more MissingReferenceExceptions.
    // This might not catch ALL exceptions when exiting the editor.
    private bool WantsToQuit()
    {
        foreach (PropertyInfo prop in typeof(CMInput).GetProperties())
        {
            if (interfaceNameToType.ContainsKey(prop.PropertyType.Name))
            {
                interfaceNameToType[prop.PropertyType.Name].InvokeMember("SetCallbacks", bindingFlags, Type.DefaultBinder, prop.GetValue(input), new object[] { null });
            }
        }
        input.Disable();
        return true;
    }

    // Here we find all MonoBehaviours with an Input Map interface and set its callbacks.
    // Looping through each monobehaviour on each object might be time consuming, but this is done only once when a scene loads.
    private void FindAndInstallCallbacksRecursive(Transform obj)
    {
        foreach (MonoBehaviour behaviour in obj.GetComponents<MonoBehaviour>())
        {
            if (behaviour is null || behaviour.GetType() is null) continue;
            foreach (Type interfaceType in behaviour.GetType().GetInterfaces())
            {
                if (interfaceNameToType.ContainsKey(interfaceType.Name))
                {
                    interfaceNameToType[interfaceType.Name].InvokeMember("SetCallbacks", bindingFlags, Type.DefaultBinder, interfaceNameToReference[interfaceType.Name], new[] { behaviour });
                }
            }
        }
        foreach (Transform child in obj)
        {
            FindAndInstallCallbacksRecursive(child);
        }
    }

    //Unsubscrbe from events here.
    private void OnDisable()
    {
        input.Disable();
        SceneManager.sceneLoaded -= SceneLoaded;
        Application.wantsToQuit -= WantsToQuit;
    }
}
