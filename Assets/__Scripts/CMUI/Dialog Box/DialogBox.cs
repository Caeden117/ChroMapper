using System;
using TMPro;
using UnityEngine;

public class DialogBox : MonoBehaviour
{
    private const float roundness = 4;

    [SerializeField] private GameObject raycastBlocker;
    [SerializeField] private GameObject titleGameObject;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ImageWithIndependentRoundedCorners bodyRoundedCorners;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private GameObject footerGameObject;
    [SerializeField] private Transform footerTransform;

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
    /// Instantiates the specified CMUI Component to the dialog box's body.
    /// </summary>
    /// <typeparam name="T">
    /// CMUI Component, such as <see cref="TextComponent"/>, <see cref="SliderComponent"/>, etc.
    /// </typeparam>
    /// <returns>The instantiated CMUI Component</returns>
    public T AddComponent<T>() where T : CMUIComponentBase
        => ComponentStoreSO.Instance.InstantiateCMUIComponentForComponentType<T>(bodyTransform);

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
          .WithUnlocalizedLabel(label)
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
        .WithLocalizedLabel(table, key, args)
        .OnClick(() => CloseAndInvokeCallback(onClick));
    }

    /// <summary>
    /// Explicitly opens this dialog box.
    /// </summary>
    public void Open() => gameObject.SetActive(true);

    /// <summary>
    /// Explicitly closes this dialog box.
    /// </summary>
    public void Close() => gameObject.SetActive(false);

    private void UpdateRoundedCorners()
    {
        var topRoundness = titleGameObject.activeSelf ? 0 : roundness;
        var bottomRoundness = footerGameObject.activeSelf ? 0 : roundness;
        bodyRoundedCorners.r.Set(topRoundness, topRoundness, bottomRoundness, bottomRoundness);
        bodyRoundedCorners.Refresh();
    }

    private void CloseAndInvokeCallback(Action callback)
    {
        Close();
        callback?.Invoke();
    }

    private void Start() => UpdateRoundedCorners();
}
