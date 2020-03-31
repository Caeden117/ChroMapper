using System.Collections.Generic;
using UnityEngine;

public class TrackLaneRingsRotationEffect : MonoBehaviour
{
    [SerializeField] public TrackLaneRingsManager manager;
    [SerializeField] public float startupRotationAngle = 45;
    [SerializeField] public float startupRotationStep = 5;
    [SerializeField] public int startupRotationPropagationSpeed = 1;
    [SerializeField] public float startupRotationFlexySpeed = 1;

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

    public void AddRingRotationEvent(float angle, float step, int propagationSpeed, float flexySpeed)
    {
        RingRotationEffect effect = SpawnRingRotationEffect();
        effect.progressPos = 0;
        effect.rotationAngle = angle;
        effect.rotationStep = step;
        effect.rotationPropagationSpeed = propagationSpeed;
        effect.rotationFlexySpeed = flexySpeed;
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
