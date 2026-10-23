using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap;
using SimpleJSON;
using UnityEngine;

public static class PointDefinitionParser
{
    public static readonly Dictionary<string, PointDefinitionInterpolator> NameCacheParsedInterpolators = new();
    public static readonly Dictionary<JSONNode, PointDefinitionInterpolator> NodeCacheParsedInterpolators =
        new(NodeIdentityComparer.Instance);

    private static readonly Dictionary<string, string> namedContent = new();
    private static readonly Dictionary<JSONNode, string> nodeContent = new(NodeIdentityComparer.Instance);
    private static readonly Dictionary<JSONNode, string> registrationContent = new(NodeIdentityComparer.Instance);
    private static int registrationDepth;
    private static readonly Dictionary<(JSONNode Node, int Dimension, bool Quaternion), (string Content, PointDefinitionInterpolator Curve)> typedCurves =
        new(TypedCurveIdentityComparer.Instance);
    private static BaseDifficulty cacheMap;
    private static readonly Dictionary<JSONNode, HashSet<JSONNode>> rootNodes = new(NodeIdentityComparer.Instance);
    private static readonly Dictionary<JSONNode, int> nodeOwners = new(NodeIdentityComparer.Instance);
    private static readonly HashSet<JSONNode> currentRoots = new(NodeIdentityComparer.Instance);
    private static readonly List<JSONNode> staleRoots = new();
    private static readonly List<JSONNode> staleNodes = new();
    private static readonly List<string> staleNames = new();
    private static readonly List<(JSONNode Node, int Dimension, bool Quaternion)> staleTypedCurves = new();
    internal static int SampleVersion => BaseProviderManager.SampleVersion;
    internal static bool ResetSmoothing => BaseProviderManager.ResetSmoothing;

    /// <summary>Clears definitions and live provider state when loading or unloading a map.</summary>
    /// <param name="preserveInvalidDefinitions">Retain unchanged failed validation during a rebuild of the same map.</param>
    public static void ResetCache(bool preserveInvalidDefinitions = false)
    {
        NameCacheParsedInterpolators.Clear();
        NodeCacheParsedInterpolators.Clear();
        namedContent.Clear();
        nodeContent.Clear();
        registrationContent.Clear();
        rootNodes.Clear();
        nodeOwners.Clear();
        BaseProviderManager.ResetForMapLoad();
        var map = BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Map : null;
        if (!preserveInvalidDefinitions || map == null || !ReferenceEquals(cacheMap, map))
        {
            typedCurves.Clear();
        }
        else
        {
            // Timing refreshes can rebuild the same map twice. Invalid curves have no live bindings;
            // retain their validation result until the authored content changes instead of logging it again.
            foreach (var cached in typedCurves)
                if (cached.Value.Curve != null) staleTypedCurves.Add(cached.Key);
            foreach (var key in staleTypedCurves) typedCurves.Remove(key);
            staleTypedCurves.Clear();
        }
        cacheMap = preserveInvalidDefinitions ? map : null;
    }

    /// <summary>Releases cached definitions no longer present in the current authored animation data.</summary>
    /// <param name="definitions">The current map's named definitions.</param>
    /// <param name="animationRoots">Current event data and object animation roots whose cached curves should be retained.</param>
    /// <param name="inspectAll">Rescan all roots after untracked changes; false reuses unchanged roots' node membership.</param>
    /// <param name="changedRoots">Roots mutated in place during a tracked edit, including named definitions.</param>
    /// <remarks>
    /// Call after registering edited definitions. Removed cache entries do not invalidate existing wrappers;
    /// their curves and live provider getters remain valid until those wrappers are rebound or released.
    /// Provider sampling and smoothing state are preserved. Changed names must still be registered with Get.
    /// </remarks>
    internal static void RetainDefinitions(IReadOnlyDictionary<string, JSONArray> definitions,
        IEnumerable<JSONNode> animationRoots, bool inspectAll = true, IEnumerable<JSONNode> changedRoots = null)
    {
        try
        {
            if (inspectAll)
            {
                rootNodes.Clear();
                nodeOwners.Clear();
            }
            else if (changedRoots != null)
                foreach (var root in changedRoots)
                    if (root != null) RemoveRoot(root);

            foreach (var definition in definitions.Values) RetainRoot(definition);
            foreach (var root in animationRoots) RetainRoot(root);
            foreach (var root in rootNodes.Keys)
                if (!currentRoots.Contains(root)) staleRoots.Add(root);
            foreach (var root in staleRoots) RemoveRoot(root);

            foreach (var name in NameCacheParsedInterpolators.Keys)
                if (!definitions.ContainsKey(name)) staleNames.Add(name);
            foreach (var name in staleNames)
            {
                NameCacheParsedInterpolators.Remove(name);
                namedContent.Remove(name);
            }

            foreach (var node in NodeCacheParsedInterpolators.Keys)
                if (!nodeOwners.ContainsKey(node)) staleNodes.Add(node);
            foreach (var node in staleNodes)
            {
                NodeCacheParsedInterpolators.Remove(node);
                nodeContent.Remove(node);
            }

            foreach (var key in typedCurves.Keys)
                if (!nodeOwners.ContainsKey(key.Node)) staleTypedCurves.Add(key);
            foreach (var key in staleTypedCurves) typedCurves.Remove(key);
        }
        finally
        {
            currentRoots.Clear();
            staleRoots.Clear();
            staleNodes.Clear();
            staleNames.Clear();
            staleTypedCurves.Clear();
        }
    }

