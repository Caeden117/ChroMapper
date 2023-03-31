using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class TrackLaneRingsRotationEffect : MonoBehaviour
{
    [FormerlySerializedAs("manager")] public TrackLaneRingsManager Manager;
    [FormerlySerializedAs("mirrorManager")] public TrackLaneRingsManager MirrorManager;
    [FormerlySerializedAs("startupRotationAngle")] public float StartupRotationAngle = 45;
    [FormerlySerializedAs("startupRotationStep")] public float StartupRotationStep = 5;
    [FormerlySerializedAs("startupRotationPropagationSpeed")] public float StartupRotationPropagationSpeed = 1;
    [FormerlySerializedAs("startupRotationFlexySpeed")] public float StartupRotationFlexySpeed = 1;
    [FormerlySerializedAs("rotationStep")] public float RotationStep = 90;
    [FormerlySerializedAs("counterSpin")] public bool CounterSpin;

    private List<RingRotationEffect> activeEffects;
    private List<RingRotationEffect> effectsPool;

    private void Awake()
    {
        activeEffects = new List<RingRotationEffect>(20);
        effectsPool = new List<RingRotationEffect>(20);
        for (var i = 0; i < effectsPool.Capacity; i++) effectsPool.Add(new RingRotationEffect());
    }

    public void Reset()
    {
        for (var i = activeEffects.Count - 1; i >= 0; i--)
        {
            RecycleRingRotationEffect(activeEffects[i]);
            activeEffects.RemoveAt(i);
        }

        foreach (var trackLaneRing in Manager.Rings) trackLaneRing.Reset();

        if (MirrorManager == null) return;

        foreach (var mirrorManagerRing in MirrorManager.Rings) mirrorManagerRing.Reset();
    }

    private void Start() => AddRingRotationEvent(StartupRotationAngle, StartupRotationStep,
        StartupRotationPropagationSpeed, StartupRotationFlexySpeed);

    private void FixedUpdate()
    {
        var rings = Manager.Rings;
        for (var i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            var progress = (int)effect.ProgressPos;
            while (progress < effect.ProgressPos + effect.RotationPropagationSpeed && progress < rings.Length)
            {
                var destZ = effect.RotationAngle + (progress * effect.RotationStep);
                rings[progress].SetRotation(destZ, effect.RotationFlexySpeed);

                if (MirrorManager != null)
                    MirrorManager.Rings[progress].SetRotation(destZ, effect.RotationFlexySpeed);

                progress++;
            }

            effect.ProgressPos += effect.RotationPropagationSpeed;
            if (effect.ProgressPos >= rings.Length)
            {
                RecycleRingRotationEffect(activeEffects[i]);
                activeEffects.RemoveAt(i);
            }
        }
    }

    public void AddRingRotationEvent(float angle, float step, float propagationSpeed, float flexySpeed, float rotation,
        bool clockwise, bool counterSpinEvent)
    {
        var effect = SpawnRingRotationEffect();
        var multiplier = clockwise ? 1 : -1;
        effect.ProgressPos = 0;
        effect.RotationStep = step;
        effect.RotationPropagationSpeed = propagationSpeed;
        effect.RotationFlexySpeed = flexySpeed;

        if (CounterSpin && counterSpinEvent) multiplier *= -1;

        effect.RotationAngle = angle + (rotation * multiplier);
        activeEffects.Add(effect);
    }

    public void AddRingRotationEvent(float angle, float step, float propagationSpeed, float flexySpeed,
        JSONNode customData = null)
    {
        var multiplier = Random.value < 0.5f;
        var rotationStepLocal = RotationStep;
        var counterSpinEvent = false;
        if (customData != null)
        {
            // Chroma still applies multipliers to individual values so they should be set first
            if (customData.HasKey(MapLoader.heckUnderscore + "step")) step = customData[MapLoader.heckUnderscore + "step"];
            if (customData.HasKey(MapLoader.heckUnderscore + "prop")) propagationSpeed = customData[MapLoader.heckUnderscore + "prop"];
            if (customData.HasKey(MapLoader.heckUnderscore + "speed")) flexySpeed = customData[MapLoader.heckUnderscore + "speed"];
            if (customData.HasKey(MapLoader.heckRotation)) rotationStepLocal = customData[MapLoader.heckRotation];

            if (customData.HasKey("_stepMult")) step *= customData["_stepMult"];
            if (customData.HasKey("_propMult")) propagationSpeed *= customData["_propMult"];
            if (customData.HasKey("_speedMult")) flexySpeed *= customData["_speedMult"];
            if (customData.HasKey(MapLoader.heckUnderscore + "direction")) multiplier = customData[MapLoader.heckUnderscore + "direction"] == 0;
            counterSpinEvent = customData.HasKey("_counterSpin") && customData["_counterSpin"].AsBool;
        }

        if (customData != null && customData.HasKey("_reset") && customData["_reset"] == true)
        {
            AddRingRotationEvent(angle, 0, 50, 50, 90, counterSpinEvent, false);
            return;
        }

        AddRingRotationEvent(angle, step, propagationSpeed, flexySpeed, rotationStepLocal, multiplier,
            counterSpinEvent);
    }

    private void RecycleRingRotationEffect(RingRotationEffect effect) => effectsPool.Add(effect);

    private RingRotationEffect SpawnRingRotationEffect()
    {
        RingRotationEffect result;
        if (effectsPool.Count > 0)
        {
            result = effectsPool[0];
            effectsPool.RemoveAt(0);
        }
        else
        {
            result = new RingRotationEffect();
        }

        return result;
    }

    private class RingRotationEffect
    {
        public float ProgressPos;

        public float RotationAngle;
        public float RotationFlexySpeed;
        public float RotationPropagationSpeed;
        public float RotationStep;
    }
}
