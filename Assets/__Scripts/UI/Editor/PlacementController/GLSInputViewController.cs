using Beatmap.Base;
using UnityEngine;

[ExecuteAlways]
public class GLSInputViewController : MonoBehaviour
{
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;

    [Header("View Controllers")] [SerializeField]
    private ToggleableViewController colorViewController;

    [SerializeField] private ToggleableViewController rotationViewController;
    [SerializeField] private ToggleableViewController translationViewController;
    [SerializeField] private ToggleableViewController floatFXViewController;
    [SerializeField] private ToggleableViewController easingViewController;

    private void Start()
    {
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
        HandleEditModeChanged(editModeContext.EditingMode);
    }

    private void OnDestroy()
    {
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
    }

    private void HandleEditModeChanged(EditingMode mode) => HandleGroupChanged(glsEventGridProvider.GroupContext);

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        if (editModeContext.EditingMode.HasFlag(EditingMode.EventBox) && glsEventGridProvider.GroupContext != null)
        {
            colorViewController.Show(glsEventGridProvider.GroupContext is BaseLightColorEventBoxGroup);
            rotationViewController.Show(glsEventGridProvider.GroupContext is BaseLightRotationEventBoxGroup);
            translationViewController.Show(glsEventGridProvider.GroupContext is BaseLightTranslationEventBoxGroup);
            floatFXViewController.Show(glsEventGridProvider.GroupContext is BaseVfxEventEventBoxGroup);

            colorViewController.Extend(true);
            rotationViewController.Extend(true);
            translationViewController.Extend(true);
            floatFXViewController.Extend(true);

            // TODO support here when adding easings to GLS color fade in ChromaGLS
            easingViewController.Show(
                glsEventGridProvider.GroupContext is ILightTransformEventBoxGroup
                    or BaseVfxEventEventBoxGroup);
            easingViewController.Extend(true);
        }
        else
        {
            colorViewController.Show(true);
            rotationViewController.Show(true);
            translationViewController.Show(true);
            floatFXViewController.Show(true);

            colorViewController.Extend(false);
            rotationViewController.Extend(false);
            translationViewController.Extend(false);
            floatFXViewController.Extend(false);

            easingViewController.Show(false);
            easingViewController.Extend(false);
        }
    }
}
