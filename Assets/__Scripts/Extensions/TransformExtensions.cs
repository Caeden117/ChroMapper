using UnityEngine;

static class TransformExtensions
{
    public static Bounds TransformBounds( this Transform _transform, Bounds _localBounds )
    {
        var center = _transform.TransformPoint(_localBounds.center);
 
        // transform the local extents' axes
        var extents = _localBounds.extents;
        var axisX = _transform.TransformVector(extents.x, 0, 0);
        var axisY = _transform.TransformVector(0, extents.y, 0);
        var axisZ = _transform.TransformVector(0, 0, extents.z);
 
        // sum their absolute value to get the world extents
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
 
        return new Bounds { center = center, extents = extents };
    }
    
    public static Bounds InverseTransformBounds( this Transform _transform, Bounds _localBounds )
    {
        var center = _transform.InverseTransformPoint(_localBounds.center);
 
        // transform the local extents' axes
        var extents = _localBounds.extents;
        var axisX = _transform.InverseTransformVector(extents.x, 0, 0);
        var axisY = _transform.InverseTransformVector(0, extents.y, 0);
        var axisZ = _transform.InverseTransformVector(0, 0, extents.z);
 
        // sum their absolute value to get the world extents
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
 
        return new Bounds { center = center, extents = extents };
    }
}
