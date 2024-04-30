using UnityEngine;

public class SpectrogramShellTextureController : MonoBehaviour
{
    private static readonly int valueCutoff = Shader.PropertyToID("_ValueCutoff");
    
    [SerializeField] private Renderer _spectrogram;

    private void Start()
    {
        var parent = _spectrogram.transform.parent;
        var origin = _spectrogram.transform.localPosition;
        var slices = Settings.Instance.SpectrogramSlices;
        
        for (var i = 0; i < slices; i++)
        {
            var progress = (float)i / slices;
                
            var spectrogramSlice = Instantiate(_spectrogram.gameObject, parent);
            spectrogramSlice.transform.localPosition = origin + (progress * Settings.Instance.SpectrogramHeight * Vector3.up);

            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetFloat(valueCutoff, progress);

            // meh
            var spectrogramRenderer = spectrogramSlice.GetComponent<Renderer>();            
            spectrogramRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
