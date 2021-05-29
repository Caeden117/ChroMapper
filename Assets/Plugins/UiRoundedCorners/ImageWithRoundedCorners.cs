using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ImageWithRoundedCorners : MonoBehaviour {
	private static readonly int Props = Shader.PropertyToID("_WidthHeightRadius");

	[SerializeField] private Material material;
	[HideInInspector] public Material mat;
	[Tooltip("When this is false the inserted material will change the inserted material shader.")] public bool cloneMaterial = true;
	public float radius;

	void OnRectTransformDimensionsChange(){
		Refresh();
	}
	
	private void OnValidate()
	{
		if (cloneMaterial)
		{
			if (mat == null || mat == material)
			{
				mat = new Material(material);
				Image i = GetComponent<Image>();
				i.material = mat;
				i.material.name = "Inherited From Round Corners";
			}
		}
		else mat = material;

		Refresh();
	}

	private void Refresh(){
		var rect = ((RectTransform) transform).rect;
        //I am tired of exceptions that dont give me the gameobject in question so I'm slightly modifying this script.
        try
        {
            if (mat != null) mat.SetVector(Props, new Vector4(rect.width, rect.height, radius, 0));
        }
        catch
        {
            Debug.LogError($"ImageWithRoundedCorners: Material is not assigned to GameObject {gameObject.name}");
        }
	}
}
