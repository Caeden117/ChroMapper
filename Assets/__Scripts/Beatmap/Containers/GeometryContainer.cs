using System;
using Beatmap.Animations;
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
            get => EnvironmentEnhancement;
            set => EnvironmentEnhancement = (BaseEnvironmentEnhancement)value;
        }

        public BaseEnvironmentEnhancement EnvironmentEnhancement;

        public ObjectAnimator MaterialAnimator;

        public override void UpdateGridPosition()
        {
        }

        public static GeometryContainer SpawnGeometry(BaseEnvironmentEnhancement eh, ref GameObject prefab)
        {
            var type_str = (string)eh.Geometry[eh.GeometryKeyType];
            if (type_str == null)
                return null;

            var container = Instantiate(prefab).GetComponent<GeometryContainer>();
            PrimitiveType type;
            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                type = PrimitiveType.Quad;
            }
            else
            {
                if (!Enum.TryParse<PrimitiveType>((string)eh.Geometry[eh.GeometryKeyType], out type))
                {
                    Debug.LogError($"Invalid geometry type '{(string)eh.Geometry[eh.GeometryKeyType]}'!");
                }
            }
            container.EnvironmentEnhancement = eh;
            container.Shape = GameObject.CreatePrimitive(type);
            container.Shape.layer = 9;

            var collider = container.Shape.GetComponentInChildren<Collider>();
            if (collider != null) DestroyImmediate(collider);

            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                if (triangleMesh == null)
                {
                    triangleMesh = CreateTriangleMesh();
                }

                container.Shape.GetComponent<MeshFilter>().sharedMesh = triangleMesh;
            }
            var mesh = container.Shape.GetComponent<MeshFilter>().sharedMesh;
            container.SelectionRenderers[0].GetComponent<MeshFilter>().sharedMesh = mesh;
            var intersection = container.Animator.AnimationThis.AddComponent<IntersectionCollider>();
            var renderer = container.Shape.GetComponent<MeshRenderer>();
            intersection.Mesh = mesh;
            intersection.BoundsRenderer = renderer;

            if (container.MaterialPropertyBlock == null)
            {
                container.MaterialPropertyBlock = new MaterialPropertyBlock();
                container.modelRenderers.Add(renderer);
            }

            container.Colliders.Add(intersection);
            container.Shape.transform.parent = container.Animator.AnimationThis.transform;
            container.Shape.transform.localScale = 1.667f * Vector3.one;
            container.Animator.AttachToGeometry(eh);
            container.gameObject.SetActive(true);
            container.UpdateCollisionGroups();
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
