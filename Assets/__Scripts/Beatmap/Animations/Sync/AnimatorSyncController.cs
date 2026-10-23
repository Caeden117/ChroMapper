using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorSyncController : SyncController
{
    private Animator animator = null!;

    public override void ResetTime()
    {
        animator.Rebind();
        if (SongTime > StartTime)
            Sync((SongTime - StartTime) / Time.deltaTime);
        else
            Sync(0f);
    }

    public override void Sync(float speed) => animator.speed = speed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.updateMode = AnimatorUpdateMode.Normal;
        // The first sync advances from the spawn offset; Awake only initializes the animator.
        animator.Update(0f);
    }
}
