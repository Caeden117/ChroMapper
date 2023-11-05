using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GeometryContainer : ObjectContainer
    {
        private static Mesh triangleMesh = null;

        public GameObject Shape;

        public override BaseObject ObjectData
        {
            get => null;
            set => _ = value;
        }

        public BaseEnvironmentEnhancement EnvironmentEnhancement;

        public override void UpdateGridPosition()
        {
        }

        public static GeometryContainer SpawnGeometry(BaseEnvironmentEnhancement eh, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GeometryContainer>();
            PrimitiveType type;
            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                type = PrimitiveType.Quad;
            }
            else
            {
                type = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), (string)eh.Geometry[eh.GeometryKeyType]);
            }
            container.EnvironmentEnhancement = eh;
            container.Shape = GameObject.CreatePrimitive(type);
            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                if (triangleMesh == null)
                {
                    triangleMesh = CreateTriangleMesh();
                }

                container.Shape.GetComponent<MeshFilter>().sharedMesh = triangleMesh;
            }
            container.Shape.transform.parent = container.Animator.AnimationThis.transform;
            container.Shape.transform.localScale = 1.667f * Vector3.one;
            container.Animator.SetGeometry(eh);
            container.gameObject.SetActive(true);
            return container;
        }

        /// <summary>
        /// https://answers.unity.com/questions/1594750/is-there-a-premade-triangle-asset.html
        /// </summary>
        private static Mesh CreateTriangleMesh()
        {
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0f, 0.5f, 0)
            };

            Vector2[] uv =
            {
                new Vector3(0, 0),
                new Vector3(1, 0),
                new Vector3(0.5f, 1)
            };

            int[] triangles = { 0, 1, 2 };

            var mesh = new Mesh()
            {
                vertices = vertices,
                uv = uv,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
