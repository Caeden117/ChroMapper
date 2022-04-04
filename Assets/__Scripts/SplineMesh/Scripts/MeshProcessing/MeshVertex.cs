using UnityEngine;
using UnityEditor;
using System;

namespace SplineMesh {
    [Serializable]
    public class MeshVertex {
#pragma warning disable IDE1006 // Naming Styles. SplinMesh is imported
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
#pragma warning restore IDE1006 // Naming Styles. SplinMesh is imported
        public MeshVertex(Vector3 position, Vector3 normal, Vector2 uv) {
            this.position = position;
            this.normal = normal;
            this.uv = uv;
        }

        public MeshVertex(Vector3 position, Vector3 normal)
            : this(position, normal, Vector2.zero)
        {
        }
    }
}
