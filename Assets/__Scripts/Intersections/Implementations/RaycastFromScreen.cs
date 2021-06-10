using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
// Unfortunately this class isn't as generic as I'd like it to be.
public static partial class Intersections
{
    public const string OBJECT_RAYCAST_INDEX_BUFFER = "_ObjectRaycastIndexBuffer";
    public const string PLACEMENT_RAYCAST_INDEX_BUFFER = "_PlacementRaycastIndexBuffer";

    private static ComputeShader shader;
    private static ComputeBuffer resultBuffer;
    private static int[] resultOutput = new int[1] { -1 };

    private static int lastCheckedFrame = -1;
    private static Dictionary<int, bool> lastCheckedFound = new Dictionary<int, bool>();
    private static Dictionary<int, IntersectionHit> lastCheckedHit = new Dictionary<int, IntersectionHit>();

    public static void AssignComputeShader(ComputeShader computeShader)
    {
        if (shader != null) return;
        
        shader = computeShader;

        resultBuffer = new ComputeBuffer(1, sizeof(int));
        shader.SetBuffer(0, "Result", resultBuffer);
    }

    public static void DisposeComputeShader()
    {
        resultBuffer.Dispose();
    }

    public static bool RaycastFromScreen(Camera camera, Vector2 screenPosition, int layer, out IntersectionHit hit)
        => RaycastFromScreen(camera, screenPosition, layer, out hit, out _);

    public static bool RaycastFromScreen(Camera camera, Vector2 screenPosition, int layer, out IntersectionHit hit, out float distance)
    {
        hit = new IntersectionHit();
        distance = float.PositiveInfinity;

        if (Time.frameCount == lastCheckedFrame && lastCheckedHit.ContainsKey(layer))
        {
            hit = lastCheckedHit[layer];
            distance = hit.Distance;
            return lastCheckedFound[layer];
        }

        lastCheckedFrame = Time.frameCount;
        lastCheckedFound[layer] = false;
        lastCheckedHit[layer] = new IntersectionHit();

        if (shader == null)
        {
            Debug.LogError("Compute Shader not applied. RaycastFromScreen cannot continue.");
            return false;
        }

        // LISSEN. This code is hella chromapper specific now.
        if (layer != 29 && layer != 30) return false;

        // This is just a dirty way of selecting which buffer we want to read from. No array shit on the GPU side, just if/else
        var bufferID = layer == 30 ? 0 : 1;
        
        // Copy raycast buffers into our Compute Shader
        shader.SetTextureFromGlobal(0, Shader.PropertyToID("_ObjectBuffer"), Shader.PropertyToID(OBJECT_RAYCAST_INDEX_BUFFER));
        shader.SetTextureFromGlobal(0, Shader.PropertyToID("_PlacementBuffer"), Shader.PropertyToID(PLACEMENT_RAYCAST_INDEX_BUFFER));

        // Send our mouse position over to the shader
        shader.SetInts("_MousePosition", Mathf.RoundToInt(screenPosition.x), Mathf.RoundToInt(screenPosition.y));
        // Send which exact buffer we are interested in looking up (this is hardcoded atm, goes through an if/else)
        shader.SetInt("_BufferIndex", bufferID);
        // Execute this boi
        shader.Dispatch(0, 1, 1, 1);

        // Grab our result buffer
        resultBuffer.GetData(resultOutput);

        // And we instantly have the index of the collider under our cursor.
        var index = resultOutput[0];

        // If our Compute Shader did not find anything, cancel execution.
        if (index < 0) return false;

        var layerColliders = allColliders[layer];

        // If our index falls outside our range, it must be invalid somehow; cancel execution.
        if (layerColliders.Count == 0 || index >= layerColliders.Count) return false;

        var collider = layerColliders[index];

        // Given we know exactly which collider we're intersecting, pass it through our internal raycast
        //  to grab more detailed information.
        var ray = camera.ScreenPointToRay(screenPosition);
        var origin = ray.origin;
        var direction = ray.direction;

        // If we SOMEHOW are not intersecting, this is something that should not happen.
        if (!RaycastIndividual_Internal(collider, in direction, in origin, out distance))
        {
            //Debug.LogError("Raycast Index Buffer is somehow not up-to-date. Raycast Index Buffer suggests an intersection, but further raycasting proves otherwise.");
            return false;
        }

        hit = lastCheckedHit[layer] = new IntersectionHit(collider.gameObject, collider.BoundsRenderer.bounds, ray, distance);
        lastCheckedFound[layer] = true;

        return true;
    }
}