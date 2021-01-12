using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class TrackLaneRingsRotationEffect : MonoBehaviour
{
    [SerializeField] public TrackLaneRingsManager manager;
    [SerializeField] public TrackLaneRingsManager mirrorManager;
    [SerializeField] public float startupRotationAngle = 45;
    [SerializeField] public float startupRotationStep = 5;
    [SerializeField] public float startupRotationPropagationSpeed = 1;
    [SerializeField] public float startupRotationFlexySpeed = 1;
    [SerializeField] public float rotationStep = 90;
    [SerializeField] public bool counterSpin = false;

    private List<RingRotationEffect> activeEffects;
    private List<RingRotationEffect> effectsPool;
    private List<int> effectIndicesToDelete = new List<int>();

    private void Awake()
    {
        activeEffects = new List<RingRotationEffect>(20);
        effectsPool = new List<RingRotationEffect>(20);
        for (int i = 0; i < effectsPool.Capacity; i++) effectsPool.Add(new RingRotationEffect());
    }

    private void Start()
    {
        AddRingRotationEvent(startupRotationAngle, startupRotationStep, startupRotationPropagationSpeed, startupRotationFlexySpeed);
    }

    private void FixedUpdate()
    {
        TrackLaneRing[] rings = manager.rings;
        TrackLaneRing[] mirrorRings = mirrorManager?.rings;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RingRotationEffect effect = activeEffects[i];
            int progress = (int) effect.progressPos;
            while (progress < effect.progressPos + effect.rotationPropagationSpeed && progress < rings.Length)
            {
                float destZ = effect.rotationAngle + progress * effect.rotationStep;
                rings[progress].SetRotation(destZ, effect.rotationFlexySpeed);

                if (mirrorRings != null)
                    mirrorRings[progress].SetRotation(destZ, effect.rotationFlexySpeed);

                progress++;
            }
            effect.progressPos += effect.rotationPropagationSpeed;
            if (effect.progressPos >= rings.Length)
            {
                RecycleRingRotationEffect(activeEffects[i]);
                activeEffects.RemoveAt(i);
            }
        }
    }

    public void AddRingRotationEvent(float angle, float step, float propagationSpeed, float flexySpeed, JSONNode customData = null)
    {
        if (customData != null && customData.HasKey("_reset") && customData["_reset"] == true)
        {
            AddRingRotationEvent(startupRotationAngle, startupRotationStep, startupRotationPropagationSpeed, startupRotationFlexySpeed);
            return;
        }
        RingRotationEffect effect = SpawnRingRotationEffect();
        int multiplier = Random.value < 0.5f ? 1 : -1;
        effect.progressPos = 0;
        effect.rotationStep = step;
        effect.rotationPropagationSpeed = propagationSpeed;
        effect.rotationFlexySpeed = flexySpeed;
        var rotationStepLocal = rotationStep;
        if (customData != null)
        {
            // Chroma still applies multipliers to individual values so they should be set first
            if (customData.HasKey("_step")) effect.rotationStep = customData["_step"];
            if (customData.HasKey("_prop")) effect.rotationPropagationSpeed = customData["_prop"];
            if (customData.HasKey("_speed")) effect.rotationFlexySpeed = customData["_speed"];
            if (customData.HasKey("_rotation")) rotationStepLocal = customData["_rotation"];

            if (customData.HasKey("_stepMult")) effect.rotationStep *= customData["_stepMult"];
            if (customData.HasKey("_propMult")) effect.rotationPropagationSpeed *= customData["_propMult"];
            if (customData.HasKey("_speedMult")) effect.rotationFlexySpeed *= customData["_speedMult"];
            if (customData.HasKey("_direction")) multiplier = customData["_direction"] == 0 ? 1 : -1;
            if (counterSpin && customData.HasKey("_counterSpin") && customData["_counterSpin"].AsBool) multiplier *= -1;
        }
        effect.rotationAngle = angle + (rotationStepLocal * multiplier);
        activeEffects.Add(effect);
    }

    private void RecycleRingRotationEffect(RingRotationEffect effect)
    {
        effectsPool.Add(effect);
    }

    private RingRotationEffect SpawnRingRotationEffect()
    {
        RingRotationEffect result;
        if (effectsPool.Count > 0)
        {
            result = effectsPool[0];
            effectsPool.RemoveAt(0);
        }
        else result = new RingRotationEffect();
        return result;
    }

    private class RingRotationEffect
    {
        public float progressPos;
        public float rotationPropagationSpeed;

        public float rotationAngle;
        public float rotationStep;
        public float rotationFlexySpeed;
    }
}
