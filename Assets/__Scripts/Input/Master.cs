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
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Paste"",
                    ""type"": ""Button"",
                    ""id"": ""e19d30e7-c044-49c7-b606-bcc91b42ad88"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Delete Objects"",
                    ""type"": ""Button"",
                    ""id"": ""f2c276f8-89bc-4baf-a952-5a39b8fbbac6"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Copy"",
                    ""type"": ""Button"",
                    ""id"": ""f1bf6ebd-cb31-4847-bd48-3d4a3376aa06"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Cut"",
                    ""type"": ""Button"",
                    ""id"": ""5f19c790-369e-4d34-9cc1-469a0d538087"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Shift in Time"",
                    ""type"": ""Button"",
                    ""id"": ""44cf8d3a-44b2-41b0-bc98-60a5752feb17"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Shift in Place"",
                    ""type"": ""Button"",
                    ""id"": ""44843eff-de98-49e6-b6df-3d3024541047"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
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
        },
        {
            ""name"": ""Actions"",
            ""id"": ""ad57992b-d8ff-4c39-a762-db68cf7d04db"",
            ""actions"": [
                {
                    ""name"": ""Undo"",
                    ""type"": ""Button"",
                    ""id"": ""fedad900-9c47-459d-bddd-f46d2be3e180"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Redo"",
                    ""type"": ""Button"",
                    ""id"": ""7d4b7471-e9f7-4b02-86f8-93f38fad7792"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard Method 1"",
                    ""id"": ""63fba69a-8406-46a8-ab67-b0dcb07b0e85"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Undo"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""3e22674a-5051-463b-bcba-5a248fa9a5fa"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Undo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""06c530a9-a274-47b8-ac73-f9ed8563d580"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Undo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard Method 2"",
                    ""id"": ""8d2a0118-7d90-404b-bc4b-7e75c6c16e77"",
                    ""path"": ""ButtonWithTwoModifiers"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Undo"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier1"",
                    ""id"": ""c820fc98-a631-4ba2-8872-cd9483499893"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Undo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""modifier2"",
                    ""id"": ""5a28b45b-80a4-42a5-83e5-ac4f005fa771"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Undo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""c1ddbbb8-0a6d-4c3a-8669-a50557a09c5c"",
                    ""path"": ""<Keyboard>/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Undo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""a52c3108-e4d8-4b89-9ee2-4a4c4667b94b"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Redo"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""9aa308dc-6d77-4fc9-a3e2-4bc7d435d5df"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Redo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""9da5084e-053a-4f50-9426-ad006cf4a589"",
                    ""path"": ""<Keyboard>/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Redo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard Method 2"",
                    ""id"": ""be16ab06-06ea-4ff7-b65a-6992a3b7f241"",
                    ""path"": ""ButtonWithTwoModifiers"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Redo"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier1"",
                    ""id"": ""8a3596a9-27e1-400e-ace9-b86b372e4bf0"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Redo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""modifier2"",
                    ""id"": ""a1c11c18-f1aa-4aed-9001-fdc2fe444b63"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Redo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""bc950e09-863a-4000-8e9b-d378889d7124"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Redo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Placement Controllers"",
            ""id"": ""55b78d3d-8ced-467c-a88f-d5cf50532d2e"",
            ""actions"": [
                {
                    ""name"": ""Place Object"",
                    ""type"": ""Button"",
                    ""id"": ""12ac2167-b8c0-4f4f-aa6e-17a1aacc42ca"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Initiate Click and Drag"",
                    ""type"": ""Button"",
                    ""id"": ""15cbdd9a-b2fe-489a-b70c-81e0261ba8b8"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Mouse Position Update"",
                    ""type"": ""Button"",
                    ""id"": ""3c3cb17e-12c8-41c6-b727-8f8f86c0f325"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Cancel Placement"",
                    ""type"": ""Button"",
                    ""id"": ""624a182f-d4ee-4fea-8f72-8886951b0688"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""5972be07-cf3b-44d3-861b-c80e8d655778"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Place Object"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""21742f4d-6389-49c4-85b0-4446e428b468"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Initiate Click and Drag"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""955b0c04-2b09-4a08-a154-6c5b86083f1a"",
                    ""path"": ""<Keyboard>/alt"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Initiate Click and Drag"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""0f79b592-c40a-47fc-9b0d-9d5244fbd4a0"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Initiate Click and Drag"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""ca96ac27-5948-4b00-83e4-0acba376a448"",
                    ""path"": ""<Pointer>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Mouse Position Update"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a8e331b6-b2c5-458d-8af7-060ec12eff7d"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Cancel Placement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Note Placement"",
            ""id"": ""d0462ea9-c717-4662-b004-e7b66e957cc9"",
            ""actions"": [
                {
                    ""name"": ""Down Note"",
                    ""type"": ""Button"",
                    ""id"": ""91d9fcf9-57ed-4bfa-a024-bd388f56a3f2"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Down Left Note"",
                    ""type"": ""Button"",
                    ""id"": ""8fbe6c94-2203-4a37-b5af-ac6a3d0d865a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Left Note"",
                    ""type"": ""Button"",
                    ""id"": ""ea80fd88-1337-4dbd-a7a9-9f1bdd9ecae4"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Up Left Note"",
                    ""type"": ""Button"",
                    ""id"": ""4fb66513-6490-4cfd-9770-4367629165c0"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Up Note"",
                    ""type"": ""Button"",
                    ""id"": ""16488a28-3686-46ed-9637-285b7e5a9e72"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Up Right Note"",
                    ""type"": ""Button"",
                    ""id"": ""834a0bab-858e-4689-bc19-73dec512abd7"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Right Note"",
                    ""type"": ""Button"",
                    ""id"": ""e63abaea-687c-486a-aee9-133901bc362c"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Down Right Note"",
                    ""type"": ""Button"",
                    ""id"": ""66b5d19d-e3ee-4b64-9803-7c938bbd98be"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""752ce1da-9d61-4779-b6ed-75d0d214292a"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Down Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""9ebfa5ba-e709-4e80-b691-b185b1d30aeb"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Down Left Note"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""89baa2c4-682e-4727-8612-e6c4890282a2"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Down Left Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""863befc4-986f-42f9-966f-3f20a59e11ee"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Down Left Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""d8232af9-72c8-46d6-bb23-023443cbf276"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Left Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""5a2f2914-9171-4a3e-aa31-b2461d1154b1"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Up Left Note"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""a3f3d9f9-c3b3-4aa5-b5e5-5180e389fe97"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Up Left Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""809f08a3-0d6e-4d5a-a84c-3ec73aa555fd"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Up Left Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""e8e63a70-7c85-4205-8648-1b10ca6aa84b"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Up Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""fe1a5e46-8da3-4820-8efd-c252cfb0f9d7"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Up Right Note"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""5382f134-7347-4ff7-bcb8-7465dab2cf29"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Up Right Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""4113b9b1-5048-460b-aa40-4d1ad2d6f7d6"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Up Right Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""a24d5b1b-8b2b-4978-84e8-1257904d2017"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Right Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""f5c89345-5acb-4bb5-9705-12466f97505a"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Down Right Note"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""a67a30aa-5408-4478-9d7d-358a3333d945"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Down Right Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""1409e043-6077-4ebb-bc99-816e88b43eef"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Down Right Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Event Placement"",
            ""id"": ""62377dd3-26a7-4161-ab65-7a5042f0dfc0"",
            ""actions"": [
                {
                    ""name"": ""Toggle Ring Propagation"",
                    ""type"": ""Button"",
                    ""id"": ""811cb2d3-e66b-4ad3-a7c5-45686d9617ba"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Add 15 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""51596ba1-f128-48c4-b563-b4553f121a1e"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Add 30 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""cb255ff2-9a90-44fd-a139-433a56a04612"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Add 45 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""2aa190df-15aa-4507-a947-7f4aaa7175bc"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Add 60 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""666291dd-9d51-4f5d-87a2-8ce834a7779c"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Subtract 15 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""ad4148e3-431e-4b56-81ab-e45633055506"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Subtract 30 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""180f7a88-8f16-45a9-a7b3-db128f534e51"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Subtract 45 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""953c3c37-6418-426f-abe1-53b14578c794"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: Subtract 60 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""940ba726-e71a-4cd6-8090-43a5d20b35cc"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""0bbc2f7a-cd64-44cb-a76e-384f29b26cd1"",
                    ""path"": ""<Keyboard>/p"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Toggle Ring Propagation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c7ad07e7-8790-452a-8bdc-a301fedaff56"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Add 15 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bc17f8d9-7868-4557-8ca6-3acd787acd43"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Add 30 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d4b70180-b3d4-47af-ae16-dda507b87380"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Add 45 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d749c574-a404-4087-a71b-b38850d3db76"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Add 60 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""983c2896-a3a5-404c-b7f9-a90920412585"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 15 Degrees"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""43f151ab-1277-41a8-bbb0-dd0b0fa61f5f"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 15 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""bd1e1fd1-8a7b-4924-a4fb-a13808ed4bfe"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 15 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""8555e3af-90b4-4188-b683-246d57618243"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 30 Degrees"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""70fbac56-6b06-4ad2-9adf-f987fbcb3078"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 30 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""9d6c7446-3e93-49fa-95a3-e3e09822d5dd"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 30 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""25c84771-1d6e-4ebd-9d2e-ddaba8832282"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 45 Degrees"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""d1e6bccc-6b57-4845-b3d0-0ee5b1d6547d"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 45 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""b379faf2-3783-498c-9c0a-97e4c37afe98"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 45 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""07728d7a-8a48-4e88-8b08-16cb3718cb98"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 60 Degrees"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""a66c57e6-03fb-40df-8fa0-bd792ea3106e"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 60 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""94ce08ca-63e6-45a0-9ca3-63008a177233"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotation: Subtract 60 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Workflows"",
            ""id"": ""5a91a51c-40c9-4e18-bff6-3c5204b446ce"",
            ""actions"": [
                {
                    ""name"": ""Change Workflows"",
                    ""type"": ""Button"",
                    ""id"": ""1bc86c2e-a434-46ba-a207-5dff13f2f156"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Update Swing Arc Visualizer"",
                    ""type"": ""Button"",
                    ""id"": ""5eb56c46-9493-4c8b-978c-195255f0f76d"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Place Red Note or Event"",
                    ""type"": ""Button"",
                    ""id"": ""7c3ce073-947a-41d5-900e-0afddf733685"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Place Blue Note or Event"",
                    ""type"": ""Button"",
                    ""id"": ""15a1d710-c76f-42b5-81fd-53dc8bde4712"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Place Bomb"",
                    ""type"": ""Button"",
                    ""id"": ""15214433-d512-4df1-aec5-3c973cef11d7"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Place Obstacle"",
                    ""type"": ""Button"",
                    ""id"": ""17d2e577-2bd7-44f2-95ad-81ce299fc218"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Toggle Delete Tool"",
                    ""type"": ""Button"",
                    ""id"": ""093f99e8-5a4f-4d76-aa7d-cce4ecfe95f1"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""95d3f60d-f8b3-4e6f-84db-ac49d290ea5d"",
                    ""path"": ""<Keyboard>/tab"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Change Workflows"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6905e695-0fae-4d51-8697-c5c164a8158e"",
                    ""path"": ""<Keyboard>/v"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Update Swing Arc Visualizer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3554826a-1869-442a-9a69-1292f35c532f"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Place Red Note or Event"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8996b934-950d-4fcf-9c11-35c09605e946"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Place Blue Note or Event"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""087476d7-5798-4db2-b4ed-15d0951c095c"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Place Bomb"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""62b1eb0b-5a29-47e6-820e-952771708ad1"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Place Obstacle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5dc2e98d-6185-400c-b701-95aad571a194"",
                    ""path"": ""<Keyboard>/5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Toggle Delete Tool"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Event UI"",
            ""id"": ""a07ff948-83f1-461b-988e-d6c1d9e6aadc"",
            ""actions"": [
                {
                    ""name"": ""Type On"",
                    ""type"": ""Button"",
                    ""id"": ""8f441098-22d1-491e-bb41-46164b9694c0"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Type Flash"",
                    ""type"": ""Button"",
                    ""id"": ""35cfcf0a-ccbf-4566-9b8e-1679430fd208"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Type Off"",
                    ""type"": ""Button"",
                    ""id"": ""2fd3f72d-db72-4f1d-bc42-ac9c9bc86a64"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Type Fade"",
                    ""type"": ""Button"",
                    ""id"": ""a417adc0-d77b-4879-8069-94d24fa7fd06"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Toggle Precision Rotation"",
                    ""type"": ""Button"",
                    ""id"": ""b8d7a87e-0bba-4071-9b9c-2591b84ff365"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""a0aa5d99-d13d-4188-94b4-deeb5ced5d3f"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Type On"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2db0903c-cdec-4867-8bee-d38a9d2c15d4"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Type Flash"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1bdca414-a179-461b-bfa2-83ac7e2bfc43"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Type Off"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9d83002a-3294-4c33-be88-e22112d9ea33"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Type Fade"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2a39437a-13b1-456f-b64d-5f77a883092c"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Toggle Precision Rotation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Saving"",
            ""id"": ""358e632a-614c-4580-adb7-64e863720b71"",
            ""actions"": [
                {
                    ""name"": ""Save"",
                    ""type"": ""Button"",
                    ""id"": ""5bd99bac-a957-4d98-b68e-e74a153090fe"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""6328c205-5a07-458b-8173-a7c982608950"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Save"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""9ffd022a-7d16-492a-9f73-ddbbcd2c484d"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Save"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""e072a367-7f62-4b49-9f0f-f39474fabfb8"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Save"",
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
        // Actions
        m_Actions = asset.FindActionMap("Actions", throwIfNotFound: true);
        m_Actions_Undo = m_Actions.FindAction("Undo", throwIfNotFound: true);
        m_Actions_Redo = m_Actions.FindAction("Redo", throwIfNotFound: true);
        // Placement Controllers
        m_PlacementControllers = asset.FindActionMap("Placement Controllers", throwIfNotFound: true);
        m_PlacementControllers_PlaceObject = m_PlacementControllers.FindAction("Place Object", throwIfNotFound: true);
        m_PlacementControllers_InitiateClickandDrag = m_PlacementControllers.FindAction("Initiate Click and Drag", throwIfNotFound: true);
        m_PlacementControllers_MousePositionUpdate = m_PlacementControllers.FindAction("Mouse Position Update", throwIfNotFound: true);
        m_PlacementControllers_CancelPlacement = m_PlacementControllers.FindAction("Cancel Placement", throwIfNotFound: true);
        // Note Placement
        m_NotePlacement = asset.FindActionMap("Note Placement", throwIfNotFound: true);
        m_NotePlacement_DownNote = m_NotePlacement.FindAction("Down Note", throwIfNotFound: true);
        m_NotePlacement_DownLeftNote = m_NotePlacement.FindAction("Down Left Note", throwIfNotFound: true);
        m_NotePlacement_LeftNote = m_NotePlacement.FindAction("Left Note", throwIfNotFound: true);
        m_NotePlacement_UpLeftNote = m_NotePlacement.FindAction("Up Left Note", throwIfNotFound: true);
        m_NotePlacement_UpNote = m_NotePlacement.FindAction("Up Note", throwIfNotFound: true);
        m_NotePlacement_UpRightNote = m_NotePlacement.FindAction("Up Right Note", throwIfNotFound: true);
        m_NotePlacement_RightNote = m_NotePlacement.FindAction("Right Note", throwIfNotFound: true);
        m_NotePlacement_DownRightNote = m_NotePlacement.FindAction("Down Right Note", throwIfNotFound: true);
        // Event Placement
        m_EventPlacement = asset.FindActionMap("Event Placement", throwIfNotFound: true);
        m_EventPlacement_ToggleRingPropagation = m_EventPlacement.FindAction("Toggle Ring Propagation", throwIfNotFound: true);
        m_EventPlacement_RotationAdd15Degrees = m_EventPlacement.FindAction("Rotation: Add 15 Degrees", throwIfNotFound: true);
        m_EventPlacement_RotationAdd30Degrees = m_EventPlacement.FindAction("Rotation: Add 30 Degrees", throwIfNotFound: true);
        m_EventPlacement_RotationAdd45Degrees = m_EventPlacement.FindAction("Rotation: Add 45 Degrees", throwIfNotFound: true);
        m_EventPlacement_RotationAdd60Degrees = m_EventPlacement.FindAction("Rotation: Add 60 Degrees", throwIfNotFound: true);
        m_EventPlacement_RotationSubtract15Degrees = m_EventPlacement.FindAction("Rotation: Subtract 15 Degrees", throwIfNotFound: true);
        m_EventPlacement_RotationSubtract30Degrees = m_EventPlacement.FindAction("Rotation: Subtract 30 Degrees", throwIfNotFound: true);
        m_EventPlacement_RotationSubtract45Degrees = m_EventPlacement.FindAction("Rotation: Subtract 45 Degrees", throwIfNotFound: true);
        m_EventPlacement_RotationSubtract60Degrees = m_EventPlacement.FindAction("Rotation: Subtract 60 Degrees", throwIfNotFound: true);
        // Workflows
        m_Workflows = asset.FindActionMap("Workflows", throwIfNotFound: true);
        m_Workflows_ChangeWorkflows = m_Workflows.FindAction("Change Workflows", throwIfNotFound: true);
        m_Workflows_UpdateSwingArcVisualizer = m_Workflows.FindAction("Update Swing Arc Visualizer", throwIfNotFound: true);
        m_Workflows_PlaceRedNoteorEvent = m_Workflows.FindAction("Place Red Note or Event", throwIfNotFound: true);
        m_Workflows_PlaceBlueNoteorEvent = m_Workflows.FindAction("Place Blue Note or Event", throwIfNotFound: true);
        m_Workflows_PlaceBomb = m_Workflows.FindAction("Place Bomb", throwIfNotFound: true);
        m_Workflows_PlaceObstacle = m_Workflows.FindAction("Place Obstacle", throwIfNotFound: true);
        m_Workflows_ToggleDeleteTool = m_Workflows.FindAction("Toggle Delete Tool", throwIfNotFound: true);
        // Event UI
        m_EventUI = asset.FindActionMap("Event UI", throwIfNotFound: true);
        m_EventUI_TypeOn = m_EventUI.FindAction("Type On", throwIfNotFound: true);
        m_EventUI_TypeFlash = m_EventUI.FindAction("Type Flash", throwIfNotFound: true);
        m_EventUI_TypeOff = m_EventUI.FindAction("Type Off", throwIfNotFound: true);
        m_EventUI_TypeFade = m_EventUI.FindAction("Type Fade", throwIfNotFound: true);
        m_EventUI_TogglePrecisionRotation = m_EventUI.FindAction("Toggle Precision Rotation", throwIfNotFound: true);
        // Saving
        m_Saving = asset.FindActionMap("Saving", throwIfNotFound: true);
        m_Saving_Save = m_Saving.FindAction("Save", throwIfNotFound: true);
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

    // Actions
    private readonly InputActionMap m_Actions;
    private IActionsActions m_ActionsActionsCallbackInterface;
    private readonly InputAction m_Actions_Undo;
    private readonly InputAction m_Actions_Redo;
    public struct ActionsActions
    {
        private @CMInput m_Wrapper;
        public ActionsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @Undo => m_Wrapper.m_Actions_Undo;
        public InputAction @Redo => m_Wrapper.m_Actions_Redo;
        public InputActionMap Get() { return m_Wrapper.m_Actions; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(ActionsActions set) { return set.Get(); }
        public void SetCallbacks(IActionsActions instance)
        {
            if (m_Wrapper.m_ActionsActionsCallbackInterface != null)
            {
                @Undo.started -= m_Wrapper.m_ActionsActionsCallbackInterface.OnUndo;
                @Undo.performed -= m_Wrapper.m_ActionsActionsCallbackInterface.OnUndo;
                @Undo.canceled -= m_Wrapper.m_ActionsActionsCallbackInterface.OnUndo;
                @Redo.started -= m_Wrapper.m_ActionsActionsCallbackInterface.OnRedo;
                @Redo.performed -= m_Wrapper.m_ActionsActionsCallbackInterface.OnRedo;
                @Redo.canceled -= m_Wrapper.m_ActionsActionsCallbackInterface.OnRedo;
            }
            m_Wrapper.m_ActionsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Undo.started += instance.OnUndo;
                @Undo.performed += instance.OnUndo;
                @Undo.canceled += instance.OnUndo;
                @Redo.started += instance.OnRedo;
                @Redo.performed += instance.OnRedo;
                @Redo.canceled += instance.OnRedo;
            }
        }
    }
    public ActionsActions @Actions => new ActionsActions(this);

    // Placement Controllers
    private readonly InputActionMap m_PlacementControllers;
    private IPlacementControllersActions m_PlacementControllersActionsCallbackInterface;
    private readonly InputAction m_PlacementControllers_PlaceObject;
    private readonly InputAction m_PlacementControllers_InitiateClickandDrag;
    private readonly InputAction m_PlacementControllers_MousePositionUpdate;
    private readonly InputAction m_PlacementControllers_CancelPlacement;
    public struct PlacementControllersActions
    {
        private @CMInput m_Wrapper;
        public PlacementControllersActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @PlaceObject => m_Wrapper.m_PlacementControllers_PlaceObject;
        public InputAction @InitiateClickandDrag => m_Wrapper.m_PlacementControllers_InitiateClickandDrag;
        public InputAction @MousePositionUpdate => m_Wrapper.m_PlacementControllers_MousePositionUpdate;
        public InputAction @CancelPlacement => m_Wrapper.m_PlacementControllers_CancelPlacement;
        public InputActionMap Get() { return m_Wrapper.m_PlacementControllers; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlacementControllersActions set) { return set.Get(); }
        public void SetCallbacks(IPlacementControllersActions instance)
        {
            if (m_Wrapper.m_PlacementControllersActionsCallbackInterface != null)
            {
                @PlaceObject.started -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnPlaceObject;
                @PlaceObject.performed -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnPlaceObject;
                @PlaceObject.canceled -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnPlaceObject;
                @InitiateClickandDrag.started -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnInitiateClickandDrag;
                @InitiateClickandDrag.performed -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnInitiateClickandDrag;
                @InitiateClickandDrag.canceled -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnInitiateClickandDrag;
                @MousePositionUpdate.started -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnMousePositionUpdate;
                @MousePositionUpdate.performed -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnMousePositionUpdate;
                @MousePositionUpdate.canceled -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnMousePositionUpdate;
                @CancelPlacement.started -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnCancelPlacement;
                @CancelPlacement.performed -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnCancelPlacement;
                @CancelPlacement.canceled -= m_Wrapper.m_PlacementControllersActionsCallbackInterface.OnCancelPlacement;
            }
            m_Wrapper.m_PlacementControllersActionsCallbackInterface = instance;
            if (instance != null)
            {
                @PlaceObject.started += instance.OnPlaceObject;
                @PlaceObject.performed += instance.OnPlaceObject;
                @PlaceObject.canceled += instance.OnPlaceObject;
                @InitiateClickandDrag.started += instance.OnInitiateClickandDrag;
                @InitiateClickandDrag.performed += instance.OnInitiateClickandDrag;
                @InitiateClickandDrag.canceled += instance.OnInitiateClickandDrag;
                @MousePositionUpdate.started += instance.OnMousePositionUpdate;
                @MousePositionUpdate.performed += instance.OnMousePositionUpdate;
                @MousePositionUpdate.canceled += instance.OnMousePositionUpdate;
                @CancelPlacement.started += instance.OnCancelPlacement;
                @CancelPlacement.performed += instance.OnCancelPlacement;
                @CancelPlacement.canceled += instance.OnCancelPlacement;
            }
        }
    }
    public PlacementControllersActions @PlacementControllers => new PlacementControllersActions(this);

    // Note Placement
    private readonly InputActionMap m_NotePlacement;
    private INotePlacementActions m_NotePlacementActionsCallbackInterface;
    private readonly InputAction m_NotePlacement_DownNote;
    private readonly InputAction m_NotePlacement_DownLeftNote;
    private readonly InputAction m_NotePlacement_LeftNote;
    private readonly InputAction m_NotePlacement_UpLeftNote;
    private readonly InputAction m_NotePlacement_UpNote;
    private readonly InputAction m_NotePlacement_UpRightNote;
    private readonly InputAction m_NotePlacement_RightNote;
    private readonly InputAction m_NotePlacement_DownRightNote;
    public struct NotePlacementActions
    {
        private @CMInput m_Wrapper;
        public NotePlacementActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @DownNote => m_Wrapper.m_NotePlacement_DownNote;
        public InputAction @DownLeftNote => m_Wrapper.m_NotePlacement_DownLeftNote;
        public InputAction @LeftNote => m_Wrapper.m_NotePlacement_LeftNote;
        public InputAction @UpLeftNote => m_Wrapper.m_NotePlacement_UpLeftNote;
        public InputAction @UpNote => m_Wrapper.m_NotePlacement_UpNote;
        public InputAction @UpRightNote => m_Wrapper.m_NotePlacement_UpRightNote;
        public InputAction @RightNote => m_Wrapper.m_NotePlacement_RightNote;
        public InputAction @DownRightNote => m_Wrapper.m_NotePlacement_DownRightNote;
        public InputActionMap Get() { return m_Wrapper.m_NotePlacement; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(NotePlacementActions set) { return set.Get(); }
        public void SetCallbacks(INotePlacementActions instance)
        {
            if (m_Wrapper.m_NotePlacementActionsCallbackInterface != null)
            {
                @DownNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownNote;
                @DownNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownNote;
                @DownNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownNote;
                @DownLeftNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownLeftNote;
                @DownLeftNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownLeftNote;
                @DownLeftNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownLeftNote;
                @LeftNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnLeftNote;
                @LeftNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnLeftNote;
                @LeftNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnLeftNote;
                @UpLeftNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpLeftNote;
                @UpLeftNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpLeftNote;
                @UpLeftNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpLeftNote;
                @UpNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpNote;
                @UpNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpNote;
                @UpNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpNote;
                @UpRightNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpRightNote;
                @UpRightNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpRightNote;
                @UpRightNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpRightNote;
                @RightNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnRightNote;
                @RightNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnRightNote;
                @RightNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnRightNote;
                @DownRightNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownRightNote;
                @DownRightNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownRightNote;
                @DownRightNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDownRightNote;
            }
            m_Wrapper.m_NotePlacementActionsCallbackInterface = instance;
            if (instance != null)
            {
                @DownNote.started += instance.OnDownNote;
                @DownNote.performed += instance.OnDownNote;
                @DownNote.canceled += instance.OnDownNote;
                @DownLeftNote.started += instance.OnDownLeftNote;
                @DownLeftNote.performed += instance.OnDownLeftNote;
                @DownLeftNote.canceled += instance.OnDownLeftNote;
                @LeftNote.started += instance.OnLeftNote;
                @LeftNote.performed += instance.OnLeftNote;
                @LeftNote.canceled += instance.OnLeftNote;
                @UpLeftNote.started += instance.OnUpLeftNote;
                @UpLeftNote.performed += instance.OnUpLeftNote;
                @UpLeftNote.canceled += instance.OnUpLeftNote;
                @UpNote.started += instance.OnUpNote;
                @UpNote.performed += instance.OnUpNote;
                @UpNote.canceled += instance.OnUpNote;
                @UpRightNote.started += instance.OnUpRightNote;
                @UpRightNote.performed += instance.OnUpRightNote;
                @UpRightNote.canceled += instance.OnUpRightNote;
                @RightNote.started += instance.OnRightNote;
                @RightNote.performed += instance.OnRightNote;
                @RightNote.canceled += instance.OnRightNote;
                @DownRightNote.started += instance.OnDownRightNote;
                @DownRightNote.performed += instance.OnDownRightNote;
                @DownRightNote.canceled += instance.OnDownRightNote;
            }
        }
    }
    public NotePlacementActions @NotePlacement => new NotePlacementActions(this);

    // Event Placement
    private readonly InputActionMap m_EventPlacement;
    private IEventPlacementActions m_EventPlacementActionsCallbackInterface;
    private readonly InputAction m_EventPlacement_ToggleRingPropagation;
    private readonly InputAction m_EventPlacement_RotationAdd15Degrees;
    private readonly InputAction m_EventPlacement_RotationAdd30Degrees;
    private readonly InputAction m_EventPlacement_RotationAdd45Degrees;
    private readonly InputAction m_EventPlacement_RotationAdd60Degrees;
    private readonly InputAction m_EventPlacement_RotationSubtract15Degrees;
    private readonly InputAction m_EventPlacement_RotationSubtract30Degrees;
    private readonly InputAction m_EventPlacement_RotationSubtract45Degrees;
    private readonly InputAction m_EventPlacement_RotationSubtract60Degrees;
    public struct EventPlacementActions
    {
        private @CMInput m_Wrapper;
        public EventPlacementActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleRingPropagation => m_Wrapper.m_EventPlacement_ToggleRingPropagation;
        public InputAction @RotationAdd15Degrees => m_Wrapper.m_EventPlacement_RotationAdd15Degrees;
        public InputAction @RotationAdd30Degrees => m_Wrapper.m_EventPlacement_RotationAdd30Degrees;
        public InputAction @RotationAdd45Degrees => m_Wrapper.m_EventPlacement_RotationAdd45Degrees;
        public InputAction @RotationAdd60Degrees => m_Wrapper.m_EventPlacement_RotationAdd60Degrees;
        public InputAction @RotationSubtract15Degrees => m_Wrapper.m_EventPlacement_RotationSubtract15Degrees;
        public InputAction @RotationSubtract30Degrees => m_Wrapper.m_EventPlacement_RotationSubtract30Degrees;
        public InputAction @RotationSubtract45Degrees => m_Wrapper.m_EventPlacement_RotationSubtract45Degrees;
        public InputAction @RotationSubtract60Degrees => m_Wrapper.m_EventPlacement_RotationSubtract60Degrees;
        public InputActionMap Get() { return m_Wrapper.m_EventPlacement; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(EventPlacementActions set) { return set.Get(); }
        public void SetCallbacks(IEventPlacementActions instance)
        {
            if (m_Wrapper.m_EventPlacementActionsCallbackInterface != null)
            {
                @ToggleRingPropagation.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnToggleRingPropagation;
                @ToggleRingPropagation.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnToggleRingPropagation;
                @ToggleRingPropagation.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnToggleRingPropagation;
                @RotationAdd15Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd15Degrees;
                @RotationAdd15Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd15Degrees;
                @RotationAdd15Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd15Degrees;
                @RotationAdd30Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd30Degrees;
                @RotationAdd30Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd30Degrees;
                @RotationAdd30Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd30Degrees;
                @RotationAdd45Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd45Degrees;
                @RotationAdd45Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd45Degrees;
                @RotationAdd45Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd45Degrees;
                @RotationAdd60Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd60Degrees;
                @RotationAdd60Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd60Degrees;
                @RotationAdd60Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationAdd60Degrees;
                @RotationSubtract15Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract15Degrees;
                @RotationSubtract15Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract15Degrees;
                @RotationSubtract15Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract15Degrees;
                @RotationSubtract30Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract30Degrees;
                @RotationSubtract30Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract30Degrees;
                @RotationSubtract30Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract30Degrees;
                @RotationSubtract45Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract45Degrees;
                @RotationSubtract45Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract45Degrees;
                @RotationSubtract45Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract45Degrees;
                @RotationSubtract60Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract60Degrees;
                @RotationSubtract60Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract60Degrees;
                @RotationSubtract60Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotationSubtract60Degrees;
            }
            m_Wrapper.m_EventPlacementActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ToggleRingPropagation.started += instance.OnToggleRingPropagation;
                @ToggleRingPropagation.performed += instance.OnToggleRingPropagation;
                @ToggleRingPropagation.canceled += instance.OnToggleRingPropagation;
                @RotationAdd15Degrees.started += instance.OnRotationAdd15Degrees;
                @RotationAdd15Degrees.performed += instance.OnRotationAdd15Degrees;
                @RotationAdd15Degrees.canceled += instance.OnRotationAdd15Degrees;
                @RotationAdd30Degrees.started += instance.OnRotationAdd30Degrees;
                @RotationAdd30Degrees.performed += instance.OnRotationAdd30Degrees;
                @RotationAdd30Degrees.canceled += instance.OnRotationAdd30Degrees;
                @RotationAdd45Degrees.started += instance.OnRotationAdd45Degrees;
                @RotationAdd45Degrees.performed += instance.OnRotationAdd45Degrees;
                @RotationAdd45Degrees.canceled += instance.OnRotationAdd45Degrees;
                @RotationAdd60Degrees.started += instance.OnRotationAdd60Degrees;
                @RotationAdd60Degrees.performed += instance.OnRotationAdd60Degrees;
                @RotationAdd60Degrees.canceled += instance.OnRotationAdd60Degrees;
                @RotationSubtract15Degrees.started += instance.OnRotationSubtract15Degrees;
                @RotationSubtract15Degrees.performed += instance.OnRotationSubtract15Degrees;
                @RotationSubtract15Degrees.canceled += instance.OnRotationSubtract15Degrees;
                @RotationSubtract30Degrees.started += instance.OnRotationSubtract30Degrees;
                @RotationSubtract30Degrees.performed += instance.OnRotationSubtract30Degrees;
                @RotationSubtract30Degrees.canceled += instance.OnRotationSubtract30Degrees;
                @RotationSubtract45Degrees.started += instance.OnRotationSubtract45Degrees;
                @RotationSubtract45Degrees.performed += instance.OnRotationSubtract45Degrees;
                @RotationSubtract45Degrees.canceled += instance.OnRotationSubtract45Degrees;
                @RotationSubtract60Degrees.started += instance.OnRotationSubtract60Degrees;
                @RotationSubtract60Degrees.performed += instance.OnRotationSubtract60Degrees;
                @RotationSubtract60Degrees.canceled += instance.OnRotationSubtract60Degrees;
            }
        }
    }
    public EventPlacementActions @EventPlacement => new EventPlacementActions(this);

    // Workflows
    private readonly InputActionMap m_Workflows;
    private IWorkflowsActions m_WorkflowsActionsCallbackInterface;
    private readonly InputAction m_Workflows_ChangeWorkflows;
    private readonly InputAction m_Workflows_UpdateSwingArcVisualizer;
    private readonly InputAction m_Workflows_PlaceRedNoteorEvent;
    private readonly InputAction m_Workflows_PlaceBlueNoteorEvent;
    private readonly InputAction m_Workflows_PlaceBomb;
    private readonly InputAction m_Workflows_PlaceObstacle;
    private readonly InputAction m_Workflows_ToggleDeleteTool;
    public struct WorkflowsActions
    {
        private @CMInput m_Wrapper;
        public WorkflowsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ChangeWorkflows => m_Wrapper.m_Workflows_ChangeWorkflows;
        public InputAction @UpdateSwingArcVisualizer => m_Wrapper.m_Workflows_UpdateSwingArcVisualizer;
        public InputAction @PlaceRedNoteorEvent => m_Wrapper.m_Workflows_PlaceRedNoteorEvent;
        public InputAction @PlaceBlueNoteorEvent => m_Wrapper.m_Workflows_PlaceBlueNoteorEvent;
        public InputAction @PlaceBomb => m_Wrapper.m_Workflows_PlaceBomb;
        public InputAction @PlaceObstacle => m_Wrapper.m_Workflows_PlaceObstacle;
        public InputAction @ToggleDeleteTool => m_Wrapper.m_Workflows_ToggleDeleteTool;
        public InputActionMap Get() { return m_Wrapper.m_Workflows; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(WorkflowsActions set) { return set.Get(); }
        public void SetCallbacks(IWorkflowsActions instance)
        {
            if (m_Wrapper.m_WorkflowsActionsCallbackInterface != null)
            {
                @ChangeWorkflows.started -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnChangeWorkflows;
                @ChangeWorkflows.performed -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnChangeWorkflows;
                @ChangeWorkflows.canceled -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnChangeWorkflows;
                @UpdateSwingArcVisualizer.started -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnUpdateSwingArcVisualizer;
                @UpdateSwingArcVisualizer.performed -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnUpdateSwingArcVisualizer;
                @UpdateSwingArcVisualizer.canceled -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnUpdateSwingArcVisualizer;
                @PlaceRedNoteorEvent.started -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceRedNoteorEvent;
                @PlaceRedNoteorEvent.performed -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceRedNoteorEvent;
                @PlaceRedNoteorEvent.canceled -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceRedNoteorEvent;
                @PlaceBlueNoteorEvent.started -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceBlueNoteorEvent;
                @PlaceBlueNoteorEvent.performed -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceBlueNoteorEvent;
                @PlaceBlueNoteorEvent.canceled -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceBlueNoteorEvent;
                @PlaceBomb.started -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceBomb;
                @PlaceBomb.performed -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceBomb;
                @PlaceBomb.canceled -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceBomb;
                @PlaceObstacle.started -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceObstacle;
                @PlaceObstacle.performed -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceObstacle;
                @PlaceObstacle.canceled -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnPlaceObstacle;
                @ToggleDeleteTool.started -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnToggleDeleteTool;
                @ToggleDeleteTool.performed -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnToggleDeleteTool;
                @ToggleDeleteTool.canceled -= m_Wrapper.m_WorkflowsActionsCallbackInterface.OnToggleDeleteTool;
            }
            m_Wrapper.m_WorkflowsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ChangeWorkflows.started += instance.OnChangeWorkflows;
                @ChangeWorkflows.performed += instance.OnChangeWorkflows;
                @ChangeWorkflows.canceled += instance.OnChangeWorkflows;
                @UpdateSwingArcVisualizer.started += instance.OnUpdateSwingArcVisualizer;
                @UpdateSwingArcVisualizer.performed += instance.OnUpdateSwingArcVisualizer;
                @UpdateSwingArcVisualizer.canceled += instance.OnUpdateSwingArcVisualizer;
                @PlaceRedNoteorEvent.started += instance.OnPlaceRedNoteorEvent;
                @PlaceRedNoteorEvent.performed += instance.OnPlaceRedNoteorEvent;
                @PlaceRedNoteorEvent.canceled += instance.OnPlaceRedNoteorEvent;
                @PlaceBlueNoteorEvent.started += instance.OnPlaceBlueNoteorEvent;
                @PlaceBlueNoteorEvent.performed += instance.OnPlaceBlueNoteorEvent;
                @PlaceBlueNoteorEvent.canceled += instance.OnPlaceBlueNoteorEvent;
                @PlaceBomb.started += instance.OnPlaceBomb;
                @PlaceBomb.performed += instance.OnPlaceBomb;
                @PlaceBomb.canceled += instance.OnPlaceBomb;
                @PlaceObstacle.started += instance.OnPlaceObstacle;
                @PlaceObstacle.performed += instance.OnPlaceObstacle;
                @PlaceObstacle.canceled += instance.OnPlaceObstacle;
                @ToggleDeleteTool.started += instance.OnToggleDeleteTool;
                @ToggleDeleteTool.performed += instance.OnToggleDeleteTool;
                @ToggleDeleteTool.canceled += instance.OnToggleDeleteTool;
            }
        }
    }
    public WorkflowsActions @Workflows => new WorkflowsActions(this);

    // Event UI
    private readonly InputActionMap m_EventUI;
    private IEventUIActions m_EventUIActionsCallbackInterface;
    private readonly InputAction m_EventUI_TypeOn;
    private readonly InputAction m_EventUI_TypeFlash;
    private readonly InputAction m_EventUI_TypeOff;
    private readonly InputAction m_EventUI_TypeFade;
    private readonly InputAction m_EventUI_TogglePrecisionRotation;
    public struct EventUIActions
    {
        private @CMInput m_Wrapper;
        public EventUIActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @TypeOn => m_Wrapper.m_EventUI_TypeOn;
        public InputAction @TypeFlash => m_Wrapper.m_EventUI_TypeFlash;
        public InputAction @TypeOff => m_Wrapper.m_EventUI_TypeOff;
        public InputAction @TypeFade => m_Wrapper.m_EventUI_TypeFade;
        public InputAction @TogglePrecisionRotation => m_Wrapper.m_EventUI_TogglePrecisionRotation;
        public InputActionMap Get() { return m_Wrapper.m_EventUI; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(EventUIActions set) { return set.Get(); }
        public void SetCallbacks(IEventUIActions instance)
        {
            if (m_Wrapper.m_EventUIActionsCallbackInterface != null)
            {
                @TypeOn.started -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeOn;
                @TypeOn.performed -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeOn;
                @TypeOn.canceled -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeOn;
                @TypeFlash.started -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeFlash;
                @TypeFlash.performed -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeFlash;
                @TypeFlash.canceled -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeFlash;
                @TypeOff.started -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeOff;
                @TypeOff.performed -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeOff;
                @TypeOff.canceled -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeOff;
                @TypeFade.started -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeFade;
                @TypeFade.performed -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeFade;
                @TypeFade.canceled -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTypeFade;
                @TogglePrecisionRotation.started -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTogglePrecisionRotation;
                @TogglePrecisionRotation.performed -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTogglePrecisionRotation;
                @TogglePrecisionRotation.canceled -= m_Wrapper.m_EventUIActionsCallbackInterface.OnTogglePrecisionRotation;
            }
            m_Wrapper.m_EventUIActionsCallbackInterface = instance;
            if (instance != null)
            {
                @TypeOn.started += instance.OnTypeOn;
                @TypeOn.performed += instance.OnTypeOn;
                @TypeOn.canceled += instance.OnTypeOn;
                @TypeFlash.started += instance.OnTypeFlash;
                @TypeFlash.performed += instance.OnTypeFlash;
                @TypeFlash.canceled += instance.OnTypeFlash;
                @TypeOff.started += instance.OnTypeOff;
                @TypeOff.performed += instance.OnTypeOff;
                @TypeOff.canceled += instance.OnTypeOff;
                @TypeFade.started += instance.OnTypeFade;
                @TypeFade.performed += instance.OnTypeFade;
                @TypeFade.canceled += instance.OnTypeFade;
                @TogglePrecisionRotation.started += instance.OnTogglePrecisionRotation;
                @TogglePrecisionRotation.performed += instance.OnTogglePrecisionRotation;
                @TogglePrecisionRotation.canceled += instance.OnTogglePrecisionRotation;
            }
        }
    }
    public EventUIActions @EventUI => new EventUIActions(this);

    // Saving
    private readonly InputActionMap m_Saving;
    private ISavingActions m_SavingActionsCallbackInterface;
    private readonly InputAction m_Saving_Save;
    public struct SavingActions
    {
        private @CMInput m_Wrapper;
        public SavingActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @Save => m_Wrapper.m_Saving_Save;
        public InputActionMap Get() { return m_Wrapper.m_Saving; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(SavingActions set) { return set.Get(); }
        public void SetCallbacks(ISavingActions instance)
        {
            if (m_Wrapper.m_SavingActionsCallbackInterface != null)
            {
                @Save.started -= m_Wrapper.m_SavingActionsCallbackInterface.OnSave;
                @Save.performed -= m_Wrapper.m_SavingActionsCallbackInterface.OnSave;
                @Save.canceled -= m_Wrapper.m_SavingActionsCallbackInterface.OnSave;
            }
            m_Wrapper.m_SavingActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Save.started += instance.OnSave;
                @Save.performed += instance.OnSave;
                @Save.canceled += instance.OnSave;
            }
        }
    }
    public SavingActions @Saving => new SavingActions(this);
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
    public interface IActionsActions
    {
        void OnUndo(InputAction.CallbackContext context);
        void OnRedo(InputAction.CallbackContext context);
    }
    public interface IPlacementControllersActions
    {
        void OnPlaceObject(InputAction.CallbackContext context);
        void OnInitiateClickandDrag(InputAction.CallbackContext context);
        void OnMousePositionUpdate(InputAction.CallbackContext context);
        void OnCancelPlacement(InputAction.CallbackContext context);
    }
    public interface INotePlacementActions
    {
        void OnDownNote(InputAction.CallbackContext context);
        void OnDownLeftNote(InputAction.CallbackContext context);
        void OnLeftNote(InputAction.CallbackContext context);
        void OnUpLeftNote(InputAction.CallbackContext context);
        void OnUpNote(InputAction.CallbackContext context);
        void OnUpRightNote(InputAction.CallbackContext context);
        void OnRightNote(InputAction.CallbackContext context);
        void OnDownRightNote(InputAction.CallbackContext context);
    }
    public interface IEventPlacementActions
    {
        void OnToggleRingPropagation(InputAction.CallbackContext context);
        void OnRotationAdd15Degrees(InputAction.CallbackContext context);
        void OnRotationAdd30Degrees(InputAction.CallbackContext context);
        void OnRotationAdd45Degrees(InputAction.CallbackContext context);
        void OnRotationAdd60Degrees(InputAction.CallbackContext context);
        void OnRotationSubtract15Degrees(InputAction.CallbackContext context);
        void OnRotationSubtract30Degrees(InputAction.CallbackContext context);
        void OnRotationSubtract45Degrees(InputAction.CallbackContext context);
        void OnRotationSubtract60Degrees(InputAction.CallbackContext context);
    }
    public interface IWorkflowsActions
    {
        void OnChangeWorkflows(InputAction.CallbackContext context);
        void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context);
        void OnPlaceRedNoteorEvent(InputAction.CallbackContext context);
        void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context);
        void OnPlaceBomb(InputAction.CallbackContext context);
        void OnPlaceObstacle(InputAction.CallbackContext context);
        void OnToggleDeleteTool(InputAction.CallbackContext context);
    }
    public interface IEventUIActions
    {
        void OnTypeOn(InputAction.CallbackContext context);
        void OnTypeFlash(InputAction.CallbackContext context);
        void OnTypeOff(InputAction.CallbackContext context);
        void OnTypeFade(InputAction.CallbackContext context);
        void OnTogglePrecisionRotation(InputAction.CallbackContext context);
    }
    public interface ISavingActions
    {
        void OnSave(InputAction.CallbackContext context);
    }
}
