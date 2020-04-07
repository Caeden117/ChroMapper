// GENERATED AUTOMATICALLY FROM 'Assets/__Scripts/Input/Master.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @CMInput : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @CMInput()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""Master"",
    ""maps"": [
        {
            ""name"": ""Camera"",
            ""id"": ""0916e8f4-adac-4f93-886e-7f72514589d5"",
            ""actions"": [
                {
                    ""name"": ""Hold to Move Camera"",
                    ""type"": ""Button"",
                    ""id"": ""37c4e574-0aea-4a6a-a4f2-575104bfd259"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Move Camera"",
                    ""type"": ""PassThrough"",
                    ""id"": ""b690809d-6128-4967-aa54-ad3b44b03278"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Rotate Camera"",
                    ""type"": ""Value"",
                    ""id"": ""2accd882-d6d0-439c-a1ed-189931751453"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Elevate Camera"",
                    ""type"": ""Button"",
                    ""id"": ""3fbaee37-d68e-4db9-8b0a-93d06160b118"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Attach to Note Grid"",
                    ""type"": ""Button"",
                    ""id"": ""3a674479-bfd9-4f9e-8d57-8c5eebd67e5e"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Movement"",
                    ""id"": ""1f652d44-378c-4d9e-8c08-62c19d5c94f1"",
                    ""path"": ""2DVector(mode=2)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move Camera"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""d2d63e78-e4cd-4476-9bb7-95f6a64724ec"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""b8fce530-a706-4d90-ac3c-ef71b30fbd25"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""67d23d5e-4301-4614-9bdb-a74d720b28ca"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""91ac3309-f9db-4830-82c5-449f7edaf79f"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""7da07213-d4f5-4c66-9fd0-b6b34595b8fa"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": ""InvertVector2(invertX=false,invertY=false)"",
                    ""groups"": """",
                    ""action"": ""Rotate Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""17ca5bb4-7047-4825-82c9-07dae2c23736"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevate Camera"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""066474db-11dd-4e08-9aa5-234b05ba63e7"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevate Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""5b427547-d298-4bdc-9aad-aa05d2e3fbf5"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevate Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""bb6887ca-e93a-40c3-a19c-7b8476e26a80"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Attach to Note Grid"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""036bf932-8107-4b8f-8851-2333b0cace51"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Hold to Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Utils"",
            ""id"": ""f4c0c3c1-a81f-4d2c-8b60-075660df638d"",
            ""actions"": [
                {
                    ""name"": ""Control Modifier"",
                    ""type"": ""Button"",
                    ""id"": ""a5c857c0-7636-4e1c-8510-22f4e72d2b01"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Alt Modifier"",
                    ""type"": ""Button"",
                    ""id"": ""e5b2efee-cf71-4d04-ba3a-c544cc5b2be2"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Shift Modifier"",
                    ""type"": ""Button"",
                    ""id"": ""80d43d78-82d6-4589-8fd9-89580f03b9c4"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""f23fa707-1a17-4b14-84ad-c47826b4f3f5"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Control Modifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c5d1cf75-5eee-4cd8-a505-0751117cee28"",
                    ""path"": ""<Keyboard>/alt"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Alt Modifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2b25fb98-6eb3-4ff9-aed4-120bf37c130e"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift Modifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Selection"",
            ""id"": ""3e26cae6-c1ff-441d-96fc-9f7505133eed"",
            ""actions"": [
                {
                    ""name"": ""Deselect All"",
                    ""type"": ""Button"",
                    ""id"": ""843ec7df-2ea7-4a04-bdd8-dab6f351222c"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Paste"",
                    ""type"": ""Button"",
                    ""id"": ""e19d30e7-c044-49c7-b606-bcc91b42ad88"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Delete Objects"",
                    ""type"": ""Button"",
                    ""id"": ""f2c276f8-89bc-4baf-a952-5a39b8fbbac6"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Copy"",
                    ""type"": ""Button"",
                    ""id"": ""f1bf6ebd-cb31-4847-bd48-3d4a3376aa06"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Cut"",
                    ""type"": ""Button"",
                    ""id"": ""5f19c790-369e-4d34-9cc1-469a0d538087"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Shift in Time"",
                    ""type"": ""Button"",
                    ""id"": ""44cf8d3a-44b2-41b0-bc98-60a5752feb17"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Shift in Place"",
                    ""type"": ""Button"",
                    ""id"": ""44843eff-de98-49e6-b6df-3d3024541047"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""6ec20df8-70e6-4e81-b88d-d49d8946c8ab"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Deselect All"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""b7fa0985-c08e-4fb3-9916-8ec8c089609d"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Deselect All"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""fd8858fe-7e16-4241-8183-7bf767569fbf"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Deselect All"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""00aa45cc-7f85-4822-a652-c7c1e4c6bb69"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Paste"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""16979eb9-35df-4af5-b6a5-ee5d90a7adb4"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Paste"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""1181f21d-b912-4257-826c-a246de23e8c8"",
                    ""path"": ""<Keyboard>/v"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Paste"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""ff7c5e80-78b3-49ab-92f5-c73e4c7b8166"",
                    ""path"": ""<Keyboard>/delete"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Delete Objects"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""288333f6-a80d-4b16-9833-812f6dc2238a"",
                    ""path"": ""<Keyboard>/backspace"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Delete Objects"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""26bb8bb3-d3c9-4a7c-8e74-184e5d384974"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Copy"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""b50a3b6d-75b8-423c-9640-b74bba32e278"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Copy"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""90cca52d-2106-479c-9f0c-cfc2bebe424d"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Copy"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""77a53992-04c2-4c16-a1af-257e6bccd3ef"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Cut"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""b19148c1-9ce5-46ba-9c0c-20d94409d938"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Cut"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""ef97f74f-4d26-4b05-a604-d45f03f5e270"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Cut"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""ad07cd1e-b7f7-4958-9a8f-e14d5e560613"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Time"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""96f514e6-19fe-464e-9e54-730aa9d45ffd"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Time"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""5a6b2c20-d831-4aa4-8b3d-06c5ba55f7e1"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Time"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""36e2ce01-db8c-4a0f-ad93-bb7bb495efb0"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Place"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""cd9e37e8-8940-400e-8b2b-bd48139e0c24"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""c96cb2f8-53c5-464e-b24c-3129ce216d46"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""1483dae6-cdaa-4ae4-ab73-89b3048cdce7"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""c28b02f5-c7bc-425f-a7a7-791a432fef17"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard and Mouse"",
            ""bindingGroup"": ""Keyboard and Mouse"",
            ""devices"": []
        }
    ]
}");
        // Camera
        m_Camera = asset.FindActionMap("Camera", throwIfNotFound: true);
        m_Camera_HoldtoMoveCamera = m_Camera.FindAction("Hold to Move Camera", throwIfNotFound: true);
        m_Camera_MoveCamera = m_Camera.FindAction("Move Camera", throwIfNotFound: true);
        m_Camera_RotateCamera = m_Camera.FindAction("Rotate Camera", throwIfNotFound: true);
        m_Camera_ElevateCamera = m_Camera.FindAction("Elevate Camera", throwIfNotFound: true);
        m_Camera_AttachtoNoteGrid = m_Camera.FindAction("Attach to Note Grid", throwIfNotFound: true);
        // Utils
        m_Utils = asset.FindActionMap("Utils", throwIfNotFound: true);
        m_Utils_ControlModifier = m_Utils.FindAction("Control Modifier", throwIfNotFound: true);
        m_Utils_AltModifier = m_Utils.FindAction("Alt Modifier", throwIfNotFound: true);
        m_Utils_ShiftModifier = m_Utils.FindAction("Shift Modifier", throwIfNotFound: true);
        // Selection
        m_Selection = asset.FindActionMap("Selection", throwIfNotFound: true);
        m_Selection_DeselectAll = m_Selection.FindAction("Deselect All", throwIfNotFound: true);
        m_Selection_Paste = m_Selection.FindAction("Paste", throwIfNotFound: true);
        m_Selection_DeleteObjects = m_Selection.FindAction("Delete Objects", throwIfNotFound: true);
        m_Selection_Copy = m_Selection.FindAction("Copy", throwIfNotFound: true);
        m_Selection_Cut = m_Selection.FindAction("Cut", throwIfNotFound: true);
        m_Selection_ShiftinTime = m_Selection.FindAction("Shift in Time", throwIfNotFound: true);
        m_Selection_ShiftinPlace = m_Selection.FindAction("Shift in Place", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Camera
    private readonly InputActionMap m_Camera;
    private ICameraActions m_CameraActionsCallbackInterface;
    private readonly InputAction m_Camera_HoldtoMoveCamera;
    private readonly InputAction m_Camera_MoveCamera;
    private readonly InputAction m_Camera_RotateCamera;
    private readonly InputAction m_Camera_ElevateCamera;
    private readonly InputAction m_Camera_AttachtoNoteGrid;
    public struct CameraActions
    {
        private @CMInput m_Wrapper;
        public CameraActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @HoldtoMoveCamera => m_Wrapper.m_Camera_HoldtoMoveCamera;
        public InputAction @MoveCamera => m_Wrapper.m_Camera_MoveCamera;
        public InputAction @RotateCamera => m_Wrapper.m_Camera_RotateCamera;
        public InputAction @ElevateCamera => m_Wrapper.m_Camera_ElevateCamera;
        public InputAction @AttachtoNoteGrid => m_Wrapper.m_Camera_AttachtoNoteGrid;
        public InputActionMap Get() { return m_Wrapper.m_Camera; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CameraActions set) { return set.Get(); }
        public void SetCallbacks(ICameraActions instance)
        {
            if (m_Wrapper.m_CameraActionsCallbackInterface != null)
            {
                @HoldtoMoveCamera.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnHoldtoMoveCamera;
                @HoldtoMoveCamera.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnHoldtoMoveCamera;
                @HoldtoMoveCamera.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnHoldtoMoveCamera;
                @MoveCamera.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMoveCamera;
                @MoveCamera.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMoveCamera;
                @MoveCamera.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMoveCamera;
                @RotateCamera.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnRotateCamera;
                @RotateCamera.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnRotateCamera;
                @RotateCamera.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnRotateCamera;
                @ElevateCamera.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevateCamera;
                @ElevateCamera.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevateCamera;
                @ElevateCamera.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevateCamera;
                @AttachtoNoteGrid.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnAttachtoNoteGrid;
                @AttachtoNoteGrid.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnAttachtoNoteGrid;
                @AttachtoNoteGrid.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnAttachtoNoteGrid;
            }
            m_Wrapper.m_CameraActionsCallbackInterface = instance;
            if (instance != null)
            {
                @HoldtoMoveCamera.started += instance.OnHoldtoMoveCamera;
                @HoldtoMoveCamera.performed += instance.OnHoldtoMoveCamera;
                @HoldtoMoveCamera.canceled += instance.OnHoldtoMoveCamera;
                @MoveCamera.started += instance.OnMoveCamera;
                @MoveCamera.performed += instance.OnMoveCamera;
                @MoveCamera.canceled += instance.OnMoveCamera;
                @RotateCamera.started += instance.OnRotateCamera;
                @RotateCamera.performed += instance.OnRotateCamera;
                @RotateCamera.canceled += instance.OnRotateCamera;
                @ElevateCamera.started += instance.OnElevateCamera;
                @ElevateCamera.performed += instance.OnElevateCamera;
                @ElevateCamera.canceled += instance.OnElevateCamera;
                @AttachtoNoteGrid.started += instance.OnAttachtoNoteGrid;
                @AttachtoNoteGrid.performed += instance.OnAttachtoNoteGrid;
                @AttachtoNoteGrid.canceled += instance.OnAttachtoNoteGrid;
            }
        }
    }
    public CameraActions @Camera => new CameraActions(this);

    // Utils
    private readonly InputActionMap m_Utils;
    private IUtilsActions m_UtilsActionsCallbackInterface;
    private readonly InputAction m_Utils_ControlModifier;
    private readonly InputAction m_Utils_AltModifier;
    private readonly InputAction m_Utils_ShiftModifier;
    public struct UtilsActions
    {
        private @CMInput m_Wrapper;
        public UtilsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ControlModifier => m_Wrapper.m_Utils_ControlModifier;
        public InputAction @AltModifier => m_Wrapper.m_Utils_AltModifier;
        public InputAction @ShiftModifier => m_Wrapper.m_Utils_ShiftModifier;
        public InputActionMap Get() { return m_Wrapper.m_Utils; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(UtilsActions set) { return set.Get(); }
        public void SetCallbacks(IUtilsActions instance)
        {
            if (m_Wrapper.m_UtilsActionsCallbackInterface != null)
            {
                @ControlModifier.started -= m_Wrapper.m_UtilsActionsCallbackInterface.OnControlModifier;
                @ControlModifier.performed -= m_Wrapper.m_UtilsActionsCallbackInterface.OnControlModifier;
                @ControlModifier.canceled -= m_Wrapper.m_UtilsActionsCallbackInterface.OnControlModifier;
                @AltModifier.started -= m_Wrapper.m_UtilsActionsCallbackInterface.OnAltModifier;
                @AltModifier.performed -= m_Wrapper.m_UtilsActionsCallbackInterface.OnAltModifier;
                @AltModifier.canceled -= m_Wrapper.m_UtilsActionsCallbackInterface.OnAltModifier;
                @ShiftModifier.started -= m_Wrapper.m_UtilsActionsCallbackInterface.OnShiftModifier;
                @ShiftModifier.performed -= m_Wrapper.m_UtilsActionsCallbackInterface.OnShiftModifier;
                @ShiftModifier.canceled -= m_Wrapper.m_UtilsActionsCallbackInterface.OnShiftModifier;
            }
            m_Wrapper.m_UtilsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ControlModifier.started += instance.OnControlModifier;
                @ControlModifier.performed += instance.OnControlModifier;
                @ControlModifier.canceled += instance.OnControlModifier;
                @AltModifier.started += instance.OnAltModifier;
                @AltModifier.performed += instance.OnAltModifier;
                @AltModifier.canceled += instance.OnAltModifier;
                @ShiftModifier.started += instance.OnShiftModifier;
                @ShiftModifier.performed += instance.OnShiftModifier;
                @ShiftModifier.canceled += instance.OnShiftModifier;
            }
        }
    }
    public UtilsActions @Utils => new UtilsActions(this);

    // Selection
    private readonly InputActionMap m_Selection;
    private ISelectionActions m_SelectionActionsCallbackInterface;
    private readonly InputAction m_Selection_DeselectAll;
    private readonly InputAction m_Selection_Paste;
    private readonly InputAction m_Selection_DeleteObjects;
    private readonly InputAction m_Selection_Copy;
    private readonly InputAction m_Selection_Cut;
    private readonly InputAction m_Selection_ShiftinTime;
    private readonly InputAction m_Selection_ShiftinPlace;
    public struct SelectionActions
    {
        private @CMInput m_Wrapper;
        public SelectionActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @DeselectAll => m_Wrapper.m_Selection_DeselectAll;
        public InputAction @Paste => m_Wrapper.m_Selection_Paste;
        public InputAction @DeleteObjects => m_Wrapper.m_Selection_DeleteObjects;
        public InputAction @Copy => m_Wrapper.m_Selection_Copy;
        public InputAction @Cut => m_Wrapper.m_Selection_Cut;
        public InputAction @ShiftinTime => m_Wrapper.m_Selection_ShiftinTime;
        public InputAction @ShiftinPlace => m_Wrapper.m_Selection_ShiftinPlace;
        public InputActionMap Get() { return m_Wrapper.m_Selection; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(SelectionActions set) { return set.Get(); }
        public void SetCallbacks(ISelectionActions instance)
        {
            if (m_Wrapper.m_SelectionActionsCallbackInterface != null)
            {
                @DeselectAll.started -= m_Wrapper.m_SelectionActionsCallbackInterface.OnDeselectAll;
                @DeselectAll.performed -= m_Wrapper.m_SelectionActionsCallbackInterface.OnDeselectAll;
                @DeselectAll.canceled -= m_Wrapper.m_SelectionActionsCallbackInterface.OnDeselectAll;
                @Paste.started -= m_Wrapper.m_SelectionActionsCallbackInterface.OnPaste;
                @Paste.performed -= m_Wrapper.m_SelectionActionsCallbackInterface.OnPaste;
                @Paste.canceled -= m_Wrapper.m_SelectionActionsCallbackInterface.OnPaste;
                @DeleteObjects.started -= m_Wrapper.m_SelectionActionsCallbackInterface.OnDeleteObjects;
                @DeleteObjects.performed -= m_Wrapper.m_SelectionActionsCallbackInterface.OnDeleteObjects;
                @DeleteObjects.canceled -= m_Wrapper.m_SelectionActionsCallbackInterface.OnDeleteObjects;
                @Copy.started -= m_Wrapper.m_SelectionActionsCallbackInterface.OnCopy;
                @Copy.performed -= m_Wrapper.m_SelectionActionsCallbackInterface.OnCopy;
                @Copy.canceled -= m_Wrapper.m_SelectionActionsCallbackInterface.OnCopy;
                @Cut.started -= m_Wrapper.m_SelectionActionsCallbackInterface.OnCut;
                @Cut.performed -= m_Wrapper.m_SelectionActionsCallbackInterface.OnCut;
                @Cut.canceled -= m_Wrapper.m_SelectionActionsCallbackInterface.OnCut;
                @ShiftinTime.started -= m_Wrapper.m_SelectionActionsCallbackInterface.OnShiftinTime;
                @ShiftinTime.performed -= m_Wrapper.m_SelectionActionsCallbackInterface.OnShiftinTime;
                @ShiftinTime.canceled -= m_Wrapper.m_SelectionActionsCallbackInterface.OnShiftinTime;
                @ShiftinPlace.started -= m_Wrapper.m_SelectionActionsCallbackInterface.OnShiftinPlace;
                @ShiftinPlace.performed -= m_Wrapper.m_SelectionActionsCallbackInterface.OnShiftinPlace;
                @ShiftinPlace.canceled -= m_Wrapper.m_SelectionActionsCallbackInterface.OnShiftinPlace;
            }
            m_Wrapper.m_SelectionActionsCallbackInterface = instance;
            if (instance != null)
            {
                @DeselectAll.started += instance.OnDeselectAll;
                @DeselectAll.performed += instance.OnDeselectAll;
                @DeselectAll.canceled += instance.OnDeselectAll;
                @Paste.started += instance.OnPaste;
                @Paste.performed += instance.OnPaste;
                @Paste.canceled += instance.OnPaste;
                @DeleteObjects.started += instance.OnDeleteObjects;
                @DeleteObjects.performed += instance.OnDeleteObjects;
                @DeleteObjects.canceled += instance.OnDeleteObjects;
                @Copy.started += instance.OnCopy;
                @Copy.performed += instance.OnCopy;
                @Copy.canceled += instance.OnCopy;
                @Cut.started += instance.OnCut;
                @Cut.performed += instance.OnCut;
                @Cut.canceled += instance.OnCut;
                @ShiftinTime.started += instance.OnShiftinTime;
                @ShiftinTime.performed += instance.OnShiftinTime;
                @ShiftinTime.canceled += instance.OnShiftinTime;
                @ShiftinPlace.started += instance.OnShiftinPlace;
                @ShiftinPlace.performed += instance.OnShiftinPlace;
                @ShiftinPlace.canceled += instance.OnShiftinPlace;
            }
        }
    }
    public SelectionActions @Selection => new SelectionActions(this);
    private int m_KeyboardandMouseSchemeIndex = -1;
    public InputControlScheme KeyboardandMouseScheme
    {
        get
        {
            if (m_KeyboardandMouseSchemeIndex == -1) m_KeyboardandMouseSchemeIndex = asset.FindControlSchemeIndex("Keyboard and Mouse");
            return asset.controlSchemes[m_KeyboardandMouseSchemeIndex];
        }
    }
    public interface ICameraActions
    {
        void OnHoldtoMoveCamera(InputAction.CallbackContext context);
        void OnMoveCamera(InputAction.CallbackContext context);
        void OnRotateCamera(InputAction.CallbackContext context);
        void OnElevateCamera(InputAction.CallbackContext context);
        void OnAttachtoNoteGrid(InputAction.CallbackContext context);
    }
    public interface IUtilsActions
    {
        void OnControlModifier(InputAction.CallbackContext context);
        void OnAltModifier(InputAction.CallbackContext context);
        void OnShiftModifier(InputAction.CallbackContext context);
    }
    public interface ISelectionActions
    {
        void OnDeselectAll(InputAction.CallbackContext context);
        void OnPaste(InputAction.CallbackContext context);
        void OnDeleteObjects(InputAction.CallbackContext context);
        void OnCopy(InputAction.CallbackContext context);
        void OnCut(InputAction.CallbackContext context);
        void OnShiftinTime(InputAction.CallbackContext context);
        void OnShiftinPlace(InputAction.CallbackContext context);
    }
}
