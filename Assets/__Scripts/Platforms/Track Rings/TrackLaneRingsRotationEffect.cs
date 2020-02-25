using System.Collections.Generic;
using UnityEngine;

public class TrackLaneRingsRotationEffect : MonoBehaviour
{
    [SerializeField] public TrackLaneRingsManager manager;
    [SerializeField] public float startupRotationAngle = 45;
    [SerializeField] public float startupRotationStep = 5;
    [SerializeField] public float startupRotationPropagationSpeed = 1;
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

    private void Update()
    {
        effectIndicesToDelete.Clear();
        for (int i = 0; i < activeEffects.Count; i++)
        {
            for (int j = i + 1; j < activeEffects.Count; j++)
            {
                if (activeEffects[i].progressPos < activeEffects[j].progressPos)
                {
                    effectIndicesToDelete.Add(i);
                    break;
                }
            }
        }
        for (int k = 0; k < effectIndicesToDelete.Count; k++)
        {
            int index = effectIndicesToDelete[k];
            RecycleRingRotationEffect(activeEffects[index]);
            activeEffects.RemoveAt(index);
        }
        TrackLaneRing[] rings = manager.rings;
        for (int l = activeEffects.Count - 1; l >= 0; l--)//todo: convert to foreach if possible
        {
            RingRotationEffect ringRotationEffect = activeEffects[l];
            float progressPos = ringRotationEffect.progressPos;
            ringRotationEffect.progressPos += Time.deltaTime * ringRotationEffect.rotationPropagationSpeed;
            for (int m = 0; m < rings.Length; m++)
            {
                float num = (float)m / rings.Length;
                if (num >= progressPos && num < ringRotationEffect.progressPos)
                    rings[m].SetRotation(ringRotationEffect.rotationAngle + m * ringRotationEffect.rotationStep, ringRotationEffect.rotationFlexySpeed);
            }
            if (ringRotationEffect.progressPos > 1f)
            {
                RecycleRingRotationEffect(activeEffects[l]);
                activeEffects.RemoveAt(l);
            }
        }
    }

    public void AddRingRotationEvent(float angle, float step, float propagationSpeed, float flexySpeed)
    {
        RingRotationEffect effect = SpawnRingRotationEffect();
        effect.progressPos = 0;
        effect.rotationAngle = angle;
        effect.rotationStep = step;
        effect.rotationPropagationSpeed = propagationSpeed;
        effect.rotationFlexySpeed = flexySpeed;
        activeEffects.Add(effect);
    }

    private float GetFirstRingRotationAngle()
    {
        return manager.rings[0].GetRotation();
    }

    private float GetFirstRingDestinationRotationAngle()
    {
        return manager.rings[0].GetDestinationRotation();
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
        public float rotationAngle;
        public float rotationStep;
        public float rotationPropagationSpeed;
        public float rotationFlexySpeed;
    }
}
