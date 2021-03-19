using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class CameraController : MonoBehaviour, CMInput.ICameraActions {

    private static CameraController instance;

    [SerializeField] Vector3[] presetPositions;
    [SerializeField] Vector3[] presetRotations;
    [SerializeField] float movementSpeed;
    [SerializeField] float mouseSensitivity;
    [SerializeField] Transform noteGridTransform;
    [SerializeField] private UIMode _uiMode;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;

    public RotationCallbackController _rotationCallbackController;
    
    public new Camera camera;

    [Header("Debug")]
    [SerializeField] float x;
    [SerializeField] float y;
    [SerializeField] float z;

    [SerializeField] float mouseX;
    [SerializeField] float mouseY;

    private bool canMoveCamera = false;

    private bool secondSetOfLocations = false;
    private bool setLocation = false;

    private bool lockOntoNoteGrid;
    public bool LockedOntoNoteGrid
    {
        get => lockOntoNoteGrid;
        set
        {
            transform.SetParent(!value ? null : noteGridTransform);
            lockOntoNoteGrid = value;
        }
    }

    private readonly Type[] actionMapsDisabledWhileMoving = new Type[]
    {
        typeof(CMInput.IPlacementControllersActions),
        typeof(CMInput.INotePlacementActions),
        typeof(CMInput.IEventPlacementActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.ITimelineActions),
        typeof(CMInput.IPlatformSoloLightGroupActions),
        typeof(CMInput.IPlaybackActions),
        typeof(CMInput.IBeatmapObjectsActions),
        typeof(CMInput.INoteObjectsActions),
        typeof(CMInput.IEventObjectsActions),
        typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.ICustomEventsContainerActions),
        typeof(CMInput.IBPMTapperActions),
        typeof(CMInput.IEventUIActions),
        typeof(CMInput.IUIModeActions),
    };

    public static void ClearCameraMovement()
    {
        if (instance is null) return;
        instance.x = instance.y = instance.z = instance.mouseX = instance.mouseY = 0;
    }

    private void Start()
    {
        instance = this;
        camera.fieldOfView = Settings.Instance.CameraFOV;
        OnLocation(0);
    }

    void Update () {
        if (PauseManager.IsPaused || SceneTransitionManager.IsLoading) return; //Dont move camera if we are in pause menu or loading screen

        camera.fieldOfView = Settings.Instance.CameraFOV;

        if (_uiMode.selectedMode == UIModeType.PLAYING)
        {
            z = z < 0 ? 0.25f : 1.8f;
            x = x < 0 ? -2f : x > 0 ? 2f : 0;

            transform.position = new Vector3(x,z,0);
            transform.rotation = Quaternion.Euler(new Vector3(0,-x,0));
            
            return;
        }

        if (canMoveCamera) {
            if (CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.ICameraActions)))
            {
                canMoveCamera = false;
                x = y = z = mouseY = mouseX = 0;
                return;
            }
            SetLockState(true);

            movementSpeed = Settings.Instance.Camera_MovementSpeed;
            mouseSensitivity = Settings.Instance.Camera_MouseSensitivity;

            transform.Translate(Vector3.right * x * movementSpeed * Time.deltaTime);
            //This one is different because we don't want the player to move vertically relatively - this should use global directions
            transform.position = transform.position + (Vector3.up * y * movementSpeed * Time.deltaTime);
            transform.Translate(Vector3.forward * z * movementSpeed * Time.deltaTime);

            //We want to force it to never rotate Z
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            float ex = eulerAngles.x;
            ex = (ex > 180) ? ex - 360 : ex;
            eulerAngles.x = Mathf.Clamp(ex + (-mouseY),-89.5f,89.5f); //pepega code to fix pepega camera :)
            eulerAngles.y = eulerAngles.y + (mouseX);
            eulerAngles.z = 0;
            transform.rotation = Quaternion.Euler(eulerAngles);

        } else {
            z = x = 0;
            SetLockState(false);
        }

    }

    public void SetLockState(bool lockMouse) {
        Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockMouse;
    }

    //Oh boy new Unity Input System POGCHAMP
    public void OnMoveCamera(CallbackContext context)
    {
        //Take our movement vector and manipulate it to work how we want.
        //Our X component (A and D) should move us left/right (X)
        //Our Y component (W and S) should move us forward/backward (Z)
        Vector2 movement = context.ReadValue<Vector2>();
        x = movement.x;
        z = movement.y;
    }

    public void OnElevateCamera(CallbackContext context)
    {
        //Elevation change is controlled by Space and Ctrl.
        float elevationChange = context.ReadValue<float>();
        y = elevationChange;
    }

    public void OnRotateCamera(CallbackContext context)
    {
        Vector2 deltaMouseMovement = context.ReadValue<Vector2>();
        mouseX = deltaMouseMovement.x * mouseSensitivity / 10f;
        mouseY = deltaMouseMovement.y * mouseSensitivity / 10f;
    }

    public void OnHoldtoMoveCamera(CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        canMoveCamera = context.performed;
        if (canMoveCamera)
        {
            CMInputCallbackInstaller.DisableActionMaps(typeof(CameraController), actionMapsDisabledWhileMoving);
        }
        else if (context.canceled)
        {
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(CameraController), actionMapsDisabledWhileMoving);
        }
    }

    public void OnAttachtoNoteGrid(CallbackContext context)
    {
        if (_rotationCallbackController.IsActive && context.performed && noteGridTransform.gameObject.activeInHierarchy)
        {
            LockedOntoNoteGrid = !LockedOntoNoteGrid;
            transform.localScale = Vector3.one;
        }
    }

    public void OnToggleFullscreen(CallbackContext context)
    {
        if (!Application.isEditor && context.performed) Screen.fullScreen = !Screen.fullScreen;
    }

    private void OnDisable()
    {
        instance = null;
    }

    public void OnLocation1(CallbackContext context)
    {
        OnLocation(0);
    }

    public void OnLocation2(CallbackContext context)
    {
        OnLocation(1);
    }

    public void OnLocation3(CallbackContext context)
    {
        OnLocation(2);
    }

    public void OnLocation4(CallbackContext context)
    {
        OnLocation(3);
    }

    private void OnLocation(int id)
    {
        // Shift for second set of hotkeys (8 total)
        if (secondSetOfLocations)
        {
            id += 4;
        }

        if (setLocation)
        {
            Settings.Instance.savedPosititons[id] = new CameraPosition(transform.position, transform.rotation);
        }
        else if (Settings.Instance.savedPosititons[id] != null)
        {
            transform.position = Settings.Instance.savedPosititons[id].Position;
            transform.rotation = Settings.Instance.savedPosititons[id].Rotation;
        }
    }

    public void OnSecondSetModifier(CallbackContext context)
    {
        secondSetOfLocations = context.performed;
    }

    public void OnOverwriteLocationModifier(CallbackContext context)
    {
        setLocation = context.performed;
    }
}
