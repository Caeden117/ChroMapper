using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    public RotationCallbackController RotationCallback;
    [SerializeField] private float rotationChangingTime = 1;
    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;
    [SerializeField] private bool rotateTransform = true;

    private float currentRotation;
    private int targetRotation;
    private List<Renderer> allRotationalRenderers = new List<Renderer>();

    private bool isRotating = true;
    private static readonly int Rotation = Shader.PropertyToID("_Rotation");
    private static readonly int Offset = Shader.PropertyToID("_Offset");

    public bool IsRotating { get => isRotating; set //todo is this useless?
        {
            isRotating = value;
            if (value)
                ChangeRotation(targetRotation);
            else transform.localEulerAngles = Vector3.zero;
        } }

    private void Start()
    {
        if (RotationCallback != null) Init();
    }

    public void Init()
    {
        RotationCallback.RotationChangedEvent += RotationChanged;
        if (!GetComponentsInChildren<Renderer>().Any()) return;
        allRotationalRenderers.AddRange(GetComponentsInChildren<Renderer>().Where(x => x.material.HasProperty("_Rotation")));
    }

    private void RotationChanged(bool natural, int rotation)
    {
        if (!RotationCallback.IsActive || !Settings.Instance.RotateTrack) return;
        targetRotation = rotation;
        if (!natural)
        {
            ChangeRotation(rotation);
            return;
        }
        StopAllCoroutines();
        if (gameObject.activeInHierarchy) StartCoroutine(ChangeRotationSmooth());
    }

    private IEnumerator ChangeRotationSmooth()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / rotationChangingTime;
            ChangeRotation(Mathf.Lerp(currentRotation, targetRotation, t));
            yield return new WaitForEndOfFrame();
        }
    }

    private void ChangeRotation(float rotation)
    {
        if (!isRotating) return;
        if (rotateTransform) transform.RotateAround(rotationPoint, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
        foreach (Renderer g in allRotationalRenderers)
        {
            g.material.SetFloat(Rotation, transform.eulerAngles.y);
            if (g.material.shader.name.Contains("Grid X"))
                g.material.SetFloat(Offset, transform.position.x * (rotateTransform ? -1 : 1));
        }
    }

    private void OnDestroy()
    {
        RotationCallback.RotationChangedEvent -= RotationChanged;
    }
}
