using UnityEngine;

namespace Beatmap.Shared
{
    [DisallowMultipleComponent]
    public sealed class GLSStrobeIconRenderer : MonoBehaviour
    {
        public static readonly Vector2 Size = new(2.84f, 1.28f);
        public const float MarginPixels = 13f;

        private static readonly Vector4[] easingParameters = CreateEasingParameters();
        private static readonly float[] easingEndValues = CreateEasingEndValues();
        private Transform surfaceTransform;
        [SerializeField] private Material material;

        internal int BatchIndex { get; set; }
        internal Matrix4x4 WorldMatrix => surfaceTransform.localToWorldMatrix;
        public Vector4 CurveParameters { get; private set; }
        public Vector4 AtlasParameters { get; private set; }
        public Vector4 CornerAtlasParameters { get; private set; }

        // Clones copy the material, but must cache their own transform before joining the draw batch.
        private void Awake() => surfaceTransform = transform;

        public void Initialize(Material sharedMaterial)
        {
            surfaceTransform = transform;
            material = sharedMaterial;
        }

        public void SetIcon(GLSEventIconType type, float pulseWidth)
        {
            var parameters = easingParameters[(int)type];
            parameters.y = Mathf.Clamp01(pulseWidth);
            CurveParameters = parameters;
            AtlasParameters = GLSStrobeSdfAtlas.GetFieldParameters(
                (int)parameters.x,
                parameters.y,
                easingEndValues[(int)type],
                out var cornerParameters);
            CornerAtlasParameters = cornerParameters;
        }

        private void OnEnable()
        {
            GLSStrobeIconBatch.Register(this, material);
        }

        private void OnDisable()
        {
            GLSStrobeIconBatch.Unregister(this);
        }

        private static Vector4[] CreateEasingParameters()
        {
            var parameters = new Vector4[(int)GLSEventIconType.NoEasingStep + 1];
            for (var type = GLSEventIconType.EaseLinear; type <= GLSEventIconType.EaseBeatSaberInOutBounce; type++)
            {
                var easeType = EaseTypeForIcon(type);
                var curve = Easing.FromID(easeType);
                var minimum = 0f;
                var maximum = 1f;
                // Normalize both phases together so asymmetric Back/Elastic overshoot joins at the same height.
                for (var sample = 0; sample < 512; sample++)
                {
                    var value = curve(sample / 511f);
                    minimum = Mathf.Min(minimum, value, 1f - value);
                    maximum = Mathf.Max(maximum, value, 1f - value);
                }

                if (minimum < 0f || maximum > 1f)
                {
                    var padding = (maximum - minimum) * 0.08f;
                    minimum -= padding;
                    maximum += padding;
                }

                parameters[(int)type] = new Vector4(Easing.EasingShaderId(easeType), 0.5f, minimum, maximum - minimum);
            }

            parameters[(int)GLSEventIconType.NoEasingStep] = new Vector4(Easing.EasingShaderId(-1), 0.5f, 0f, 1f);
            return parameters;
        }

        public static int EaseTypeForIcon(GLSEventIconType type)
        {
            return type == GLSEventIconType.NoEasingStep
                ? -1
                : type >= GLSEventIconType.EaseBeatSaberInOutBack
                    ? 100 + (type - GLSEventIconType.EaseBeatSaberInOutBack)
                    : type - GLSEventIconType.EaseLinear;
        }

        public static Vector4 EasingParameters(GLSEventIconType type)
        {
            return easingParameters[(int)type];
        }

        private static float[] CreateEasingEndValues()
        {
            var values = new float[(int)GLSEventIconType.NoEasingStep + 1];
            for (var type = GLSEventIconType.EaseLinear; type <= GLSEventIconType.EaseBeatSaberInOutBounce; type++)
            {
                values[(int)type] = Easing.FromID(EaseTypeForIcon(type))(1f);
            }

            values[(int)GLSEventIconType.NoEasingStep] = 1f;
            return values;
        }
    }
}
