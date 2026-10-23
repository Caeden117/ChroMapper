using System;
using System.Linq;
using UnityEngine;
using ZLinq;

public interface IPointDefinitionValue
{
    float W { get; set; }
    float X { get; set; }
    float Y { get; set; }
    float Z { get; set; }

    float T { get; set; }
    Func<float, float> Easing { get; set; }

    bool SplineCatmullRom { get; set; }
    bool LerpHSV { get; set; }

    float Float => X;
    Vector3 Vector2 => new(X, Y);
    Vector3 Vector3 => new(X, Y, Z);
    Vector4 Vector4 => new(X, Y, Z, W);
    Quaternion Quaternion => Quaternion.Euler(X, Y, Z);
    Color Color => new(X, Y, Z, W);
}

public abstract class PointDefinitionValueBase : IPointDefinitionValue
{
    public float Px;
    public float Py;
    public float Pz;
    public float Pw;

    public abstract float W { get; set; }
    public abstract float X { get; set; }
    public abstract float Y { get; set; }
    public abstract float Z { get; set; }

    public float T { get; set; }
    public Func<float, float> Easing { get; set; } = global::Easing.Linear;

    public bool SplineCatmullRom { get; set; }
    public bool LerpHSV { get; set; }

    protected PointOperation[] PointOperations = Array.Empty<PointOperation>();

    public void SetGetter(int index, float val)
    {
        switch (index)
        {
            case 0:
                Px = val;
                break;
            case 1:
                Py = val;
                break;
            case 2:
                Pz = val;
                break;
            case 3:
                Pw = val;
                break;
        }
    }

    public void AddOperation(PointOperation operation) => PointOperations = PointOperations.Append(operation).ToArray();
}

public sealed class PointDefinitionValue : PointDefinitionValueBase
{
    public override float X
    {
        get => Px;
        set => Px = value;
    }

    public override float Y
    {
        get => Py;
        set => Py = value;
    }

    public override float Z
    {
        get => Pz;
        set => Pz = value;
    }

    public override float W
    {
        get => Pw;
        set => Pw = value;
    }

    public PointDefinitionValue(IPointDefinitionValue pdv)
    {
        Px = pdv.X;
        Py = pdv.Y;
        Pz = pdv.Z;
        Pw = pdv.W;

        T = pdv.T;
        Easing = pdv.Easing;

        SplineCatmullRom = pdv.SplineCatmullRom;
        LerpHSV = pdv.LerpHSV;
    }
}

public sealed class PointDefinitionValueOp : PointDefinitionValueBase
{
    public override float X
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(Px, (current, pointOperation) => pointOperation.EvaluateX(current));
        set => Px = value;
    }

    public override float Y
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(Py, (current, pointOperation) => pointOperation.EvaluateY(current));
        set => Py = value;
    }

    public override float Z
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(Pz, (current, pointOperation) => pointOperation.EvaluateZ(current));
        set => Pz = value;
    }

    public override float W
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(Pw, (current, pointOperation) => pointOperation.EvaluateW(current));
        set => Pw = value;
    }

    public PointDefinitionValueOp(IPointDefinitionValue pdv, PointOperation[] operations)
    {
        // Copy authored components, not the builder's already-modified getters; operations run on evaluation.
        if (pdv is PointDefinitionValueBase value)
        {
            Px = value.Px;
            Py = value.Py;
            Pz = value.Pz;
            Pw = value.Pw;
        }
        else
        {
            Px = pdv.X;
            Py = pdv.Y;
            Pz = pdv.Z;
            Pw = pdv.W;
        }

        PointOperations = operations;

        T = pdv.T;
        Easing = pdv.Easing;

        SplineCatmullRom = pdv.SplineCatmullRom;
        LerpHSV = pdv.LerpHSV;
    }
}

public class PointDefinitionValueContextual : PointDefinitionValueBase
{
    protected bool HasGetter;

    public Func<float> GetterX;
    protected bool HasGetterX;
    public Func<float> GetterY;
    protected bool HasGetterY;
    public Func<float> GetterZ;
    protected bool HasGetterZ;
    public Func<float> GetterW;
    protected bool HasGetterW;

