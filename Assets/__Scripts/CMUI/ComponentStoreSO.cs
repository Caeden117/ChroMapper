using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CMUI/Component Store")]
public class ComponentStoreSO : ScriptableObject
{
    public static ComponentStoreSO Instance;

    [SerializeField] private List<CMUIComponentBase> components;

    private void OnEnable()
    {
        if (Instance != null)
        {
            Debug.LogError("Component Store instance has already been assigned.");

            DestroyImmediate(this);
        }

        Instance = this;
    }

    /// <summary>
    /// Instantiates and returns a new <see cref="CMUIComponent{T}"/> parented to <paramref name="parent"/>,
    /// if there are any registered CMUI Components that handle type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// If multiple <see cref="CMUIComponent{T}"/>s exist that handle type <typeparamref name="T"/>, the first result is
    /// chosen, instantiated, and returned. If you need a specific <see cref="CMUIComponent{T}"/>,
    /// use <see cref="InstantiateCMUIComponentForComponentType{T}(Transform)"/>.
    /// </remarks>
    /// <typeparam name="T">Value type to search an </typeparam>
    /// <param name="parent">Parent for instantiated CMUI component.</param>
    /// <returns>Instantiated <see cref="CMUIComponent{T}"/>.</returns>
    /// <exception cref="MissingReferenceException">
    /// No CMUI Components are registered that handle type <typeparamref name="T"/>.
    /// </exception>
    public CMUIComponent<T> InstantiateCMUIComponentForValueType<T>(Transform parent)
    {
        var component = components.Find(x => x is CMUIComponent<T>);

        return component == null
            ? throw new MissingReferenceException($"No registered CMUI Component that handles type {typeof(T)}.")
            : Instantiate(component, parent) as CMUIComponent<T>;
    }

    /// <summary>
    /// Instantiates and returns a new <typeparamref name="T"/> paranted to <paramref name="parent"/>,
    /// if type <typeparamref name="T"/> is registered.
    /// </summary>
    /// <typeparam name="T">CMUI Component type to search for and instantiate.</typeparam>
    /// <param name="parent">Parent for instantiated CMUI component.</param>
    /// <returns>Instantiated <typeparamref name="T"/>.</returns>
    /// <exception cref="MissingReferenceException">
    /// The given type <typeparamref name="T"/> is not registered in the Component Store.
    /// </exception>
    public T InstantiateCMUIComponentForComponentType<T>(Transform parent) where T : CMUIComponentBase
    {
        var component = components.Find(x => x is T);

        return component == null
            ? throw new MissingReferenceException($"CMUI Component type {typeof(T)} is not registered.")
            : Instantiate(component, parent) as T;
    }
}