    private static void RetainRoot(JSONNode root)
    {
        if (root == null || !currentRoots.Add(root) || rootNodes.ContainsKey(root)) return;
        var nodes = new HashSet<JSONNode>(NodeIdentityComparer.Instance);
        CollectNodes(root, nodes);
        rootNodes.Add(root, nodes);
        foreach (var node in nodes) nodeOwners[node] = nodeOwners.GetValueOrDefault(node) + 1;
    }

    private static void RemoveRoot(JSONNode root)
    {
        if (!rootNodes.Remove(root, out var nodes)) return;
        foreach (var node in nodes)
        {
            if (nodeOwners[node] == 1) nodeOwners.Remove(node);
            else nodeOwners[node]--;
        }
    }

    private static void CollectNodes(JSONNode node, HashSet<JSONNode> nodes)
    {
        if (node == null || !nodes.Add(node)) return;
        foreach (var child in node.Values) CollectNodes(child, nodes);
    }

    // Numeric JSON nodes are mutable and hash by value. Cache ownership follows the authored
    // node's identity so editing its value cannot strand an entry under its previous hash.
    private sealed class NodeIdentityComparer : IEqualityComparer<JSONNode>
    {
        internal static readonly NodeIdentityComparer Instance = new();
        public bool Equals(JSONNode left, JSONNode right) => ReferenceEquals(left, right);
        public int GetHashCode(JSONNode node) => RuntimeHelpers.GetHashCode(node);
    }

    private sealed class TypedCurveIdentityComparer : IEqualityComparer<(JSONNode Node, int Dimension, bool Quaternion)>
    {
        internal static readonly TypedCurveIdentityComparer Instance = new();
        public bool Equals((JSONNode Node, int Dimension, bool Quaternion) left,
            (JSONNode Node, int Dimension, bool Quaternion) right) => ReferenceEquals(left.Node, right.Node)
            && left.Dimension == right.Dimension && left.Quaternion == right.Quaternion;
        public int GetHashCode((JSONNode Node, int Dimension, bool Quaternion) key) =>
            HashCode.Combine(RuntimeHelpers.GetHashCode(key.Node), key.Dimension, key.Quaternion);
    }

    /// <summary>Samples live providers once before evaluating consumers. A seek resets smoothing to current input.</summary>
    public static void BeginSample(float time, bool seek = false) => BaseProviderManager.BeginSample(time, seek);

    /// <summary>Reuses the current evaluation sample, or starts one for a new Unity frame.</summary>
    public static void BeginSample() => BaseProviderManager.BeginSample();

    /// <summary>Binds the preview camera and movement provider without scene searches while sampling.</summary>
    public static void BindSceneReferences(CameraManager cameras, VariableNJSProvider movement,
        AudioTimeSyncController audioTime = null) => BaseProviderManager.BindSceneReferences(cameras, movement, audioTime);

    internal static int Dimension<T>() where T : struct => typeof(T) == typeof(float) ? 1
        : typeof(T) == typeof(Vector3) || typeof(T) == typeof(Quaternion) ? 3
        : typeof(T) == typeof(Color) || typeof(T) == typeof(Vector4) ? 4
        : throw new ArgumentException($"Unsupported point definition type {typeof(T).Name}.");

    // A synchronous rebuild cannot change authored JSON between its consumers. Serialize each
    // definition once in that scope; the next rebuild and ordinary Get calls inspect fresh content.
    internal static void BeginRegistration()
    {
        if (registrationDepth++ == 0) registrationContent.Clear();
    }

    internal static void EndRegistration()
    {
        if (--registrationDepth == 0) registrationContent.Clear();
    }