    public override float X
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterX(), (current, pointOperation) => pointOperation.EvaluateX(current));
        set => Px = value;
    }

    public override float Y
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterY(), (current, pointOperation) => pointOperation.EvaluateY(current));
        set => Py = value;
    }

    public override float Z
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterZ(), (current, pointOperation) => pointOperation.EvaluateZ(current));
        set => Pz = value;
    }

    public override float W
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterW(), (current, pointOperation) => pointOperation.EvaluateW(current));
        set => Pw = value;
    }

    public PointDefinitionValueContextual()
    {
        GetterX = () => Px;
        GetterY = () => Py;
        GetterZ = () => Pz;
        GetterW = () => Pw;
    }

    public PointDefinitionValueContextual(
        IPointDefinitionValue pdv,
        bool hasGetterX,
        bool hasGetterY,
        bool hasGetterZ,
        bool hasGetterW
    )
    {
        GetterX = () => Px;
        GetterY = () => Py;
        GetterZ = () => Pz;
        GetterW = () => Pw;

        if (pdv is PointDefinitionValueContextual contextual)
        {
            Px = contextual.Px;
            Py = contextual.Py;
            Pz = contextual.Pz;
            Pw = contextual.Pw;

            if (hasGetterX) GetterX = contextual.GetterX;
            if (hasGetterY) GetterY = contextual.GetterY;
            if (hasGetterZ) GetterZ = contextual.GetterZ;
            if (hasGetterW) GetterW = contextual.GetterW;
        }

        T = pdv.T;
        Easing = pdv.Easing;

        SplineCatmullRom = pdv.SplineCatmullRom;
        LerpHSV = pdv.LerpHSV;
    }

    public void SetGetter(int index, Func<float> getter)
    {
        HasGetter = true;
        switch (index)
        {
            case 0:
                GetterX = getter;
                HasGetterX = true;
                break;
            case 1:
                GetterY = getter;
                HasGetterY = true;
                break;
            case 2:
                GetterZ = getter;
                HasGetterZ = true;
                break;
            case 3:
                GetterW = getter;
                HasGetterW = true;
                break;
        }
    }
}

public sealed class PointDefinitionValueContextualOp : PointDefinitionValueContextual
{
    public override float X
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterX(), (current, pointOperation) => pointOperation.EvaluateX(current));
        set => Px = value;
    }

    public override float Y
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterY(), (current, pointOperation) => pointOperation.EvaluateY(current));
        set => Py = value;
    }

    public override float Z
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterZ(), (current, pointOperation) => pointOperation.EvaluateZ(current));
        set => Pz = value;
    }

    public override float W
    {
        get =>
            PointOperations
                .AsValueEnumerable()
                .Aggregate(GetterW(), (current, pointOperation) => pointOperation.EvaluateW(current));
        set => Pw = value;
    }

    public PointDefinitionValueContextualOp(
        IPointDefinitionValue pdv,
        bool hasGetterX,
        bool hasGetterY,
        bool hasGetterZ,
        bool hasGetterW,
        PointOperation[] pointOperations
    )
    {
        GetterX = () => Px;
        GetterY = () => Py;
        GetterZ = () => Pz;
        GetterW = () => Pw;

        if (pdv is PointDefinitionValueContextual contextual)
        {
            Px = contextual.Px;
            Py = contextual.Py;
            Pz = contextual.Pz;
            Pw = contextual.Pw;

            if (hasGetterX) GetterX = contextual.GetterX;
            if (hasGetterY) GetterY = contextual.GetterY;
            if (hasGetterZ) GetterZ = contextual.GetterZ;
            if (hasGetterW) GetterW = contextual.GetterW;
        }

        PointOperations = pointOperations;

        T = pdv.T;
        Easing = pdv.Easing;

        SplineCatmullRom = pdv.SplineCatmullRom;
        LerpHSV = pdv.LerpHSV;
    }
}

public sealed class PointDefinitionValueQuaternionOp : PointDefinitionValueContextual
{
    public override float X { get => Evaluate().x; set => Px = value; }
    public override float Y { get => Evaluate().y; set => Py = value; }
    public override float Z { get => Evaluate().z; set => Pz = value; }
    public Quaternion Quaternion => UnityEngine.Quaternion.Euler(Evaluate());

    public PointDefinitionValueQuaternionOp(IPointDefinitionValue value, bool x, bool y, bool z, bool w,
        PointOperation[] operations) : base(value, x, y, z, w) => PointOperations = operations;

    private Vector3 Evaluate()
    {
        // Heck converts each operand to a quaternion before applying Euler-angle arithmetic.
        var current = UnityEngine.Quaternion.Euler(GetterX(), GetterY(), GetterZ()).eulerAngles;
        foreach (var operation in PointOperations)
        {
            var modifier = operation.Rhs.Quaternion.eulerAngles;
            current = operation switch
            {
                PointOperationAdd => current + modifier,
                PointOperationSub => current - modifier,
                PointOperationMul => Vector3.Scale(current, modifier),
                PointOperationDiv => new Vector3(current.x / modifier.x, current.y / modifier.y, current.z / modifier.z),
                _ => current
            };
            current = UnityEngine.Quaternion.Euler(current).eulerAngles;
        }
        return current;
    }
}

public class PointDefinitionValueBuilder : PointDefinitionValueContextual
{
    public IPointDefinitionValue Build(bool quaternion = false)
    {
        if (quaternion && PointOperations.Length > 0)
            return new PointDefinitionValueQuaternionOp(this, HasGetterX, HasGetterY, HasGetterZ, HasGetterW, PointOperations);
        if (HasGetter && PointOperations.Length > 0)
        {
            return new PointDefinitionValueContextualOp(
                this,
                HasGetterX,
                HasGetterY,
                HasGetterZ,
                HasGetterW,
                PointOperations);
        }

        if (HasGetter) return new PointDefinitionValueContextual(this, HasGetterX, HasGetterY, HasGetterZ, HasGetterW);
        if (PointOperations.Length > 0) return new PointDefinitionValueOp(this, PointOperations);
        return new PointDefinitionValue(this);
    }
}
