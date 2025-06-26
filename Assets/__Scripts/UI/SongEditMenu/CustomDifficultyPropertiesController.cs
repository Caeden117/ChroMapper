using UnityEngine;

public class CustomDifficultyPropertiesController : MonoBehaviour
{
    [SerializeField] private GameObject EditDialog;
    
    public void ToggleEditDialog() => EditDialog.SetActive(!EditDialog.activeSelf);
}
