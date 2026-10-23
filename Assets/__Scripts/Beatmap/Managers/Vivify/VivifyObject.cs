using System;
using Beatmap.Animations;
using UnityEngine;

public class VivifyObject : MonoBehaviour
{
    public string AssetPath;
    public AudioTimeSyncController Atsc;
    public ObjectAnimator Animator;
    public Animator[] Animators;

    private ISync[] syncs = Array.Empty<ISync>();
    private ParticleSystem[] particles = Array.Empty<ParticleSystem>();

    public Vector3 InitialPosition;
    public Quaternion InitialRotation;
    public Vector3 InitialScale;

    public void Awake()
    {
        syncs = gameObject.GetComponentsInChildren<ISync>(true);
        particles = gameObject.GetComponentsInChildren<ParticleSystem>(true);
    }

    public void SongSynchronize(float startTime)
    {
        foreach (var sync in syncs)
        {
            sync.SetStartTime(startTime);
            // A seek can run after this frame's Update. Do not apply the previous sample's
            // speed again before the sync controller computes the new elapsed time.
            if (sync is SyncController controller && controller.isActiveAndEnabled) controller.Sync(0f);
        }
    }

    /// <summary>Restores the authored pose and disables track animation until this instance is attached again.</summary>
    public void Initialize()
    {
        if (Animator != null)
        {
            Animator.enabled = false;
            Animator.ResetData();
        }
        transform.localScale = InitialScale;
        transform.SetPositionAndRotation(InitialPosition, InitialRotation);
    }

    public void SetDefault()
    {
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
        InitialScale = transform.localScale;
    }

    public void SetAnimatorDefault()
    {
        Animator.OffsetPosition.Default = transform.localPosition;
        Animator.WorldPosition.Default = transform.position;
        Animator.LocalRotation.Default = transform.localRotation;
        Animator.WorldRotation.Default = transform.rotation;
        Animator.Scale.Default = transform.localScale;
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        // Rebind while active so pooled controllers restore child poses and parameter defaults
        // before custom-event properties capture their baseline for this lifetime.
        foreach (var animator in Animators)
        {
            animator.Rebind();
            animator.ResetControllerState(true);
            animator.Update(0f);
        }
        foreach (var system in particles)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (system.gameObject.activeInHierarchy && system.main.playOnAwake) system.Play(false);
        }
    }
    public void Deactivate() => gameObject.SetActive(false);
}
