using Newtonsoft.Json;

#nullable enable
/// <summary>
/// A single object in an environment, with its properties and components.
/// </summary>
public class EnvironmentDataObject
{
    [JsonProperty("name")] public string GameObjectName = string.Empty;
    [JsonProperty("id")] public string ChromaID = string.Empty;
    public bool ActiveSelf;
    public string Layer = "Default";
    public EnvironmentComponents Components = new();

    // We can leave this to standard Newtonsoft.Json serialization.
    public class EnvironmentComponents
    {
        // Unity Components
        public TransformData[]? Transform;
        public BoxColliderData[]? BoxCollider;
        public CapsuleColliderData[]? CapsuleCollider;
        public SphereColliderData[]? SphereCollider;
        public MeshColliderData[]? MeshCollider;
        public MeshFilterData[]? MeshFilter;
        public MeshRendererData[]? MeshRenderer;
        public ParticleSystemData[]? ParticleSystem;
        public RigidbodyData[]? RigidBody;
        public SpringJointData[]? SpringJoint;
        public SpriteRendererData[]? SpriteRenderer;

        // Bloom Pre Pass
        public BloomPrePassBackgroundColorsGradientData[]? BloomPrePassBackgroundColorsGradient;

        public BloomPrePassBackgroundColorsGradientFromColorSchemeColorsData[]?
            BloomPrePassBackgroundColorsGradientFromColorSchemeColors;

        public BloomPrePassBackgroundNonLightInstancedGroupRendererData[]?
            BloomPrePassBackgroundNonLightInstancedGroupRenderer;

        public BloomPrePassBackgroundNonLightRendererData[]? BloomPrePassBackgroundNonLightRenderer;

        // Lighting
        public BloomPrePassBackgroundColorsGradientTintColorWithLightIdsData[]?
            BloomPrePassBackgroundColorsGradientTintColorWithLightIds;

        public DirectionalLightWithIdData[]? DirectionalLightWithId;
        public EnableRendererLightWithIdData[]? EnableRendererLightWithId;
        public InstancedMaterialLightWithIdData[]? InstancedMaterialLightWithId;
        public MaterialLightWithIdData[]? MaterialLightWithId;
        public ParticleSystemLightWithIdData[]? ParticleSystemLightWithId;
        public RectangleFakeGlowLightWithIdData[]? RectangleFakeGlowLightWithLightId;
        public SpriteArrayLightWithIdData[]? SpriteArrayLightWithId;
        public SpriteLightWithIdData[]? SpriteLightWithId;
        public TubeBloomPrePassLightWithIdData[]? TubeBloomPrePassLightWithId;

        // Runtime Lighting
        public BloomPrePassBackgroundColorsGradientTintColorWithLightIdData[]?
            BloomPrePassBackgroundColorsGradientTintColorWithLightId;

        public ColorArrayLightWithIdsData[]? ColorArrayLightWithIds;
        public DirectionalLightWithGroupIdsData[]? DirectionalLightWithGroupIds;
        public DirectionalLightWithIdsData[]? DirectionalLightWithIds;
        public GlobalShaderColorLightWithIdsData[]? GlobalShaderColorLightWithIds;
        public LightmapLightWithIdsData[]? LightmapLightWithIds;
        public LightmapLightsWithIdsData[]? LightmapLightsWithIds;
        public MaterialLightWithIdsData[]? MaterialLightWithIds;
        public MixedLightsColorSetterRuntimeLightWithIdsData[]? MixedLightsColorSetterRuntimeLightWithIds;
        public ParticleSystemLightWithIdsData[]? ParticleSystemLightWithIds;
        public PointLightWithIdsData[]? PointLightWithIds;

        // Controller
        public ParametricBoxControllerData[]? ParametricBoxController;
        public Parametric3SliceSpriteControllerData[]? Parametric3SliceSpriteController;

        // Basic
        public BackgroundTextureGradientSwitchEventEffectData[]? BackgroundTextureGradientSwitchEventEffect;
        public GameObjectIntSwitchEventEffectData[]? GameObjectIntSwitchEventEffect;
        public GameObjectSwitchEventEffectData[]? GameObjectSwitchEventEffect;
        public HydraulicCarJumpEffectData[]? HydraulicCarJumpEffect;
        public HydraulicCarSuspensionEffectData[]? HydraulicCarSuspensionEffect;
        public LightRotationEventEffectData[]? LightRotationEventEffect;
        public LightPairRotationEventEffectData[]? LightPairRotationEventEffect;
        public LightPairSinMoveEventEffectData[]? LightPairSinMoveEventEffect;
        public LightSwitchEventEffectData[]? LightSwitchEventEffect;
        public MeshRendererSwitchEventEffectData[]? MeshRendererSwitchEventEffect;
        public MovementBeatmapEventEffectData[]? MovementBeatmapEventEffect;
        public ParticleSystemContinuousEventEffectData[]? ParticleSystemContinuousEventEffect;
        public ParticleSystemEmitEventEffectData[]? ParticleSystemEmitEventEffect;
        public ParticleSystemEventEffectData[]? ParticleSystemEventEffect;
        public SmoothStepPositionEventEffectData[]? SmoothStepPositionEventEffect;
        public SmoothStepPositionGroupEventEffectData[]? SmoothStepPositionGroupEventEffect;
        public TextureIntSwitchEventEffectData[]? TextureIntSwitchEventEffect;
        public TrackLaneRingData[]? TrackLaneRing;
        public TrackLaneRingsManagerData[]? TrackLaneRingsManager;
        public TrackLaneRingsPositionStepEffectSpawnerData[]? TrackLaneRingsPositionStepEffectSpawner;
        public TrackLaneRingsRotationEffectData[]? TrackLaneRingsRotationEffect;
        public TrackLaneRingsRotationEffectSpawnerData[]? TrackLaneRingsRotationEffectSpawner;

