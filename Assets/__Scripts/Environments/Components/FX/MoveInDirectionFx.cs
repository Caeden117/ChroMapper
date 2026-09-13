using UnityEngine;

public class MoveInDirectionFx : FxTarget
{
    public Transform TargetTransform;
    public Vector3 MoveOrigin;
    public float MoveScale = 1f;

    private Vector3 startPosition;
    private Vector3 moveDirection;

    private void Awake()
    {
        startPosition = TargetTransform.localPosition;
        moveDirection = (startPosition - MoveOrigin).normalized;
    }

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value) =>
        TargetTransform.localPosition = startPosition + (value * MoveScale * moveDirection);
}
