using System;
using System.Collections.Generic;

public class TrackModelState
{
    public enum Kind : byte
    {
        DirectionalNote,
        AnyNote,
        Bomb,
        BurstSlider,
        BurstSliderElement,
    }

    public class ModelContainer
    {
        public struct PriorityModel
        {
            public VisualModelSO Model;
            public int Priority;
        }

        public bool HasSet;
        public PriorityModel Model;
        public List<PriorityModel> Models = new();

        public void Set(VisualModelSO model, int priority)
        {
            HasSet = true;
            Model = new PriorityModel { Model = model, Priority = priority };
            Models.Clear();
        }

        public void Add(VisualModelSO model, int priority) =>
            Models.Add(new PriorityModel { Model = model, Priority = priority });

        public void Reset()
        {
            HasSet = false;
            Model = default;
            Models.Clear();
        }
    }

    public ModelContainer DirectionalNote = new();
    public ModelContainer AnyNote = new();
    public ModelContainer Bomb = new();
    public ModelContainer BurstSlider = new();
    public ModelContainer BurstSliderElement = new();

    public void SetModel(Kind kind, VisualModelSO model, int priority)
    {
        GetModel(kind).Set(model, priority);
    }

    public void AddModel(Kind kind, VisualModelSO model, int priority)
    {
        GetModel(kind).Add(model, priority);
    }

    public ModelContainer GetModel(Kind kind)
    {
        return kind switch
        {
            Kind.DirectionalNote => DirectionalNote,
            Kind.AnyNote => AnyNote,
            Kind.Bomb => Bomb,
            Kind.BurstSlider => BurstSlider,
            Kind.BurstSliderElement => BurstSliderElement,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public void Reset()
    {
        DirectionalNote.Reset();
        AnyNote.Reset();
        Bomb.Reset();
        BurstSlider.Reset();
        BurstSliderElement.Reset();
    }
}
