using System;
using UnityEngine;

public class CameraController : MonoBehaviour {

    [SerializeField] Vector3[] presetPositions;

    [SerializeField] Vector3[] presetRotations;

    [SerializeField] float movementSpeed;

    [SerializeField] float mouseSensitivity;

    [SerializeField] float sprintMult;

    [SerializeField] float sprintMultPerSecond;

    [SerializeField] Transform noteGridTransform;

    [SerializeField] private UIMode _uiMode;

    public RotationCallbackController _rotationCallbackController;
    
    public Camera camera;

    [Header("Debug")]
    [SerializeField] float x;
    [SerializeField] float y;
    [SerializeField] float z;

    private bool lockOntoNoteGrid;
    public bool LockedOntoNoteGrid
    {
        get => lockOntoNoteGrid;
        set
        {
            transform.SetParent(!value ? null : noteGridTransform);
            transform.localScale = Vector3.one; // This is optional, but recommended
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
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
            z = Input.GetAxisRaw("Forward");
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _uiMode.SetUIMode(UIModeType.NORMAL, false);
                return;
            }

            z = z < 0 ? 0.25f : 1.8f;

            transform.position = new Vector3(x,z,0);
            
            if (x > 0) x = -5f;
            else if (x < 0) x = 5f;
            
            transform.rotation = Quaternion.Euler(new Vector3(0,0,x));
            
            return;
        }

        if (Input.GetKeyDown(KeyCode.X) && _rotationCallbackController.IsActive)
        {
            LockedOntoNoteGrid = !LockedOntoNoteGrid;
        }

        if (Input.GetMouseButton(1)) {
            SetLockState(true);

            movementSpeed = Settings.Instance.Camera_MovementSpeed;
            mouseSensitivity = Settings.Instance.Camera_MouseSensitivity;

            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
            z = Input.GetAxisRaw("Forward");

            transform.Translate(Vector3.right * x * movementSpeed * Time.deltaTime);
            //This one is different because we don't want the player to move vertically relatively - this should use global directions
            transform.position = transform.position + (Vector3.up * y * movementSpeed * Time.deltaTime);
            transform.Translate(Vector3.forward * z * movementSpeed * Time.deltaTime);

            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            //We want to force it to never rotate Z
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            float ex = eulerAngles.x;
            ex = (ex > 180) ? ex - 360 : ex;
            eulerAngles.x = Mathf.Clamp(ex + (-my * mouseSensitivity),-89.5f,89.5f); //pepega code to fix pepega camera :)
            eulerAngles.y = eulerAngles.y + (mx * mouseSensitivity);
            eulerAngles.z = 0;
            transform.rotation = Quaternion.Euler(eulerAngles);

        } else {
            SetLockState(false);
        }

        if (Input.GetKeyDown(KeyCode.Keypad0)) GoToPreset(0);
        if (Input.GetKeyDown(KeyCode.Keypad1)) GoToPreset(1);

    }

    public void GoToPreset(int id) {
        if (presetPositions.Length < id && presetRotations.Length < id) {
            transform.position = presetPositions[id];
            transform.rotation = Quaternion.Euler(presetRotations[id]);
        }
        else
        {
            throw new IndexOutOfRangeException("The Camera preset entered (" + id + ") was not valid");
        }
    }

    public void SetLockState(bool lockMouse) {
        Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockMouse;
    }
}
