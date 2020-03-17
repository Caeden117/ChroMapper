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
			mat = new Material(material);
			Image i = GetComponent<Image>();
			i.material = mat;
			i.material.name = "Inherited From Round Corners";
		}
		else mat = material;

		Refresh();
	}

	private void Refresh(){
		var rect = ((RectTransform) transform).rect;
		mat.SetVector(Props, new Vector4(rect.width, rect.height, radius, 0));
	}
}
