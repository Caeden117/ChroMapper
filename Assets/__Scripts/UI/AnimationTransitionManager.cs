using System;
using System.Collections;
using UnityEngine;

public class AnimationTransitionManager : MonoBehaviour
{
    public static AnimationTransitionManager instance;

    public AnimationClip transitionAway;
    public AnimationClip transitionTo;
    public Animation animation;

    void Awake()
    {
        instance = this;
        if (transitionTo != null)
        {
            animation.clip = transitionTo;
            animation.Play();
        }
    }

    public void TransitionAway(Action onFinish)
    {
        animation.clip = transitionAway;
        animation.Play();
        StartCoroutine(CallbackOnComplete(onFinish));
    }

    private IEnumerator CallbackOnComplete(Action onFinish)
    {
        while (animation.isPlaying)
            yield return null;
        onFinish?.Invoke();
    }
}
