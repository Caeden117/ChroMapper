using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct BloomfogQuad
{
    public Vector3 Vertex0Position;
    public Vector3 Vertex0ViewPos;
    public Color Vertex0Color;
    public Vector3 Vertex0UV;

    public Vector3 Vertex1Position;
    public Vector3 Vertex1ViewPos;
    public Color Vertex1Color;
    public Vector3 Vertex1UV;

    public Vector3 Vertex2Position;
    public Vector3 Vertex2ViewPos;
    public Color Vertex2Color;
    public Vector3 Vertex2UV;

    public Vector3 Vertex3Position;
    public Vector3 Vertex3ViewPos;
    public Color Vertex3Color;
    public Vector3 Vertex3UV;

    public readonly void CopyVerticesTo(BloomfogVertex[] vertices, int startIndex)
    {
        vertices[startIndex] = new BloomfogVertex(Vertex0Position, Vertex0ViewPos, Vertex0Color, Vertex0UV);
        vertices[startIndex + 1] = new BloomfogVertex(Vertex1Position, Vertex1ViewPos, Vertex1Color, Vertex1UV);
        vertices[startIndex + 2] = new BloomfogVertex(Vertex2Position, Vertex2ViewPos, Vertex2Color, Vertex2UV);
        vertices[startIndex + 3] = new BloomfogVertex(Vertex3Position, Vertex3ViewPos, Vertex3Color, Vertex3UV);
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct BloomfogVertex
{
    public readonly Vector3 Position;
    public readonly Vector3 ViewPosition;
    public readonly Color Color;
    public readonly Vector3 UV;

    public BloomfogVertex(Vector3 position, Vector3 viewPosition, Color color, Vector3 uv)
    {
        Position = position;
        ViewPosition = viewPosition;
        Color = color;
        UV = uv;
    }
}