        // MPB
        public MaterialPropertyBlockControllerData[]? MaterialPropertyBlockController;

        public MaterialPropertyBlockControllerArrayRandomValueSetterData[]?
            MaterialPropertyBlockControllerArrayRandomValueSetter;

        public MaterialPropertyBlockColorSetterData[]? MaterialPropertyBlockColorSetter;
        public MaterialPropertyBlockControllerRandomValueSetterData[]? MaterialPropertyBlockControllerRandomValueSetter;
        public MaterialPropertyBlockPositionUpdaterData[]? MaterialPropertyBlockPositionUpdater;
        public MaterialPropertyBlockRandomValueSetterData[]? MaterialPropertyBlockRandomValueSetter;
        public MaterialPropertyValuesSetterData[]? MaterialPropertyValuesSetter;

        // FX
        public AlphaFloatFxGroupEffectTargetData[]? AlphaFloatFxGroupEffectTarget;
        public ColliderEventEffectData[]? ColliderEventEffect;
        public CombineGroupIdToVector4FloatFxGroupEffectTargetData[]? CombineGroupIdToVector4FloatFxGroupEffectTarget;
        public FloatArrayMaterialPropertyEffectTargetData[]? FloatArrayMaterialPropertyEffectTarget;
        public FloatFxGroupEffectCollectionTargetData[]? FloatFxGroupEffectCollectionTarget;
        public FloatLocalScaleEffectData[]? FloatLocalScaleEffect;
        public FloatMaterialPropertyEffectTargetData[]? FloatMaterialPropertyEffectTarget;
        public FloatSDFPointScaleEffectData[]? FloatSDFPointScaleEffect;
        public FloatTextureProcessor3DMappingFloatEffectTargetData[]? FloatTextureProcessor3DMappingFloatEffectTarget;
        public FloatTextureProcessor3DMappingVectorEffectTargetData[]? FloatTextureProcessor3DMappingVectorEffectTarget;

        public FloatTextureProcessor3DMaterialSwitchEffectTargetData[]?
            FloatTextureProcessor3DMaterialSwitchEffectTarget;

        public FloatTextureProcessor3DParameterEffectTargetData[]? FloatTextureProcessor3DParameterEffectTarget;
        public FloatTextureProcessor3DPresetEffectTargetData[]? FloatTextureProcessor3DPresetEffectTarget;
        public MoveInDirectionEffectData[]? MoveInDirectionEffect;

        public Parametric3SliceSpriteWidthEndFloatFxEffectTargetData[]?
            Parametric3SliceSpriteWidthEndFloatFxEffectTarget;

        public SpectrogramMultiplierFloatFxEffectTargetData[]? SpectrogramMultiplierFloatFxEffectTarget;
        public StepFloatMaterialPropertyEffectTargetData[]? StepFloatMaterialPropertyEffectTarget;
        public SwitchGameObjectArrayEffectTargetData[]? SwitchGameObjectArrayEffectTarget;
        public SwitchGameObjectEffectTargetData[]? SwitchGameObjectEffectTarget;
        public VertexDisplacementFloatFxGroupEffectTargetData[]? VertexDisplacementFloatFxGroupEffectTarget;

        // Others
        public TextureProcessor3DData[]? TextureProcessor3D;
        public GridElementControllerData[]? GridElementController;
        public BakedLightsNormalizerData[]? BakedLightsNormalizer;
        public BakedReflectionProbeData[]? BakedReflectionProbe;
        public MirrorData[]? Mirror;
        public SDFPointData[]? SDFPoint;
        public SDFArrayManagerData[]? SDFArrayManager;
        public SpectrogramData[]? Spectrogram;
        public SpectrogramRowPropertyAnimatorData[]? SpectrogramRowPropertyAnimator;
        public TransformSpectrogramData[]? TransformSpectrogram;
        public LightWithIdManagerData[]? LightWithIdManager;
        public RectangleFakeGlowData[]? RectangleFakeGlow;
        public TubeBloomPrePassLightCollisionData[]? TubeBloomPrePassLightCollisionEffect;
        public TubeBloomPrePassLightReflectionData[]? TubeBloomPrePassLightReflectionEffect;
        public CopyPositionData[]? CopyPosition;
        public PointLightData[]? PointLight;
        public DirectionalLightData[]? DirectionalLight;
        public LightManagerData[]? LightManager;
        public BurstFireEffectData[]? BurstFireEffect;
        public ContinuousFireEffectData[]? ContinuousFireEffect;

        // GLS
        public LightColorGroupData[]? LightColorGroup;
        public LightColorGroupEffectManagerData[]? LightColorGroupEffectManager;

        public LightRotationGroupData[]? LightRotationGroup;
        public LightRotationGroupEffectManagerData[]? LightRotationGroupEffectManager;

        public LightTranslationGroupData[]? LightTranslationGroup;
        public LightTranslationGroupEffectManagerData[]? LightTranslationGroupEffectManager;

        public FloatFxGroupData[]? FloatFxGroup;
        public FloatFxGroupEffectData[]? FloatFxGroupEffect;
        public FloatFxGroupEffectManagerData[]? FloatFxGroupEffectManager;
        public FloatFxGroupEffectManagerData[]? TriggerFloatFxGroupEffectManager;
    }
}
#nullable restore
