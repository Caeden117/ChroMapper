using System;
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
    /// <typeparam name="T">Handled type to search for.</typeparam>
    /// <param name="parent">Parent for instantiated CMUI component.</param>
    /// <returns>Instantiated <see cref="CMUIComponent{T}"/>.</returns>
    /// <exception cref="MissingReferenceException">
    /// No CMUI Components are registered that handle type <typeparamref name="T"/>.
    /// </exception>
    public CMUIComponent<T> InstantiateCMUIComponentForHandledType<T>(Transform parent)
        => InstantiateCMUIComponentForHandledType(parent, typeof(T)) as CMUIComponent<T>;

    /// <summary>
    /// Instantiates and returns a new <see cref="CMUIComponentBase"/> parented to <paramref name="parent"/>,
    /// if there are any registered CMUI Components that handle type <paramref name="handledType"/>.
    /// </summary>
    /// <remarks>
    /// If multiple <see cref="CMUIComponentBase"/>s exist that handle type <paramref name="handledType"/>, the first result
    /// is chosen, instantiated, and returned. If you need a specific <see cref="CMUIComponentBase"/>,
    /// use <see cref="InstantiateCMUIComponentForComponentType(Transform, Type)"/>.
    /// </remarks>
    /// <param name="parent">Parent for instantiated CMUI component.</param>
    /// <param name="handledType">Handled type to search for.</param>
    /// <returns>Instantiated <see cref="CMUIComponentBase"/>.</returns>
    /// <exception cref="MissingReferenceException">
    /// No CMUI Components are registered that handle type <paramref name="handledType"/>.
    /// </exception>
    public CMUIComponentBase InstantiateCMUIComponentForHandledType(Transform parent, Type handledType)
    {
        // Maybe slow, might be worth it to cache the generic type argument so we're not regenerating this.
        var component = components.Find(x =>
            x.GetType().BaseType.IsGenericType && x.GetType().BaseType.GenericTypeArguments[0] == handledType);

        return component == null
            ? throw new MissingReferenceException($"No registered CMUI Component that handles type {handledType.Name}.")
            : Instantiate(component, parent);
    }

    /// <summary>
    /// Instantiates and returns a new <typeparamref name="T"/> parented to <paramref name="parent"/>,
    /// if type <typeparamref name="T"/> is registered.
    /// </summary>
    /// <typeparam name="T">CMUI Component type to search for and instantiate.</typeparam>
    /// <param name="parent">Parent for instantiated CMUI component.</param>
    /// <returns>Instantiated <typeparamref name="T"/>.</returns>
    /// <exception cref="MissingReferenceException">
    /// The given type <typeparamref name="T"/> is not registered in the Component Store.
    /// </exception>
    public T InstantiateCMUIComponentForComponentType<T>(Transform parent) where T : CMUIComponentBase
        => InstantiateCMUIComponentForComponentType(parent, typeof(T)) as T;

    /// <summary>
    /// Instantiates and returns a new <see cref="CMUIComponentBase"/> parented to <paramref name="parent"/>,
    /// if type <paramref name="componentType"/> is registered.
    /// </summary>
    /// <param name="parent">Parent for instantiated CMUI component.</param>
    /// <param name="componentType">CMUI component type to search and instantiate.</param>
    /// <returns>Instantiated <typeparamref name="CMUIComponentBase"/>.</returns>
    /// <exception cref="MissingReferenceException">
    /// The given type <paramref name="componentType"/> is not registered in the Component Store.
    /// </exception>
    public CMUIComponentBase InstantiateCMUIComponentForComponentType(Transform parent, Type componentType)
    {
        var component = components.Find(x => x.GetType() == componentType);

        return component == null
            ? throw new MissingReferenceException($"CMUI Component type {componentType.Name} is not registered.")
            : Instantiate(component, parent);
    }
}
