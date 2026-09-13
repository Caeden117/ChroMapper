using UnityEngine;

public class SpectrogramRowPropertyAnimator : MonoBehaviour
{
    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public int DataIndex;
    [SerializeField] public string PropertyName;
    [SerializeField] public float Multiplier = 5f;
    [SerializeField] public AnimationCurve AnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] public SpectrogramDataProvider SpectrogramDataProvider;

    private int propertyId;
    private bool isInitialized;
    private float spectrogramValue;

    private void Awake()
    {
        InitIfNeeded();
        enabled = MpbController != null;
    }

    private void Update()
    {
        spectrogramValue = Multiplier * AnimationCurve.Evaluate(SpectrogramDataProvider.ProcessedSamples[DataIndex]);
        SetProperty();
        MpbController.ApplyChanges();
    }

    private void InitIfNeeded()
    {
        if (isInitialized) return;
        isInitialized = true;
        propertyId = Shader.PropertyToID(PropertyName);
    }

    private void SetProperty() => MpbController.Mpb.SetFloat(propertyId, spectrogramValue);

    public void SetMultiplier(float value) => Multiplier = value;
}
