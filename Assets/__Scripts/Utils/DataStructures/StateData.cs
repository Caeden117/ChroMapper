using System;
using Beatmap.Base;

public class StateData<T> : IEquatable<StateData<T>> where T : BaseObject
{
    private static int ID;
    private readonly int id = ID++; // maybe reference equality is better, idk

    public StateData(T data) => Base = data;

    public readonly T Base;
    public float StartTime = short.MinValue;
    public float EndTime = float.MaxValue;

    public bool Equals(StateData<T> other) => id == other!.id;
    public bool IsWithinRange(float value) => StartTime <= value && value < EndTime;
}
