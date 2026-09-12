using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class ParticleSystemData : EnvironmentComponentData<ParticleSystem>
{
    public bool UseAutoRandomSeed;
    public uint RandomSeed;

    public MainModuleData MainModule;
    public EmissionModuleData EmissionModule;
    public ShapeModuleData ShapeModule;
    public VelocityOverLifetimeModuleData VelocityOverLifetimeModule;
    public LimitVelocityOverLifetimeModuleData LimitVelocityOverLifetimeModule;
    public InheritVelocityModuleData InheritVelocityModule;
    public ForceOverLifetimeModuleData ForceOverLifetimeModule;
    public ColorOverLifetimeModuleData ColorOverLifetimeModule;
    public ColorBySpeedModuleData ColorBySpeedModule;
    public SizeOverLifetimeModuleData SizeOverLifetimeModule;
    public SizeBySpeedModuleData SizeBySpeedModule;
    public RotationOverLifetimeModuleData RotationOverLifetimeModule;
    public RotationBySpeedModuleData RotationBySpeedModule;
    public ExternalForcesModuleData ExternalForcesModule;
    public NoiseModuleData NoiseModule;
    public CollisionModuleData CollisionModule;
    public SubEmittersModuleData SubEmittersModule;
    public TextureSheetAnimationModuleData TextureSheetAnimationModule;
    public LightsModuleData LightsModule;
    public TrailModuleData TrailModule;
    public ParticleSystemRendererData Renderer;

    public override void FillComponents(GameObject self, ParticleSystem comp, CreateContainer container)
    {
        comp.useAutoRandomSeed = UseAutoRandomSeed;
        comp.randomSeed = RandomSeed;

        MainModule?.CopyTo(comp.main);

        var emissionModule = comp.emission;
        emissionModule.enabled = EmissionModule != null;
        EmissionModule?.CopyTo(emissionModule);

        var shape = comp.shape;
        shape.enabled = ShapeModule != null;
        ShapeModule?.CopyTo(shape);

        var velocityOverLifetime = comp.velocityOverLifetime;
        velocityOverLifetime.enabled = VelocityOverLifetimeModule != null;
        VelocityOverLifetimeModule?.CopyTo(velocityOverLifetime);

        var limitVelocityOverLifetime = comp.limitVelocityOverLifetime;
        limitVelocityOverLifetime.enabled = LimitVelocityOverLifetimeModule != null;
        LimitVelocityOverLifetimeModule?.CopyTo(limitVelocityOverLifetime);

        var inheritVelocity = comp.inheritVelocity;
        inheritVelocity.enabled = InheritVelocityModule != null;
        InheritVelocityModule?.CopyTo(inheritVelocity);

        var forceOverLifetime = comp.forceOverLifetime;
        forceOverLifetime.enabled = ForceOverLifetimeModule != null;
        ForceOverLifetimeModule?.CopyTo(forceOverLifetime);

        var colorOverLifetime = comp.colorOverLifetime;
        colorOverLifetime.enabled = ColorOverLifetimeModule != null;
        ColorOverLifetimeModule?.CopyTo(colorOverLifetime);

        var colorBySpeed = comp.colorBySpeed;
        colorBySpeed.enabled = ColorBySpeedModule != null;
        ColorBySpeedModule?.CopyTo(colorBySpeed);

        var sizeOverLifetime = comp.sizeOverLifetime;
        sizeOverLifetime.enabled = SizeOverLifetimeModule != null;
        SizeOverLifetimeModule?.CopyTo(sizeOverLifetime);

        var sizeBySpeed = comp.sizeBySpeed;
        sizeBySpeed.enabled = SizeBySpeedModule != null;
        SizeBySpeedModule?.CopyTo(sizeBySpeed);

        var rotationOverLifetime = comp.rotationOverLifetime;
        rotationOverLifetime.enabled = RotationOverLifetimeModule != null;
        RotationOverLifetimeModule?.CopyTo(rotationOverLifetime);

        var rotationBySpeed = comp.rotationBySpeed;
        rotationBySpeed.enabled = RotationBySpeedModule != null;
        RotationBySpeedModule?.CopyTo(rotationBySpeed);

        var externalForces = comp.externalForces;
        externalForces.enabled = ExternalForcesModule != null;
        ExternalForcesModule?.CopyTo(externalForces);

        var noise = comp.noise;
        noise.enabled = NoiseModule != null;
        NoiseModule?.CopyTo(noise);

        var collision = comp.collision;
        collision.enabled = CollisionModule != null;
        CollisionModule?.CopyTo(collision);

        var subEmitters = comp.subEmitters;
        subEmitters.enabled = SubEmittersModule != null;
        SubEmittersModule?.CopyTo(subEmitters, container);

        var textureSheetAnimation = comp.textureSheetAnimation;
        textureSheetAnimation.enabled = TextureSheetAnimationModule != null;
        TextureSheetAnimationModule?.CopyTo(textureSheetAnimation);

        var lights = comp.lights;
        lights.enabled = LightsModule != null;
        LightsModule?.CopyTo(lights);

        var trails = comp.trails;
        trails.enabled = TrailModule != null;
        TrailModule?.CopyTo(trails);

        Renderer?.CopyTo(comp.GetComponent<ParticleSystemRenderer>(), container);
    }

    public class MainModuleData
    {
        public float Duration;
        public bool Loop;
        public bool Prewarm;
        public MinMaxCurveData StartDelay;
        public MinMaxCurveData StartLifetime;
        public MinMaxCurveData StartSpeed;
        public bool StartSize3D;
        public MinMaxCurveData[] StartSize;
        public bool StartRotation3D;
        public MinMaxCurveData[] StartRotation;
        public float FlipRotation;
        public MinMaxCurveGradientData StartColor;
        public MinMaxCurveData GravityMultiplier;
        public string SimulationSpace;
        public float SimulationSpeed;
        public string ScalingMode;
        public bool PlayOnAwake;
        public string EmitterVelocityMode;
        public int MaxParticles;
        public string StopAction;
        public string CullingMode;
        public string RingBufferMode;

        public void CopyTo(ParticleSystem.MainModule module)
        {
            module.duration = Duration;
            module.loop = Loop;
            module.prewarm = Prewarm;
            module.startDelay = StartDelay.Create();
            module.startLifetime = StartLifetime.Create();
            module.startSpeed = StartSpeed.Create();
            module.startSize3D = StartSize3D;
            module.startSizeX = StartSize[0].Create();
            module.startSizeY = StartSize[1].Create();
            module.startSizeZ = StartSize[2].Create();
            module.startRotation3D = StartRotation3D;
            module.startRotationX = StartRotation[0].Create();
            module.startRotationY = StartRotation[1].Create();
            module.startRotationZ = StartRotation[2].Create();
            module.flipRotation = FlipRotation;
            module.startColor = StartColor.Create();
            module.gravityModifier = GravityMultiplier.Create();
            module.simulationSpace = Enum.Parse<ParticleSystemSimulationSpace>(SimulationSpace);
            module.simulationSpeed = SimulationSpeed;
            module.scalingMode = Enum.Parse<ParticleSystemScalingMode>(ScalingMode);
            module.playOnAwake = PlayOnAwake;
            module.emitterVelocityMode = Enum.Parse<ParticleSystemEmitterVelocityMode>(EmitterVelocityMode);
            module.maxParticles = MaxParticles;
            module.stopAction = Enum.Parse<ParticleSystemStopAction>(StopAction);
            module.cullingMode = Enum.Parse<ParticleSystemCullingMode>(CullingMode);
            module.ringBufferMode = Enum.Parse<ParticleSystemRingBufferMode>(RingBufferMode);
        }
    }

    public class EmissionModuleData
    {
        public MinMaxCurveData RateOverTime;
        public MinMaxCurveData RateOverDistance;
        public BurstData[] Bursts;

        public void CopyTo(ParticleSystem.EmissionModule module)
        {
            module.rateOverTime = RateOverTime.Create();
            module.rateOverDistance = RateOverDistance.Create();
            module.SetBursts(Bursts.Select(b => b.Create()).ToArray());
        }
    }

    public class ShapeModuleData
    {
        public string ShapeType;
        public float Radius;
        public float RadiusThickness;
        public float Arc;
        public string ArcMode;
        public float ArcSpread;

        // Fields present in JSON but missing from your class:
        public float[] Position;
        public float[] Rotation;
        public float[] Scale;
        public string Texture;
        public bool AlignToDirection;
        public float RandomDirectionAmount;
        public float RandomPositionAmount;
        public float SphericalDirectionAmount;

        public void CopyTo(ParticleSystem.ShapeModule module)
        {
            module.shapeType = Enum.Parse<ParticleSystemShapeType>(ShapeType);
            module.radius = Radius;
            module.radiusThickness = RadiusThickness;
            module.arc = Arc;
            module.arcMode = Enum.Parse<ParticleSystemShapeMultiModeValue>(ArcMode);
            module.arcSpread = ArcSpread;

            if (Position != null)
                module.position = new Vector3(Position[0], Position[1], Position[2]);
            if (Rotation != null)
                module.rotation = new Vector3(Rotation[0], Rotation[1], Rotation[2]);
            if (Scale != null)
                module.scale = new Vector3(Scale[0], Scale[1], Scale[2]);

            module.alignToDirection = AlignToDirection;
            module.randomDirectionAmount = RandomDirectionAmount;
            module.randomPositionAmount = RandomPositionAmount;
            module.sphericalDirectionAmount = SphericalDirectionAmount;
        }
    }

    public class VelocityOverLifetimeModuleData
    {
        public MinMaxCurveData X;
        public MinMaxCurveData Y;
        public MinMaxCurveData Z;
        public string Space;
        public MinMaxCurveData OrbitalX;
        public MinMaxCurveData OrbitalY;
        public MinMaxCurveData OrbitalZ;
        public MinMaxCurveData OrbitalOffsetX;
        public MinMaxCurveData OrbitalOffsetY;
        public MinMaxCurveData OrbitalOffsetZ;
        public MinMaxCurveData Radial;
        public MinMaxCurveData SpeedModifier;

        public void CopyTo(ParticleSystem.VelocityOverLifetimeModule module)
        {
            module.x = X.Create();
            module.y = Y.Create();
            module.z = Z.Create();
            module.space = Enum.Parse<ParticleSystemSimulationSpace>(Space);
            module.orbitalX = OrbitalX.Create();
            module.orbitalY = OrbitalY.Create();
            module.orbitalZ = OrbitalZ.Create();
            module.orbitalOffsetX = OrbitalOffsetX.Create();
            module.orbitalOffsetY = OrbitalOffsetY.Create();
            module.orbitalOffsetZ = OrbitalOffsetZ.Create();
            module.radial = Radial.Create();
            module.speedModifier = SpeedModifier.Create();
        }
    }

    public class LimitVelocityOverLifetimeModuleData
    {
        public bool SeparateAxes;
        public MinMaxCurveData Limit;
        public float Dampen;
        public MinMaxCurveData Drag;
        public bool MultiplyDragByParticleSize;
        public bool MultiplyDragByParticleVelocity;

        public void CopyTo(ParticleSystem.LimitVelocityOverLifetimeModule module)
        {
            module.separateAxes = SeparateAxes;
            module.limit = Limit.Create();
            module.dampen = Dampen;
            module.drag = Drag.Create();
            module.multiplyDragByParticleSize = MultiplyDragByParticleSize;
            module.multiplyDragByParticleVelocity = MultiplyDragByParticleVelocity;
        }
    }

    public class InheritVelocityModuleData
    {
        public void CopyTo(ParticleSystem.InheritVelocityModule module)
        {
        }
    }

    public class ForceOverLifetimeModuleData
    {
        public MinMaxCurveData X;
        public MinMaxCurveData Y;
        public MinMaxCurveData Z;
        public string Space;
        public bool Randomized;

        public void CopyTo(ParticleSystem.ForceOverLifetimeModule module)
        {
            module.x = X.Create();
            module.y = Y.Create();
            module.z = Z.Create();
            module.space = Enum.Parse<ParticleSystemSimulationSpace>(Space);
            module.randomized = Randomized;
        }
    }

    public class ColorOverLifetimeModuleData
    {
        public MinMaxCurveGradientData Color;

        public void CopyTo(ParticleSystem.ColorOverLifetimeModule module)
        {
            module.color = Color.Create();
        }
    }

    public class ColorBySpeedModuleData
    {
        public MinMaxCurveGradientData Color;
        public Vector2 Range;

        public void CopyTo(ParticleSystem.ColorBySpeedModule module)
        {
            module.color = Color.Create();
            module.range = Range;
        }
    }

    public class SizeOverLifetimeModuleData
    {
        public bool SeparateAxes;
        public MinMaxCurveData Size;
        public MinMaxCurveData X;
        public MinMaxCurveData Y;
        public MinMaxCurveData Z;

        public void CopyTo(ParticleSystem.SizeOverLifetimeModule module)
        {
            module.separateAxes = SeparateAxes;
            if (Size != null) module.size = Size.Create();
            if (X != null) module.x = X.Create();
            if (Y != null) module.y = Y.Create();
            if (Z != null) module.z = Z.Create();
        }
    }

    public class SizeBySpeedModuleData
    {
        public bool SeparateAxes;
        public MinMaxCurveData Size;
        public MinMaxCurveData X;
        public MinMaxCurveData Y;
        public MinMaxCurveData Z;
        public Vector2 Range;

        public void CopyTo(ParticleSystem.SizeBySpeedModule module)
        {
            module.separateAxes = SeparateAxes;
            if (Size != null) module.size = Size.Create();
            if (X != null) module.x = X.Create();
            if (Y != null) module.y = Y.Create();
            if (Z != null) module.z = Z.Create();
            module.range = Range;
        }
    }

    public class RotationOverLifetimeModuleData
    {
        public bool SeparateAxes;
        public MinMaxCurveData X;
        public MinMaxCurveData Y;
        public MinMaxCurveData Z;

        public void CopyTo(ParticleSystem.RotationOverLifetimeModule module)
        {
            module.separateAxes = SeparateAxes;
            if (X != null) module.x = X.Create();
            if (Y != null) module.y = Y.Create();
            if (Z != null) module.z = Z.Create();
        }
    }

    public class RotationBySpeedModuleData
    {
        public void CopyTo(ParticleSystem.RotationBySpeedModule module)
        {
        }
    }

    public class ExternalForcesModuleData
    {
        public void CopyTo(ParticleSystem.ExternalForcesModule module)
        {
        }
    }

    public class NoiseModuleData
    {
        public bool SeparateAxes;
        public MinMaxCurveData Strength;
        public float Frequency;
        public MinMaxCurveData ScrollSpeed;
        public bool Damping;
        public int OctaveCount;
        public float OctaveMultiplier;
        public float OctaveScale;
        public string Quality;
        public bool RemapEnabled;
        public MinMaxCurveData Remap;
        public MinMaxCurveData PositionAmount;
        public MinMaxCurveData RotationAmount;
        public MinMaxCurveData SizeAmount;

        public void CopyTo(ParticleSystem.NoiseModule module)
        {
            module.separateAxes = SeparateAxes;
            module.strength = Strength.Create();
            module.frequency = Frequency;
            module.scrollSpeed = ScrollSpeed.Create();
            module.damping = Damping;
            module.octaveCount = OctaveCount;
            module.octaveMultiplier = OctaveMultiplier;
            module.octaveScale = OctaveScale;
            module.quality = Enum.Parse<ParticleSystemNoiseQuality>(Quality);
            module.remapEnabled = RemapEnabled;
            module.remap = Remap.Create();
            module.positionAmount = PositionAmount.Create();
            module.rotationAmount = RotationAmount.Create();
            module.sizeAmount = SizeAmount.Create();
        }
    }

    public class CollisionModuleData
    {
        public void CopyTo(ParticleSystem.CollisionModule module)
        {
        }
    }

    public class SubEmittersModuleData
    {
        public SubEmitterData[] SubEmitters;

        public void CopyTo(ParticleSystem.SubEmittersModule module, CreateContainer container)
        {
            foreach (var data in SubEmitters)
            {
                module.AddSubEmitter(
                    container.ComponentInstances[data.SubEmitter].Instance as ParticleSystem,
                    Enum.Parse<ParticleSystemSubEmitterType>(data.Type),
                    (ParticleSystemSubEmitterProperties)data.Inherit,
                    data.Probability);
            }
        }
    }

    public class SubEmitterData
    {
        public int SubEmitter;
        public string Type;
        public int Inherit;
        public float Probability;
    }

    public class TextureSheetAnimationModuleData
    {
        public string Mode;
        public int NumTilesX;
        public int NumTilesY;
        public string Animation;
        public string TimeMode;
        public MinMaxCurveData FrameOverTime;
        public MinMaxCurveData StartFrame;
        public int CycleCount;
        public dynamic UVChannelMask;

        public void CopyTo(ParticleSystem.TextureSheetAnimationModule module)
        {
            module.mode = Enum.Parse<ParticleSystemAnimationMode>(Mode);
            module.numTilesX = NumTilesX;
            module.numTilesY = NumTilesY;
            module.animation = Enum.Parse<ParticleSystemAnimationType>(Animation);
            module.timeMode = Enum.Parse<ParticleSystemAnimationTimeMode>(TimeMode);
            module.frameOverTime = FrameOverTime.Create();
            module.startFrame = StartFrame.Create();
            module.cycleCount = CycleCount;
            module.uvChannelMask = UVChannelMask is string c
                ? Enum.Parse<UVChannelFlags>(c)
                : (UVChannelFlags)UVChannelMask;
        }
    }

    public class LightsModuleData
    {
        public void CopyTo(ParticleSystem.LightsModule module)
        {
        }
    }

    public class TrailModuleData
    {
        public void CopyTo(ParticleSystem.TrailModule module)
        {
        }
    }

    public class ParticleSystemRendererData
    {
        public int InstanceId;

        public string RenderMode;
        public string[] Meshes;
        public float NormalDirection;
        public string Material;
        public string SortMode;
        public float SortingFudge;
        public float MinParticleSize;
        public float MaxParticleSize;
        public string Alignment;
        public Vector3 Flip;
        public bool AllowRoll;
        public Vector3 Pivot;
        public string MaskInteraction;
        public string ShadowCastingMode;
        public bool RecieveShadows;
        public float ShadowBias;
        public string MotionVectorGenerationMode;
        public int SortingLayerId;
        public string ReflectionProbeUsage;
        public bool? UseCustomVertexStreams;
        public string[] VertexStreams;
        public bool? UseCustomTrailVertexStreams;
        public string[] TrailVertexStreams;
        public float? VelocityScale;
        public float? LengthScale;
        public float? CameraVelocityScale;
        public bool? FreeformStretching;
        public bool? RotateWithStretchDirection;
        public bool? EnableGPUInstancing;
        public string MeshDistribution;

        public void CopyTo(ParticleSystemRenderer module, CreateContainer container)
        {
            module.renderMode = Enum.Parse<ParticleSystemRenderMode>(RenderMode);
            module.normalDirection = NormalDirection;
            module.material = container.GetMaterialSafe(Material);
            module.sortMode = Enum.Parse<ParticleSystemSortMode>(SortMode);
            module.sortingFudge = SortingFudge;
            module.minParticleSize = MinParticleSize;
            module.maxParticleSize = MaxParticleSize;
            module.alignment = Enum.Parse<ParticleSystemRenderSpace>(Alignment);
            module.flip = Flip;
            module.allowRoll = AllowRoll;
            module.pivot = Pivot;
            module.maskInteraction = Enum.Parse<SpriteMaskInteraction>(MaskInteraction);
            module.shadowCastingMode = Enum.Parse<ShadowCastingMode>(ShadowCastingMode);
            module.receiveShadows = RecieveShadows;
            module.shadowBias = ShadowBias;
            module.motionVectorGenerationMode = Enum.Parse<MotionVectorGenerationMode>(MotionVectorGenerationMode);
            module.sortingLayerID = SortingLayerId;
            module.reflectionProbeUsage = Enum.Parse<ReflectionProbeUsage>(ReflectionProbeUsage);
            if (UseCustomVertexStreams == true && VertexStreams != null)
                module.SetActiveVertexStreams(
                    VertexStreams.Select(Enum.Parse<ParticleSystemVertexStream>).ToList());
            if (UseCustomTrailVertexStreams == true && TrailVertexStreams != null)
                module.SetActiveTrailVertexStreams(
                    TrailVertexStreams.Select(Enum.Parse<ParticleSystemVertexStream>).ToList());
            if (VelocityScale.HasValue) module.velocityScale = VelocityScale.Value;
            if (LengthScale.HasValue) module.lengthScale = LengthScale.Value;
            if (CameraVelocityScale.HasValue) module.cameraVelocityScale = CameraVelocityScale.Value;
            if (FreeformStretching.HasValue) module.freeformStretching = FreeformStretching.Value;
            if (RotateWithStretchDirection.HasValue)
                module.rotateWithStretchDirection = RotateWithStretchDirection.Value;
            if (EnableGPUInstancing.HasValue) module.enableGPUInstancing = EnableGPUInstancing.Value;
            if (!string.IsNullOrEmpty(MeshDistribution))
                module.meshDistribution = Enum.Parse<ParticleSystemMeshDistribution>(MeshDistribution);

            if (Meshes is { Length: > 0 })
            {
                var meshes = Meshes
                    .Select(hash => container.Library.Meshes.GetSafe(hash)
                        ?? throw new InvalidOperationException(
                            $"Mesh hash '{hash}' for particle system renderer {InstanceId} was not found."))
                    .ToArray();
                module.SetMeshes(meshes);
            }
        }
    }

    public class MinMaxCurveData
    {
        public string Mode;
        public float Constant;
        public AnimationCurveData Curve;
        public float ConstantMin;
        public float ConstantMax;

        public ParticleSystem.MinMaxCurve Create() =>
            Mode switch
            {
                "Constant" => new ParticleSystem.MinMaxCurve(Constant),
                "TwoConstants" => new ParticleSystem.MinMaxCurve(ConstantMin, ConstantMax),
                "Curve" => new ParticleSystem.MinMaxCurve(1f, Curve?.Create()),
                "TwoCurves" => new ParticleSystem.MinMaxCurve(1f, Curve?.Create(), Curve?.Create()),
                _ => new ParticleSystem.MinMaxCurve(Constant)
            };
    }

    public class MinMaxCurveGradientData
    {
        public string Mode;
        public Color Color;
        public Color ColorMin;
        public Color ColorMax;
        public GradientData Gradient;
        public GradientData GradientMin;
        public GradientData GradientMax;

        public ParticleSystem.MinMaxGradient Create() =>
            Mode switch
            {
                "Color" => new ParticleSystem.MinMaxGradient(Color),
                "Gradient" => new ParticleSystem.MinMaxGradient(Gradient?.Create()),
                "TwoColors" => new ParticleSystem.MinMaxGradient(ColorMin, ColorMax),
                "TwoGradients" => new ParticleSystem.MinMaxGradient(GradientMin?.Create(), GradientMax?.Create()),
                "RandomColor" => new ParticleSystem.MinMaxGradient
                {
                    mode = ParticleSystemGradientMode.RandomColor,
                    colorMin = ColorMin,
                    colorMax = ColorMax
                },
                _ => new ParticleSystem.MinMaxGradient
                {
                    mode = Enum.Parse<ParticleSystemGradientMode>(Mode),
                    color = Color,
                    colorMin = ColorMin,
                    colorMax = ColorMax,
                    gradient = Gradient?.Create(),
                    gradientMin = GradientMin?.Create(),
                    gradientMax = GradientMax?.Create()
                }
            };
    }

    public class GradientData
    {
        public string Mode;
        public string ColorSpace;
        public GradientAlphaKeyData[] AlphaKeys;
        public GradientColorKeyData[] ColorKeys;

        public Gradient Create() =>
            new Gradient()
            {
                mode = Enum.Parse<GradientMode>(Mode),
                colorSpace = ColorSpace == "Uninitialized"
                    ? UnityEngine.ColorSpace.Gamma
                    : Enum.Parse<UnityEngine.ColorSpace>(ColorSpace),
                alphaKeys = AlphaKeys.Select(x => x.Create()).ToArray(),
                colorKeys = ColorKeys.Select(x => x.Create()).ToArray(),
            };
    }

    public class GradientAlphaKeyData
    {
        public float Alpha;
        public float Time;

        public GradientAlphaKey Create() => new() { alpha = Alpha, time = Time };
    }

    public class GradientColorKeyData
    {
        public Color Color;
        public float Time;

        public GradientColorKey Create() => new() { color = Color, time = Time };
    }

    public class BurstData
    {
        public float Time;
        public MinMaxCurveData Count;
        public int CycleCount;
        public float RepeatInterval;
        public float Probability;

        public ParticleSystem.Burst Create() =>
            new()
            {
                time = Time,
                count = Count.Create(),
                cycleCount = CycleCount,
                repeatInterval = RepeatInterval,
                probability = Probability
            };
    }
}
