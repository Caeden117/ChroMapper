using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beatmap.Shared
{
    public static class GLSStrobeIconBatch
    {
        private static readonly int iconsId = Shader.PropertyToID("_Icons");
        private static readonly int sdfLayoutId = Shader.PropertyToID("_SdfLayout");
        private static readonly List<GLSStrobeIconRenderer> surfaces = new();
        private static readonly Dictionary<Camera, CameraBatch> cameras = new();
        private static Mesh quad;
        private static Material material;

        [StructLayout(LayoutKind.Sequential)]
        private struct InstanceData
        {
            public Matrix4x4 Transform;
            public Vector4 Curve;
            public Vector4 Atlas;
            public Vector4 CornerAtlas;
        }

        private sealed class DepthComparer : IComparer<InstanceData>
        {
            public Vector3 Forward;

            public int Compare(InstanceData left, InstanceData right)
            {
                return Vector3.Dot(Forward, right.Transform.GetColumn(3))
                    .CompareTo(Vector3.Dot(Forward, left.Transform.GetColumn(3)));
            }
        }

        private sealed class CameraBatch : IDisposable
        {
            private readonly Camera camera;
            private readonly GLSStrobeIconCamera lifetime;
            private readonly CommandBuffer commands = new() { name = "GLS Strobe Icons" };
            private readonly MaterialPropertyBlock properties = new();
            private readonly Plane[] planes = new Plane[6];
            private readonly DepthComparer comparer = new();
            private InstanceData[] instances = Array.Empty<InstanceData>();
            private ComputeBuffer buffer;

            public CameraBatch(Camera camera)
            {
                this.camera = camera;
                properties.SetVector(sdfLayoutId, new Vector4(
                    GLSStrobeSdfAtlas.TileWidth,
                    GLSStrobeSdfAtlas.TileHeight,
                    GLSStrobeSdfAtlas.Columns,
                    GLSStrobeSdfAtlas.PulseWidthCount));
                // Draw after node sprites and before post processing, preserving the icons' no-bloom alpha mask.
                camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, commands);
                // Cameras can disappear while pooled nodes stay active, so their buffers follow the camera's lifetime.
                lifetime = camera.gameObject.AddComponent<GLSStrobeIconCamera>();
                lifetime.hideFlags = HideFlags.HideAndDontSave;
                lifetime.Initialize(CameraDestroyed);
            }

            public void Render()
            {
                commands.Clear();
                if (instances.Length < surfaces.Count)
                {
                    instances = new InstanceData[Mathf.NextPowerOfTwo(Mathf.Max(64, surfaces.Count))];
                    buffer?.Release();
                    buffer = new ComputeBuffer(instances.Length, Marshal.SizeOf<InstanceData>());
                    properties.SetBuffer(iconsId, buffer);
                }

                GeometryUtility.CalculateFrustumPlanes(camera, planes);
                var count = 0;
                for (var i = 0; i < surfaces.Count; i++)
                {
                    var surface = surfaces[i];
                    if ((camera.cullingMask & (1 << surface.gameObject.layer)) == 0)
                    {
                        continue;
                    }

                    var matrix = surface.WorldMatrix;
                    var horizontal = matrix.GetColumn(0) * (GLSStrobeIconRenderer.Size.x * 0.5f);
                    var vertical = matrix.GetColumn(1) * (GLSStrobeIconRenderer.Size.y * 0.5f);
                    var extents = new Vector3(
                        Mathf.Abs(horizontal.x) + Mathf.Abs(vertical.x),
                        Mathf.Abs(horizontal.y) + Mathf.Abs(vertical.y),
                        Mathf.Abs(horizontal.z) + Mathf.Abs(vertical.z));
                    if (!GeometryUtility.TestPlanesAABB(planes, new Bounds(matrix.GetColumn(3), extents * 2f)))
                    {
                        continue;
                    }

                    instances[count++] = new InstanceData
                    {
                        Transform = matrix,
                        Curve = surface.CurveParameters,
                        Atlas = surface.AtlasParameters,
                        CornerAtlas = surface.CornerAtlasParameters
                    };
                }

                if (count == 0)
                {
                    return;
                }

                // Keep transparent faces in back-to-front order within the single instanced submission.
                comparer.Forward = camera.transform.forward;
                Array.Sort(instances, 0, count, comparer);
                buffer.SetData(instances, 0, 0, count);
                // A structured buffer avoids the 1023-instance limit of matrix-array instancing.
                commands.DrawMeshInstancedProcedural(quad, 0, material, 0, count, properties);
            }

            public void Dispose()
            {
                lifetime.Detach();
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(lifetime);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(lifetime);
                }

                ReleaseResources();
            }

            private void CameraDestroyed()
            {
                cameras.Remove(camera);
                ReleaseResources();
            }

            private void ReleaseResources()
            {
                if (camera != null)
                {
                    camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, commands);
                }

                commands.Release();
                buffer?.Release();
            }
        }

        internal static void Register(GLSStrobeIconRenderer surface, Material sharedMaterial)
        {
            if (surfaces.Count == 0)
            {
                material = sharedMaterial;
                var half = GLSStrobeIconRenderer.Size * 0.5f;
                quad = new Mesh
                {
                    name = "GLS Strobe Icon Quad",
                    vertices = new[]
                    {
                        new Vector3(-half.x, -half.y), new Vector3(half.x, -half.y),
                        new Vector3(half.x, half.y), new Vector3(-half.x, half.y)
                    },
                    uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                    triangles = new[] { 0, 1, 2, 0, 2, 3 }
                };
                quad.UploadMeshData(true);
                Camera.onPreCull += Render;
            }

            surface.BatchIndex = surfaces.Count;
            surfaces.Add(surface);
        }

        internal static void Unregister(GLSStrobeIconRenderer surface)
        {
            var last = surfaces[^1];
            last.BatchIndex = surface.BatchIndex;
            surfaces[surface.BatchIndex] = last;
            surfaces.RemoveAt(surfaces.Count - 1);
            if (surfaces.Count != 0)
            {
                return;
            }

            Camera.onPreCull -= Render;
            foreach (var batch in cameras.Values)
            {
                batch.Dispose();
            }

            cameras.Clear();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(quad);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(quad);
            }

            quad = null;
            material = null;
        }

        private static void Render(Camera camera)
        {
            if (!cameras.TryGetValue(camera, out var batch))
            {
                batch = new CameraBatch(camera);
                cameras.Add(camera, batch);
            }

            batch.Render();
        }
    }
}
