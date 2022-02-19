using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(BoxSlider), typeof(RawImage))]
[ExecuteInEditMode]
public class SVBoxSlider : MonoBehaviour
{
    [FormerlySerializedAs("picker")] public ColorPicker Picker;

    [SerializeField] private bool overrideComputeShader;
    private readonly int textureHeight = 100;
    private readonly int textureWidth = 100;

    private ComputeShader compute;
    private RawImage image;
    private int kernelID;

    private float lastH = -1;
    private bool listen = true;
    private RenderTexture renderTexture;

    private BoxSlider slider;
    private bool supportsComputeShaders;

    public RectTransform RectTransform => transform as RectTransform;

    private void Awake()
    {
        slider = GetComponent<BoxSlider>();
        image = GetComponent<RawImage>();
        if (Application.isPlaying)
        {
            supportsComputeShaders = SystemInfo.supportsComputeShaders; //check for compute shader support

#if PLATFORM_ANDROID
            supportsComputeShaders = false; //disable on android for now. Issue with compute shader
#endif

            if (overrideComputeShader) supportsComputeShaders = false;
            if (supportsComputeShaders)
                InitializeCompute();
            RegenerateSvTexture();
        }
    }


    private void OnEnable()
    {
        if (Application.isPlaying && Picker != null)
        {
            slider.ONValueChanged.AddListener(SliderChanged);
            Picker.OnhsvChanged.AddListener(HSVChanged);
        }
    }

    private void OnDisable()
    {
        if (Picker != null)
        {
            slider.ONValueChanged.RemoveListener(SliderChanged);
            Picker.OnhsvChanged.RemoveListener(HSVChanged);
        }
    }

    private void OnDestroy()
    {
        if (image.texture != null)
        {
            if (supportsComputeShaders)
                renderTexture.Release();
            else
                DestroyImmediate(image.texture);
        }
    }

    private void InitializeCompute()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.RGB111110Float)
            {
                enableRandomWrite = true
            };
            renderTexture.Create();
        }

        compute = Resources.Load<ComputeShader>("Shaders/Compute/GenerateSVTexture");
        kernelID = compute.FindKernel("CSMain");

        image.texture = renderTexture;
    }

    private void SliderChanged(float saturation, float value)
    {
        if (listen)
        {
            Picker.AssignColor(ColorValues.Saturation, saturation);
            Picker.AssignColor(ColorValues.Value, value);
        }

        listen = true;
    }

    private void HSVChanged(float h, float s, float v)
    {
        if (!lastH.Equals(h))
        {
            lastH = h;
            RegenerateSvTexture();
        }

        if (!s.Equals(slider.NormalizedValue))
        {
            listen = false;
            slider.NormalizedValue = s;
        }

        if (!v.Equals(slider.NormalizedValueY))
        {
            listen = false;
            slider.NormalizedValueY = v;
        }
    }

    private void RegenerateSvTexture()
    {
        if (supportsComputeShaders)
        {
            var hue = Picker != null ? Picker.H : 0;

            compute.SetTexture(kernelID, "Texture", renderTexture);
            compute.SetFloat("Hue", hue);

            var threadGroupsX = Mathf.CeilToInt(textureWidth / 32f);
            var threadGroupsY = Mathf.CeilToInt(textureHeight / 32f);
            compute.Dispatch(kernelID, threadGroupsX, threadGroupsY, 1);
        }
        else
        {
            double h = Picker != null ? Picker.H * 360 : 0;

            if (image.texture != null)
                DestroyImmediate(image.texture);

            var texture = new Texture2D(textureWidth, textureHeight) { hideFlags = HideFlags.DontSave };

            for (var s = 0; s < textureWidth; s++)
            {
                var colors = new Color32[textureHeight];
                for (var v = 0; v < textureHeight; v++)
                    colors[v] = HSVUtil.ConvertHsvToRgb(h, (float)s / 100, (float)v / 100, 1);
                texture.SetPixels32(s, 0, 1, textureHeight, colors);
            }

            texture.Apply();

            image.texture = texture;
        }
    }
}