    private static string GetContent(JSONNode node)
    {
        if (node == null) return null;
        if (registrationDepth == 0) return node.ToString();
        if (registrationContent.TryGetValue(node, out var content)) return content;
        content = node.ToString();
        registrationContent.Add(node, content);
        return content;
    }

    internal static PointDefinitionInterpolator Get(JSONNode node, int dimension, AnimationLogContext context,
        bool quaternion = false, string valueTypeName = null)
    {
        if (node == null || node.IsNull) return null;
        if (node is JSONString named)
        {
            var map = BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Map : null;
            if (map == null || !map.PointDefinitions.TryGetValue(named.Value, out var definition))
            {
                Debug.LogError($"Could not find point definition [{named.Value}]. {context.Describe()}");
                return null;
            }
            node = definition;
        }
        var content = GetContent(node);
        var key = (node, dimension, quaternion);
        if (typedCurves.TryGetValue(key, out var cached) && cached.Content == content) return cached.Curve;
        PointDefinitionInterpolator curve;
        if (node.IsNumber && dimension == 1)
            curve = Parse(node);
        else if (!node.IsArray || node.Count == 0)
            curve = null;
        else
        {
            var points = new List<IPointDefinitionValue>();
            var bare = !node[0].IsArray;
            var rows = bare ? new[] { node } : node.Children;
            var index = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (!row.IsArray) throw new FormatException("Point definition rows must be arrays.");
                    points.Add(ParseTypedPoint(row.AsArray, dimension, bare, quaternion));
                }
                catch (PointComponentCountException error)
                {
                    var typeName = valueTypeName ?? (dimension == 1 ? nameof(Single)
                        : dimension == 3 ? quaternion ? nameof(Quaternion) : nameof(Vector3) : nameof(Vector4));
                    Debug.LogError($"Point for [{typeName}] must have {dimension} numbers plus an optional time; "
                        + $"got {error.Count} and it was skipped. {context.Describe(index)}");
                }
                catch (FormatException error)
                {
                    Debug.LogError($"{error.Message} The point was skipped. {context.Describe(index)}");
                }
                index++;
            }
            curve = points.Count == 0 ? null : new PointDefinitionInterpolator(points.OrderBy(point => point.T).ToArray(), ContainsBaseProvider(node));
        }
        typedCurves[key] = (content, curve);
        return curve;
    }

    private static bool ContainsBaseProvider(JSONNode node)
    {
        if (node.IsString) return node.Value.StartsWith("base", StringComparison.Ordinal);
        foreach (var child in node.Values)
            if (child != null && ContainsBaseProvider(child)) return true;
        return false;
    }

    private static IPointDefinitionValue ParseTypedPoint(JSONArray row, int dimension, bool bare, bool quaternion = false)
    {
        var value = new PointDefinitionValueBuilder();
        var count = 0;
        var hasEasing = false;
        foreach (var child in row.Children)
        {
            if (child.IsArray)
            {
                value.AddOperation(ParseTypedOperation(child.AsArray, dimension, quaternion));
                continue;
            }
            if (child.IsNumber || child == null || child.IsNull)
            {
                var component = child == null || child.IsNull ? 0 : child.AsFloat;
                if (count < dimension) value.SetGetter(count, component);
                else if (count == dimension) value.T = component;
                count++;
                continue;
            }
            if (!child.IsString) throw new FormatException($"Point contains an unsupported entry [{child.Value}].");
            var token = child.Value;
            if (token.StartsWith("base", StringComparison.Ordinal))
            {
                foreach (var getter in GetBaseGetter(token))
                {
                    if (count < dimension) value.SetGetter(count, getter);
                    else if (count == dimension) value.T = getter();
                    count++;
                }
            }
            else if (!hasEasing && token.StartsWith("ease", StringComparison.Ordinal))
            {
                hasEasing = true;
                value.Easing = Easing.HeckNamed(token);
            }
            else if (token == "splineCatmullRom" || token == "lerpHSV")
            {
                value.SplineCatmullRom = token == "splineCatmullRom";
                value.LerpHSV = token == "lerpHSV";
            }
        }
        if (count < (bare ? dimension : dimension + 1) || count > dimension + 1)
            throw new PointComponentCountException(dimension, bare, count);
        return value.Build(quaternion);
    }

    private sealed class PointComponentCountException : FormatException
    {
        internal int Count { get; }

        internal PointComponentCountException(int dimension, bool bare, int count)
            : base($"Point must have {dimension} components plus {(bare ? "an optional" : "a")} time; got {count}.")
            => Count = count;
    }

    private static PointOperation ParseTypedOperation(JSONArray row, int dimension, bool quaternion = false)
    {
        var value = new PointDefinitionValueBuilder();
        string operation = null;
        var count = 0;
        foreach (var child in row.Children)
        {
            if (child.IsArray)
                value.AddOperation(ParseTypedOperation(child.AsArray, dimension, quaternion));
            else if (child == null || child.IsNull || child.IsNumber)
                value.SetGetter(count++, child == null || child.IsNull ? 0 : child.AsFloat);
            else if (child.IsString && child.Value.StartsWith("base", StringComparison.Ordinal))
            {
                foreach (var getter in GetBaseGetter(child.Value)) value.SetGetter(count++, getter);
            }
            else if (child.IsString && operation == null)
                operation = child.Value;
            else
                throw new FormatException("Modifier must contain components and exactly one operation.");
        }
        if (count != dimension) throw new FormatException($"Modifier must have {dimension} components; got {count}.");
        var rhs = value.Build(quaternion);
        return operation switch
        {
            "opNone" => new PointOperationNone(rhs),
            "opAdd" => new PointOperationAdd(rhs),
            "opSub" => new PointOperationSub(rhs),
            "opMul" => new PointOperationMul(rhs),
            "opDiv" => new PointOperationDiv(rhs),
            _ => throw new FormatException($"Unknown modifier operation [{operation}].")
        };
    }

    public static PointDefinitionInterpolator Get(string name, JSONNode node)
    {
        var content = GetContent(node);
        if (namedContent.TryGetValue(name, out var previous) && previous == content)
            return NameCacheParsedInterpolators.GetValueOrDefault(name);
        var parsed = Get(node);
        namedContent[name] = content;
        NameCacheParsedInterpolators[name] = parsed;
        return parsed;
    }

    public static PointDefinitionInterpolator Get(string name) => NameCacheParsedInterpolators.GetValueOrDefault(name);

    public static PointDefinitionInterpolator Get(JSONNode node)
    {
        if (node == null || node.IsNull) return null;
        if (node.IsString) return Get((string)node);
        var content = GetContent(node);
        if (nodeContent.TryGetValue(node, out var previous) && previous == content)
            return NodeCacheParsedInterpolators.GetValueOrDefault(node);
        var parsed = Parse(node);
        nodeContent[node] = content;
        NodeCacheParsedInterpolators[node] = parsed;
        return parsed;
    }

    private static PointDefinitionInterpolator Parse(JSONNode node)
    {
        if (node.IsNumber)
        {
            var value = new PointDefinitionValueBuilder();
            value.SetGetter(0, node.AsFloat);
            return new PointDefinitionInterpolator(value.Build());
        }
        if (!node.IsArray || node.Count == 0) return null;
        var bare = !node[0].IsArray;
        var first = bare ? node.AsArray : node[0].AsArray;
        try
        {
            return Get(node, InferDimension(first, bare), default);
        }
        catch (FormatException error)
        {
            Debug.LogError($"{error.Message} The point definition was skipped.");
            return null;
        }
    }

    private static int InferDimension(JSONArray row, bool bare)
    {
        var count = 0;
        foreach (var child in row.Children)
        {
            if (child.IsNumber || child == null || child.IsNull) count++;
            else if (child.IsString && child.Value.StartsWith("base", StringComparison.Ordinal))
                count += GetBaseGetter(child.Value).Count();
        }
        var dimension = bare ? Mathf.Min(count, 4) : count - 1;
        if (dimension <= 0 || dimension > 4)
            throw new FormatException($"Point has an invalid component count [{count}].");
        return dimension;
    }

    public static IPointDefinitionValue ParsePoint(JSONArray row) => ParseTypedPoint(row, InferDimension(row, false), false);

    internal static IPointDefinitionValue ParsePoint<T>(JSONArray row) where T : struct =>
        ParseTypedPoint(row, Dimension<T>(), false, typeof(T) == typeof(Quaternion));

    public static PointOperation ParseOperation(JSONNode node)
    {
        var dimension = InferDimension(node.AsArray, true);
        return node.Children.Any(child => child.IsString && child.Value.StartsWith("op", StringComparison.Ordinal))
            ? ParseTypedOperation(node.AsArray, dimension)
            : new PointOperationNone(ParseTypedPoint(node.AsArray, dimension, true));
    }

    public static IEnumerable<Func<float>> GetBaseGetter(string modifier) =>
        BaseProviderManager.GetBaseGetter(modifier);
}
