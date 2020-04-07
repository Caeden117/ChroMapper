using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class CameraController : MonoBehaviour, CMInput.ICameraActions {

    [SerializeField] Vector3[] presetPositions;

    [SerializeField] Vector3[] presetRotations;

    [SerializeField] float movementSpeed;

    [SerializeField] float mouseSensitivity;

    [SerializeField] float sprintMult;

    [SerializeField] float sprintMultPerSecond;

    [SerializeField] Transform noteGridTransform;

    [SerializeField] private UIMode _uiMode;

    public RotationCallbackController _rotationCallbackController;
    
    public new Camera camera;

    [Header("Debug")]
    [SerializeField] float x;
    [SerializeField] float y;
    [SerializeField] float z;

    [SerializeField] float mouseX;
    [SerializeField] float mouseY;

    private bool canMoveCamera = false;

    private bool lockOntoNoteGrid;
    public bool LockedOntoNoteGrid
    {
        get => lockOntoNoteGrid;
        set
        {
            transform.SetParent(!value ? null : noteGridTransform);
            transform.localScale = transform.worldToLocalMatrix.MultiplyPoint(Vector3.one); // This is optional, but recommended
            lockOntoNoteGrid = value;
        }
    }

    private void Start()
    {
        camera.fieldOfView = Settings.Instance.CameraFOV;
        GoToPreset(1);
    }

    void Update () {
        if (PauseManager.IsPaused || SceneTransitionManager.IsLoading) return; //Dont move camera if we are in pause menu or loading screen

        camera.fieldOfView = Settings.Instance.CameraFOV;

        if (_uiMode.selectedMode == UIModeType.PLAYING)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _uiMode.SetUIMode(UIModeType.NORMAL, false); //todo fix: it makes the esc menu unclickable so you have to exit and reopen it.
                return;
            }

            z = z < 0 ? 0.25f : 1.8f;

            transform.position = new Vector3(x,z,0);
            
            if (x > 0) x = -5f;
            else if (x < 0) x = 5f;
            
            transform.rotation = Quaternion.Euler(new Vector3(0,0,x));
            
            return;
        }

        if (canMoveCamera) {
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
            SetLockState(false);
        }

    }

    public void GoToPreset(int id) {
        if (presetPositions.Length < id && presetRotations.Length < id) {
            transform.position = presetPositions[id];
            transform.rotation = Quaternion.Euler(presetRotations[id]);
        }
        else
        { //todo see why this is throwing an error when mapper loads
            //throw new IndexOutOfRangeException("The Camera preset entered (" + id + ") was not valid");
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
        mouseX = deltaMouseMovement.x / mouseSensitivity;
        mouseY = deltaMouseMovement.y / mouseSensitivity;
    }

    public void OnHoldtoMoveCamera(CallbackContext context)
    {
        canMoveCamera = !context.canceled;
    }

    public void OnAttachtoNoteGrid(CallbackContext context)
    {
        if (_rotationCallbackController.IsActive)
        {
            LockedOntoNoteGrid = !LockedOntoNoteGrid;
        }
    }
}
