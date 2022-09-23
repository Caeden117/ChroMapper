using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class DialogBox : MonoBehaviour, CMInput.IDialogBoxActions
{
    private const float roundness = 4;

    private static readonly IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes()
        .Where(t => t.IsInterface && t != typeof(CMInput.IUtilsActions) && t != typeof(CMInput.IDialogBoxActions));

    [SerializeField] private GameObject raycastBlocker;
    [SerializeField] private GameObject titleGameObject;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ImageWithIndependentRoundedCorners bodyRoundedCorners;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private GameObject footerGameObject;
    [SerializeField] private Transform footerTransform;

    private LinkedList<INavigable> navigableComponents = new LinkedList<INavigable>();
    private LinkedList<INavigable> navigableFooterButtons = new LinkedList<INavigable>();
    private Selectable currentSelectable;

    private bool destroyOnClose = true;
    private bool callbacksInstalled = false;
    private DialogBox parent = null;
    private Action quickSubmitCallback = null;

    /// <summary>
    /// Assigns the specified title to the dialog box.
    /// </summary>
    /// <param name="title">Title of the dialog box.</param>
    /// <returns>Itself, for method chaining.</returns>
    public DialogBox WithTitle(string title)
    {
        titleGameObject.SetActive(true);
        UpdateRoundedCorners();
        titleText.text = title;
        return this;
    }

    /// <summary>
    /// Assigns a localized title to the dialog box.
    /// </summary>
    /// <param name="table">Table collection containing the key.</param>
    /// <param name="key">Localization key.</param>
    /// <param name="args">Additional arguments if string formatting is involved.</param>
    /// <returns>Itself, for method chaining.</returns>
    public DialogBox WithTitle(string table, string key, params object[] args)
    {
        var str = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);

        return WithTitle(str);
    }

    /// <summary>
    /// Hides the title bar.
    /// </summary>
    /// <returns>Itself, for method chaining.</returns>
    public DialogBox WithNoTitle()
    {
        titleGameObject.SetActive(false);
        UpdateRoundedCorners();
        return this;
    }

    /// <summary>
    /// Removes the dark overlay behind the dialog box, which prevents raycasts from interacting with anything behind it.
    /// </summary>
    /// <remarks>
    /// Interacting with the Dialog Box itself will still block raycasts from interacting with anything directly behind it.
    /// </remarks>
    /// <returns>Itself, for method chaining.</returns>
    public DialogBox DisableRaycastBlocker()
    {
        raycastBlocker.SetActive(false);
        return this;
    }

    /// <summary>
    /// This dialog box will not destroy itself after closing.
    /// </summary>
    /// <remarks>
    /// Once this has been called, the caller is expected to handle the lifetime of the Dialog Box.
    /// Please cache this Dialog Box to prevent duplicates, or destroy it at a time deemed appropriate.
    /// </remarks>
    /// <returns>Itself, for method chaining.</returns>
    public DialogBox DontDestroyOnClose()
    {
        destroyOnClose = false;
        return this;
    }

    /// <summary>
    /// On supported CMUI components, pressing the Quick Submit key ("Enter" by default) with the CMUI component selected
    ///   can prematurely close the dialog box.
    /// To enable this behavior, register a callback with <paramref name="onQuickSubmit"/>.
    /// </summary>
    /// <remarks>
    /// The Quick Submit behavior is not active by default. A callback *must* be given for Quick Submit to work.
    /// </remarks>
    /// <param name="onQuickSubmit">Callback on quick submit</param>
    /// <returns>Itself, for method chaining.</returns>
    public DialogBox OnQuickSubmit(Action onQuickSubmit)
    {
        quickSubmitCallback = onQuickSubmit;
        return this;
    }

    /// <summary>
    /// Instantiates the specified CMUI Component to the dialog box's body.
    /// </summary>
    /// <typeparam name="T">
    /// CMUI Component, such as <see cref="TextComponent"/>, <see cref="SliderComponent"/>, etc.
    /// </typeparam>
    /// <returns>The instantiated CMUI Component</returns>
    public T AddComponent<T>() where T : CMUIComponentBase => AddComponent(typeof(T)) as T;

    /// <summary>
    /// Instantiates the specified CMUI Component to the dialog box's body.
    /// </summary>
    /// <param name="componentType">CMUI Component type</param>
    /// <returns>The instantiated CMUI Component</returns>
    public CMUIComponentBase AddComponent(Type componentType)
    {
        var component = ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType(bodyTransform, componentType);

        if (component is INavigable navigable)
        {
            navigableComponents.AddLast(navigable);
        }

        return component;
    }

    /// <summary>
    /// Add a button to the footer of the dialog box with an unlocalized label.
    /// </summary>
    /// <remarks>
    /// When this button is clicked, the Dialog Box is automatically closed.
    /// To override this behavior, manually call <see cref="ButtonComponent.OnClick(Action)"/>.
    /// </remarks>
    /// <param name="onClick">Callback when the button is pressed.</param>
    /// <param name="label">Unlocalized label</param>
    /// <returns><see cref="ButtonComponent"/> for additional modification</returns>
    public ButtonComponent AddFooterButton(Action onClick, string label)
    {
        footerGameObject.SetActive(true);

        var button = ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType<ButtonComponent>(footerTransform)
            .WithLabel(label)
            .OnClick(() => CloseAndInvokeCallback(onClick));

        navigableFooterButtons.AddLast(button);

        return button;
    }

    /// <summary>
    /// Add a button to the footer of the dialog box with a localized label.
    /// </summary>
    /// <remarks>
    /// When this button is clicked, the Dialog Box is automatically closed.
    /// To override this behavior, manually call <see cref="ButtonComponent.OnClick(Action)"/>.
    /// </remarks>
    /// <param name="onClick">Callback when the button is pressed.</param>
    /// <param name="table">Table which holds the localized text</param>
    /// <param name="key">Key for the localized text</param>
    /// <param name="args">Object arguments, if string formatting is involved.</param>
    /// <returns><see cref="ButtonComponent"/> for additional modification</returns>
    public ButtonComponent AddFooterButton(Action onClick, string table, string key, params object[] args)
    {
        footerGameObject.SetActive(true);

        var button = ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType<ButtonComponent>(footerTransform)
            .WithLabel(table, key, args)
            .OnClick(() => CloseAndInvokeCallback(onClick));

        navigableFooterButtons.AddLast(button);

        return button;
    }

    /// <summary>
    /// Explicitly opens this dialog box.
    /// </summary>
    public void Open(DialogBox parent = null)
    {
        this.parent = parent;

        if (!callbacksInstalled)
        {
            callbacksInstalled = true;

            if (parent != null) CMInputCallbackInstaller.FindAndRemoveCallbacksRecursive(parent.transform);
            CMInputCallbackInstaller.FindAndInstallCallbacksRecursive(transform);
        }

        if (parent == null) CMInputCallbackInstaller.DisableActionMaps(typeof(DialogBox), disabledActionMaps);
        CameraController.ClearCameraMovement();

        gameObject.SetActive(true);
        transform.SetSiblingIndex(transform.parent.childCount);

        ReconstructDialogBoxNavigation();
    }

    /// <summary>
    /// Explicitly closes this dialog box.
    /// </summary>
    public void Close()
    {
        if (callbacksInstalled)
        {
            callbacksInstalled = false;
            CMInputCallbackInstaller.FindAndRemoveCallbacksRecursive(transform);
            if (parent != null) CMInputCallbackInstaller.FindAndInstallCallbacksRecursive(parent.transform);
        }

        if (parent == null) CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(DialogBox), disabledActionMaps);
        gameObject.SetActive(false);

        if (destroyOnClose)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Clears all CMUI Components and footer buttons assigned to this dialog box.
    /// </summary>
    public void Clear()
    {
        RemoveAllChildren(bodyTransform);
        RemoveAllChildren(footerTransform);
        navigableComponents.Clear();
        navigableFooterButtons.Clear();
    }

    public void OnCloseDialogBox(InputAction.CallbackContext context)
    {
        if (context.performed) Close();
    }

    public void OnNavigateDown(InputAction.CallbackContext context)
    {
        if (context.performed && currentSelectable != null)
        {
            var oldSelectable = currentSelectable;
            currentSelectable = currentSelectable.FindSelectableOnDown();

            if (currentSelectable == null) currentSelectable = oldSelectable;

            currentSelectable.Select();
        }
    }

    public void OnNavigateUp(InputAction.CallbackContext context)
    {
        if (context.performed && currentSelectable != null)
        {
            var oldSelectable = currentSelectable;
            currentSelectable = currentSelectable.FindSelectableOnUp();

            if (currentSelectable == null) currentSelectable = oldSelectable;

            currentSelectable.Select();
        }
    }

    public void OnAttemptQuickSubmit(InputAction.CallbackContext context)
    {
        if (context.performed
            && currentSelectable != null
            // TODO: meh i'm not the biggest fan of this GetComponentInParent call.
            && currentSelectable.GetComponentInParent<CMUIComponentBase>() is IQuickSubmitComponent
            && quickSubmitCallback != null)
        {
            quickSubmitCallback.Invoke();
            Close();
        }
    }

    private void UpdateRoundedCorners()
    {
        var topRoundness = titleGameObject.activeSelf ? 0 : roundness;
        var bottomRoundness = footerGameObject.activeSelf ? 0 : roundness;
        bodyRoundedCorners.r.Set(topRoundness, topRoundness, bottomRoundness, bottomRoundness);
        bodyRoundedCorners.Refresh();
    }

    private void CloseAndInvokeCallback(Action callback)
    {
        callback?.Invoke();
        Close();
    }

    private void RemoveAllChildren(Transform parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        while (parent.childCount > 0)
        {
            var child = parent.GetChild(0);

            DestroyImmediate(child.gameObject);
        }
    }

    private void ReconstructDialogBoxNavigation()
    {
        var navigableNode = navigableComponents.First;

        if (navigableNode != null)
        {
            if (currentSelectable == null) currentSelectable = navigableNode.Value.Selectable;

            IterateNavigableList(ref navigableNode);

            SetNavigation(navigableNode.Value, navigableNode.Previous?.Value, navigableFooterButtons.First?.Value);
        }

        navigableNode = navigableFooterButtons.First;

        if (navigableNode != null)
        {
            if (currentSelectable == null) currentSelectable = navigableNode.Value.Selectable;

            SetNavigation(navigableNode.Value, navigableComponents.Last?.Value, navigableNode.Next?.Value);
            navigableNode = navigableNode.Next;

            if (navigableNode != null)
            {
                IterateNavigableList(ref navigableNode);
                SetNavigation(navigableNode.Value, navigableNode.Previous?.Value, null);
            }
        }

        if (currentSelectable != null) currentSelectable.Select();
    }

    private void IterateNavigableList(ref LinkedListNode<INavigable> navigableNode)
    {
        while (navigableNode.Next != null)
        {
            SetNavigation(navigableNode.Value, navigableNode.Previous?.Value, navigableNode.Next?.Value);
            navigableNode = navigableNode.Next;
        }
    }

    private void SetNavigation(INavigable navigable, INavigable up, INavigable down)
    {
        navigable.Selectable.navigation = new Navigation()
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = up?.Selectable,
            selectOnDown = down?.Selectable,
            selectOnLeft = null,
            selectOnRight = null
        };
    }

    private void Start() => UpdateRoundedCorners();
}
