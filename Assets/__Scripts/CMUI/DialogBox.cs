using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class DialogBox : MonoBehaviour
{
    private const float roundness = 4;

    // TODO: Make a IDialogBoxActions for navigation purposes
    // (yes it would essentially be a MenuBase re-implementation which would be bad *BUT* we do need it to be separate
    //   from MenuBase anyways since IMenusExtendedActtions is getting blocked here.
    //   Prevents scene transitions while inside a box.)
    private static readonly IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes()
        .Where(t => t.IsInterface && t != typeof(CMInput.IUtilsActions));

    [SerializeField] private GameObject raycastBlocker;
    [SerializeField] private GameObject titleGameObject;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ImageWithIndependentRoundedCorners bodyRoundedCorners;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private GameObject footerGameObject;
    [SerializeField] private Transform footerTransform;

    private bool destroyOnClose = true;

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
        => ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType(bodyTransform, componentType);

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

        return ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType<ButtonComponent>(footerTransform)
            .WithLabel(label)
            .OnClick(() => CloseAndInvokeCallback(onClick));
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

        return ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType<ButtonComponent>(footerTransform)
            .WithLabel(table, key, args)
            .OnClick(() => CloseAndInvokeCallback(onClick));
    }

    /// <summary>
    /// Explicitly opens this dialog box.
    /// </summary>
    public void Open()
    {
        CMInputCallbackInstaller.DisableActionMaps(typeof(DialogBox), disabledActionMaps);
        CameraController.ClearCameraMovement();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Explicitly closes this dialog box.
    /// </summary>
    public void Close()
    {
        // TODO: With multiple dialog boxes open at the same time, closing one box might enable inputs for the rest.
        //   Perhaps tie the blocking type to the calling method's type, rather than the Dialog Box type itself?
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(DialogBox), disabledActionMaps);
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

    private void Start() => UpdateRoundedCorners();
}
