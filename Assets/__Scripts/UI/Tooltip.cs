using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Serialization;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float MinimumAppearDelay = 0.25f;

    [FormerlySerializedAs("tooltip")] public LocalizedString LocalizedTooltip;

    [FormerlySerializedAs("tooltipOverride")] [HideInInspector] public string TooltipOverride;

    [FormerlySerializedAs("advancedTooltip")] public string AdvancedTooltip;

    public float AppearDelay = MinimumAppearDelay;

    public bool TooltipActive;

    [Tooltip("Action map name to look up the hotkey from (e.g. 'Edit Mode')")]
    [SerializeField] public string HotkeyActionMap;

    [Tooltip("Input action name within the map to display as a hotkey hint (e.g. 'GLSEdit')")]
    [SerializeField] public string HotkeyActionName;

    // GLS page tabs need a second action hint so both remappable directions appear together.
    [SerializeField] public string AdditionalHotkeyActionName;

    // Some action descriptions read naturally as an instruction, so preserve the explicit "Press" wording before a remappable hint.
    [SerializeField] public string HotkeyDisplayPrefix;

    private Coroutine routine;

    private void OnDisable() => OnPointerExit(null);

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Saved zero delays predate this minimum and skip the initializer, so clamp at hover.
        if (routine == null)
            routine = StartCoroutine(TooltipRoutine(Mathf.Max(AppearDelay, MinimumAppearDelay)));

        TooltipActive = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        PersistentUI.Instance.HideTooltip();
        TooltipActive = false;
    }

    private IEnumerator TooltipRoutine(float timeToWait)
    {
        var tooltipTextResult = TooltipOverride;
        if (string.IsNullOrEmpty(TooltipOverride)) 
            tooltipTextResult = LocalizedTooltip.GetLocalizedString();

        var hotkey = GetHotkeyDisplayString();
        if (!string.IsNullOrEmpty(hotkey))
        {
            // Keep the binding itself dynamic while allowing the owning control to supply a short instructional prefix.
            tooltipTextResult = $"{tooltipTextResult} [{HotkeyDisplayPrefix}{hotkey}]";
        }

        PersistentUI.Instance.SetTooltip(tooltipTextResult, AdvancedTooltip);
        yield return new WaitForSecondsRealtime(timeToWait);
        PersistentUI.Instance.ShowTooltip();
    }

    private string GetHotkeyDisplayString()
    {
        if (string.IsNullOrEmpty(HotkeyActionName)) 
            return null;
        var input = CMInputCallbackInstaller.InputInstance;
        if (input == null)
            return null;

        // Resolve the action map once so an optional second hotkey can use the same remappable binding context.
        InputActionMap map = null;
        InputAction action = null;
        if (!string.IsNullOrEmpty(HotkeyActionMap))
        {
            map = input.asset.FindActionMap(HotkeyActionMap);
            action = map?.FindAction(HotkeyActionName);
        }
        else
        {
            action = input.asset.FindAction(HotkeyActionName);
        }

        if (action == null) 
            return null;

        var displayString = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
        if (string.IsNullOrEmpty(AdditionalHotkeyActionName))
        {
            return string.IsNullOrEmpty(displayString) ? null : displayString;
        }

        var additionalAction = (map ?? action.actionMap)?.FindAction(AdditionalHotkeyActionName);
        var additionalDisplayString = additionalAction?.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
        if (string.IsNullOrEmpty(displayString))
        {
            return string.IsNullOrEmpty(additionalDisplayString) ? null : additionalDisplayString;
        }

        return string.IsNullOrEmpty(additionalDisplayString) ? displayString : $"{displayString}/{additionalDisplayString}";
    }
}
