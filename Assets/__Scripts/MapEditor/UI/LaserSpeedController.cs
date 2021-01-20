using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public class LaserSpeedController : DisableActionsField, CMInput.ILaserSpeedActions
{
    [SerializeField] private TMP_InputField laserSpeed;
    private float timeSinceLastInput = 0;
    private float delayBeforeReset = 0.5f;

    public bool Activated {
        get; private set;
    }

    // Start is called before the first frame update
    void Start()
    {
        /*
         * Since Laser Speed will be controlled a number of ways, a basic Action Map will not be enough.
         * 
         * To get the functionality we want, we need to hook into the Input System and figure out if a key
         * has been pressed at any time. While that's simple to do regularly, we also need to grab WHAT key
         * has been pressed, which is why we need to use this complicated route.
         */
        InputSystem.onEvent += TryGetButtonControl;
    }

    private void TryGetButtonControl(InputEventPtr eventPtr, InputDevice device)
    {
        if (!Activated || (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()))
            return;
        var controls = device.allControls;
        var buttonPressPoint = InputSystem.settings.defaultButtonPressPoint;
        for (var i = 0; i < controls.Count; ++i)
        {
            var control = controls[i] as ButtonControl;
            if (control == null || control.synthetic || control.noisy)
                continue;
            if (control.ReadValueFromEvent(eventPtr, out var value) && value >= buttonPressPoint)
                OnChangeLaserSpeed(control);
        }
    }

    private void OnChangeLaserSpeed(ButtonControl control)
    {
        if (laserSpeed.isFocused) return;
        string num = control.name.Split("numpad".ToCharArray()).Last();
        if (int.TryParse(num, out int digit))
        {
            //We have a valid number (top row or numpad), let's add it to laser speed.
            if (Time.time >= timeSinceLastInput + delayBeforeReset) laserSpeed.text = "";
            timeSinceLastInput = Time.time;
            laserSpeed.text += digit;
        }
    }

    // Update is called once per frame
    void OnDestroy()
    {
        InputSystem.onEvent -= TryGetButtonControl;
    }

    public void OnActivateTopRowInput(InputAction.CallbackContext context)
    {
        Activated = context.performed;
    }
}
