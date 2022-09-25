using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class CameraController : MonoBehaviour, CMInput.ICameraActions
{
    private static CameraController instance;

    [SerializeField] private Vector3[] presetPositions;
    [SerializeField] private Vector3[] presetRotations;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float mouseSensitivity;
    [SerializeField] private Transform noteGridTransform;
    [FormerlySerializedAs("_uiMode")] [SerializeField] private UIMode uiMode;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;

    [FormerlySerializedAs("_rotationCallbackController")] public RotationCallbackController RotationCallbackController;

    [FormerlySerializedAs("camera")] public Camera Camera;
    [SerializeField] private UniversalRenderPipelineAsset urpAsset;

    [Header("Debug")] [SerializeField] private float x;

    [SerializeField] private float y;
    [SerializeField] private float z;

    [SerializeField] private float mouseX;
    [SerializeField] private float mouseY;

    private readonly Type[] actionMapsDisabledWhileMoving =
    {
        typeof(CMInput.IPlacementControllersActions), typeof(CMInput.INotePlacementActions),
        typeof(CMInput.IEventPlacementActions), typeof(CMInput.ISavingActions), typeof(CMInput.ITimelineActions),
        typeof(CMInput.IPlatformSoloLightGroupActions), typeof(CMInput.IPlaybackActions),
        typeof(CMInput.IBeatmapObjectsActions), typeof(CMInput.INoteObjectsActions),
        typeof(CMInput.IEventObjectsActions), typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.ICustomEventsContainerActions), typeof(CMInput.IBPMTapperActions),
        typeof(CMInput.IEventUIActions), typeof(CMInput.IUIModeActions)
    };

    private Vector2 savedMousePos = Vector2.zero;

    private UniversalAdditionalCameraData cameraExtraData;

    private bool canMoveCamera;

    private bool lockOntoNoteGrid;

    private bool secondSetOfLocations;
    private bool setLocation;

    public bool LockedOntoNoteGrid
    {
        get => lockOntoNoteGrid;
        set
        {
            var camTransform = transform;
            camTransform.SetParent(!value ? null : noteGridTransform);
            camTransform.localScale = Vector3.one;
            lockOntoNoteGrid = value;
        }
    }

    private void Start()
    {
        instance = this;
        Camera.fieldOfView = Settings.Instance.CameraFOV;
        cameraExtraData = Camera.GetUniversalAdditionalCameraData();
        UpdateAA(Settings.Instance.CameraAA);
        UpdateRenderScale(Settings.Instance.RenderScale);
        Settings.NotifyBySettingName(nameof(Settings.CameraAA), UpdateAA);
        Settings.NotifyBySettingName(nameof(Settings.RenderScale), UpdateRenderScale);
        OnLocation(0);
        LockedOntoNoteGrid = true;
    }

    private void Update()
    {
        if (PauseManager.IsPaused || SceneTransitionManager.IsLoading)
            return; //Dont move camera if we are in pause menu or loading screen

        Camera.fieldOfView = Settings.Instance.CameraFOV;

        if (UIMode.SelectedMode == UIModeType.Playing)
        {
            var posY = z < 0 ? 0.25f : 1.8f;
            var posX = x < 0 ? -2f : x > 0 ? 2f : 0;

            transform.SetPositionAndRotation(new Vector3(posX, posY, -6), Quaternion.Euler(new Vector3(0, -posX, 0)));

            return;
        }

        if (canMoveCamera)
        {
            if (CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.ICameraActions)))
            {
                canMoveCamera = false;
                x = y = z = mouseY = mouseX = 0;
                return;
            }

            SetLockState(true);

            movementSpeed = Settings.Instance.Camera_MovementSpeed;
            mouseSensitivity = Settings.Instance.Camera_MouseSensitivity;

            var movementSpeedInFrame = movementSpeed * Time.deltaTime;

            var sideTranslation = movementSpeedInFrame * new Vector3(x, 0, z);
            transform.Translate(sideTranslation);
            // Y translation should always be in World space
            transform.Translate(movementSpeedInFrame * y * Vector3.up, Space.World);

            // We want to force it to never rotate Z
            var eulerAngles = transform.eulerAngles;
            var ex = eulerAngles.x;
            ex = ex > 180 ? ex - 360 : ex;
            eulerAngles.x = Mathf.Clamp(ex + -mouseY, -89.5f, 89.5f); //pepega code to fix pepega camera :)
            eulerAngles.y += mouseX;
            eulerAngles.z = 0;
            transform.eulerAngles = eulerAngles;
        }
        else
        {
            z = x = 0;
            SetLockState(false);
        }
    }

    private void UpdateAA(object aaValue)
    {
        switch ((int)aaValue)
        {
            case 0:
                cameraExtraData.antialiasing = AntialiasingMode.None;
                break;
            case 1:
                cameraExtraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                break;
            case 2:
                cameraExtraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cameraExtraData.antialiasingQuality = AntialiasingQuality.Low;
                break;
            case 3:
                cameraExtraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cameraExtraData.antialiasingQuality = AntialiasingQuality.Medium;
                break;
            case 4:
                cameraExtraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cameraExtraData.antialiasingQuality = AntialiasingQuality.High;
                break;
        }
    }

    private void UpdateRenderScale(object renderScale)
    {
        urpAsset.renderScale = Mathf.Sqrt((int)renderScale / 100f); // Sqrt to get scale per dimension
    }

    public void SetLockState(bool lockMouse)
    {
        var mouseLocked = Cursor.lockState == CursorLockMode.Locked;
        if (lockMouse && !mouseLocked)
        {
            savedMousePos = Mouse.current.position.ReadValue();

            // Locked state automatically hides the cursor, so no need to set visibility
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (!lockMouse && mouseLocked)
        {
            Cursor.lockState = CursorLockMode.None;

            // Apparently these bugs are fixed in more recent Unity versions, so remove this when we upgrade
#if UNITY_STANDALONE_WIN
            Mouse.current.WarpCursorPosition(new Vector2(savedMousePos.x, Screen.height - savedMousePos.y));
#elif UNITY_STANDALONE_OSX
            // it's extra broken on macOS so just don't move the cursor I guess
#else
            Mouse.current.WarpCursorPosition(savedMousePos);
#endif
        }
    }

    //Oh boy new Unity Input System POGCHAMP
    public void OnMoveCamera(CallbackContext context)
    {
        //Take our movement vector and manipulate it to work how we want.
        //Our X component (A and D) should move us left/right (X)
        //Our Y component (W and S) should move us forward/backward (Z)
        var movement = context.ReadValue<Vector2>();
        x = movement.x;
        z = movement.y;
    }

    public void OnElevateCamera(CallbackContext context)
    {
        //Elevation change is controlled by Space and Ctrl.
        var elevationChange = context.ReadValue<float>();
        y = elevationChange;
    }

    public void OnRotateCamera(CallbackContext context)
    {
        var deltaMouseMovement = context.ReadValue<Vector2>();
        mouseX = deltaMouseMovement.x * mouseSensitivity / 10f;
        mouseY = deltaMouseMovement.y * mouseSensitivity / 10f;
    }

    public void OnHoldtoMoveCamera(CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        canMoveCamera = context.performed;
        if (canMoveCamera)
            CMInputCallbackInstaller.DisableActionMaps(typeof(CameraController), actionMapsDisabledWhileMoving);
        else if (context.canceled)
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(CameraController), actionMapsDisabledWhileMoving);
    }

    public void OnAttachtoNoteGrid(CallbackContext context)
    {
        if (RotationCallbackController.IsActive && context.performed && noteGridTransform.gameObject.activeInHierarchy)
            LockedOntoNoteGrid = !LockedOntoNoteGrid;
    }

    public void OnToggleFullscreen(CallbackContext context)
    {
        if (!Application.isEditor && context.performed) Screen.fullScreen = !Screen.fullScreen;
    }

    public void OnLocation1(CallbackContext context) => OnLocation(0);

    public void OnLocation2(CallbackContext context) => OnLocation(1);

    public void OnLocation3(CallbackContext context) => OnLocation(2);

    public void OnLocation4(CallbackContext context) => OnLocation(3);

    private void OnDisable()
    {
        Settings.ClearSettingNotifications(nameof(Settings.CameraAA));
        instance = null;
    }

    public void OnSecondSetModifier(CallbackContext context) => secondSetOfLocations = context.performed;

    public void OnOverwriteLocationModifier(CallbackContext context) => setLocation = context.performed;

    public static void ClearCameraMovement()
    {
        if (instance is null) return;
        instance.x = instance.y = instance.z = instance.mouseX = instance.mouseY = 0;
    }

    private void OnLocation(int id)
    {
        // Shift for second set of hotkeys (8 total)
        if (secondSetOfLocations) id += 4;

        if (setLocation)
        {
            Settings.Instance.SavedPositions[id] = new CameraPosition(transform.position, transform.rotation);
        }
        else if (Settings.Instance.SavedPositions[id] != null)
        {
            transform.SetPositionAndRotation(Settings.Instance.SavedPositions[id].Position, Settings.Instance.SavedPositions[id].Rotation);
        }
    }
}
