using System;
using Boo.Lang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace __Scripts.UI.PopupMessage
{
    public class CM_PopUpMessage : MonoBehaviour
    {
        //SETTINGS
        
        public static readonly Color DefaultButtonColor = Color.white;
        
        //END OF SETTINGS

        [SerializeField] private TMP_FontAsset greenFont;
        [SerializeField] private TMP_FontAsset redFont;
        [SerializeField] private TMP_FontAsset goldFont;
        
        public static readonly Color EmptyColor = Color.clear;


        public CM_PopUpBuilder LoadPreset(DialogBoxPresetType presetType, dynamic title, dynamic message, TMP_FontAsset titleFont, TMP_FontAsset messageFont)
        {
            switch (presetType)
            {
                case DialogBoxPresetType.Ok:
                    return CreateBuilder().Create(
                        new CM_Text().Create(title, titleFont), 
                        new CM_Text().Create(message, messageFont))
                        .AddButton(new CM_Text().Create("Okay", greenFont));
                    break;
                case DialogBoxPresetType.OkCancel:
                    break;
                case DialogBoxPresetType.YesNo:
                    break;
                case DialogBoxPresetType.YesNoCancel:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(presetType), presetType, null);
                /*
               dialogBox.SetParams(message, result, "OK", null, null, greenFont);
               dialogBox.SetParams(message, result, "OK", "Cancel", null, greenFont, goldFont);
               dialogBox.SetParams(message, result, "Yes", "No", null, greenFont, redFont);
               dialogBox.SetParams(message, result, "Yes", "No", "Cancel", greenFont, redFont, goldFont);
                */
            }

            return null;
        }
        
        public CM_PopUpBuilder CreateBuilder()
        {
            return new CM_PopUpBuilder();
        }
    }
    
    public class CM_PopUpBuilder
    {
        protected CM_Text Title;
        protected CM_Text Message;
        protected List<CM_Button> Buttons;

        protected GameObject DialogBox;

        public CM_PopUpBuilder Create(CM_Text title, CM_Text message)
        {
            Title = title;
            Message = message;
            return this;
        }

        //Default Button Color
        public CM_PopUpBuilder AddButton(CM_Button button)
        {
            Buttons.Add(button);
            return this;
        }
        
        public void Build(GameObject parent)
        {

            //DialogBox.AddComponent<>()
            
            
            if(parent != null) DialogBox.transform.parent = parent.transform;
        }
    }

    public class CM_Button
    {
        public CM_Button Create(CM_Text text)
        {
            return Create(text, CM_PopUpMessage.EmptyColor);
        }
        
        public CM_Button Create(CM_Text text, Color buttonColor)
        {
            if (buttonColor == CM_PopUpMessage.EmptyColor) buttonColor = CM_PopUpMessage.DefaultButtonColor;
            return this;
        }
    }
    
    public class CM_Text
    {
        protected string Text;
        protected TextMeshProUGUI Font;

        public CM_Text Create(dynamic text, TMP_FontAsset font)
        {
           Text = text.ToString();
           TextMeshProUGUI mesh = new TextMeshProUGUI();//replace with gameobject
           mesh.font = font;
           Font = mesh;
            return this;
        }
        
        public CM_Text Create(dynamic text, TextMeshProUGUI font)
        {
           Text = text.ToString();
            Font = font;
            return this;
        }
        
        //This will disable the auto font size.
        public CM_Text SetFontSize(float fontSize)
        {
            Font.fontSize = fontSize;
            Font.enableAutoSizing = false;
            return this;
        }
    }
    
    public enum DialogBoxPresetType
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel
    }
}