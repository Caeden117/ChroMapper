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
                    ""name"": ""+Move Camera"",
                    ""type"": ""PassThrough"",
                    ""id"": ""b690809d-6128-4967-aa54-ad3b44b03278"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""+Rotate Camera"",
                    ""type"": ""Value"",
                    ""id"": ""2accd882-d6d0-439c-a1ed-189931751453"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""+Elevate Camera"",
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
                },
                {
                    ""name"": ""Toggle Fullscreen"",
                    ""type"": ""Button"",
                    ""id"": ""15237594-4027-4cbc-92ed-cad40331f90e"",
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
                    ""action"": ""+Move Camera"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""d2d63e78-e4cd-4476-9bb7-95f6a64724ec"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""b8fce530-a706-4d90-ac3c-ef71b30fbd25"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""67d23d5e-4301-4614-9bdb-a74d720b28ca"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""91ac3309-f9db-4830-82c5-449f7edaf79f"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""7da07213-d4f5-4c66-9fd0-b6b34595b8fa"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Rotate Camera"",
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
                    ""action"": ""+Elevate Camera"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""066474db-11dd-4e08-9aa5-234b05ba63e7"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Elevate Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""5b427547-d298-4bdc-9aad-aa05d2e3fbf5"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Elevate Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""bb6887ca-e93a-40c3-a19c-7b8476e26a80"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Hold to Move Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4e90cd3d-5414-4527-96b2-7c362f3cd07e"",
                    ""path"": ""<Keyboard>/f11"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Fullscreen"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""+Utils"",
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
                },
                {
                    ""name"": ""Mouse Movement"",
                    ""type"": ""PassThrough"",
                    ""id"": ""965f30e2-f75b-4a64-a00c-e21258ef8eb3"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Shift Modifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f18d965b-6bfa-4a42-8abb-5c01ee7d15dc"",
                    ""path"": ""<Pointer>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Mouse Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Actions"",
            ""id"": ""3e26cae6-c1ff-441d-96fc-9f7505133eed"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Redo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Placement Controllers"",
            ""id"": ""ad57992b-d8ff-4c39-a762-db68cf7d04db"",
            ""actions"": [
                {
                    ""name"": ""Place Object"",
                    ""type"": ""Button"",
                    ""id"": ""12ac2167-b8c0-4f4f-aa6e-17a1aacc42ca"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Initiate Click and Drag"",
                    ""type"": ""Button"",
                    ""id"": ""15cbdd9a-b2fe-489a-b70c-81e0261ba8b8"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""+Mouse Position Update"",
                    ""type"": ""Button"",
                    ""id"": ""3c3cb17e-12c8-41c6-b727-8f8f86c0f325"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Initiate Click and Drag"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""5972be07-cf3b-44d3-861b-c80e8d655778"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Place Object"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ca96ac27-5948-4b00-83e4-0acba376a448"",
                    ""path"": ""<Pointer>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Mouse Position Update"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Note Placement"",
            ""id"": ""55b78d3d-8ced-467c-a88f-d5cf50532d2e"",
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
                    ""name"": ""Right Note"",
                    ""type"": ""Button"",
                    ""id"": ""e63abaea-687c-486a-aee9-133901bc362c"",
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
                    ""name"": ""Left Note"",
                    ""type"": ""Button"",
                    ""id"": ""ea80fd88-1337-4dbd-a7a9-9f1bdd9ecae4"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Dot Note"",
                    ""type"": ""Button"",
                    ""id"": ""2235cb33-d8e5-4ee9-bfad-a0e197fdd475"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Down Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d8232af9-72c8-46d6-bb23-023443cbf276"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Left Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a24d5b1b-8b2b-4978-84e8-1257904d2017"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Right Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e8e63a70-7c85-4205-8648-1b10ca6aa84b"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Up Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ec4a55fa-16bb-4f5d-9ad1-97012d764a81"",
                    ""path"": ""<Keyboard>/f"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Dot Note"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Event Placement"",
            ""id"": ""d0462ea9-c717-4662-b004-e7b66e957cc9"",
            ""actions"": [
                {
                    ""name"": ""Rotation: 15 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""51596ba1-f128-48c4-b563-b4553f121a1e"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: 30 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""cb255ff2-9a90-44fd-a139-433a56a04612"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: 45 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""2aa190df-15aa-4507-a947-7f4aaa7175bc"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Rotation: 60 Degrees"",
                    ""type"": ""Button"",
                    ""id"": ""666291dd-9d51-4f5d-87a2-8ce834a7779c"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Negative Rotation Modifier"",
                    ""type"": ""Button"",
                    ""id"": ""88d0b651-1178-4c79-a5f1-8ea75fafec79"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""c7ad07e7-8790-452a-8bdc-a301fedaff56"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Rotation: 15 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bc17f8d9-7868-4557-8ca6-3acd787acd43"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Rotation: 30 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d4b70180-b3d4-47af-ae16-dda507b87380"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Rotation: 45 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d749c574-a404-4087-a71b-b38850d3db76"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Rotation: 60 Degrees"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""16ae0d9f-09a8-4e34-9ba2-ca2af05b34aa"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Negative Rotation Modifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Workflows"",
            ""id"": ""62377dd3-26a7-4161-ab65-7a5042f0dfc0"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Delete Tool"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Event UI"",
            ""id"": ""5a91a51c-40c9-4e18-bff6-3c5204b446ce"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Precision Rotation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""+Saving"",
            ""id"": ""a07ff948-83f1-461b-988e-d6c1d9e6aadc"",
            ""actions"": [
                {
                    ""name"": ""Save"",
                    ""type"": ""Button"",
                    ""id"": ""5bd99bac-a957-4d98-b68e-e74a153090fe"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Save"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Bookmarks"",
            ""id"": ""358e632a-614c-4580-adb7-64e863720b71"",
            ""actions"": [
                {
                    ""name"": ""Create New Bookmark"",
                    ""type"": ""Button"",
                    ""id"": ""5bd99bac-a957-4d98-b68e-e74a153090fe"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Next Bookmark"",
                    ""type"": ""Button"",
                    ""id"": ""9cf9a467-5b4f-4564-af39-2d2dc34ae6c8"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Previous Bookmark"",
                    ""type"": ""Button"",
                    ""id"": ""a52b1ae8-89b5-4136-a550-f09b65a78fe8"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""5c946efd-12d6-4d24-a7d5-e85bc9925ef2"",
                    ""path"": ""<Keyboard>/b"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Create New Bookmark"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c626b60b-2b5d-4d04-a749-55aebb76584a"",
                    ""path"": ""<Keyboard>/rightBracket"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Next Bookmark"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""471da8dd-be12-4744-b508-edafcabf5e35"",
                    ""path"": ""<Keyboard>/leftBracket"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Previous Bookmark"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Refresh Map"",
            ""id"": ""0d506325-9cd5-4fc8-a448-4b7c30acc9fa"",
            ""actions"": [
                {
                    ""name"": ""Refresh Map"",
                    ""type"": ""Button"",
                    ""id"": ""3ae94594-1532-421a-b6c2-1e1f4cded029"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""38aa9fd6-7b8e-456e-88f6-4d0f56a66d44"",
                    ""path"": ""ButtonWithTwoModifiers"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Refresh Map"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier1"",
                    ""id"": ""104a0fc2-15da-480f-b5bc-d5c3a84f6dd1"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Refresh Map"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""modifier2"",
                    ""id"": ""c2bb7644-ac4e-41fb-8943-e6666c9fe94d"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Refresh Map"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""41064415-c0bb-4961-b49f-4e4c38342c16"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Refresh Map"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Platform Solo Light Group"",
            ""id"": ""351f7d9a-87c1-4cf2-9582-f586a9dd7c90"",
            ""actions"": [
                {
                    ""name"": ""Solo Event Type"",
                    ""type"": ""Button"",
                    ""id"": ""4f7e5c53-1468-4229-bd30-2b74b9696191"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""adef5b67-3d4d-474d-83ea-0e37aac81aa2"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Solo Event Type"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""18d099be-c5eb-426c-b99d-a7316507933b"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Solo Event Type"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""e4db9218-5c0e-44a9-851d-f021814908b1"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Solo Event Type"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Platform Disableable Objects"",
            ""id"": ""a91099fe-dc0a-48f1-9b76-6f3fc152bd1c"",
            ""actions"": [
                {
                    ""name"": ""Toggle Potentially Obstructing Objects"",
                    ""type"": ""Button"",
                    ""id"": ""993b4b91-a432-4657-997d-21b116b30dea"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""44bb4a62-8136-4714-82ad-bc68eaa6611e"",
                    ""path"": ""<Keyboard>/l"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Potentially Obstructing Objects"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Playback"",
            ""id"": ""0d5d9c0b-70f8-4457-a4dc-6d4a43631147"",
            ""actions"": [
                {
                    ""name"": ""Toggle Playing"",
                    ""type"": ""Button"",
                    ""id"": ""84057134-9c36-4d2f-bcc5-ba76c04e98fa"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Reset Time"",
                    ""type"": ""Button"",
                    ""id"": ""996859cc-5dc0-4ef5-9e53-27c5a45f9e7d"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""12f09c2c-d135-4283-b28f-59694bbdaaab"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Playing"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""59ab5455-dd74-429f-808f-1c09186ae5de"",
                    ""path"": ""<Keyboard>/semicolon"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Reset Time"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""+Timeline"",
            ""id"": ""a71da820-2d7a-44ff-b989-f6bc4b2a172b"",
            ""actions"": [
                {
                    ""name"": ""Change Time and Precision"",
                    ""type"": ""Button"",
                    ""id"": ""e046fbab-3ebb-4a53-8594-08a0ae193ab6"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""b84aada6-3206-469c-876e-43da8d5cc266"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Change Time and Precision"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Beatmap Objects"",
            ""id"": ""b5cba2db-88ec-4e5b-9b0a-095ae70a1a75"",
            ""actions"": [
                {
                    ""name"": ""Select Objects"",
                    ""type"": ""Button"",
                    ""id"": ""99ccd235-d96f-4545-b9da-116c171bb16b"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Quick Delete"",
                    ""type"": ""Button"",
                    ""id"": ""f1e5f862-455a-4d77-bc08-92e2c7849e0d"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Delete Tool"",
                    ""type"": ""Button"",
                    ""id"": ""0b907b9a-c55c-4ff8-a752-aba188528875"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""+Mouse Position Update"",
                    ""type"": ""Button"",
                    ""id"": ""0ad31924-f097-4801-a571-a40b54af42e6"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""b19df616-95f6-4715-b98a-1d18373d95ca"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Select Objects"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""e4a67a51-9079-460b-a6c9-9103896219d8"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Select Objects"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""962af3e5-c6b2-40d0-8e6b-7a7c0e57ac2f"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Select Objects"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""25dfcc81-8a03-4484-b81d-29f0a85c3ece"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Quick Delete"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""a029bb83-1266-415b-b150-0ecc2c4d96de"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Quick Delete"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""7468f859-883c-46a0-b612-9e15eeb0fecd"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Quick Delete"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""f872bf2a-d2da-47b0-a049-362822fbf94a"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Delete Tool"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6cc9107d-1265-4184-ace8-b8fa13cb35a8"",
                    ""path"": ""<Pointer>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Mouse Position Update"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Note Objects"",
            ""id"": ""d18a448a-b849-4a22-a103-3a79418bc61b"",
            ""actions"": [
                {
                    ""name"": ""Update Note Direction"",
                    ""type"": ""Button"",
                    ""id"": ""2c396990-1761-4b06-8301-2d3bad5e169a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Invert Note Colors"",
                    ""type"": ""Button"",
                    ""id"": ""29d1bb17-443a-4e78-88a6-46d1ccb56ffc"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""7a4c3196-f21c-4291-a92d-1cca3a61bdbb"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Update Note Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a1a9d032-f76b-4e85-8d27-46380767f735"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Invert Note Colors"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Obstacle Objects"",
            ""id"": ""7895963d-20b8-4609-a0eb-28b539c831ec"",
            ""actions"": [
                {
                    ""name"": ""Toggle Hyper Wall"",
                    ""type"": ""Button"",
                    ""id"": ""2c396990-1761-4b06-8301-2d3bad5e169a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""+Change Wall Duration"",
                    ""type"": ""Button"",
                    ""id"": ""29d1bb17-443a-4e78-88a6-46d1ccb56ffc"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""b252418c-0c5b-4c91-9b75-93454144dd39"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Hyper Wall"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a92dd98a-8921-4318-9713-72d1039da0bd"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Change Wall Duration"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Event Objects"",
            ""id"": ""399c4e52-1861-42ce-bc2b-76ff7ae2c165"",
            ""actions"": [
                {
                    ""name"": ""Invert Event Value"",
                    ""type"": ""Button"",
                    ""id"": ""1a34df05-8e5b-4e52-b486-6eaceeca0b52"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Tweak Event Value"",
                    ""type"": ""Button"",
                    ""id"": ""7a637f25-63f4-434a-b1bc-5db5e34f9e04"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""4358d51d-1757-4e95-83d2-50ad305a79a2"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Invert Event Value"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""09d87f71-afca-418b-b67b-3992407c2c6d"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Tweak Event Value"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""+Custom Events Container"",
            ""id"": ""e2891758-ef87-4350-99ec-a8364257d952"",
            ""actions"": [
                {
                    ""name"": ""Assign Objects to Track"",
                    ""type"": ""Button"",
                    ""id"": ""7b9d7a9c-c2bf-4d85-acfb-2d4d60bb846a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Set Track Filter"",
                    ""type"": ""Button"",
                    ""id"": ""3bb98cf0-faf0-4b09-a8db-c2ce67a39072"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Create New Event Type"",
                    ""type"": ""Button"",
                    ""id"": ""0066a42b-9006-49c9-9b8f-1b96aefb6bb4"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""d4474274-cce5-4e8e-8336-845139548ea8"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Set Track Filter"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""120fc764-48b0-4653-b396-8b9a76f3011f"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Set Track Filter"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""0f44db47-b01e-4b54-a1f8-2826433cccd6"",
                    ""path"": ""<Keyboard>/t"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Set Track Filter"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""f2dc95bd-c422-4e7d-98bb-4934a422cae3"",
                    ""path"": ""ButtonWithTwoModifiers"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Create New Event Type"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier1"",
                    ""id"": ""1498f587-521e-49fa-9d37-908c5a3c544d"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Create New Event Type"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""modifier2"",
                    ""id"": ""9633101d-1c57-4618-9d51-a912352a1162"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Create New Event Type"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""79fdcb8d-af86-4bea-9e3a-d86bb81f32a6"",
                    ""path"": ""<Keyboard>/t"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Create New Event Type"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""79f665a6-db04-4f1e-aeeb-e5de4e61fffc"",
                    ""path"": ""<Keyboard>/t"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Assign Objects to Track"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Node Editor"",
            ""id"": ""26e68c1b-3a6c-4785-8f50-3e4ce8dd036e"",
            ""actions"": [
                {
                    ""name"": ""Toggle Node Editor"",
                    ""type"": ""Button"",
                    ""id"": ""c9a9c9a6-a5d3-488f-b54a-aba65b60d56a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""e78f5977-dcd5-4b94-98fc-9253f2f0f396"",
                    ""path"": ""<Keyboard>/n"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Node Editor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""BPM Tapper"",
            ""id"": ""2badd83b-e1b2-41b0-abb2-cb8a4b79e7da"",
            ""actions"": [
                {
                    ""name"": ""Toggle BPM Tapper"",
                    ""type"": ""Button"",
                    ""id"": ""c9a9c9a6-a5d3-488f-b54a-aba65b60d56a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""e9fa4476-2c7f-4dee-bbeb-a02ad348c6dd"",
                    ""path"": ""<Keyboard>/rightShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle BPM Tapper"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Pause Menu"",
            ""id"": ""3b859155-292c-4d12-8778-4e0133970c44"",
            ""actions"": [
                {
                    ""name"": ""Pause Editor"",
                    ""type"": ""Button"",
                    ""id"": ""fa86a48b-260e-44cc-9436-e4155eaeb871"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""b8d726be-7155-47b3-8ae1-649f26f670c9"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Pause Editor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Selecting"",
            ""id"": ""6964f0f1-558d-46e7-aad0-caa88576d216"",
            ""actions"": [
                {
                    ""name"": ""Deselect All"",
                    ""type"": ""Button"",
                    ""id"": ""843ec7df-2ea7-4a04-bdd8-dab6f351222c"",
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
                    ""groups"": ""ChroMapper Default"",
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
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Deselect All"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Modifying Selection"",
            ""id"": ""b3792a28-92fa-4821-90b9-504529c52fd1"",
            ""actions"": [
                {
                    ""name"": ""Delete Objects"",
                    ""type"": ""Button"",
                    ""id"": ""9b6770ad-b031-4ab2-86fe-c85f0b55501f"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""+Shift in Time"",
                    ""type"": ""Button"",
                    ""id"": ""14a1a829-ac3b-4625-b373-d4e43706ec8b"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""+Shift in Place"",
                    ""type"": ""Button"",
                    ""id"": ""27818e31-e5a6-4b4d-806a-f8de698ba4ee"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Cut"",
                    ""type"": ""Button"",
                    ""id"": ""e9677e31-a29a-40d9-9831-3b0bf425e410"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Paste"",
                    ""type"": ""Button"",
                    ""id"": ""1dc463d8-b058-4507-92da-42817903ce61"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Copy"",
                    ""type"": ""Button"",
                    ""id"": ""ba0f5eb7-64f2-46b8-afd4-219d6a5b9321"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""ea2c9e32-8af1-4937-a4ae-db24fb9a3682"",
                    ""path"": ""<Keyboard>/delete"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Delete Objects"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""eb79eb6e-e652-45f9-b920-af291f3284a7"",
                    ""path"": ""1DAxis(whichSideWins=1)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""+Shift in Time"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""1cd9d133-0d2e-407e-9691-531420b903d0"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Shift in Time"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""71df5f1d-8b1b-4b58-8cd8-2cd449d6893f"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Shift in Time"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""9c22b812-71a8-4961-ab13-212454782e02"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""+Shift in Place"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""dca471d5-701d-4735-92fe-974ad1bf6c91"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""fe41e156-034e-45cf-9ad0-7a2e76986029"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""b813d17f-c262-4b3c-a28f-072c8647364a"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""5de7b57b-9115-406b-add2-c8b5877a39af"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Shift in Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""52d4022b-41e9-4bb7-b451-fea39d569968"",
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
                    ""id"": ""7c14d3c7-c2b2-4954-b831-dca8f7b4169c"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Cut"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""eb15991c-e91e-4f22-aea7-6f15a1ffb5cd"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Cut"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""5ee8b28e-cf8e-4dc7-80a6-bff077609d02"",
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
                    ""id"": ""e8429e9a-ee74-4307-9759-54b5099dd3f3"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Copy"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""3830cfe9-2fdf-4f8b-8b15-93e18b392e4d"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Copy"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""b5a7a345-5a41-4e0a-9527-2ca046a0e361"",
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
                    ""id"": ""0dd280bb-bcfa-44c8-ab93-f496061b6bbe"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Paste"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""de4aafba-900a-42c4-8543-783acedf0997"",
                    ""path"": ""<Keyboard>/v"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Paste"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""2e15fffc-29fe-4cb1-b601-e7a67cea0f96"",
                    ""path"": ""<Keyboard>/backspace"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Delete Objects"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""UI Mode"",
            ""id"": ""e3f966df-bf7d-4718-9605-fbaba1af857b"",
            ""actions"": [
                {
                    ""name"": ""Toggle UI Mode"",
                    ""type"": ""Button"",
                    ""id"": ""d49f10e6-ab3d-4821-b386-3083cb9ef3d1"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""c9097b75-658f-4331-a6c1-1889024eb62c"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Toggle UI Mode"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""4d06a4a4-c18e-4e66-8ab1-a21bcead640d"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle UI Mode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""aa694fd9-9a8b-47b8-a9ac-682e08347b75"",
                    ""path"": ""<Keyboard>/h"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle UI Mode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        },
        {
            ""name"": ""Song Speed"",
            ""id"": ""8190159d-1cd6-4a3b-a047-0a6b3f7e9b38"",
            ""actions"": [
                {
                    ""name"": ""Decrease Song Speed"",
                    ""type"": ""Button"",
                    ""id"": ""1810e054-2601-41ff-96dc-d7d407c30f99"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Increase Song Speed"",
                    ""type"": ""Button"",
                    ""id"": ""27b6f64a-429b-4433-97e3-135d5092ace9"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""8c2f4104-14e0-4a31-b6c6-ae076b925d59"",
                    ""path"": ""<Keyboard>/equals"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Increase Song Speed"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b2f59880-adb4-49c7-b200-e3edc208ec75"",
                    ""path"": ""<Keyboard>/minus"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Decrease Song Speed"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Cancel Placement"",
            ""id"": ""181ef8a3-4a33-4fdb-980e-80e1f05dcaac"",
            ""actions"": [
                {
                    ""name"": ""Cancel Placement"",
                    ""type"": ""Button"",
                    ""id"": ""e5739486-8857-4097-bff2-191f94393ee5"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""21ee156d-d800-46cd-8f54-5457a708e891"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Cancel Placement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""BPM Change Objects"",
            ""id"": ""dcc47838-c346-486c-8c4c-6a3074023cb3"",
            ""actions"": [
                {
                    ""name"": ""Replace BPM (Modifier)"",
                    ""type"": ""Button"",
                    ""id"": ""9f9b573b-d4b1-439c-b3f9-17336ada4933"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""+Replace BPM in Existing BPM Change Click"",
                    ""type"": ""Button"",
                    ""id"": ""1840dc59-261c-4647-ad05-fa7f399f5aeb"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""09e1239a-51e5-461a-a65a-f01509b07b36"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Replace BPM (Modifier)"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""93ee00af-52e6-4198-b72a-92d5887403ab"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""+Replace BPM in Existing BPM Change Click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Event Grid"",
            ""id"": ""f8ccba1c-4f16-4025-a961-27d464f268cc"",
            ""actions"": [
                {
                    ""name"": ""Toggle Light Propagation"",
                    ""type"": ""Button"",
                    ""id"": ""6bf43256-b1f0-4d4a-ba4d-876b58744144"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Cycle Light Propagation Up"",
                    ""type"": ""Button"",
                    ""id"": ""d3e63cd4-46d0-4a81-b686-0594c5aa6074"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Cycle Light Propagation Down"",
                    ""type"": ""Button"",
                    ""id"": ""5937d0d7-0f16-49e8-9739-fc490da69a6d"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""09ae53e0-b74f-4726-9a10-409917cda5fc"",
                    ""path"": ""<Keyboard>/p"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Toggle Light Propagation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1082d3ac-60ce-4652-942d-6328961814c8"",
                    ""path"": ""<Keyboard>/pageUp"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Cycle Light Propagation Up"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""76ef441a-d690-460c-8757-2d3be22165c8"",
                    ""path"": ""<Keyboard>/pageDown"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""ChroMapper Default"",
                    ""action"": ""Cycle Light Propagation Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""MenusExtended"",
            ""id"": ""0a34d3d3-4820-4928-bf89-0c72500b3025"",
            ""actions"": [
                {
                    ""name"": ""Tab"",
                    ""type"": ""Button"",
                    ""id"": ""4468e9ca-6edd-4a3f-a163-91afa51f77c8"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Leave Menu"",
                    ""type"": ""Button"",
                    ""id"": ""ad0f8cea-e5cf-486e-bb47-dac40c46f9bc"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""03c8a6ee-0c97-4c95-87cb-9c7a070e6a81"",
                    ""path"": ""<Keyboard>/tab"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Tab"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""040fc1e4-92db-4011-8025-6ad6704238df"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Leave Menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""ChroMapper Default"",
            ""bindingGroup"": ""ChroMapper Default"",
            ""devices"": []
        }
    ]
}");
        // Camera
        m_Camera = asset.FindActionMap("Camera", throwIfNotFound: true);
        m_Camera_HoldtoMoveCamera = m_Camera.FindAction("Hold to Move Camera", throwIfNotFound: true);
        m_Camera_MoveCamera = m_Camera.FindAction("+Move Camera", throwIfNotFound: true);
        m_Camera_RotateCamera = m_Camera.FindAction("+Rotate Camera", throwIfNotFound: true);
        m_Camera_ElevateCamera = m_Camera.FindAction("+Elevate Camera", throwIfNotFound: true);
        m_Camera_AttachtoNoteGrid = m_Camera.FindAction("Attach to Note Grid", throwIfNotFound: true);
        m_Camera_ToggleFullscreen = m_Camera.FindAction("Toggle Fullscreen", throwIfNotFound: true);
        // +Utils
        m_Utils = asset.FindActionMap("+Utils", throwIfNotFound: true);
        m_Utils_ControlModifier = m_Utils.FindAction("Control Modifier", throwIfNotFound: true);
        m_Utils_AltModifier = m_Utils.FindAction("Alt Modifier", throwIfNotFound: true);
        m_Utils_ShiftModifier = m_Utils.FindAction("Shift Modifier", throwIfNotFound: true);
        m_Utils_MouseMovement = m_Utils.FindAction("Mouse Movement", throwIfNotFound: true);
        // Actions
        m_Actions = asset.FindActionMap("Actions", throwIfNotFound: true);
        m_Actions_Undo = m_Actions.FindAction("Undo", throwIfNotFound: true);
        m_Actions_Redo = m_Actions.FindAction("Redo", throwIfNotFound: true);
        // Placement Controllers
        m_PlacementControllers = asset.FindActionMap("Placement Controllers", throwIfNotFound: true);
        m_PlacementControllers_PlaceObject = m_PlacementControllers.FindAction("Place Object", throwIfNotFound: true);
        m_PlacementControllers_InitiateClickandDrag = m_PlacementControllers.FindAction("Initiate Click and Drag", throwIfNotFound: true);
        m_PlacementControllers_MousePositionUpdate = m_PlacementControllers.FindAction("+Mouse Position Update", throwIfNotFound: true);
        // Note Placement
        m_NotePlacement = asset.FindActionMap("Note Placement", throwIfNotFound: true);
        m_NotePlacement_DownNote = m_NotePlacement.FindAction("Down Note", throwIfNotFound: true);
        m_NotePlacement_RightNote = m_NotePlacement.FindAction("Right Note", throwIfNotFound: true);
        m_NotePlacement_UpNote = m_NotePlacement.FindAction("Up Note", throwIfNotFound: true);
        m_NotePlacement_LeftNote = m_NotePlacement.FindAction("Left Note", throwIfNotFound: true);
        m_NotePlacement_DotNote = m_NotePlacement.FindAction("Dot Note", throwIfNotFound: true);
        // Event Placement
        m_EventPlacement = asset.FindActionMap("Event Placement", throwIfNotFound: true);
        m_EventPlacement_Rotation15Degrees = m_EventPlacement.FindAction("Rotation: 15 Degrees", throwIfNotFound: true);
        m_EventPlacement_Rotation30Degrees = m_EventPlacement.FindAction("Rotation: 30 Degrees", throwIfNotFound: true);
        m_EventPlacement_Rotation45Degrees = m_EventPlacement.FindAction("Rotation: 45 Degrees", throwIfNotFound: true);
        m_EventPlacement_Rotation60Degrees = m_EventPlacement.FindAction("Rotation: 60 Degrees", throwIfNotFound: true);
        m_EventPlacement_NegativeRotationModifier = m_EventPlacement.FindAction("Negative Rotation Modifier", throwIfNotFound: true);
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
        // +Saving
        m_Saving = asset.FindActionMap("+Saving", throwIfNotFound: true);
        m_Saving_Save = m_Saving.FindAction("Save", throwIfNotFound: true);
        // Bookmarks
        m_Bookmarks = asset.FindActionMap("Bookmarks", throwIfNotFound: true);
        m_Bookmarks_CreateNewBookmark = m_Bookmarks.FindAction("Create New Bookmark", throwIfNotFound: true);
        m_Bookmarks_NextBookmark = m_Bookmarks.FindAction("Next Bookmark", throwIfNotFound: true);
        m_Bookmarks_PreviousBookmark = m_Bookmarks.FindAction("Previous Bookmark", throwIfNotFound: true);
        // Refresh Map
        m_RefreshMap = asset.FindActionMap("Refresh Map", throwIfNotFound: true);
        m_RefreshMap_RefreshMap = m_RefreshMap.FindAction("Refresh Map", throwIfNotFound: true);
        // Platform Solo Light Group
        m_PlatformSoloLightGroup = asset.FindActionMap("Platform Solo Light Group", throwIfNotFound: true);
        m_PlatformSoloLightGroup_SoloEventType = m_PlatformSoloLightGroup.FindAction("Solo Event Type", throwIfNotFound: true);
        // Platform Disableable Objects
        m_PlatformDisableableObjects = asset.FindActionMap("Platform Disableable Objects", throwIfNotFound: true);
        m_PlatformDisableableObjects_TogglePotentiallyObstructingObjects = m_PlatformDisableableObjects.FindAction("Toggle Potentially Obstructing Objects", throwIfNotFound: true);
        // Playback
        m_Playback = asset.FindActionMap("Playback", throwIfNotFound: true);
        m_Playback_TogglePlaying = m_Playback.FindAction("Toggle Playing", throwIfNotFound: true);
        m_Playback_ResetTime = m_Playback.FindAction("Reset Time", throwIfNotFound: true);
        // +Timeline
        m_Timeline = asset.FindActionMap("+Timeline", throwIfNotFound: true);
        m_Timeline_ChangeTimeandPrecision = m_Timeline.FindAction("Change Time and Precision", throwIfNotFound: true);
        // Beatmap Objects
        m_BeatmapObjects = asset.FindActionMap("Beatmap Objects", throwIfNotFound: true);
        m_BeatmapObjects_SelectObjects = m_BeatmapObjects.FindAction("Select Objects", throwIfNotFound: true);
        m_BeatmapObjects_QuickDelete = m_BeatmapObjects.FindAction("Quick Delete", throwIfNotFound: true);
        m_BeatmapObjects_DeleteTool = m_BeatmapObjects.FindAction("Delete Tool", throwIfNotFound: true);
        m_BeatmapObjects_MousePositionUpdate = m_BeatmapObjects.FindAction("+Mouse Position Update", throwIfNotFound: true);
        // Note Objects
        m_NoteObjects = asset.FindActionMap("Note Objects", throwIfNotFound: true);
        m_NoteObjects_UpdateNoteDirection = m_NoteObjects.FindAction("Update Note Direction", throwIfNotFound: true);
        m_NoteObjects_InvertNoteColors = m_NoteObjects.FindAction("Invert Note Colors", throwIfNotFound: true);
        // Obstacle Objects
        m_ObstacleObjects = asset.FindActionMap("Obstacle Objects", throwIfNotFound: true);
        m_ObstacleObjects_ToggleHyperWall = m_ObstacleObjects.FindAction("Toggle Hyper Wall", throwIfNotFound: true);
        m_ObstacleObjects_ChangeWallDuration = m_ObstacleObjects.FindAction("+Change Wall Duration", throwIfNotFound: true);
        // Event Objects
        m_EventObjects = asset.FindActionMap("Event Objects", throwIfNotFound: true);
        m_EventObjects_InvertEventValue = m_EventObjects.FindAction("Invert Event Value", throwIfNotFound: true);
        m_EventObjects_TweakEventValue = m_EventObjects.FindAction("Tweak Event Value", throwIfNotFound: true);
        // +Custom Events Container
        m_CustomEventsContainer = asset.FindActionMap("+Custom Events Container", throwIfNotFound: true);
        m_CustomEventsContainer_AssignObjectstoTrack = m_CustomEventsContainer.FindAction("Assign Objects to Track", throwIfNotFound: true);
        m_CustomEventsContainer_SetTrackFilter = m_CustomEventsContainer.FindAction("Set Track Filter", throwIfNotFound: true);
        m_CustomEventsContainer_CreateNewEventType = m_CustomEventsContainer.FindAction("Create New Event Type", throwIfNotFound: true);
        // Node Editor
        m_NodeEditor = asset.FindActionMap("Node Editor", throwIfNotFound: true);
        m_NodeEditor_ToggleNodeEditor = m_NodeEditor.FindAction("Toggle Node Editor", throwIfNotFound: true);
        // BPM Tapper
        m_BPMTapper = asset.FindActionMap("BPM Tapper", throwIfNotFound: true);
        m_BPMTapper_ToggleBPMTapper = m_BPMTapper.FindAction("Toggle BPM Tapper", throwIfNotFound: true);
        // Pause Menu
        m_PauseMenu = asset.FindActionMap("Pause Menu", throwIfNotFound: true);
        m_PauseMenu_PauseEditor = m_PauseMenu.FindAction("Pause Editor", throwIfNotFound: true);
        // Selecting
        m_Selecting = asset.FindActionMap("Selecting", throwIfNotFound: true);
        m_Selecting_DeselectAll = m_Selecting.FindAction("Deselect All", throwIfNotFound: true);
        // Modifying Selection
        m_ModifyingSelection = asset.FindActionMap("Modifying Selection", throwIfNotFound: true);
        m_ModifyingSelection_DeleteObjects = m_ModifyingSelection.FindAction("Delete Objects", throwIfNotFound: true);
        m_ModifyingSelection_ShiftinTime = m_ModifyingSelection.FindAction("+Shift in Time", throwIfNotFound: true);
        m_ModifyingSelection_ShiftinPlace = m_ModifyingSelection.FindAction("+Shift in Place", throwIfNotFound: true);
        m_ModifyingSelection_Cut = m_ModifyingSelection.FindAction("Cut", throwIfNotFound: true);
        m_ModifyingSelection_Paste = m_ModifyingSelection.FindAction("Paste", throwIfNotFound: true);
        m_ModifyingSelection_Copy = m_ModifyingSelection.FindAction("Copy", throwIfNotFound: true);
        // UI Mode
        m_UIMode = asset.FindActionMap("UI Mode", throwIfNotFound: true);
        m_UIMode_ToggleUIMode = m_UIMode.FindAction("Toggle UI Mode", throwIfNotFound: true);
        // Song Speed
        m_SongSpeed = asset.FindActionMap("Song Speed", throwIfNotFound: true);
        m_SongSpeed_DecreaseSongSpeed = m_SongSpeed.FindAction("Decrease Song Speed", throwIfNotFound: true);
        m_SongSpeed_IncreaseSongSpeed = m_SongSpeed.FindAction("Increase Song Speed", throwIfNotFound: true);
        // Cancel Placement
        m_CancelPlacement = asset.FindActionMap("Cancel Placement", throwIfNotFound: true);
        m_CancelPlacement_CancelPlacement = m_CancelPlacement.FindAction("Cancel Placement", throwIfNotFound: true);
        // BPM Change Objects
        m_BPMChangeObjects = asset.FindActionMap("BPM Change Objects", throwIfNotFound: true);
        m_BPMChangeObjects_ReplaceBPMModifier = m_BPMChangeObjects.FindAction("Replace BPM (Modifier)", throwIfNotFound: true);
        m_BPMChangeObjects_ReplaceBPMinExistingBPMChangeClick = m_BPMChangeObjects.FindAction("+Replace BPM in Existing BPM Change Click", throwIfNotFound: true);
        // Event Grid
        m_EventGrid = asset.FindActionMap("Event Grid", throwIfNotFound: true);
        m_EventGrid_ToggleLightPropagation = m_EventGrid.FindAction("Toggle Light Propagation", throwIfNotFound: true);
        m_EventGrid_CycleLightPropagationUp = m_EventGrid.FindAction("Cycle Light Propagation Up", throwIfNotFound: true);
        m_EventGrid_CycleLightPropagationDown = m_EventGrid.FindAction("Cycle Light Propagation Down", throwIfNotFound: true);
        // MenusExtended
        m_MenusExtended = asset.FindActionMap("MenusExtended", throwIfNotFound: true);
        m_MenusExtended_Tab = m_MenusExtended.FindAction("Tab", throwIfNotFound: true);
        m_MenusExtended_LeaveMenu = m_MenusExtended.FindAction("Leave Menu", throwIfNotFound: true);
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
    private readonly InputAction m_Camera_ToggleFullscreen;
    public struct CameraActions
    {
        private @CMInput m_Wrapper;
        public CameraActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @HoldtoMoveCamera => m_Wrapper.m_Camera_HoldtoMoveCamera;
        public InputAction @MoveCamera => m_Wrapper.m_Camera_MoveCamera;
        public InputAction @RotateCamera => m_Wrapper.m_Camera_RotateCamera;
        public InputAction @ElevateCamera => m_Wrapper.m_Camera_ElevateCamera;
        public InputAction @AttachtoNoteGrid => m_Wrapper.m_Camera_AttachtoNoteGrid;
        public InputAction @ToggleFullscreen => m_Wrapper.m_Camera_ToggleFullscreen;
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
                @ToggleFullscreen.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleFullscreen;
                @ToggleFullscreen.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleFullscreen;
                @ToggleFullscreen.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleFullscreen;
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
                @ToggleFullscreen.started += instance.OnToggleFullscreen;
                @ToggleFullscreen.performed += instance.OnToggleFullscreen;
                @ToggleFullscreen.canceled += instance.OnToggleFullscreen;
            }
        }
    }
    public CameraActions @Camera => new CameraActions(this);

    // +Utils
    private readonly InputActionMap m_Utils;
    private IUtilsActions m_UtilsActionsCallbackInterface;
    private readonly InputAction m_Utils_ControlModifier;
    private readonly InputAction m_Utils_AltModifier;
    private readonly InputAction m_Utils_ShiftModifier;
    private readonly InputAction m_Utils_MouseMovement;
    public struct UtilsActions
    {
        private @CMInput m_Wrapper;
        public UtilsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ControlModifier => m_Wrapper.m_Utils_ControlModifier;
        public InputAction @AltModifier => m_Wrapper.m_Utils_AltModifier;
        public InputAction @ShiftModifier => m_Wrapper.m_Utils_ShiftModifier;
        public InputAction @MouseMovement => m_Wrapper.m_Utils_MouseMovement;
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
                @MouseMovement.started -= m_Wrapper.m_UtilsActionsCallbackInterface.OnMouseMovement;
                @MouseMovement.performed -= m_Wrapper.m_UtilsActionsCallbackInterface.OnMouseMovement;
                @MouseMovement.canceled -= m_Wrapper.m_UtilsActionsCallbackInterface.OnMouseMovement;
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
                @MouseMovement.started += instance.OnMouseMovement;
                @MouseMovement.performed += instance.OnMouseMovement;
                @MouseMovement.canceled += instance.OnMouseMovement;
            }
        }
    }
    public UtilsActions @Utils => new UtilsActions(this);

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
    public struct PlacementControllersActions
    {
        private @CMInput m_Wrapper;
        public PlacementControllersActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @PlaceObject => m_Wrapper.m_PlacementControllers_PlaceObject;
        public InputAction @InitiateClickandDrag => m_Wrapper.m_PlacementControllers_InitiateClickandDrag;
        public InputAction @MousePositionUpdate => m_Wrapper.m_PlacementControllers_MousePositionUpdate;
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
            }
        }
    }
    public PlacementControllersActions @PlacementControllers => new PlacementControllersActions(this);

    // Note Placement
    private readonly InputActionMap m_NotePlacement;
    private INotePlacementActions m_NotePlacementActionsCallbackInterface;
    private readonly InputAction m_NotePlacement_DownNote;
    private readonly InputAction m_NotePlacement_RightNote;
    private readonly InputAction m_NotePlacement_UpNote;
    private readonly InputAction m_NotePlacement_LeftNote;
    private readonly InputAction m_NotePlacement_DotNote;
    public struct NotePlacementActions
    {
        private @CMInput m_Wrapper;
        public NotePlacementActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @DownNote => m_Wrapper.m_NotePlacement_DownNote;
        public InputAction @RightNote => m_Wrapper.m_NotePlacement_RightNote;
        public InputAction @UpNote => m_Wrapper.m_NotePlacement_UpNote;
        public InputAction @LeftNote => m_Wrapper.m_NotePlacement_LeftNote;
        public InputAction @DotNote => m_Wrapper.m_NotePlacement_DotNote;
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
                @RightNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnRightNote;
                @RightNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnRightNote;
                @RightNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnRightNote;
                @UpNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpNote;
                @UpNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpNote;
                @UpNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnUpNote;
                @LeftNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnLeftNote;
                @LeftNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnLeftNote;
                @LeftNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnLeftNote;
                @DotNote.started -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDotNote;
                @DotNote.performed -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDotNote;
                @DotNote.canceled -= m_Wrapper.m_NotePlacementActionsCallbackInterface.OnDotNote;
            }
            m_Wrapper.m_NotePlacementActionsCallbackInterface = instance;
            if (instance != null)
            {
                @DownNote.started += instance.OnDownNote;
                @DownNote.performed += instance.OnDownNote;
                @DownNote.canceled += instance.OnDownNote;
                @RightNote.started += instance.OnRightNote;
                @RightNote.performed += instance.OnRightNote;
                @RightNote.canceled += instance.OnRightNote;
                @UpNote.started += instance.OnUpNote;
                @UpNote.performed += instance.OnUpNote;
                @UpNote.canceled += instance.OnUpNote;
                @LeftNote.started += instance.OnLeftNote;
                @LeftNote.performed += instance.OnLeftNote;
                @LeftNote.canceled += instance.OnLeftNote;
                @DotNote.started += instance.OnDotNote;
                @DotNote.performed += instance.OnDotNote;
                @DotNote.canceled += instance.OnDotNote;
            }
        }
    }
    public NotePlacementActions @NotePlacement => new NotePlacementActions(this);

    // Event Placement
    private readonly InputActionMap m_EventPlacement;
    private IEventPlacementActions m_EventPlacementActionsCallbackInterface;
    private readonly InputAction m_EventPlacement_Rotation15Degrees;
    private readonly InputAction m_EventPlacement_Rotation30Degrees;
    private readonly InputAction m_EventPlacement_Rotation45Degrees;
    private readonly InputAction m_EventPlacement_Rotation60Degrees;
    private readonly InputAction m_EventPlacement_NegativeRotationModifier;
    public struct EventPlacementActions
    {
        private @CMInput m_Wrapper;
        public EventPlacementActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @Rotation15Degrees => m_Wrapper.m_EventPlacement_Rotation15Degrees;
        public InputAction @Rotation30Degrees => m_Wrapper.m_EventPlacement_Rotation30Degrees;
        public InputAction @Rotation45Degrees => m_Wrapper.m_EventPlacement_Rotation45Degrees;
        public InputAction @Rotation60Degrees => m_Wrapper.m_EventPlacement_Rotation60Degrees;
        public InputAction @NegativeRotationModifier => m_Wrapper.m_EventPlacement_NegativeRotationModifier;
        public InputActionMap Get() { return m_Wrapper.m_EventPlacement; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(EventPlacementActions set) { return set.Get(); }
        public void SetCallbacks(IEventPlacementActions instance)
        {
            if (m_Wrapper.m_EventPlacementActionsCallbackInterface != null)
            {
                @Rotation15Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation15Degrees;
                @Rotation15Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation15Degrees;
                @Rotation15Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation15Degrees;
                @Rotation30Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation30Degrees;
                @Rotation30Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation30Degrees;
                @Rotation30Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation30Degrees;
                @Rotation45Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation45Degrees;
                @Rotation45Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation45Degrees;
                @Rotation45Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation45Degrees;
                @Rotation60Degrees.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation60Degrees;
                @Rotation60Degrees.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation60Degrees;
                @Rotation60Degrees.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnRotation60Degrees;
                @NegativeRotationModifier.started -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnNegativeRotationModifier;
                @NegativeRotationModifier.performed -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnNegativeRotationModifier;
                @NegativeRotationModifier.canceled -= m_Wrapper.m_EventPlacementActionsCallbackInterface.OnNegativeRotationModifier;
            }
            m_Wrapper.m_EventPlacementActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Rotation15Degrees.started += instance.OnRotation15Degrees;
                @Rotation15Degrees.performed += instance.OnRotation15Degrees;
                @Rotation15Degrees.canceled += instance.OnRotation15Degrees;
                @Rotation30Degrees.started += instance.OnRotation30Degrees;
                @Rotation30Degrees.performed += instance.OnRotation30Degrees;
                @Rotation30Degrees.canceled += instance.OnRotation30Degrees;
                @Rotation45Degrees.started += instance.OnRotation45Degrees;
                @Rotation45Degrees.performed += instance.OnRotation45Degrees;
                @Rotation45Degrees.canceled += instance.OnRotation45Degrees;
                @Rotation60Degrees.started += instance.OnRotation60Degrees;
                @Rotation60Degrees.performed += instance.OnRotation60Degrees;
                @Rotation60Degrees.canceled += instance.OnRotation60Degrees;
                @NegativeRotationModifier.started += instance.OnNegativeRotationModifier;
                @NegativeRotationModifier.performed += instance.OnNegativeRotationModifier;
                @NegativeRotationModifier.canceled += instance.OnNegativeRotationModifier;
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

    // +Saving
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

    // Bookmarks
    private readonly InputActionMap m_Bookmarks;
    private IBookmarksActions m_BookmarksActionsCallbackInterface;
    private readonly InputAction m_Bookmarks_CreateNewBookmark;
    private readonly InputAction m_Bookmarks_NextBookmark;
    private readonly InputAction m_Bookmarks_PreviousBookmark;
    public struct BookmarksActions
    {
        private @CMInput m_Wrapper;
        public BookmarksActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @CreateNewBookmark => m_Wrapper.m_Bookmarks_CreateNewBookmark;
        public InputAction @NextBookmark => m_Wrapper.m_Bookmarks_NextBookmark;
        public InputAction @PreviousBookmark => m_Wrapper.m_Bookmarks_PreviousBookmark;
        public InputActionMap Get() { return m_Wrapper.m_Bookmarks; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(BookmarksActions set) { return set.Get(); }
        public void SetCallbacks(IBookmarksActions instance)
        {
            if (m_Wrapper.m_BookmarksActionsCallbackInterface != null)
            {
                @CreateNewBookmark.started -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnCreateNewBookmark;
                @CreateNewBookmark.performed -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnCreateNewBookmark;
                @CreateNewBookmark.canceled -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnCreateNewBookmark;
                @NextBookmark.started -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnNextBookmark;
                @NextBookmark.performed -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnNextBookmark;
                @NextBookmark.canceled -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnNextBookmark;
                @PreviousBookmark.started -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnPreviousBookmark;
                @PreviousBookmark.performed -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnPreviousBookmark;
                @PreviousBookmark.canceled -= m_Wrapper.m_BookmarksActionsCallbackInterface.OnPreviousBookmark;
            }
            m_Wrapper.m_BookmarksActionsCallbackInterface = instance;
            if (instance != null)
            {
                @CreateNewBookmark.started += instance.OnCreateNewBookmark;
                @CreateNewBookmark.performed += instance.OnCreateNewBookmark;
                @CreateNewBookmark.canceled += instance.OnCreateNewBookmark;
                @NextBookmark.started += instance.OnNextBookmark;
                @NextBookmark.performed += instance.OnNextBookmark;
                @NextBookmark.canceled += instance.OnNextBookmark;
                @PreviousBookmark.started += instance.OnPreviousBookmark;
                @PreviousBookmark.performed += instance.OnPreviousBookmark;
                @PreviousBookmark.canceled += instance.OnPreviousBookmark;
            }
        }
    }
    public BookmarksActions @Bookmarks => new BookmarksActions(this);

    // Refresh Map
    private readonly InputActionMap m_RefreshMap;
    private IRefreshMapActions m_RefreshMapActionsCallbackInterface;
    private readonly InputAction m_RefreshMap_RefreshMap;
    public struct RefreshMapActions
    {
        private @CMInput m_Wrapper;
        public RefreshMapActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @RefreshMap => m_Wrapper.m_RefreshMap_RefreshMap;
        public InputActionMap Get() { return m_Wrapper.m_RefreshMap; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(RefreshMapActions set) { return set.Get(); }
        public void SetCallbacks(IRefreshMapActions instance)
        {
            if (m_Wrapper.m_RefreshMapActionsCallbackInterface != null)
            {
                @RefreshMap.started -= m_Wrapper.m_RefreshMapActionsCallbackInterface.OnRefreshMap;
                @RefreshMap.performed -= m_Wrapper.m_RefreshMapActionsCallbackInterface.OnRefreshMap;
                @RefreshMap.canceled -= m_Wrapper.m_RefreshMapActionsCallbackInterface.OnRefreshMap;
            }
            m_Wrapper.m_RefreshMapActionsCallbackInterface = instance;
            if (instance != null)
            {
                @RefreshMap.started += instance.OnRefreshMap;
                @RefreshMap.performed += instance.OnRefreshMap;
                @RefreshMap.canceled += instance.OnRefreshMap;
            }
        }
    }
    public RefreshMapActions @RefreshMap => new RefreshMapActions(this);

    // Platform Solo Light Group
    private readonly InputActionMap m_PlatformSoloLightGroup;
    private IPlatformSoloLightGroupActions m_PlatformSoloLightGroupActionsCallbackInterface;
    private readonly InputAction m_PlatformSoloLightGroup_SoloEventType;
    public struct PlatformSoloLightGroupActions
    {
        private @CMInput m_Wrapper;
        public PlatformSoloLightGroupActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @SoloEventType => m_Wrapper.m_PlatformSoloLightGroup_SoloEventType;
        public InputActionMap Get() { return m_Wrapper.m_PlatformSoloLightGroup; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlatformSoloLightGroupActions set) { return set.Get(); }
        public void SetCallbacks(IPlatformSoloLightGroupActions instance)
        {
            if (m_Wrapper.m_PlatformSoloLightGroupActionsCallbackInterface != null)
            {
                @SoloEventType.started -= m_Wrapper.m_PlatformSoloLightGroupActionsCallbackInterface.OnSoloEventType;
                @SoloEventType.performed -= m_Wrapper.m_PlatformSoloLightGroupActionsCallbackInterface.OnSoloEventType;
                @SoloEventType.canceled -= m_Wrapper.m_PlatformSoloLightGroupActionsCallbackInterface.OnSoloEventType;
            }
            m_Wrapper.m_PlatformSoloLightGroupActionsCallbackInterface = instance;
            if (instance != null)
            {
                @SoloEventType.started += instance.OnSoloEventType;
                @SoloEventType.performed += instance.OnSoloEventType;
                @SoloEventType.canceled += instance.OnSoloEventType;
            }
        }
    }
    public PlatformSoloLightGroupActions @PlatformSoloLightGroup => new PlatformSoloLightGroupActions(this);

    // Platform Disableable Objects
    private readonly InputActionMap m_PlatformDisableableObjects;
    private IPlatformDisableableObjectsActions m_PlatformDisableableObjectsActionsCallbackInterface;
    private readonly InputAction m_PlatformDisableableObjects_TogglePotentiallyObstructingObjects;
    public struct PlatformDisableableObjectsActions
    {
        private @CMInput m_Wrapper;
        public PlatformDisableableObjectsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @TogglePotentiallyObstructingObjects => m_Wrapper.m_PlatformDisableableObjects_TogglePotentiallyObstructingObjects;
        public InputActionMap Get() { return m_Wrapper.m_PlatformDisableableObjects; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlatformDisableableObjectsActions set) { return set.Get(); }
        public void SetCallbacks(IPlatformDisableableObjectsActions instance)
        {
            if (m_Wrapper.m_PlatformDisableableObjectsActionsCallbackInterface != null)
            {
                @TogglePotentiallyObstructingObjects.started -= m_Wrapper.m_PlatformDisableableObjectsActionsCallbackInterface.OnTogglePotentiallyObstructingObjects;
                @TogglePotentiallyObstructingObjects.performed -= m_Wrapper.m_PlatformDisableableObjectsActionsCallbackInterface.OnTogglePotentiallyObstructingObjects;
                @TogglePotentiallyObstructingObjects.canceled -= m_Wrapper.m_PlatformDisableableObjectsActionsCallbackInterface.OnTogglePotentiallyObstructingObjects;
            }
            m_Wrapper.m_PlatformDisableableObjectsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @TogglePotentiallyObstructingObjects.started += instance.OnTogglePotentiallyObstructingObjects;
                @TogglePotentiallyObstructingObjects.performed += instance.OnTogglePotentiallyObstructingObjects;
                @TogglePotentiallyObstructingObjects.canceled += instance.OnTogglePotentiallyObstructingObjects;
            }
        }
    }
    public PlatformDisableableObjectsActions @PlatformDisableableObjects => new PlatformDisableableObjectsActions(this);

    // Playback
    private readonly InputActionMap m_Playback;
    private IPlaybackActions m_PlaybackActionsCallbackInterface;
    private readonly InputAction m_Playback_TogglePlaying;
    private readonly InputAction m_Playback_ResetTime;
    public struct PlaybackActions
    {
        private @CMInput m_Wrapper;
        public PlaybackActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @TogglePlaying => m_Wrapper.m_Playback_TogglePlaying;
        public InputAction @ResetTime => m_Wrapper.m_Playback_ResetTime;
        public InputActionMap Get() { return m_Wrapper.m_Playback; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlaybackActions set) { return set.Get(); }
        public void SetCallbacks(IPlaybackActions instance)
        {
            if (m_Wrapper.m_PlaybackActionsCallbackInterface != null)
            {
                @TogglePlaying.started -= m_Wrapper.m_PlaybackActionsCallbackInterface.OnTogglePlaying;
                @TogglePlaying.performed -= m_Wrapper.m_PlaybackActionsCallbackInterface.OnTogglePlaying;
                @TogglePlaying.canceled -= m_Wrapper.m_PlaybackActionsCallbackInterface.OnTogglePlaying;
                @ResetTime.started -= m_Wrapper.m_PlaybackActionsCallbackInterface.OnResetTime;
                @ResetTime.performed -= m_Wrapper.m_PlaybackActionsCallbackInterface.OnResetTime;
                @ResetTime.canceled -= m_Wrapper.m_PlaybackActionsCallbackInterface.OnResetTime;
            }
            m_Wrapper.m_PlaybackActionsCallbackInterface = instance;
            if (instance != null)
            {
                @TogglePlaying.started += instance.OnTogglePlaying;
                @TogglePlaying.performed += instance.OnTogglePlaying;
                @TogglePlaying.canceled += instance.OnTogglePlaying;
                @ResetTime.started += instance.OnResetTime;
                @ResetTime.performed += instance.OnResetTime;
                @ResetTime.canceled += instance.OnResetTime;
            }
        }
    }
    public PlaybackActions @Playback => new PlaybackActions(this);

    // +Timeline
    private readonly InputActionMap m_Timeline;
    private ITimelineActions m_TimelineActionsCallbackInterface;
    private readonly InputAction m_Timeline_ChangeTimeandPrecision;
    public struct TimelineActions
    {
        private @CMInput m_Wrapper;
        public TimelineActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ChangeTimeandPrecision => m_Wrapper.m_Timeline_ChangeTimeandPrecision;
        public InputActionMap Get() { return m_Wrapper.m_Timeline; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(TimelineActions set) { return set.Get(); }
        public void SetCallbacks(ITimelineActions instance)
        {
            if (m_Wrapper.m_TimelineActionsCallbackInterface != null)
            {
                @ChangeTimeandPrecision.started -= m_Wrapper.m_TimelineActionsCallbackInterface.OnChangeTimeandPrecision;
                @ChangeTimeandPrecision.performed -= m_Wrapper.m_TimelineActionsCallbackInterface.OnChangeTimeandPrecision;
                @ChangeTimeandPrecision.canceled -= m_Wrapper.m_TimelineActionsCallbackInterface.OnChangeTimeandPrecision;
            }
            m_Wrapper.m_TimelineActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ChangeTimeandPrecision.started += instance.OnChangeTimeandPrecision;
                @ChangeTimeandPrecision.performed += instance.OnChangeTimeandPrecision;
                @ChangeTimeandPrecision.canceled += instance.OnChangeTimeandPrecision;
            }
        }
    }
    public TimelineActions @Timeline => new TimelineActions(this);

    // Beatmap Objects
    private readonly InputActionMap m_BeatmapObjects;
    private IBeatmapObjectsActions m_BeatmapObjectsActionsCallbackInterface;
    private readonly InputAction m_BeatmapObjects_SelectObjects;
    private readonly InputAction m_BeatmapObjects_QuickDelete;
    private readonly InputAction m_BeatmapObjects_DeleteTool;
    private readonly InputAction m_BeatmapObjects_MousePositionUpdate;
    public struct BeatmapObjectsActions
    {
        private @CMInput m_Wrapper;
        public BeatmapObjectsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @SelectObjects => m_Wrapper.m_BeatmapObjects_SelectObjects;
        public InputAction @QuickDelete => m_Wrapper.m_BeatmapObjects_QuickDelete;
        public InputAction @DeleteTool => m_Wrapper.m_BeatmapObjects_DeleteTool;
        public InputAction @MousePositionUpdate => m_Wrapper.m_BeatmapObjects_MousePositionUpdate;
        public InputActionMap Get() { return m_Wrapper.m_BeatmapObjects; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(BeatmapObjectsActions set) { return set.Get(); }
        public void SetCallbacks(IBeatmapObjectsActions instance)
        {
            if (m_Wrapper.m_BeatmapObjectsActionsCallbackInterface != null)
            {
                @SelectObjects.started -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnSelectObjects;
                @SelectObjects.performed -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnSelectObjects;
                @SelectObjects.canceled -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnSelectObjects;
                @QuickDelete.started -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnQuickDelete;
                @QuickDelete.performed -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnQuickDelete;
                @QuickDelete.canceled -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnQuickDelete;
                @DeleteTool.started -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnDeleteTool;
                @DeleteTool.performed -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnDeleteTool;
                @DeleteTool.canceled -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnDeleteTool;
                @MousePositionUpdate.started -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnMousePositionUpdate;
                @MousePositionUpdate.performed -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnMousePositionUpdate;
                @MousePositionUpdate.canceled -= m_Wrapper.m_BeatmapObjectsActionsCallbackInterface.OnMousePositionUpdate;
            }
            m_Wrapper.m_BeatmapObjectsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @SelectObjects.started += instance.OnSelectObjects;
                @SelectObjects.performed += instance.OnSelectObjects;
                @SelectObjects.canceled += instance.OnSelectObjects;
                @QuickDelete.started += instance.OnQuickDelete;
                @QuickDelete.performed += instance.OnQuickDelete;
                @QuickDelete.canceled += instance.OnQuickDelete;
                @DeleteTool.started += instance.OnDeleteTool;
                @DeleteTool.performed += instance.OnDeleteTool;
                @DeleteTool.canceled += instance.OnDeleteTool;
                @MousePositionUpdate.started += instance.OnMousePositionUpdate;
                @MousePositionUpdate.performed += instance.OnMousePositionUpdate;
                @MousePositionUpdate.canceled += instance.OnMousePositionUpdate;
            }
        }
    }
    public BeatmapObjectsActions @BeatmapObjects => new BeatmapObjectsActions(this);

    // Note Objects
    private readonly InputActionMap m_NoteObjects;
    private INoteObjectsActions m_NoteObjectsActionsCallbackInterface;
    private readonly InputAction m_NoteObjects_UpdateNoteDirection;
    private readonly InputAction m_NoteObjects_InvertNoteColors;
    public struct NoteObjectsActions
    {
        private @CMInput m_Wrapper;
        public NoteObjectsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @UpdateNoteDirection => m_Wrapper.m_NoteObjects_UpdateNoteDirection;
        public InputAction @InvertNoteColors => m_Wrapper.m_NoteObjects_InvertNoteColors;
        public InputActionMap Get() { return m_Wrapper.m_NoteObjects; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(NoteObjectsActions set) { return set.Get(); }
        public void SetCallbacks(INoteObjectsActions instance)
        {
            if (m_Wrapper.m_NoteObjectsActionsCallbackInterface != null)
            {
                @UpdateNoteDirection.started -= m_Wrapper.m_NoteObjectsActionsCallbackInterface.OnUpdateNoteDirection;
                @UpdateNoteDirection.performed -= m_Wrapper.m_NoteObjectsActionsCallbackInterface.OnUpdateNoteDirection;
                @UpdateNoteDirection.canceled -= m_Wrapper.m_NoteObjectsActionsCallbackInterface.OnUpdateNoteDirection;
                @InvertNoteColors.started -= m_Wrapper.m_NoteObjectsActionsCallbackInterface.OnInvertNoteColors;
                @InvertNoteColors.performed -= m_Wrapper.m_NoteObjectsActionsCallbackInterface.OnInvertNoteColors;
                @InvertNoteColors.canceled -= m_Wrapper.m_NoteObjectsActionsCallbackInterface.OnInvertNoteColors;
            }
            m_Wrapper.m_NoteObjectsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @UpdateNoteDirection.started += instance.OnUpdateNoteDirection;
                @UpdateNoteDirection.performed += instance.OnUpdateNoteDirection;
                @UpdateNoteDirection.canceled += instance.OnUpdateNoteDirection;
                @InvertNoteColors.started += instance.OnInvertNoteColors;
                @InvertNoteColors.performed += instance.OnInvertNoteColors;
                @InvertNoteColors.canceled += instance.OnInvertNoteColors;
            }
        }
    }
    public NoteObjectsActions @NoteObjects => new NoteObjectsActions(this);

    // Obstacle Objects
    private readonly InputActionMap m_ObstacleObjects;
    private IObstacleObjectsActions m_ObstacleObjectsActionsCallbackInterface;
    private readonly InputAction m_ObstacleObjects_ToggleHyperWall;
    private readonly InputAction m_ObstacleObjects_ChangeWallDuration;
    public struct ObstacleObjectsActions
    {
        private @CMInput m_Wrapper;
        public ObstacleObjectsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleHyperWall => m_Wrapper.m_ObstacleObjects_ToggleHyperWall;
        public InputAction @ChangeWallDuration => m_Wrapper.m_ObstacleObjects_ChangeWallDuration;
        public InputActionMap Get() { return m_Wrapper.m_ObstacleObjects; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(ObstacleObjectsActions set) { return set.Get(); }
        public void SetCallbacks(IObstacleObjectsActions instance)
        {
            if (m_Wrapper.m_ObstacleObjectsActionsCallbackInterface != null)
            {
                @ToggleHyperWall.started -= m_Wrapper.m_ObstacleObjectsActionsCallbackInterface.OnToggleHyperWall;
                @ToggleHyperWall.performed -= m_Wrapper.m_ObstacleObjectsActionsCallbackInterface.OnToggleHyperWall;
                @ToggleHyperWall.canceled -= m_Wrapper.m_ObstacleObjectsActionsCallbackInterface.OnToggleHyperWall;
                @ChangeWallDuration.started -= m_Wrapper.m_ObstacleObjectsActionsCallbackInterface.OnChangeWallDuration;
                @ChangeWallDuration.performed -= m_Wrapper.m_ObstacleObjectsActionsCallbackInterface.OnChangeWallDuration;
                @ChangeWallDuration.canceled -= m_Wrapper.m_ObstacleObjectsActionsCallbackInterface.OnChangeWallDuration;
            }
            m_Wrapper.m_ObstacleObjectsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ToggleHyperWall.started += instance.OnToggleHyperWall;
                @ToggleHyperWall.performed += instance.OnToggleHyperWall;
                @ToggleHyperWall.canceled += instance.OnToggleHyperWall;
                @ChangeWallDuration.started += instance.OnChangeWallDuration;
                @ChangeWallDuration.performed += instance.OnChangeWallDuration;
                @ChangeWallDuration.canceled += instance.OnChangeWallDuration;
            }
        }
    }
    public ObstacleObjectsActions @ObstacleObjects => new ObstacleObjectsActions(this);

    // Event Objects
    private readonly InputActionMap m_EventObjects;
    private IEventObjectsActions m_EventObjectsActionsCallbackInterface;
    private readonly InputAction m_EventObjects_InvertEventValue;
    private readonly InputAction m_EventObjects_TweakEventValue;
    public struct EventObjectsActions
    {
        private @CMInput m_Wrapper;
        public EventObjectsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @InvertEventValue => m_Wrapper.m_EventObjects_InvertEventValue;
        public InputAction @TweakEventValue => m_Wrapper.m_EventObjects_TweakEventValue;
        public InputActionMap Get() { return m_Wrapper.m_EventObjects; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(EventObjectsActions set) { return set.Get(); }
        public void SetCallbacks(IEventObjectsActions instance)
        {
            if (m_Wrapper.m_EventObjectsActionsCallbackInterface != null)
            {
                @InvertEventValue.started -= m_Wrapper.m_EventObjectsActionsCallbackInterface.OnInvertEventValue;
                @InvertEventValue.performed -= m_Wrapper.m_EventObjectsActionsCallbackInterface.OnInvertEventValue;
                @InvertEventValue.canceled -= m_Wrapper.m_EventObjectsActionsCallbackInterface.OnInvertEventValue;
                @TweakEventValue.started -= m_Wrapper.m_EventObjectsActionsCallbackInterface.OnTweakEventValue;
                @TweakEventValue.performed -= m_Wrapper.m_EventObjectsActionsCallbackInterface.OnTweakEventValue;
                @TweakEventValue.canceled -= m_Wrapper.m_EventObjectsActionsCallbackInterface.OnTweakEventValue;
            }
            m_Wrapper.m_EventObjectsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @InvertEventValue.started += instance.OnInvertEventValue;
                @InvertEventValue.performed += instance.OnInvertEventValue;
                @InvertEventValue.canceled += instance.OnInvertEventValue;
                @TweakEventValue.started += instance.OnTweakEventValue;
                @TweakEventValue.performed += instance.OnTweakEventValue;
                @TweakEventValue.canceled += instance.OnTweakEventValue;
            }
        }
    }
    public EventObjectsActions @EventObjects => new EventObjectsActions(this);

    // +Custom Events Container
    private readonly InputActionMap m_CustomEventsContainer;
    private ICustomEventsContainerActions m_CustomEventsContainerActionsCallbackInterface;
    private readonly InputAction m_CustomEventsContainer_AssignObjectstoTrack;
    private readonly InputAction m_CustomEventsContainer_SetTrackFilter;
    private readonly InputAction m_CustomEventsContainer_CreateNewEventType;
    public struct CustomEventsContainerActions
    {
        private @CMInput m_Wrapper;
        public CustomEventsContainerActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @AssignObjectstoTrack => m_Wrapper.m_CustomEventsContainer_AssignObjectstoTrack;
        public InputAction @SetTrackFilter => m_Wrapper.m_CustomEventsContainer_SetTrackFilter;
        public InputAction @CreateNewEventType => m_Wrapper.m_CustomEventsContainer_CreateNewEventType;
        public InputActionMap Get() { return m_Wrapper.m_CustomEventsContainer; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CustomEventsContainerActions set) { return set.Get(); }
        public void SetCallbacks(ICustomEventsContainerActions instance)
        {
            if (m_Wrapper.m_CustomEventsContainerActionsCallbackInterface != null)
            {
                @AssignObjectstoTrack.started -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnAssignObjectstoTrack;
                @AssignObjectstoTrack.performed -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnAssignObjectstoTrack;
                @AssignObjectstoTrack.canceled -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnAssignObjectstoTrack;
                @SetTrackFilter.started -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnSetTrackFilter;
                @SetTrackFilter.performed -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnSetTrackFilter;
                @SetTrackFilter.canceled -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnSetTrackFilter;
                @CreateNewEventType.started -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnCreateNewEventType;
                @CreateNewEventType.performed -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnCreateNewEventType;
                @CreateNewEventType.canceled -= m_Wrapper.m_CustomEventsContainerActionsCallbackInterface.OnCreateNewEventType;
            }
            m_Wrapper.m_CustomEventsContainerActionsCallbackInterface = instance;
            if (instance != null)
            {
                @AssignObjectstoTrack.started += instance.OnAssignObjectstoTrack;
                @AssignObjectstoTrack.performed += instance.OnAssignObjectstoTrack;
                @AssignObjectstoTrack.canceled += instance.OnAssignObjectstoTrack;
                @SetTrackFilter.started += instance.OnSetTrackFilter;
                @SetTrackFilter.performed += instance.OnSetTrackFilter;
                @SetTrackFilter.canceled += instance.OnSetTrackFilter;
                @CreateNewEventType.started += instance.OnCreateNewEventType;
                @CreateNewEventType.performed += instance.OnCreateNewEventType;
                @CreateNewEventType.canceled += instance.OnCreateNewEventType;
            }
        }
    }
    public CustomEventsContainerActions @CustomEventsContainer => new CustomEventsContainerActions(this);

    // Node Editor
    private readonly InputActionMap m_NodeEditor;
    private INodeEditorActions m_NodeEditorActionsCallbackInterface;
    private readonly InputAction m_NodeEditor_ToggleNodeEditor;
    public struct NodeEditorActions
    {
        private @CMInput m_Wrapper;
        public NodeEditorActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleNodeEditor => m_Wrapper.m_NodeEditor_ToggleNodeEditor;
        public InputActionMap Get() { return m_Wrapper.m_NodeEditor; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(NodeEditorActions set) { return set.Get(); }
        public void SetCallbacks(INodeEditorActions instance)
        {
            if (m_Wrapper.m_NodeEditorActionsCallbackInterface != null)
            {
                @ToggleNodeEditor.started -= m_Wrapper.m_NodeEditorActionsCallbackInterface.OnToggleNodeEditor;
                @ToggleNodeEditor.performed -= m_Wrapper.m_NodeEditorActionsCallbackInterface.OnToggleNodeEditor;
                @ToggleNodeEditor.canceled -= m_Wrapper.m_NodeEditorActionsCallbackInterface.OnToggleNodeEditor;
            }
            m_Wrapper.m_NodeEditorActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ToggleNodeEditor.started += instance.OnToggleNodeEditor;
                @ToggleNodeEditor.performed += instance.OnToggleNodeEditor;
                @ToggleNodeEditor.canceled += instance.OnToggleNodeEditor;
            }
        }
    }
    public NodeEditorActions @NodeEditor => new NodeEditorActions(this);

    // BPM Tapper
    private readonly InputActionMap m_BPMTapper;
    private IBPMTapperActions m_BPMTapperActionsCallbackInterface;
    private readonly InputAction m_BPMTapper_ToggleBPMTapper;
    public struct BPMTapperActions
    {
        private @CMInput m_Wrapper;
        public BPMTapperActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleBPMTapper => m_Wrapper.m_BPMTapper_ToggleBPMTapper;
        public InputActionMap Get() { return m_Wrapper.m_BPMTapper; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(BPMTapperActions set) { return set.Get(); }
        public void SetCallbacks(IBPMTapperActions instance)
        {
            if (m_Wrapper.m_BPMTapperActionsCallbackInterface != null)
            {
                @ToggleBPMTapper.started -= m_Wrapper.m_BPMTapperActionsCallbackInterface.OnToggleBPMTapper;
                @ToggleBPMTapper.performed -= m_Wrapper.m_BPMTapperActionsCallbackInterface.OnToggleBPMTapper;
                @ToggleBPMTapper.canceled -= m_Wrapper.m_BPMTapperActionsCallbackInterface.OnToggleBPMTapper;
            }
            m_Wrapper.m_BPMTapperActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ToggleBPMTapper.started += instance.OnToggleBPMTapper;
                @ToggleBPMTapper.performed += instance.OnToggleBPMTapper;
                @ToggleBPMTapper.canceled += instance.OnToggleBPMTapper;
            }
        }
    }
    public BPMTapperActions @BPMTapper => new BPMTapperActions(this);

    // Pause Menu
    private readonly InputActionMap m_PauseMenu;
    private IPauseMenuActions m_PauseMenuActionsCallbackInterface;
    private readonly InputAction m_PauseMenu_PauseEditor;
    public struct PauseMenuActions
    {
        private @CMInput m_Wrapper;
        public PauseMenuActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @PauseEditor => m_Wrapper.m_PauseMenu_PauseEditor;
        public InputActionMap Get() { return m_Wrapper.m_PauseMenu; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PauseMenuActions set) { return set.Get(); }
        public void SetCallbacks(IPauseMenuActions instance)
        {
            if (m_Wrapper.m_PauseMenuActionsCallbackInterface != null)
            {
                @PauseEditor.started -= m_Wrapper.m_PauseMenuActionsCallbackInterface.OnPauseEditor;
                @PauseEditor.performed -= m_Wrapper.m_PauseMenuActionsCallbackInterface.OnPauseEditor;
                @PauseEditor.canceled -= m_Wrapper.m_PauseMenuActionsCallbackInterface.OnPauseEditor;
            }
            m_Wrapper.m_PauseMenuActionsCallbackInterface = instance;
            if (instance != null)
            {
                @PauseEditor.started += instance.OnPauseEditor;
                @PauseEditor.performed += instance.OnPauseEditor;
                @PauseEditor.canceled += instance.OnPauseEditor;
            }
        }
    }
    public PauseMenuActions @PauseMenu => new PauseMenuActions(this);

    // Selecting
    private readonly InputActionMap m_Selecting;
    private ISelectingActions m_SelectingActionsCallbackInterface;
    private readonly InputAction m_Selecting_DeselectAll;
    public struct SelectingActions
    {
        private @CMInput m_Wrapper;
        public SelectingActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @DeselectAll => m_Wrapper.m_Selecting_DeselectAll;
        public InputActionMap Get() { return m_Wrapper.m_Selecting; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(SelectingActions set) { return set.Get(); }
        public void SetCallbacks(ISelectingActions instance)
        {
            if (m_Wrapper.m_SelectingActionsCallbackInterface != null)
            {
                @DeselectAll.started -= m_Wrapper.m_SelectingActionsCallbackInterface.OnDeselectAll;
                @DeselectAll.performed -= m_Wrapper.m_SelectingActionsCallbackInterface.OnDeselectAll;
                @DeselectAll.canceled -= m_Wrapper.m_SelectingActionsCallbackInterface.OnDeselectAll;
            }
            m_Wrapper.m_SelectingActionsCallbackInterface = instance;
            if (instance != null)
            {
                @DeselectAll.started += instance.OnDeselectAll;
                @DeselectAll.performed += instance.OnDeselectAll;
                @DeselectAll.canceled += instance.OnDeselectAll;
            }
        }
    }
    public SelectingActions @Selecting => new SelectingActions(this);

    // Modifying Selection
    private readonly InputActionMap m_ModifyingSelection;
    private IModifyingSelectionActions m_ModifyingSelectionActionsCallbackInterface;
    private readonly InputAction m_ModifyingSelection_DeleteObjects;
    private readonly InputAction m_ModifyingSelection_ShiftinTime;
    private readonly InputAction m_ModifyingSelection_ShiftinPlace;
    private readonly InputAction m_ModifyingSelection_Cut;
    private readonly InputAction m_ModifyingSelection_Paste;
    private readonly InputAction m_ModifyingSelection_Copy;
    public struct ModifyingSelectionActions
    {
        private @CMInput m_Wrapper;
        public ModifyingSelectionActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @DeleteObjects => m_Wrapper.m_ModifyingSelection_DeleteObjects;
        public InputAction @ShiftinTime => m_Wrapper.m_ModifyingSelection_ShiftinTime;
        public InputAction @ShiftinPlace => m_Wrapper.m_ModifyingSelection_ShiftinPlace;
        public InputAction @Cut => m_Wrapper.m_ModifyingSelection_Cut;
        public InputAction @Paste => m_Wrapper.m_ModifyingSelection_Paste;
        public InputAction @Copy => m_Wrapper.m_ModifyingSelection_Copy;
        public InputActionMap Get() { return m_Wrapper.m_ModifyingSelection; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(ModifyingSelectionActions set) { return set.Get(); }
        public void SetCallbacks(IModifyingSelectionActions instance)
        {
            if (m_Wrapper.m_ModifyingSelectionActionsCallbackInterface != null)
            {
                @DeleteObjects.started -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnDeleteObjects;
                @DeleteObjects.performed -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnDeleteObjects;
                @DeleteObjects.canceled -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnDeleteObjects;
                @ShiftinTime.started -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnShiftinTime;
                @ShiftinTime.performed -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnShiftinTime;
                @ShiftinTime.canceled -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnShiftinTime;
                @ShiftinPlace.started -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnShiftinPlace;
                @ShiftinPlace.performed -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnShiftinPlace;
                @ShiftinPlace.canceled -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnShiftinPlace;
                @Cut.started -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnCut;
                @Cut.performed -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnCut;
                @Cut.canceled -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnCut;
                @Paste.started -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnPaste;
                @Paste.performed -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnPaste;
                @Paste.canceled -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnPaste;
                @Copy.started -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnCopy;
                @Copy.performed -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnCopy;
                @Copy.canceled -= m_Wrapper.m_ModifyingSelectionActionsCallbackInterface.OnCopy;
            }
            m_Wrapper.m_ModifyingSelectionActionsCallbackInterface = instance;
            if (instance != null)
            {
                @DeleteObjects.started += instance.OnDeleteObjects;
                @DeleteObjects.performed += instance.OnDeleteObjects;
                @DeleteObjects.canceled += instance.OnDeleteObjects;
                @ShiftinTime.started += instance.OnShiftinTime;
                @ShiftinTime.performed += instance.OnShiftinTime;
                @ShiftinTime.canceled += instance.OnShiftinTime;
                @ShiftinPlace.started += instance.OnShiftinPlace;
                @ShiftinPlace.performed += instance.OnShiftinPlace;
                @ShiftinPlace.canceled += instance.OnShiftinPlace;
                @Cut.started += instance.OnCut;
                @Cut.performed += instance.OnCut;
                @Cut.canceled += instance.OnCut;
                @Paste.started += instance.OnPaste;
                @Paste.performed += instance.OnPaste;
                @Paste.canceled += instance.OnPaste;
                @Copy.started += instance.OnCopy;
                @Copy.performed += instance.OnCopy;
                @Copy.canceled += instance.OnCopy;
            }
        }
    }
    public ModifyingSelectionActions @ModifyingSelection => new ModifyingSelectionActions(this);

    // UI Mode
    private readonly InputActionMap m_UIMode;
    private IUIModeActions m_UIModeActionsCallbackInterface;
    private readonly InputAction m_UIMode_ToggleUIMode;
    public struct UIModeActions
    {
        private @CMInput m_Wrapper;
        public UIModeActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleUIMode => m_Wrapper.m_UIMode_ToggleUIMode;
        public InputActionMap Get() { return m_Wrapper.m_UIMode; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(UIModeActions set) { return set.Get(); }
        public void SetCallbacks(IUIModeActions instance)
        {
            if (m_Wrapper.m_UIModeActionsCallbackInterface != null)
            {
                @ToggleUIMode.started -= m_Wrapper.m_UIModeActionsCallbackInterface.OnToggleUIMode;
                @ToggleUIMode.performed -= m_Wrapper.m_UIModeActionsCallbackInterface.OnToggleUIMode;
                @ToggleUIMode.canceled -= m_Wrapper.m_UIModeActionsCallbackInterface.OnToggleUIMode;
            }
            m_Wrapper.m_UIModeActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ToggleUIMode.started += instance.OnToggleUIMode;
                @ToggleUIMode.performed += instance.OnToggleUIMode;
                @ToggleUIMode.canceled += instance.OnToggleUIMode;
            }
        }
    }
    public UIModeActions @UIMode => new UIModeActions(this);

    // Song Speed
    private readonly InputActionMap m_SongSpeed;
    private ISongSpeedActions m_SongSpeedActionsCallbackInterface;
    private readonly InputAction m_SongSpeed_DecreaseSongSpeed;
    private readonly InputAction m_SongSpeed_IncreaseSongSpeed;
    public struct SongSpeedActions
    {
        private @CMInput m_Wrapper;
        public SongSpeedActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @DecreaseSongSpeed => m_Wrapper.m_SongSpeed_DecreaseSongSpeed;
        public InputAction @IncreaseSongSpeed => m_Wrapper.m_SongSpeed_IncreaseSongSpeed;
        public InputActionMap Get() { return m_Wrapper.m_SongSpeed; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(SongSpeedActions set) { return set.Get(); }
        public void SetCallbacks(ISongSpeedActions instance)
        {
            if (m_Wrapper.m_SongSpeedActionsCallbackInterface != null)
            {
                @DecreaseSongSpeed.started -= m_Wrapper.m_SongSpeedActionsCallbackInterface.OnDecreaseSongSpeed;
                @DecreaseSongSpeed.performed -= m_Wrapper.m_SongSpeedActionsCallbackInterface.OnDecreaseSongSpeed;
                @DecreaseSongSpeed.canceled -= m_Wrapper.m_SongSpeedActionsCallbackInterface.OnDecreaseSongSpeed;
                @IncreaseSongSpeed.started -= m_Wrapper.m_SongSpeedActionsCallbackInterface.OnIncreaseSongSpeed;
                @IncreaseSongSpeed.performed -= m_Wrapper.m_SongSpeedActionsCallbackInterface.OnIncreaseSongSpeed;
                @IncreaseSongSpeed.canceled -= m_Wrapper.m_SongSpeedActionsCallbackInterface.OnIncreaseSongSpeed;
            }
            m_Wrapper.m_SongSpeedActionsCallbackInterface = instance;
            if (instance != null)
            {
                @DecreaseSongSpeed.started += instance.OnDecreaseSongSpeed;
                @DecreaseSongSpeed.performed += instance.OnDecreaseSongSpeed;
                @DecreaseSongSpeed.canceled += instance.OnDecreaseSongSpeed;
                @IncreaseSongSpeed.started += instance.OnIncreaseSongSpeed;
                @IncreaseSongSpeed.performed += instance.OnIncreaseSongSpeed;
                @IncreaseSongSpeed.canceled += instance.OnIncreaseSongSpeed;
            }
        }
    }
    public SongSpeedActions @SongSpeed => new SongSpeedActions(this);

    // Cancel Placement
    private readonly InputActionMap m_CancelPlacement;
    private ICancelPlacementActions m_CancelPlacementActionsCallbackInterface;
    private readonly InputAction m_CancelPlacement_CancelPlacement;
    public struct CancelPlacementActions
    {
        private @CMInput m_Wrapper;
        public CancelPlacementActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @CancelPlacement => m_Wrapper.m_CancelPlacement_CancelPlacement;
        public InputActionMap Get() { return m_Wrapper.m_CancelPlacement; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CancelPlacementActions set) { return set.Get(); }
        public void SetCallbacks(ICancelPlacementActions instance)
        {
            if (m_Wrapper.m_CancelPlacementActionsCallbackInterface != null)
            {
                @CancelPlacement.started -= m_Wrapper.m_CancelPlacementActionsCallbackInterface.OnCancelPlacement;
                @CancelPlacement.performed -= m_Wrapper.m_CancelPlacementActionsCallbackInterface.OnCancelPlacement;
                @CancelPlacement.canceled -= m_Wrapper.m_CancelPlacementActionsCallbackInterface.OnCancelPlacement;
            }
            m_Wrapper.m_CancelPlacementActionsCallbackInterface = instance;
            if (instance != null)
            {
                @CancelPlacement.started += instance.OnCancelPlacement;
                @CancelPlacement.performed += instance.OnCancelPlacement;
                @CancelPlacement.canceled += instance.OnCancelPlacement;
            }
        }
    }
    public CancelPlacementActions @CancelPlacement => new CancelPlacementActions(this);

    // BPM Change Objects
    private readonly InputActionMap m_BPMChangeObjects;
    private IBPMChangeObjectsActions m_BPMChangeObjectsActionsCallbackInterface;
    private readonly InputAction m_BPMChangeObjects_ReplaceBPMModifier;
    private readonly InputAction m_BPMChangeObjects_ReplaceBPMinExistingBPMChangeClick;
    public struct BPMChangeObjectsActions
    {
        private @CMInput m_Wrapper;
        public BPMChangeObjectsActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ReplaceBPMModifier => m_Wrapper.m_BPMChangeObjects_ReplaceBPMModifier;
        public InputAction @ReplaceBPMinExistingBPMChangeClick => m_Wrapper.m_BPMChangeObjects_ReplaceBPMinExistingBPMChangeClick;
        public InputActionMap Get() { return m_Wrapper.m_BPMChangeObjects; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(BPMChangeObjectsActions set) { return set.Get(); }
        public void SetCallbacks(IBPMChangeObjectsActions instance)
        {
            if (m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface != null)
            {
                @ReplaceBPMModifier.started -= m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface.OnReplaceBPMModifier;
                @ReplaceBPMModifier.performed -= m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface.OnReplaceBPMModifier;
                @ReplaceBPMModifier.canceled -= m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface.OnReplaceBPMModifier;
                @ReplaceBPMinExistingBPMChangeClick.started -= m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface.OnReplaceBPMinExistingBPMChangeClick;
                @ReplaceBPMinExistingBPMChangeClick.performed -= m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface.OnReplaceBPMinExistingBPMChangeClick;
                @ReplaceBPMinExistingBPMChangeClick.canceled -= m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface.OnReplaceBPMinExistingBPMChangeClick;
            }
            m_Wrapper.m_BPMChangeObjectsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ReplaceBPMModifier.started += instance.OnReplaceBPMModifier;
                @ReplaceBPMModifier.performed += instance.OnReplaceBPMModifier;
                @ReplaceBPMModifier.canceled += instance.OnReplaceBPMModifier;
                @ReplaceBPMinExistingBPMChangeClick.started += instance.OnReplaceBPMinExistingBPMChangeClick;
                @ReplaceBPMinExistingBPMChangeClick.performed += instance.OnReplaceBPMinExistingBPMChangeClick;
                @ReplaceBPMinExistingBPMChangeClick.canceled += instance.OnReplaceBPMinExistingBPMChangeClick;
            }
        }
    }
    public BPMChangeObjectsActions @BPMChangeObjects => new BPMChangeObjectsActions(this);

    // Event Grid
    private readonly InputActionMap m_EventGrid;
    private IEventGridActions m_EventGridActionsCallbackInterface;
    private readonly InputAction m_EventGrid_ToggleLightPropagation;
    private readonly InputAction m_EventGrid_CycleLightPropagationUp;
    private readonly InputAction m_EventGrid_CycleLightPropagationDown;
    public struct EventGridActions
    {
        private @CMInput m_Wrapper;
        public EventGridActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleLightPropagation => m_Wrapper.m_EventGrid_ToggleLightPropagation;
        public InputAction @CycleLightPropagationUp => m_Wrapper.m_EventGrid_CycleLightPropagationUp;
        public InputAction @CycleLightPropagationDown => m_Wrapper.m_EventGrid_CycleLightPropagationDown;
        public InputActionMap Get() { return m_Wrapper.m_EventGrid; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(EventGridActions set) { return set.Get(); }
        public void SetCallbacks(IEventGridActions instance)
        {
            if (m_Wrapper.m_EventGridActionsCallbackInterface != null)
            {
                @ToggleLightPropagation.started -= m_Wrapper.m_EventGridActionsCallbackInterface.OnToggleLightPropagation;
                @ToggleLightPropagation.performed -= m_Wrapper.m_EventGridActionsCallbackInterface.OnToggleLightPropagation;
                @ToggleLightPropagation.canceled -= m_Wrapper.m_EventGridActionsCallbackInterface.OnToggleLightPropagation;
                @CycleLightPropagationUp.started -= m_Wrapper.m_EventGridActionsCallbackInterface.OnCycleLightPropagationUp;
                @CycleLightPropagationUp.performed -= m_Wrapper.m_EventGridActionsCallbackInterface.OnCycleLightPropagationUp;
                @CycleLightPropagationUp.canceled -= m_Wrapper.m_EventGridActionsCallbackInterface.OnCycleLightPropagationUp;
                @CycleLightPropagationDown.started -= m_Wrapper.m_EventGridActionsCallbackInterface.OnCycleLightPropagationDown;
                @CycleLightPropagationDown.performed -= m_Wrapper.m_EventGridActionsCallbackInterface.OnCycleLightPropagationDown;
                @CycleLightPropagationDown.canceled -= m_Wrapper.m_EventGridActionsCallbackInterface.OnCycleLightPropagationDown;
            }
            m_Wrapper.m_EventGridActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ToggleLightPropagation.started += instance.OnToggleLightPropagation;
                @ToggleLightPropagation.performed += instance.OnToggleLightPropagation;
                @ToggleLightPropagation.canceled += instance.OnToggleLightPropagation;
                @CycleLightPropagationUp.started += instance.OnCycleLightPropagationUp;
                @CycleLightPropagationUp.performed += instance.OnCycleLightPropagationUp;
                @CycleLightPropagationUp.canceled += instance.OnCycleLightPropagationUp;
                @CycleLightPropagationDown.started += instance.OnCycleLightPropagationDown;
                @CycleLightPropagationDown.performed += instance.OnCycleLightPropagationDown;
                @CycleLightPropagationDown.canceled += instance.OnCycleLightPropagationDown;
            }
        }
    }
    public EventGridActions @EventGrid => new EventGridActions(this);

    // MenusExtended
    private readonly InputActionMap m_MenusExtended;
    private IMenusExtendedActions m_MenusExtendedActionsCallbackInterface;
    private readonly InputAction m_MenusExtended_Tab;
    private readonly InputAction m_MenusExtended_LeaveMenu;
    public struct MenusExtendedActions
    {
        private @CMInput m_Wrapper;
        public MenusExtendedActions(@CMInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @Tab => m_Wrapper.m_MenusExtended_Tab;
        public InputAction @LeaveMenu => m_Wrapper.m_MenusExtended_LeaveMenu;
        public InputActionMap Get() { return m_Wrapper.m_MenusExtended; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(MenusExtendedActions set) { return set.Get(); }
        public void SetCallbacks(IMenusExtendedActions instance)
        {
            if (m_Wrapper.m_MenusExtendedActionsCallbackInterface != null)
            {
                @Tab.started -= m_Wrapper.m_MenusExtendedActionsCallbackInterface.OnTab;
                @Tab.performed -= m_Wrapper.m_MenusExtendedActionsCallbackInterface.OnTab;
                @Tab.canceled -= m_Wrapper.m_MenusExtendedActionsCallbackInterface.OnTab;
                @LeaveMenu.started -= m_Wrapper.m_MenusExtendedActionsCallbackInterface.OnLeaveMenu;
                @LeaveMenu.performed -= m_Wrapper.m_MenusExtendedActionsCallbackInterface.OnLeaveMenu;
                @LeaveMenu.canceled -= m_Wrapper.m_MenusExtendedActionsCallbackInterface.OnLeaveMenu;
            }
            m_Wrapper.m_MenusExtendedActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Tab.started += instance.OnTab;
                @Tab.performed += instance.OnTab;
                @Tab.canceled += instance.OnTab;
                @LeaveMenu.started += instance.OnLeaveMenu;
                @LeaveMenu.performed += instance.OnLeaveMenu;
                @LeaveMenu.canceled += instance.OnLeaveMenu;
            }
        }
    }
    public MenusExtendedActions @MenusExtended => new MenusExtendedActions(this);
    private int m_ChroMapperDefaultSchemeIndex = -1;
    public InputControlScheme ChroMapperDefaultScheme
    {
        get
        {
            if (m_ChroMapperDefaultSchemeIndex == -1) m_ChroMapperDefaultSchemeIndex = asset.FindControlSchemeIndex("ChroMapper Default");
            return asset.controlSchemes[m_ChroMapperDefaultSchemeIndex];
        }
    }
    public interface ICameraActions
    {
        void OnHoldtoMoveCamera(InputAction.CallbackContext context);
        void OnMoveCamera(InputAction.CallbackContext context);
        void OnRotateCamera(InputAction.CallbackContext context);
        void OnElevateCamera(InputAction.CallbackContext context);
        void OnAttachtoNoteGrid(InputAction.CallbackContext context);
        void OnToggleFullscreen(InputAction.CallbackContext context);
    }
    public interface IUtilsActions
    {
        void OnControlModifier(InputAction.CallbackContext context);
        void OnAltModifier(InputAction.CallbackContext context);
        void OnShiftModifier(InputAction.CallbackContext context);
        void OnMouseMovement(InputAction.CallbackContext context);
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
    }
    public interface INotePlacementActions
    {
        void OnDownNote(InputAction.CallbackContext context);
        void OnRightNote(InputAction.CallbackContext context);
        void OnUpNote(InputAction.CallbackContext context);
        void OnLeftNote(InputAction.CallbackContext context);
        void OnDotNote(InputAction.CallbackContext context);
    }
    public interface IEventPlacementActions
    {
        void OnRotation15Degrees(InputAction.CallbackContext context);
        void OnRotation30Degrees(InputAction.CallbackContext context);
        void OnRotation45Degrees(InputAction.CallbackContext context);
        void OnRotation60Degrees(InputAction.CallbackContext context);
        void OnNegativeRotationModifier(InputAction.CallbackContext context);
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
    public interface IBookmarksActions
    {
        void OnCreateNewBookmark(InputAction.CallbackContext context);
        void OnNextBookmark(InputAction.CallbackContext context);
        void OnPreviousBookmark(InputAction.CallbackContext context);
    }
    public interface IRefreshMapActions
    {
        void OnRefreshMap(InputAction.CallbackContext context);
    }
    public interface IPlatformSoloLightGroupActions
    {
        void OnSoloEventType(InputAction.CallbackContext context);
    }
    public interface IPlatformDisableableObjectsActions
    {
        void OnTogglePotentiallyObstructingObjects(InputAction.CallbackContext context);
    }
    public interface IPlaybackActions
    {
        void OnTogglePlaying(InputAction.CallbackContext context);
        void OnResetTime(InputAction.CallbackContext context);
    }
    public interface ITimelineActions
    {
        void OnChangeTimeandPrecision(InputAction.CallbackContext context);
    }
    public interface IBeatmapObjectsActions
    {
        void OnSelectObjects(InputAction.CallbackContext context);
        void OnQuickDelete(InputAction.CallbackContext context);
        void OnDeleteTool(InputAction.CallbackContext context);
        void OnMousePositionUpdate(InputAction.CallbackContext context);
    }
    public interface INoteObjectsActions
    {
        void OnUpdateNoteDirection(InputAction.CallbackContext context);
        void OnInvertNoteColors(InputAction.CallbackContext context);
    }
    public interface IObstacleObjectsActions
    {
        void OnToggleHyperWall(InputAction.CallbackContext context);
        void OnChangeWallDuration(InputAction.CallbackContext context);
    }
    public interface IEventObjectsActions
    {
        void OnInvertEventValue(InputAction.CallbackContext context);
        void OnTweakEventValue(InputAction.CallbackContext context);
    }
    public interface ICustomEventsContainerActions
    {
        void OnAssignObjectstoTrack(InputAction.CallbackContext context);
        void OnSetTrackFilter(InputAction.CallbackContext context);
        void OnCreateNewEventType(InputAction.CallbackContext context);
    }
    public interface INodeEditorActions
    {
        void OnToggleNodeEditor(InputAction.CallbackContext context);
    }
    public interface IBPMTapperActions
    {
        void OnToggleBPMTapper(InputAction.CallbackContext context);
    }
    public interface IPauseMenuActions
    {
        void OnPauseEditor(InputAction.CallbackContext context);
    }
    public interface ISelectingActions
    {
        void OnDeselectAll(InputAction.CallbackContext context);
    }
    public interface IModifyingSelectionActions
    {
        void OnDeleteObjects(InputAction.CallbackContext context);
        void OnShiftinTime(InputAction.CallbackContext context);
        void OnShiftinPlace(InputAction.CallbackContext context);
        void OnCut(InputAction.CallbackContext context);
        void OnPaste(InputAction.CallbackContext context);
        void OnCopy(InputAction.CallbackContext context);
    }
    public interface IUIModeActions
    {
        void OnToggleUIMode(InputAction.CallbackContext context);
    }
    public interface ISongSpeedActions
    {
        void OnDecreaseSongSpeed(InputAction.CallbackContext context);
        void OnIncreaseSongSpeed(InputAction.CallbackContext context);
    }
    public interface ICancelPlacementActions
    {
        void OnCancelPlacement(InputAction.CallbackContext context);
    }
    public interface IBPMChangeObjectsActions
    {
        void OnReplaceBPMModifier(InputAction.CallbackContext context);
        void OnReplaceBPMinExistingBPMChangeClick(InputAction.CallbackContext context);
    }
    public interface IEventGridActions
    {
        void OnToggleLightPropagation(InputAction.CallbackContext context);
        void OnCycleLightPropagationUp(InputAction.CallbackContext context);
        void OnCycleLightPropagationDown(InputAction.CallbackContext context);
    }
    public interface IMenusExtendedActions
    {
        void OnTab(InputAction.CallbackContext context);
        void OnLeaveMenu(InputAction.CallbackContext context);
    }
}
