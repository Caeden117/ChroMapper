using System;
using UnityEngine;

public abstract class LightController : MonoBehaviour, IEnvironmentComponentUpdate
{
    public LightKind Kind;
    public int Type;
    public int ID;

    public virtual bool IsPhysical => false;

    protected static readonly int ColorId = Shader.PropertyToID("_Color");

    protected bool HasInitialized;
    protected MaterialPropertyBlock Mpb;
    [NonSerialized] public Color Color;

    protected virtual void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        HasInitialized = false;
        Color = new Color(0f, 0.5f, 1f);
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Start();
        };
    #endif
    }

    public void Start()
    {
        Mpb = new MaterialPropertyBlock();
        if (!HasInitialized)
        {
            HasInitialized = Initialize();
            if (!HasInitialized && this is not LightSink)
                Debug.LogError(
                    $"[LightController] Initialize() returned false on '{name}' ({GetType().Name}). Light will not function.");
        }

        SetColor(Color);
    }

    protected abstract bool Initialize();
    public abstract void SetColor(Color color);
    public virtual void SetColor(Color color, LightColorEventStateData evt, float time) => SetColor(color);

    public enum LightKind : byte
    {
        Basic,
        Group
    }

    public virtual bool ShouldInclude => false;
    public virtual bool ShouldRefresh => false;
    public virtual void Refresh() { }
}
