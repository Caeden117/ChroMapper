using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public static bool TestMode = false;
    public static CMInput InputInstance;
    private static CMInputCallbackInstaller instance;

    private static readonly List<EventHandler> allEventHandlers = new();

    private static readonly BindingFlags
        bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

    private static readonly List<EventHandler> disabledEventHandlers = new();

    private static readonly Dictionary<string, object> interfaceNameToReference = new(); //Interface names to action map references

    private static readonly Dictionary<string, Type> interfaceNameToType = new(); //Interface names to action map types

    private static readonly List<Transform> persistentObjects = new();

    //Because I would like all actions to fully complete before being disabled,
    //we will use a queue that will then be cleared and processed on the next frame.
    private static readonly List<QueueInfo> queuedToDisable = new();
    private static readonly List<QueueInfo> queuedToEnable = new();

    private CMInput input; //Singular CMInput object that will be shared to every class that requires it.

    /*
     * This Start method looks a little messy.
     * Essentially, we create our dictionaries for Interface names to Action Map types and object references.
     * The first loop grabs our interface types, the second loop grabs action map types, and the third loop grabs references.
     */
    private void Start()
    {
#if UNITY_STANDALONE_OSX // Harmony patch doesn't work on Apple Silicon so use Unity's shortcut consuming
        InputSystem.settings.shortcutKeysConsumeInput = true;
#endif
        
        SendMessage("InputObjectCreated", input);
        foreach (var childClass in typeof(CMInput).GetNestedTypes())
        {
            if (childClass.IsInterface)
            {
                interfaceNameToType.Add(childClass.Name,
                    null); //Default them to null, we'll fill these up in the second loop.
            }
        }

        foreach (var interfaceName in new List<string>(interfaceNameToType.Keys))
        {
            var actionType = typeof(CMInput).GetNestedType(interfaceName.Substring(1));
            if (actionType != null) interfaceNameToType[interfaceName] = actionType;
        }

        foreach (var prop in typeof(CMInput).GetProperties())
        {
            name = prop.PropertyType.Name;
            if (interfaceNameToType.ContainsKey($"I{name}"))
                interfaceNameToReference.Add($"I{name}", prop.GetValue(input));
        }
    }

    //Here we will clear and process any queued list of types to disable or enable.
    private void Update()
    {
        if (queuedToDisable.Any())
        {
            foreach (var queueInfo in queuedToDisable)
            {
                foreach (var interfaceType in queueInfo.ToChange)
                {
                    foreach (var eventHandler in allEventHandlers.Where(x => x.InterfaceType == interfaceType))
                    {
                        if (eventHandler.Blockers.TryGetValue(queueInfo.Owner, out var count))
                        {
                            eventHandler.Blockers[queueInfo.Owner] = count + 1;
                        }
                        else
                        {
                            eventHandler.Blockers[queueInfo.Owner] = 1;
                        }
                        if (eventHandler.IsDisabled) continue;
                        eventHandler.DisableEventHandler();
                        disabledEventHandlers.Add(eventHandler);
                    }
                }
            }

            queuedToDisable.Clear();
        }

        if (queuedToEnable.Any())
        {
            foreach (var queueInfo in queuedToEnable)
            {
                foreach (var interfaceType in queueInfo.ToChange)
                {
                    foreach (var eventHandler in allEventHandlers.Where(x => x.InterfaceType == interfaceType && x.IsDisabled))
                    {
                        if (eventHandler.Blockers.TryGetValue(queueInfo.Owner, out var count))
                        {
                            count--;
                            eventHandler.Blockers[queueInfo.Owner] = count;
                            if (count <= 0)
                            {
                                eventHandler.Blockers.Remove(queueInfo.Owner);
                            }
                        }

                        if (eventHandler.Blockers.Count > 0) continue;
                        eventHandler.EnableEventHandler();
                        disabledEventHandlers.Remove(eventHandler);
                    }
                }
            }

            queuedToEnable.Clear();
        }
    }

    // Subscribe to events here.
    private void OnEnable()
    {
        instance = this;
        input = new CMInput();
        input.Enable();
        InputInstance = input;
        SceneManager.sceneLoaded += SceneLoaded;
        Application.wantsToQuit += WantsToQuit;
    }

    // Unsubscribe from events here.
    private void OnDisable()
    {
        instance = null;
        input.Disable();
        ClearAllEvents();
        SceneManager.sceneLoaded -= SceneLoaded;
        Application.wantsToQuit -= WantsToQuit;
    }

    public static void DisableActionMaps(Type you, IEnumerable<Type> interfaceTypesToDisable) =>
        //To preserve actions occuring on the same frame, we
        //add it to a queue thats cleared and processed on the next frame.
        queuedToDisable.Add(new QueueInfo(you, interfaceTypesToDisable));

    public static void ClearDisabledActionMaps(Type you, IEnumerable<Type> interfaceTypesToEnable) =>
        queuedToEnable.Add(new QueueInfo(you, interfaceTypesToEnable));

    public static bool IsActionMapDisabled(Type actionMap) =>
        disabledEventHandlers.Any(x => x.InterfaceType == actionMap);

    // Here we find our GameObjects that need our input map, install it, and then enable the input map.
    // Then we wait to re-enable our input map.
    // GameObjects that require our Input Map should be present when the scene loads, not created dynamically.
    private void SceneLoaded(Scene scene, LoadSceneMode sceneMode)
    {
        if (sceneMode == LoadSceneMode.Single) ClearAllEvents();
        foreach (var obj in scene.GetRootGameObjects()) FindAndInstallCallbacksRecursive(obj.transform);
        foreach (var transform in persistentObjects) FindAndInstallCallbacksRecursive(transform);
        StartCoroutine(WaitThenReenableInputs());
    }

    // Wait for the Scene Transition Manager to fade out, then enable our input.
    private IEnumerator WaitThenReenableInputs()
    {
        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        input.Enable();
        SceneTransitionManager.Instance.AddLoadRoutine(DisableInputs());
    }

    // Automatically disables our Input Map when we change scenes. This is to prevent MissingReferenceExceptions.
    private IEnumerator DisableInputs()
    {
        if (!TestMode)
            yield return new WaitForEndOfFrame();

        input.Disable();
    }

    // Set all callbacks to null, then disable our input when we try to exit the application. Prevents more MissingReferenceExceptions.
    // This might not catch ALL exceptions when exiting the editor.
    private bool WantsToQuit()
    {
        foreach (var prop in typeof(CMInput).GetProperties())
        {
            if (interfaceNameToType.ContainsKey(prop.PropertyType.Name))
            {
                interfaceNameToType[prop.PropertyType.Name].InvokeMember("SetCallbacks", bindingFlags,
                    Type.DefaultBinder, prop.GetValue(input), new object[] { null });
            }
        }

        input.Disable();
        return true;
    }

    public static void PersistentObject(Transform obj) => persistentObjects.Add(obj);

    // Here we find all MonoBehaviours with an Input Map interface and set its callbacks.
    // Looping through each monobehaviour on each object might be time consuming, but this is done only once when a scene loads.
    public static void FindAndInstallCallbacksRecursive(Transform obj)
    {
        foreach (var behaviour in obj.GetComponents<MonoBehaviour>())
        {
            if (behaviour is null || behaviour.GetType() is null) continue;
            foreach (var interfaceType in behaviour.GetType().GetInterfaces())
            {
                if (interfaceNameToType.ContainsKey(interfaceType.Name))
                {
                    Debug.Log($"Found {interfaceType.Name} in {behaviour.name}");
                    foreach (var info in interfaceNameToReference[interfaceType.Name].GetType().GetProperties())
                    {
                        if (info.PropertyType == typeof(InputAction))
                        {
                            var action = (InputAction)info.GetValue(interfaceNameToReference[interfaceType.Name]);
                            foreach (var e in info.PropertyType.GetEvents())
                            {
                                AddEventHandler(e, action, behaviour, interfaceType.GetMethod($"On{info.Name}"),
                                    interfaceType);
                            }
                        }
                    }
                }
            }
        }

        foreach (Transform child in obj) FindAndInstallCallbacksRecursive(child);
    }

    public static void FindAndRemoveCallbacksRecursive(Transform obj)
    {
        foreach (var behaviour in obj.GetComponents<MonoBehaviour>())
        {
            if (behaviour is null || behaviour.GetType() is null) continue;
            foreach (var interfaceType in behaviour.GetType().GetInterfaces())
            {
                var eventHandlers = allEventHandlers.FindAll(it => it.InterfaceType == interfaceType);

                foreach (var eventHandler in eventHandlers)
                {
                    eventHandler.DisableEventHandler(true);
                    allEventHandlers.Remove(eventHandler);
                }
            }
        }

        foreach (Transform child in obj) FindAndInstallCallbacksRecursive(child);
    }

    private void ClearAllEvents()
    {
        foreach (var handler in allEventHandlers) handler.DisableEventHandler(true);
        allEventHandlers.Clear();
        disabledEventHandlers.Clear();
    }

    /*
     * This function is pretty performance heavy, and is called for each CMInput interface that is found in a given scene.
     * 
     * I'm essentially trading off load times for convenience. With this solution, in the rest of the Editor I only have to
     * inherit from a given CMInput interface and fill out its methods without any hastle or extra work. Without this solution,
     * load times WOULD be quicker, however I'd have to make a new CMInput object and assign callbacks for every class that
     * handles input.
     * 
     * Thanks to Serj-Tm on StackOverflow for base code:
     * https://stackoverflow.com/questions/9753366/subscribing-an-action-to-any-event-type-via-reflection
    */
    private static void AddEventHandler(EventInfo eventInfo, object eventObject, object item, MethodInfo action,
        Type interfaceType)
    {
        var parameters = eventInfo.EventHandlerType
            .GetMethod("Invoke")
            .GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType))
            .ToArray();

        var handler = Expression.Lambda(
                eventInfo.EventHandlerType,
                Expression.Call(Expression.Constant(item), action, parameters[0]),
                parameters
            )
            .Compile();

        eventInfo.AddEventHandler(eventObject, handler);
        var eventHandler = new EventHandler(eventInfo, eventObject, handler, interfaceType);
        allEventHandlers.Add(eventHandler);
    }

    private class EventHandler
    {
        public readonly Dictionary<Type, int> Blockers = new Dictionary<Type, int>();

        public readonly EventInfo EventInfo;
        public readonly object EventObject;
        public readonly Delegate Handler;
        public readonly Type InterfaceType;
        public bool IsDisabled;

        public EventHandler(EventInfo eventInfo, object eventObject, Delegate handler, Type interfaceType)
        {
            EventInfo = eventInfo;
            EventObject = eventObject;
            Handler = handler;
            InterfaceType = interfaceType;

            EventInfo.AddEventHandler(EventObject, (Action<InputAction.CallbackContext>)ReleaseListenerFunc);
        }

        public void EnableEventHandler()
        {
            EventInfo.AddEventHandler(EventObject, Handler);
            IsDisabled = false;
        }

        public void DisableEventHandler(bool fully = false)
        {
            if (fully)
                EventInfo.RemoveEventHandler(EventObject, (Action<InputAction.CallbackContext>)ReleaseListenerFunc);

            EventInfo.RemoveEventHandler(EventObject, Handler);
            IsDisabled = true;
        }

        private void ReleaseListenerFunc(InputAction.CallbackContext context)
        {
            if (IsDisabled && context.canceled) Handler.DynamicInvoke(context);
        }

        public override int GetHashCode() => InterfaceType.GetHashCode();
    }

    private class QueueInfo
    {
        public readonly Type Owner;
        public readonly IEnumerable<Type> ToChange;

        public QueueInfo(Type owner, IEnumerable<Type> toChange)
        {
            Owner = owner;
            ToChange = toChange;
        }
    }
}
