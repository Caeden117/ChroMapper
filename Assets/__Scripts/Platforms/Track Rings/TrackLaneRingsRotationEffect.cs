using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class TrackLaneRingsRotationEffect : MonoBehaviour
{
    [SerializeField] public TrackLaneRingsManager manager;
    [SerializeField] public float startupRotationAngle = 45;
    [SerializeField] public float startupRotationStep = 5;
    [SerializeField] public int startupRotationPropagationSpeed = 1;
    [SerializeField] public float startupRotationFlexySpeed = 1;
    [SerializeField] public float rotationStep = 90;

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
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RingRotationEffect effect = activeEffects[i];
            int progress = effect.progressPos;
            while (progress < effect.progressPos + effect.rotationPropagationSpeed && progress < rings.Length)
            {
                rings[progress].SetRotation(effect.rotationAngle + (float)progress * effect.rotationStep, effect.rotationFlexySpeed);
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

    public void AddRingRotationEvent(float angle, float step, int propagationSpeed, float flexySpeed, JSONNode customData = null)
    {
        if (customData?.HasKey("_reset") ?? false && customData["_reset"] == true)
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
        if (customData != null)
        {
            effect.rotationStep *= customData.HasKey("_stepMult") ? customData["_stepMult"].AsFloat : 1;
            effect.rotationPropagationSpeed *= Mathf.CeilToInt(customData.HasKey("_propMult") ? customData["_propMult"].AsFloat : 1);
            effect.rotationFlexySpeed *= customData.HasKey("_speedMult") ? customData["_speedMult"].AsFloat : 1;
            if (customData.HasKey("_direction")) multiplier = customData["_direction"] == 0 ? 1 : -1;
        }
        effect.rotationAngle = angle  + (rotationStep * multiplier);
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
        public int progressPos;
        public int rotationPropagationSpeed;

        public float rotationAngle;
        public float rotationStep;
        public float rotationFlexySpeed;
    }
}
