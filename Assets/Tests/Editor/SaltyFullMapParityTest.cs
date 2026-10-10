using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // Temporary local diagnostic for the Salty map report: loads the FULL
    // ExpertPlusStandard.dat (no fixture trimming) so the authored `hehe`/`hehe2`
    // spheres and their `die` parent banish can be inspected in the real load path.
    // Explicit keeps these machine-specific full-map diagnostics (real-map load, HDR captures)
    // out of batch runs; run on demand with
    // -TestFilter 'Tests.Editor.SaltyFullMapParityTest.<name>'.
    [Explicit]
    public class SaltyFullMapParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private Vector3 editingCameraPosition;
        private Quaternion editingCameraRotation;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private EditModeContext editMode;
        private EditingMode previousEditingMode;
        private bool editingModeChanged;

        private const string SourceMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/4b476 (G1ll35 d3 R415 - Salty)/ExpertPlusStandard.dat";

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            // Deployed ChroMapperSettings.json runs camera FOV 90 with zero offset; the test-runner
            // defaults (FOV 60, offset 3.6) produced materially different captures, so match the
            // deployed camera settings before the map load applies them.
            previousPlayerCameraFOV = Settings.Instance.PlayerCameraFOV;
            previousPlayerCameraOffsetZ = Settings.Instance.PlayerCameraOffsetZ;
            previousCameraFOV = Settings.Instance.CameraFOV;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(SourceMapPath)),
                beatsPerMinute: 215,
                environmentName: "PanicEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // The game hides both authored spheres from the opening: b=0 AssignTrackParent
        // [126] puts hehe/hehe2 under `die`, then AnimateTrack [127] scales `die` to zero
        // at (0,-6969,-6969). Either sphere still rendering means the banish was lost.
        [UnityTest]
        public IEnumerator DieParentBanishesHeheSpheresFromPlayingCamera()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;

            var failures = new List<string>();
            foreach (var beat in new[] { 0.1f, 2.1f, 217f, 222f, 0.1f, 222f })
            {
                yield return SeekTo(beat);
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                foreach (var trackName in new[] { "hehe", "hehe2" })
                {
                    var container = Object.FindAnyObjectByType<GeometryGridContainer>()
                        .LoadedContainers.Values
                        .OfType<Beatmap.Containers.GeometryContainer>()
                        .SingleOrDefault(c => c.EnvironmentEnhancement.Track == trackName);
                    if (container == null)
                    {
                        failures.Add($"beat {beat}: no geometry container for track '{trackName}'.");
                        continue;
                    }

                    var animator = Object.FindAnyObjectByType<TracksManager>()
                        .GetAnimationTrack(trackName);
                    Transform proxy = animator != null && animator.Track != null
                        ? animator.Track.ObjectParentTransform
                        : null;
                    if (proxy != null && proxy.localScale.magnitude > 0.01f)
                        failures.Add($"beat {beat}: '{trackName}' proxy localScale={proxy.localScale} (expected ~zero).");
                    if (proxy != null && Mathf.Abs(proxy.localPosition.y) < 1000f)
                        failures.Add($"beat {beat}: '{trackName}' proxy localPosition={proxy.localPosition} (expected ~(0,-6969,-6969)).");

                    foreach (var renderer in container.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var inFrustum = renderer.gameObject.activeInHierarchy
                            && renderer.enabled
                            && GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
                        if (inFrustum)
                            failures.Add($"beat {beat}: '{trackName}' renderer {renderer.name} intersects the playing frustum; bounds={renderer.bounds}.");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // User report: a NullReferenceException may fire when the map preview scrubs the
        // editing camera across the banished spheres; drive the camera transform directly
        // (no native input) so any real exception fails this test through Unity's log.
        [UnityTest]
        public IEnumerator PreviewCameraSweepPastHiddenSpheres()
        {
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            var editingCamera = cameraManager.CameraControllers[0].Camera.transform;
            editingCameraPosition = editingCamera.position;
            editingCameraRotation = editingCamera.rotation;

            try
            {
                foreach (var z in new[] { 0f, 10f, 100f })
                {
                    editingCamera.position = new Vector3(0f, 1.65f, z);
                    editingCamera.rotation = Quaternion.Euler(0f, z == 100f ? 180f : 0f, 0f);
                    yield return null;
                    yield return null;
                }
            }
            finally
            {
                editingCamera.position = editingCameraPosition;
                editingCamera.rotation = editingCameraRotation;
            }
        }

        // Deployed ChroMapper.log shows a repeated NRE from CameraController.Update ->
        // SetLockState during the Salty map load: SetLockState dereferences the static
        // `instance` (cleared by ANY CameraController.OnDisable, set only by the editing
        // camera's Start) and Mouse.current. This drives the production OnHoldtoMoveCamera
        // path through a dedicated virtual mouse in an isolated Input System runtime.
        [UnityTest]
        public IEnumerator PreviewRightMouseHoldReachesSetLockState()
        {
            var inputFixture = new InputTestFixture();
            inputFixture.Setup();
            var virtualMouse = InputSystem.AddDevice<Mouse>();
            var virtualKeyboard = InputSystem.AddDevice<Keyboard>();
            InputAction holdAction = null;

            try
            {
                // The deployed failure walked a Playing -> Preview UI transition before the
                // camera moved, so reproduce that ordering before the right-button hold.
                uiMode.SetUIMode(UIModeType.Playing, false);
                yield return null;
                uiMode.SetUIMode(UIModeType.Preview, false);
                cameraManager.SelectCamera(CameraType.Editing);
                yield return null;

                var editingController = cameraManager.CameraControllers[0];
                holdAction = new InputAction("holdToMoveCamera", binding: "<Mouse>/rightButton");
                holdAction.performed += context => editingController.OnHoldtoMoveCamera(context);
                holdAction.canceled += context => editingController.OnHoldtoMoveCamera(context);
                holdAction.Enable();

                var instanceField = typeof(CameraController).GetField(
                    "instance", BindingFlags.Static | BindingFlags.NonPublic);
                Debug.Log("[SaltyDiag] before lock: " +
                    $"cameraControllerInstanceNull={instanceField.GetValue(null) == null} " +
                    $"virtualMouseCurrent={Mouse.current == virtualMouse} " +
                    $"virtualKeyboardAdded={virtualKeyboard.added}");

                InputSystem.QueueStateEvent(virtualMouse,
                    new MouseState().WithButton(MouseButton.Right));
                InputSystem.Update();
                Assert.That(editingController.MovingCamera, Is.True,
                    "The production hold-to-move callback did not latch; the lock-state path was not reached.");

                // Let CameraController.Update run so the latched movement calls SetLockState(true).
                yield return null;
                yield return null;
                Debug.Log("[SaltyDiag] after frames: " +
                    $"cursorLockState={Cursor.lockState} " +
                    $"movingCamera={editingController.MovingCamera}");
            }
            finally
            {
                if (holdAction != null)
                {
                    holdAction.Disable();
                    holdAction.Dispose();
                }
                if (virtualMouse != null && virtualMouse.rightButton.isPressed)
                    InputSystem.QueueStateEvent(virtualMouse, new MouseState());
                InputSystem.Update();
                Cursor.lockState = CursorLockMode.None;
                virtualMouse = null;
                virtualKeyboard = null;
                inputFixture.TearDown();
            }
        }

        // The published Salty map authors BLACK as Standard with explicit shaderKeywords:[]; native
        // Chroma upgrades that to unlit Glowing with alpha 0, so the user's captured sphere at
        // _SongBpmTime.y=355.25 must block fog/specular content rather than render as a lit column.
        // The lit-material control proves the interior-pixel metric detects the wrong path.
        [UnityTest]
        public IEnumerator PublishedBlackSphereBlocksFogAndSpecularAtCapturedBeat()
        {
            var source =
                "C:/Users/tdrak/AppData/Local/Temp/devin_salty_original_ExpertPlusStandard.dat";
            var raw = JSON.Parse(File.ReadAllText(source));
            Assert.That(raw["customData"]["materials"]["BLACK"]["shaderKeywords"].IsArray, Is.True);
            Assert.That(raw["customData"]["materials"]["BLACK"]["shaderKeywords"].Count, Is.Zero);
            yield return TestUtils.ReloadMap(3, raw, beatsPerMinute: 215,
                environmentName: "PanicEnvironment", songLengthSeconds: 250);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();

            var previousBloom = Settings.Instance.Bloom;
            var previousBloomFog = Settings.Instance.BloomFog;
            var shotDir = Path.Combine(Application.temporaryCachePath, "SaltyMaterialParity",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(shotDir);
            var camera = cameraManager.CameraControllers[1].Camera;
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;
            var previousFov = camera.fieldOfView;
            Texture2D baseline = null;
            Texture2D offCapture = null;
            Texture2D control = null;
            Material controlMaterial = null;
            Camera.CameraCallback preRender = null;
            var failures = new List<string>();
            try
            {
                Settings.ApplyOptionByName("Bloom", true);
                Settings.ApplyOptionByName("BloomFog", true);
                ApplyDeployedCameraSettings();
                uiMode.SetUIMode(UIModeType.Playing, false);
                cameraManager.SelectCamera(CameraType.Playing);
                var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
                // The user's capture is keyed on SongBpmTime, not JSON time; log both.
                atsc.MoveToSongBpmTime(355.25f);
                Debug.Log($"[SaltyMat] MoveToSongBpmTime(355.25) -> jsonTime={atsc.CurrentJsonTime}");
                yield return null;
                yield return null;

                var container = Object.FindAnyObjectByType<GeometryGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.GeometryContainer>()
                    .SingleOrDefault(c => c.EnvironmentEnhancement.Track == "hehe");
                Assert.That(container, Is.Not.Null,
                    "Precondition: no geometry container for track 'hehe'.");
                var sphere = container.MpbController.Renderers[0];
                Assert.That(sphere, Is.Not.Null,
                    "Precondition: hehe has no shape renderer in MpbController.");

                // Exact world pose from the user's captured main camera.
                camera.transform.position = new Vector3(1.272731f, 2.211891f, -0.8168829f);
                camera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(.0426857f, -.3147805f, .9482042f), Vector3.up);
                camera.fieldOfView = 90f;

                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                Assert.That(sphere.gameObject.activeInHierarchy && sphere.enabled, Is.True,
                    "Precondition: hehe sphere renderer is disabled.");
                Assert.That(sphere.bounds.extents.sqrMagnitude, Is.GreaterThan(0.001f),
                    $"Precondition: hehe sphere bounds collapsed ({sphere.bounds}).");
                Assert.That(GeometryUtility.TestPlanesAABB(planes, sphere.bounds), Is.True,
                    "Precondition: hehe sphere is outside the captured camera frustum.");

                // Log the actual fog/bloom globals while this camera draws so a missing
                // fog environment can't masquerade as an identical lit/unlit result.
                preRender = cam =>
                {
                    if (cam != camera) return;
                    var dirColors = Shader.GetGlobalVectorArray("_DirectionalLightColors");
                    Debug.Log("[SaltyMat] onPreRender: " +
                        $"BLOOM_FOG={Shader.IsKeywordEnabled("BLOOM_FOG")} " +
                        $"POST_BLOOM={Shader.IsKeywordEnabled("POST_BLOOM")} " +
                        $"fogOffset={Shader.GetGlobalFloat("_CustomFogOffset")} " +
                        $"fogAtten={Shader.GetGlobalFloat("_CustomFogAttenuation")} " +
                        $"heightStartY={Shader.GetGlobalFloat("_CustomFogHeightFogStartY")} " +
                        $"heightH={Shader.GetGlobalFloat("_CustomFogHeightFogHeight")} " +
                        $"dirLightColors=[{string.Join(", ", dirColors.Select(c => c.ToString("F3")))}]");
                };
                Camera.onPreRender += preRender;

                baseline = RenderPixelsAndSave(camera,
                    Path.Combine(shotDir, "blacksphere-baseline.png"));

                var sphereShader = sphere.sharedMaterial == null
                    ? "null" : sphere.sharedMaterial.shader.name;
                if (sphereShader != "ChroMapper/Glowing")
                    failures.Add($"hehe sphere shader={sphereShader}, expected ChroMapper/Glowing");
                var mpb = new MaterialPropertyBlock();
                sphere.GetPropertyBlock(mpb);
                var authored = mpb.GetColor("_Color");
                if (authored.maxColorComponent > 0.01f || authored.a > 0.01f)
                    failures.Add($"hehe sphere MPB _Color={authored}, expected (0,0,0,0)");

                // Exact ray vs 0.9-inset world sphere: projecting bounds corners after the
                // capture RT is released would measure GameView aspect and include background.
                (int total, int bright, int red) MeasureInterior(Texture2D image)
                {
                    var center = sphere.bounds.center;
                    var radius = .9f * Mathf.Min(sphere.bounds.extents.x,
                        Mathf.Min(sphere.bounds.extents.y, sphere.bounds.extents.z));
                    var origin = camera.transform.position;
                    var offset = origin - center;
                    var c = Vector3.Dot(offset, offset) - radius * radius;
                    var tan = Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
                    var aspect = image.width / (float)image.height;
                    var forward = camera.transform.forward;
                    var right = camera.transform.right;
                    var up = camera.transform.up;
                    int total = 0, bright = 0, red = 0;
                    var pixels = image.GetPixels32();
                    for (var y = 0; y < image.height; y++)
                    for (var x = 0; x < image.width; x++)
                    {
                        var u = (x + .5f) / image.width;
                        var v = (y + .5f) / image.height;
                        var direction = (forward + right * ((2f * u - 1f) * tan * aspect)
                            + up * ((2f * v - 1f) * tan)).normalized;
                        var b = Vector3.Dot(offset, direction);
                        var discriminant = b * b - c;
                        if (discriminant <= 0f || -b + Mathf.Sqrt(discriminant) <= 0f) continue;
                        total++;
                        var p = pixels[y * image.width + x];
                        if (Mathf.Max(p.r, Mathf.Max(p.g, p.b)) > 40) bright++;
                        if (p.r > 80 && p.r > 2 * p.g && p.r > 2 * p.b) red++;
                    }
                    return (total, bright, red);
                }

                var baseStats = MeasureInterior(baseline);
                var sphereCenter = sphere.bounds.center;
                var insetRadius = .9f * Mathf.Min(sphere.bounds.extents.x,
                    Mathf.Min(sphere.bounds.extents.y, sphere.bounds.extents.z));
                Debug.Log($"[SaltyMat] baseline interior: total={baseStats.total} " +
                    $"bright={baseStats.bright} red={baseStats.red} " +
                    $"aspect={baseline.width}/{baseline.height} insetRadius={insetRadius:F2} " +
                    $"center={sphereCenter} capture={shotDir}");
                if (baseStats.total <= 500)
                {
                    failures.Add($"sphere interior sample too small (total={baseStats.total}, " +
                        "expected >500); sphere not covering the captured pose");
                }
                else if (baseStats.bright >= baseStats.total * .01f)
                {
                    failures.Add($"sphere interior has {baseStats.bright} bright pixels of " +
                        $"{baseStats.total} (expected <1%); the lit-path regression is back");
                }

                // Prove this renderer contributes to the captured frame at this pose/clock
                // before trusting the control comparison.
                try
                {
                    sphere.enabled = false;
                    offCapture = RenderPixelsAndSave(camera,
                        Path.Combine(shotDir, "blacksphere-sphere-off.png"));
                }
                finally
                {
                    sphere.enabled = true;
                }
                var offDelta = CountPixelDelta(
                    baseline.GetPixels32(), offCapture.GetPixels32());
                Debug.Log($"[SaltyMat] sphere-off pixel delta vs baseline={offDelta} " +
                    $"sphere: track={container.EnvironmentEnhancement.Track} " +
                    $"pos={sphere.transform.position} bounds={sphere.bounds}");

                // Negative control: force the photographed lit-Standard path on this one renderer
                // and confirm the metric detects the bright sphere the user captured.
                var savedMaterial = sphere.sharedMaterial;
                var savedMpb = new MaterialPropertyBlock();
                sphere.GetPropertyBlock(savedMpb);
                var litShader = Shader.Find("ChroMapper/Lit");
                controlMaterial = new Material(litShader);
                foreach (var keyword in new[] { "DIFFUSE", "SPECULAR", "FOG", "HEIGHT_FOG" })
                    controlMaterial.EnableKeyword(keyword);
                controlMaterial.SetFloat("_Metallic", 0f);
                controlMaterial.SetFloat("_Smoothness", .5f);
                controlMaterial.SetFloat("_SpecularIntensity", 1f);
                controlMaterial.SetFloat("_FogStartOffset", 1f);
                controlMaterial.SetFloat("_FogScale", 1f);
                // A cold shader variant can render identical to baseline on the swap frame;
                // warm the exact lit keyword combination before the control capture.
                try
                {
                    var variants = new ShaderVariantCollection();
                    variants.Add(new ShaderVariantCollection.ShaderVariant(litShader, PassType.Normal,
                        "DIFFUSE", "SPECULAR", "FOG", "HEIGHT_FOG", "BLOOM_FOG", "POST_BLOOM"));
                    variants.WarmUp();
                }
                catch (System.Exception e)
                {
                    Debug.Log($"[SaltyMat] lit variant warmup failed: {e.Message}");
                }
                try
                {
                    sphere.sharedMaterial = controlMaterial;
                    // Two frames so the material animator can push its own MPB; then reapply the
                    // captured pose and the control's black alpha=1 color right before rendering.
                    yield return null;
                    yield return null;
                    camera.transform.position = new Vector3(1.272731f, 2.211891f, -0.8168829f);
                    camera.transform.rotation = Quaternion.LookRotation(
                        new Vector3(.0426857f, -.3147805f, .9482042f), Vector3.up);
                    camera.fieldOfView = 90f;
                    var controlMpb = new MaterialPropertyBlock();
                    sphere.GetPropertyBlock(controlMpb);
                    controlMpb.SetColor("_Color", new Color(0f, 0f, 0f, 1f));
                    sphere.SetPropertyBlock(controlMpb);
                    Debug.Log("[SaltyMat] control material: " +
                        $"name={controlMaterial.name}#{controlMaterial.GetInstanceID()} " +
                        $"shader={controlMaterial.shader.name} " +
                        $"keywords={string.Join(",", controlMaterial.shaderKeywords)} " +
                        $"fogOffset={controlMaterial.GetFloat("_FogStartOffset")} " +
                        $"fogScale={controlMaterial.GetFloat("_FogScale")} " +
                        $"savedMat={(savedMaterial == null ? "null" : savedMaterial.name)}" +
                        $"#{(savedMaterial == null ? -1 : savedMaterial.GetInstanceID())}");
                    control = RenderPixelsAndSave(camera,
                        Path.Combine(shotDir, "blacksphere-lit-control.png"));
                    var controlStats = MeasureInterior(control);
                    Debug.Log($"[SaltyMat] lit control interior: total={controlStats.total} " +
                        $"bright={controlStats.bright} red={controlStats.red}");
                    if (baseStats.total > 500
                        && controlStats.bright <= baseStats.bright + 20)
                        failures.Add($"lit control bright={controlStats.bright} did not exceed " +
                            $"baseline+20 ({baseStats.bright}); metric cannot detect the lit path");
                }
                finally
                {
                    sphere.sharedMaterial = savedMaterial;
                    sphere.SetPropertyBlock(savedMpb);
                }
            }
            finally
            {
                if (preRender != null) Camera.onPreRender -= preRender;
                camera.transform.position = previousPosition;
                camera.transform.rotation = previousRotation;
                camera.fieldOfView = previousFov;
                Settings.ApplyOptionByName("Bloom", previousBloom);
                Settings.ApplyOptionByName("BloomFog", previousBloomFog);
                if (baseline != null) Object.DestroyImmediate(baseline);
                if (offCapture != null) Object.DestroyImmediate(offCapture);
                if (control != null) Object.DestroyImmediate(control);
                if (controlMaterial != null) Object.DestroyImmediate(controlMaterial);
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Prove whether Camera.Render in batch mode fires onPreRender/onPostRender and the
        // post-process command buffer; if the callbacks never fire, every prior dark frame is
        // evidence about the batch path only, not the deployed app.
        [UnityTest]
        public IEnumerator CameraPreRenderExecutionAtBeatZero()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            yield return SeekTo(0f);

            var bloom = Object.FindAnyObjectByType<PyramidBloomController>();
            var postProcess = Object.FindAnyObjectByType<PostProcessRenderingController>();
            Debug.Log($"[SaltyDiag] probe: bloom={Settings.Instance.Bloom} " +
                $"bloomFog={Settings.Instance.BloomFog} " +
                $"bloomReady={(bloom == null ? "none" : bloom.IsReady.ToString())} " +
                $"buffers={(postProcess == null ? -1 : camera.GetCommandBuffers(CameraEvent.BeforeImageEffects).Length)}");

            var mpb = new MaterialPropertyBlock();
            var ring = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.transform.root.name.StartsWith("Panels4TrackLaneRing"))
                .OrderBy(r => r.bounds.center.z)
                .FirstOrDefault();
            if (ring != null)
            {
                mpb.Clear();
                ring.GetPropertyBlock(mpb);
                Debug.Log($"[SaltyDiag] nearest Ring: mat=" +
                    $"{(ring.sharedMaterial == null ? "null" : ring.sharedMaterial.name)} " +
                    $"mpbColor={mpb.GetColor("_Color")} bounds={ring.bounds}");
            }

            var pre = 0;
            var post = 0;
            void OnPre(Camera c) { if (c == camera) pre++; }
            void OnPost(Camera c) { if (c == camera) post++; }
            Camera.onPreRender += OnPre;
            Camera.onPostRender += OnPost;
            try
            {
                var before = Shader.GetGlobalVectorArray("_DirectionalLightColors");
                CaptureCamera(camera, PathUtils.Combine(shotDir, "salty-hdr-beat0-callbackprobe.png"));
                var after = Shader.GetGlobalVectorArray("_DirectionalLightColors");
                Debug.Log($"[SaltyDiag] callbacks: preRender={pre} postRender={post} " +
                    $"dirLightColorsBefore={(before == null ? "null" : string.Join(";", before.Select(v => v.ToString())))} " +
                    $"after={(after == null ? "null" : string.Join(";", after.Select(v => v.ToString())))}");
            }
            finally
            {
                Camera.onPreRender -= OnPre;
                Camera.onPostRender -= OnPost;
            }

            var hehe2 = Object.FindAnyObjectByType<GeometryGridContainer>()
                .LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>()
                .SingleOrDefault(c => c.EnvironmentEnhancement.Track == "hehe2");
            var glowComponents = hehe2 == null
                ? new List<Behaviour>()
                : hehe2.GetComponentsInChildren<Behaviour>(true)
                    .Where(b => b != null && b.enabled
                        && (b is ParametricBloomFogLightController || b is BloomFogObject))
                    .ToList();
            try
            {
                foreach (var component in glowComponents) component.enabled = false;
                CaptureCamera(camera,
                    PathUtils.Combine(shotDir, "salty-hdr-beat0-hehe2glow-off.png"));
            }
            finally
            {
                foreach (var component in glowComponents) component.enabled = true;
            }
            Debug.Log($"[SaltyDiag] hehe2 glow components toggled={glowComponents.Count}");
        }

        // Deployed CM shows an intense white ring on the opening frame while an explicit seek to
        // beat 0 renders dark. If the b=0 ring-light event is only applied by seeking, the frame
        // right after load should differ from the frame after MoveToJsonTime(0).
        [UnityTest]
        public IEnumerator InitialLoadAndExplicitSeekKeepPanicRingsDark()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            yield return null;
            yield return null;

            // No seek: capture exactly what the freshly loaded map draws.
            var noSeek = CountNearWhiteAfterCapture(
                camera, PathUtils.Combine(shotDir, "salty-initial-no-seek.png"), "initial-no-seek");
            Assert.That(noSeek, Is.LessThan(100),
                $"Initial load frame already contains {noSeek} near-white pixels in the ring window.");

            yield return SeekTo(0f);
            var afterSeek = CountNearWhiteAfterCapture(
                camera, PathUtils.Combine(shotDir, "salty-after-seek0.png"), "after-seek0");
            Assert.That(afterSeek, Is.LessThan(100),
                $"Post-seek frame contains {afterSeek} near-white pixels in the ring window.");
        }

        private int CountNearWhiteAfterCapture(Camera camera, string path, string label)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var directionalLights = DirectionalLight.Lights == null
                ? "null"
                : string.Join(", ", DirectionalLight.Lights.Select(l =>
                    $"{l.name} c={l.Color} i={l.Intensity}"));
            Debug.Log($"[SaltyDiag] {label}: jsonTime={atsc.CurrentJsonTime} " +
                $"bpmTime={atsc.CurrentSongBpmTime} directionalLights=[{directionalLights}]");
            var mpb = new MaterialPropertyBlock();
            var ring = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.transform.root.name.StartsWith("Panels4TrackLaneRing"))
                .OrderBy(r => r.bounds.center.z)
                .FirstOrDefault();
            if (ring != null)
            {
                mpb.Clear();
                ring.GetPropertyBlock(mpb);
                Debug.Log($"[SaltyDiag] {label} nearest Ring: mat=" +
                    $"{(ring.sharedMaterial == null ? "null" : ring.sharedMaterial.name)} " +
                    $"mpbColor={mpb.GetColor("_Color")} bounds={ring.bounds}");
            }

            CaptureCamera(camera, path);
            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var nearWhite = 0;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                for (var topY = 120; topY <= 430; topY++)
                {
                    var y = 575 - topY;
                    for (var x = 350; x <= 680; x++)
                    {
                        var p = pixels[y * 1024 + x];
                        if (p.r > 204 && p.g > 204 && p.b > 204) nearWhite++;
                    }
                }
                Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            Debug.Log($"[SaltyDiag] {label}: nearWhite={nearWhite}");
            return nearWhite;
        }

        private void ApplyDeployedCameraSettings()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
        }

        private static void LogCameraState(StringBuilder report, Camera camera, float beat, string label)
        {
            Debug.Log($"[SaltyDiag] {label} settings: PlayerCameraFOV={Settings.Instance.PlayerCameraFOV} " +
                $"CameraFOV={Settings.Instance.CameraFOV} offsetZ={Settings.Instance.PlayerCameraOffsetZ} " +
                $"isTestRunnerSettings={ReferenceEquals(Settings.Instance, Settings.TestRunnerSettings)} " +
                $"cameraFovNow={camera.fieldOfView}");
            report.AppendLine($"=== {label} camera at beat {beat} === pos={camera.transform.position} " +
                $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} " +
                $"near={camera.nearClipPlane} far={camera.farClipPlane} " +
                $"pixels={camera.pixelWidth}x{camera.pixelHeight}");
        }

        private static IEnumerable<Renderer> CenterContributors(Camera camera)
        {
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            foreach (var renderer in Object.FindObjectsByType<Renderer>(
                FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var depth = Vector3.Dot(renderer.bounds.center - ray.origin, ray.direction);
                if (depth < 0f || depth > 100f) continue;
                var nearest = renderer.bounds.ClosestPoint(ray.origin + ray.direction * depth);
                var miss = Vector3.Distance(nearest, ray.origin + ray.direction * depth);
                if (miss < 1.5f) yield return renderer;
            }
        }

        private static void LogCenterContributors(StringBuilder report, Camera camera, float beat)
        {
            foreach (var renderer in CenterContributors(camera))
            {
                var root = renderer.transform.root.name;
                report.AppendLine($"  beat {beat}: {renderer.name} mat=" +
                    $"{(renderer.sharedMaterial == null ? "null" : renderer.sharedMaterial.name)} " +
                    $"root={root} bounds={renderer.bounds}");
            }
        }

        private static void DumpSphereState(StringBuilder report)
        {
            var mpb = new MaterialPropertyBlock();
            report.AppendLine("=== hehe/hehe2 sphere hierarchy ===");
            foreach (var trackName in new[] { "hehe", "hehe2" })
            {
                var container = Object.FindAnyObjectByType<GeometryGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.GeometryContainer>()
                    .SingleOrDefault(c => c.EnvironmentEnhancement.Track == trackName);
                if (container == null)
                {
                    report.AppendLine($"  {trackName}: no container");
                    continue;
                }
                var chain = new List<string>();
                for (var t = container.transform; t != null; t = t.parent)
                    chain.Add($"{t.name}(localScale={t.localScale})");
                report.AppendLine($"  {trackName}: worldScale={container.transform.lossyScale} " +
                    $"chain={string.Join(" <- ", chain)}");
                foreach (var renderer in container.GetComponentsInChildren<Renderer>(true))
                {
                    mpb.Clear();
                    renderer.GetPropertyBlock(mpb);
                    report.AppendLine($"    renderer={renderer.name} enabled={renderer.enabled} " +
                        $"mat={(renderer.sharedMaterial == null ? "null" : renderer.sharedMaterial.name)} " +
                        $"mpbColor={mpb.GetColor("_Color")} bounds={renderer.bounds}");
                }
                foreach (var fog in container.GetComponentsInChildren<BloomFogObject>(true))
                    report.AppendLine($"    fog={fog.name} enabled={fog.enabled}");
            }
        }

        // Deployed-vs-game discriminator: with deployed camera settings (FOV 90, offset 0) the game
        // shows a dark opening while CM draws a white circular ring around a black center roughly at
        // x390..634 / top-down y178..414 of a 1024x576 frame. Near-white pixels inside that window
        // (excluding the bottom red platform) mean the ring is being rendered.
        [UnityTest]
        public IEnumerator OpeningPlayingViewHasNoBrightCentralRing()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            yield return SeekTo(0f);

            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var postProcess = Object.FindAnyObjectByType<PostProcessRenderingController>();
            var bloom = Object.FindAnyObjectByType<PyramidBloomController>();
            Debug.Log($"[SaltyDiag] ring assertion: allowHDR={camera.allowHDR} rtFormat={rt.graphicsFormat} " +
                $"beforeImageEffectsBuffers={(postProcess == null ? -1 : camera.GetCommandBuffers(CameraEvent.BeforeImageEffects).Length)} " +
                $"bloomIsReady={(bloom == null ? "none" : bloom.IsReady.ToString())} " +
                $"settingsBloom={Settings.Instance.Bloom}");
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var nearWhite = 0;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                // ReadPixels is bottom-up; the reported ring spans top-down y120..430.
                for (var topY = 120; topY <= 430; topY++)
                {
                    var y = 575 - topY;
                    for (var x = 350; x <= 680; x++)
                    {
                        var p = pixels[y * 1024 + x];
                        if (p.r > 204 && p.g > 204 && p.b > 204) nearWhite++;
                    }
                }
                Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            Assert.That(nearWhite, Is.LessThan(100),
                $"The opening frame contains {nearWhite} near-white pixels inside the reported ring window.");
        }

        // Opening-circle contributor trial (diagnostic only): the live CM screenshot shows a
        // huge white ring at beat0 while the batch frame and game are dark, and the ring meshes
        // carry MPB _Color black. Force _Color to white on ONLY the Panels4TrackLaneRing "Ring"
        // mesh renderers (not BoxLight/EnvLightOpaque) via property block, restore originals in
        // finally, and compare the near-white pixel footprint to see whether _Color alone is
        // sufficient to produce the user's circle.
        [UnityTest]
        public IEnumerator PanicRingColorContributorTrial()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            yield return SeekTo(0f);
            Debug.Log($"[SaltyDiag] ringTrial camera pos={camera.transform.position} " +
                $"rot={camera.transform.rotation.eulerAngles} fov={camera.fieldOfView}");

            var rings = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled
                    && r.name == "Ring"
                    && r.sharedMaterial != null
                    && r.sharedMaterial.name == "Panels4TrackLaneRing"
                    && HasAncestorNamed(r.transform, "Panels4TrackLaneRing"))
                .ToList();
            // Keep each renderer's untouched original block; restoring via a fresh block with
            // only _Color would erase any other live MPB properties.
            var originals = new List<(Renderer renderer, MaterialPropertyBlock block)>();
            foreach (var ring in rings)
            {
                var original = new MaterialPropertyBlock();
                ring.GetPropertyBlock(original);
                var marker = ring.GetComponentInParent<ChromaIDMarker>();
                originals.Add((ring, original));
                if (originals.Count <= 6)
                    Debug.Log($"[SaltyDiag] ring[{originals.Count - 1}] {GetPath(ring.transform)} " +
                        $"chromaID={(marker == null ? "<none>" : marker.ChromaID)} " +
                        $"mat={ring.sharedMaterial.name} active={ring.gameObject.activeInHierarchy} " +
                        $"mpbColor={original.GetColor("_Color")} bounds={ring.bounds}");
            }
            Debug.Log($"[SaltyDiag] ringTrial rings={rings.Count}");

            var baseline = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-ring-beat0-baseline.png"));
            // Combined trial without an intervening yield: the ring shader is lit multiplicatively,
            // so MPB _Color and the directional globals must be white in the same frame.
            var savedGlobals = Shader.GetGlobalVectorArray("_DirectionalLightColors") ?? new Vector4[0];
            Texture2D combined = null;
            Texture2D combinedRingsOff = null;
            try
            {
                var block = new MaterialPropertyBlock();
                foreach (var ring in rings)
                {
                    ring.GetPropertyBlock(block);
                    block.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
                    ring.SetPropertyBlock(block);
                }
                var white = Enumerable.Repeat(new Vector4(3f, 3f, 3f, 3f), Mathf.Max(savedGlobals.Length, 5)).ToArray();
                Shader.SetGlobalVectorArray("_DirectionalLightColors", white);
                combined = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, "salty-ring-beat0-combined.png"));

                // A/B under the combined input: disable only the ring meshes to confirm they own
                // any white circle that appears.
                try
                {
                    foreach (var ring in rings) ring.enabled = false;
                    combinedRingsOff = RenderPixelsAndSave(camera,
                        PathUtils.Combine(shotDir, "salty-ring-beat0-combined-rings-off.png"));
                }
                finally
                {
                    foreach (var ring in rings) ring.enabled = true;
                }
            }
            finally
            {
                if (savedGlobals.Length > 0)
                    Shader.SetGlobalVectorArray("_DirectionalLightColors", savedGlobals);
                foreach (var (renderer, block) in originals)
                    renderer.SetPropertyBlock(block);
            }

            var baseWhite = CountNearWhiteRingPixels(baseline);
            var combinedWhite = CountNearWhiteRingPixels(combined);
            var offWhite = CountNearWhiteRingPixels(combinedRingsOff);
            var combinedDelta = CountPixelDelta(baseline.GetPixels32(), combined.GetPixels32());
            var offDelta = CountPixelDelta(combined.GetPixels32(), combinedRingsOff.GetPixels32());
            Object.DestroyImmediate(baseline);
            Object.DestroyImmediate(combined);
            Object.DestroyImmediate(combinedRingsOff);
            Debug.Log($"[SaltyDiag] ringTrial combined near-white px baseline={baseWhite} " +
                $"combined={combinedWhite} ringsOff={offWhite} | full-frame delta combined={combinedDelta} off={offDelta} " +
                $"savedGlobalsLen={savedGlobals.Length}");
        }

        // The user's CM beat0 circle has 4 cardinal notches matching the ring clones' two
        // GlowLine children (type2 TubeBloomPrePassLightWithId + BoxLight + BakedBloom), which
        // the prior trial never drove. The map has NO b0 type2 event, so the game leaves them
        // off; force each ring GlowLine controller to (3,3,3,1) without yielding and check
        // whether the lit ring stack reproduces the screenshot silhouette. Diagnostic only.
        [UnityTest]
        public IEnumerator PanicGlowLineContributorTrial()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            yield return SeekTo(0f);

            var controllers = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsSortMode.None)
                .Where(c => c.enabled && HasAncestorNamed(c.transform, "Panels4TrackLaneRing"))
                .ToList();
            Debug.Log($"[SaltyDiag] glowlineTrial controllers={controllers.Count}");
            var originalColors = new List<(ParametricBloomFogLightController controller, Color color)>();
            foreach (var controller in controllers)
            {
                originalColors.Add((controller, controller.Color));
                if (originalColors.Count <= 6)
                    Debug.Log($"[SaltyDiag] glowline[{originalColors.Count - 1}] {GetPath(controller.transform)} " +
                        $"type={controller.Type} id={controller.ID} color={controller.Color}");
            }

            var baseline = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-ring-beat0-glowline-baseline.png"));
            Texture2D lit = null;
            Texture2D litOff = null;
            try
            {
                foreach (var controller in controllers)
                    controller.SetColor(new Color(3f, 3f, 3f, 1f));
                // No yield so the state loop cannot overwrite the injected colors.
                lit = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, "salty-ring-beat0-glowline-on.png"));

                var disabled = new List<Behaviour>();
                try
                {
                    foreach (var behaviour in controllers
                        .SelectMany(c => c.GetComponentsInChildren<Behaviour>(true))
                        .Where(b => b.enabled && (b is MeshRenderer || b is BloomFogObject)))
                    {
                        behaviour.enabled = false;
                        disabled.Add(behaviour);
                    }
                    litOff = RenderPixelsAndSave(camera,
                        PathUtils.Combine(shotDir, "salty-ring-beat0-glowline-on-off.png"));
                }
                finally
                {
                    foreach (var behaviour in disabled) behaviour.enabled = true;
                }
            }
            finally
            {
                foreach (var (controller, color) in originalColors)
                    controller.SetColor(color);
            }

            var baseWhite = CountNearWhiteRingPixels(baseline);
            var litWhite = CountNearWhiteRingPixels(lit);
            var offWhite = CountNearWhiteRingPixels(litOff);
            var litDelta = CountPixelDelta(baseline.GetPixels32(), lit.GetPixels32());
            var offDelta = CountPixelDelta(lit.GetPixels32(), litOff.GetPixels32());
            Object.DestroyImmediate(baseline);
            Object.DestroyImmediate(lit);
            Object.DestroyImmediate(litOff);
            Debug.Log($"[SaltyDiag] glowlineTrial near-white px baseline={baseWhite} lit={litWhite} " +
                $"litOff={offWhite} | full-frame delta lit={litDelta} off={offDelta}");
        }

        // Fresh-load discriminator for the opening circle: every prior dark result used the
        // in-place map swap; the deployed live session instead goes through the real
        // 03->02->03 LoadInitialMap.LoadMap scene pipeline. forceSceneReload pays that cost so
        // initial-load state (sphere banish, ring light colors) is produced by the same path
        // the user sees. Asserts spheres stay out of the playing frustum and <100 near-white.
        [UnityTest]
        public IEnumerator FreshMapperLoadOpeningSphereAndRingState()
        {
            ApplyDeployedCameraSettings();
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(SourceMapPath)),
                beatsPerMinute: 215,
                environmentName: "PanicEnvironment",
                songLengthSeconds: 250,
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            // The scene round trip destroyed the original UIMode/CameraManager references.
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            var failures = new List<string>();

            // First capture at beat0 without any seek, mirroring the live opening.
            yield return null;
            yield return null;
            var initial = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-freshload-beat0-initial.png"));
            var initialWhite = LogOpeningState(camera, "initial-no-seek", initial);

            yield return SeekTo(0f);
            var seek = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-freshload-beat0-seek.png"));
            var seekWhite = LogOpeningState(camera, "after-seek0", seek);

            foreach (var (label, texture, nearWhite) in new[]
                { ("initial-no-seek", initial, initialWhite), ("after-seek0", seek, seekWhite) })
            {
                if (!BothSpheresOutsideFrustum(camera))
                    failures.Add($"{label}: a hehe/hehe2 sphere renderer intersects the playing frustum.");
                if (nearWhite >= 100)
                    failures.Add($"{label}: {nearWhite} near-white pixels in the ring window.");
                Object.DestroyImmediate(texture);
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Live-deployed discriminator for the opening circle: the user's Info.dat editor state
        // keeps EditingMode.BasicEvent, and deployed GeoDiag at beat6 shows hehe2's transform
        // animator with track=<none>, scale16, unbanished at (0,0.37,20). Hypothesis under test:
        // entering Playing from the BasicEvent workspace deactivates Gameplay Container Tracks,
        // ObjectAnimator.OnDisable drops the track attachment, and OnEnable never reattaches,
        // losing the die-parent banish. Expected RED if that path drops the track.
        [UnityTest]
        public IEnumerator PlayingFromBasicEventWorkspaceKeepsDieSphereBanish()
        {
            ApplyDeployedCameraSettings();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            Assert.That(editMode, Is.Not.Null, "No EditModeContext in the loaded scene.");
            var savedMode = editMode.EditingMode;
            var failures = new List<string>();
            try
            {
                editMode.EditingMode = EditingMode.BasicEvent;
                yield return null; // let UpdateGrid deactivate the gameplay track container
                uiMode.SetUIMode(UIModeType.Playing, false);
                cameraManager.SelectCamera(CameraType.Playing);
                var camera = cameraManager.CameraControllers[1].Camera;
                if (!UIMode.AnimationMode)
                    failures.Add("UIMode.AnimationMode was false in Playing; the test did not " +
                        "match the deployed preview state.");
                foreach (var beat in new[] { 0.1f, 6f, 0.1f })
                {
                    yield return SeekTo(beat);
                    InspectSphereState(camera, beat, failures);
                }
            }
            finally
            {
                editMode.EditingMode = savedMode;
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private void InspectSphereState(Camera camera, float beat, List<string> failures)
        {
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(geometry, Is.Not.Null, "No GeometryGridContainer in the loaded scene.");
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var container in geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>())
            {
                var track = container.EnvironmentEnhancement.Track;
                if (track != "hehe" && track != "hehe2") continue;
                var scale = container.transform.lossyScale;
                var renderer = container.GetComponentInChildren<MeshRenderer>();
                var boundsText = renderer == null ? "none" : renderer.bounds.ToString();
                var inFrustum = renderer != null
                    && renderer.enabled
                    && GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
                var animTrack = tracks == null ? null : tracks.GetAnimationTrack(track);
                var attached = animTrack != null
                    && animTrack.Children.Contains(container.Animator);
                Debug.Log($"[SaltyDiag] basicEventPlaying beat={beat} sphere={track} " +
                    $"lossyScale={scale} bounds={boundsText} inFrustum={inFrustum} " +
                    $"trackAttached={attached} animatorEnabled={container.Animator.enabled}");
                if (scale.magnitude > 0.5f)
                    failures.Add($"beat {beat}: {track} lossyScale={scale} - the die banish is lost.");
                if (inFrustum)
                    failures.Add($"beat {beat}: {track} renderer inside the playing frustum.");
                if (!attached)
                    failures.Add($"beat {beat}: {track} animator detached from its track " +
                        $"(trackChildren={(animTrack == null ? "no track" : animTrack.Children.Count.ToString())}).");
            }
        }

        private bool BothSpheresOutsideFrustum(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            if (geometry == null) return true;
            foreach (var container in geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>())
            {
                var track = container.EnvironmentEnhancement.Track;
                if (track != "hehe" && track != "hehe2") continue;
                foreach (var renderer in container.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                        return false;
            }
            return true;
        }

        private int LogOpeningState(Camera camera, string label, Texture2D captured)
        {
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            if (geometry != null)
            {
                foreach (var container in geometry.LoadedContainers.Values
                    .OfType<Beatmap.Containers.GeometryContainer>())
                {
                    var track = container.EnvironmentEnhancement.Track;
                    if (track != "hehe" && track != "hehe2") continue;
                    var t = container.transform;
                    var renderer = container.GetComponentInChildren<Renderer>();
                    Debug.Log($"[SaltyDiag] freshload {label} sphere {track} pos={t.position} " +
                        $"localScale={t.localScale} lossyScale={t.lossyScale} " +
                        $"bounds={(renderer == null ? "none" : renderer.bounds.ToString())}");
                }
            }
            var glows = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsSortMode.None)
                .Where(c => c.enabled && HasAncestorNamed(c.transform, "Panels4TrackLaneRing"))
                .ToList();
            var lit = glows.Count(c => c.Color.a > 0.01f);
            Debug.Log($"[SaltyDiag] freshload {label} ring GlowLines={glows.Count} lit={lit} " +
                $"camera pos={camera.transform.position} fov={camera.fieldOfView}");
            var nearWhite = CountNearWhiteRingPixels(captured);
            Debug.Log($"[SaltyDiag] freshload {label} near-white ring window={nearWhite}");
            return nearWhite;
        }

        private static bool HasAncestorNamed(Transform t, string prefix)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name.StartsWith(prefix)) return true;
            return false;
        }

        // Near-white count in the reported ring window x350..680, top-down y120..430
        // (same window/criteria as OpeningPlayingViewHasNoBrightCentralRing).
        private static int CountNearWhiteRingPixels(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var count = 0;
            for (var topY = 120; topY <= 430; topY++)
            {
                var y = 575 - topY;
                for (var x = 350; x <= 680; x++)
                {
                    var p = pixels[y * 1024 + x];
                    if (p.r > 204 && p.g > 204 && p.b > 204) count++;
                }
            }
            return count;
        }

        private static IEnumerable<Renderer> FakeWallRenderers(HashSet<string> trackNames)
        {
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            if (walls == null) yield break;
            foreach (var container in walls.LoadedContainers.Values.OfType<Beatmap.Containers.ObstacleContainer>())
            {
                if (!container.ObstacleData.CustomFake || !TrackMatches(container.ObstacleData.CustomTrack, trackNames))
                    continue;
                foreach (var renderer in container.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled) yield return renderer;
            }
        }

        private static bool TrackMatches(JSONNode track, HashSet<string> names)
        {
            if (track == null) return false;
            if (track.IsString) return names.Contains(track.Value);
            if (track.IsArray) return track.Children.Any(child => child.IsString && names.Contains(child.Value));
            return false;
        }

        private static void DumpFakeWallState(StringBuilder report, Camera camera, float beat)
        {
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var names = new HashSet<string>(Enumerable.Range(0, 8).Select(i => $"beat{i}"))
            {
                "1/4", "pee2"
            };
            report.AppendLine($"=== fake walls at beat {beat} ===");
            if (walls == null)
            {
                report.AppendLine("  no ObstacleGridContainer");
                return;
            }
            var mpb = new MaterialPropertyBlock();
            foreach (var container in walls.LoadedContainers.Values.OfType<Beatmap.Containers.ObstacleContainer>())
            {
                if (!container.ObstacleData.CustomFake
                    || !TrackMatches(container.ObstacleData.CustomTrack, names))
                    continue;
                foreach (var renderer in container.GetComponentsInChildren<Renderer>(true))
                {
                    var min = camera.WorldToScreenPoint(renderer.bounds.min);
                    var max = camera.WorldToScreenPoint(renderer.bounds.max);
                    mpb.Clear();
                    renderer.GetPropertyBlock(mpb);
                    report.AppendLine($"  {container.ObstacleData.JsonTime} track={container.ObstacleData.CustomTrack} " +
                        $"renderer={renderer.name} " +
                        $"mat={(renderer.sharedMaterial == null ? "null" : renderer.sharedMaterial.name)} " +
                        $"active={renderer.gameObject.activeInHierarchy} enabled={renderer.enabled} " +
                        $"pos={container.transform.position} scale={container.transform.localScale} " +
                        $"bounds={renderer.bounds} screenY={min.y:F0}->{max.y:F0}px576 " +
                        $"mpbColor={mpb.GetColor("_Color")} sizeParams={mpb.GetVector("_SizeParams")}");
                }
            }
        }

        // Render the camera once, write the PNG, and hand back the raw pixels for the
        // per-row wall-footprint diff (caller owns the texture).
        // Deployed-vs-game discriminator for the beat-222 white wall: the game draws a visible
        // horizontal white line, but the HDR capture showed whitewalls-off byte-identical to
        // normal. Count every pixel that disappears when only this wall's Core+Outline are off.
        [UnityTest]
        public IEnumerator WhiteFakeWallContributesPixelsAtBeat222()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            yield return SeekTo(222.25f);

            var wall = FindWhiteFakeWall();
            var normalPixels = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-hdr-beat222.25-white-normal.png"));

            var disabled = new List<Renderer>();
            Texture2D offPixels = null;
            try
            {
                foreach (var renderer in wall.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.enabled && (r.name == "Core" || r.name == "Outline")))
                {
                    renderer.enabled = false;
                    disabled.Add(renderer);
                }
                Assert.That(disabled, Is.Not.Empty,
                    "The white fake wall had no enabled Core/Outline renderers to isolate.");
                offPixels = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, "salty-hdr-beat222.25-white-off.png"));
            }
            finally
            {
                foreach (var renderer in disabled) renderer.enabled = true;
            }

            var delta = CountPixelDelta(normalPixels.GetPixels32(), offPixels.GetPixels32());
            Object.DestroyImmediate(normalPixels);
            Object.DestroyImmediate(offPixels);
            Assert.That(delta, Is.GreaterThan(0),
                $"Disabling the white wall changed {delta} pixels; the game draws it visibly at beat 222.25.");
        }

        // Same wall at the frame shader's edge thickness: if the collapsed .006 outline renders no
        // pixels where the authored scale does, a missing wall is the collapsed scale, not the
        // renderer path. The authored outline is already .05 in the current map, so the A/B must
        // capture the thin scale temporarily and compare it against the restored authored render;
        // thickening to .05 again was identity and invalidated the original delta.
        [UnityTest]
        public IEnumerator WhiteFakeWallAtOutlineEdgeThicknessRenders()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            yield return SeekTo(222.25f);

            var wall = FindWhiteFakeWall();
            var outline = wall.GetComponent<Beatmap.Containers.ObstacleContainer>().OutlineTransform;
            var authoredScale = outline.localScale;
            Assert.That(authoredScale.y, Is.EqualTo(0.05f).Within(0.001f),
                "The A/B premise broke: the authored white wall outline is no longer frame-edge thickness.");

            Texture2D thinPixels;
            try
            {
                outline.localScale = new Vector3(authoredScale.x, 0.006f, authoredScale.z);
                thinPixels = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, "salty-hdr-beat222.25-white-thin.png"));
            }
            finally
            {
                outline.localScale = authoredScale;
            }

            var authoredPixels = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-hdr-beat222.25-white-authored.png"));

            var delta = CountPixelDelta(thinPixels.GetPixels32(), authoredPixels.GetPixels32());
            Object.DestroyImmediate(thinPixels);
            Object.DestroyImmediate(authoredPixels);
            Debug.Log($"[SaltyDiag] white wall outline y=0.006->{authoredScale.y} delta={delta} px");
            Assert.That(delta, Is.GreaterThan(0),
                "Restoring the authored frame-edge outline over the collapsed .006 scale produced no pixels.");
        }

        // Beat-2.625 laser routing: the authored b2.594 et0 lightID[1] event maps (Chroma Panic
        // type1 key1 -> controller index 6) to ConstructionGlowLine (6). The user confirms live
        // CM now matches the game's side/angle after the ObjectAnimator lifecycle fix, so the
        // headless right>left screenshot assertion was a false negative and is removed; this
        // test now only asserts the registered ID6 controller is lit and owns visible red beam
        // pixels. The A/B ID6-off capture still proves those pixels belong to ID6.
        [UnityTest]
        public IEnumerator Beat2625LightIdOneDrivesRegisteredPanicLaser()
        {
            ApplyDeployedCameraSettings();
            // Deployed-state discriminator: the user's Info.dat keeps EditingMode.BasicEvent, so
            // exercise the same workspace->Playing path as the sphere-detachment repro to see
            // whether the laser side shares that lifecycle root.
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var savedMode = editMode == null ? EditingMode.Gameplay : editMode.EditingMode;
            try
            {
                if (editMode != null)
                {
                    editMode.EditingMode = EditingMode.BasicEvent;
                    yield return null; // let UpdateGrid deactivate the gameplay track container
                }
                uiMode.SetUIMode(UIModeType.Playing, false);
                cameraManager.SelectCamera(CameraType.Playing);
                if (!UIMode.AnimationMode)
                    Debug.Log("[SaltyDiag] laser2625 UIMode.AnimationMode=false after workspace switch");
                var camera = cameraManager.CameraControllers[1].Camera;
                var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
                Directory.CreateDirectory(shotDir);
                yield return SeekTo(2.625f);

                var normalPixels = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, "salty-laser2625-normal.png"));

                var laserController = Object.FindObjectsByType<ParametricBloomFogLightController>(
                        FindObjectsSortMode.None)
                    .Where(c => c.Type == 0 && c.ID == 6)
                    .ToList();
                foreach (var controller in laserController)
                {
                    var t = controller.transform;
                    Debug.Log($"[SaltyDiag] laser controller {t.name} path={GetPath(t)} " +
                        $"pos={t.position} rot={t.rotation.eulerAngles} type={controller.Type} id={controller.ID} color={controller.Color}");
                }
                Assert.That(laserController.Count(c => c.Color.a > 0.01f), Is.GreaterThan(0),
                    "No registered type0 ID6 controller carried light at beat 2.625; " +
                    "the authored lightID[1] event did not reach the remapped laser.");

                var disabled = new List<Behaviour>();
                Texture2D offPixels = null;
                try
                {
                    foreach (var behaviour in laserController
                        .SelectMany(c => c.GetComponentsInChildren<Behaviour>(true))
                        .Where(b => b.enabled
                            && (b is BloomFogObject || (b is Renderer && b.name == "BoxLight"))))
                    {
                        behaviour.enabled = false;
                        disabled.Add(behaviour);
                    }
                    offPixels = RenderPixelsAndSave(camera,
                        PathUtils.Combine(shotDir, "salty-laser2625-off.png"));
                }
                finally
                {
                    foreach (var behaviour in disabled) behaviour.enabled = true;
                }

                var normalL = CountRedLaserPixels(normalPixels, 260, 460);
                var normalR = CountRedLaserPixels(normalPixels, 564, 764);
                var offL = CountRedLaserPixels(offPixels, 260, 460);
                var offR = CountRedLaserPixels(offPixels, 564, 764);
                Object.DestroyImmediate(normalPixels);
                Object.DestroyImmediate(offPixels);
                Debug.Log($"[SaltyDiag] laser2625 red-px normal L={normalL} R={normalR} " +
                    $"| ID6-off L={offL} R={offR} (controllers={laserController.Count}, toggled={disabled.Count})");
                Assert.That(normalL + normalR, Is.GreaterThan(0),
                    $"No red laser pixels at beat 2.625; left={normalL} right={normalR}.");
                Assert.That(offL + offR, Is.LessThan(normalL + normalR),
                    $"Disabling ID6's BoxLight/BloomFog children changed no red pixels; " +
                    $"normal={normalL + normalR} off={offL + offR}.");
            }
            finally
            {
                if (editMode != null) editMode.EditingMode = savedMode;
            }
        }

        // Routing-vs-transform discriminator for the beat-2.625 laser side: if handing ID6's
        // live color to sibling controller ID7 moves the red beam to the right-hand window,
        // the wrong side is a light-ID routing choice, not a transform error. A right-side
        // trial would show routing is *sufficient* but would NOT prove the game uses the
        // inverse table. Colors are restored in finally with no yielded frame in between.
        [UnityTest]
        public IEnumerator Beat2625LaserRoutingTrial()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            yield return SeekTo(2.625f);

            var runtimeContext = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Debug.Log($"[SaltyDiag] descriptor id={runtimeContext.Descriptor.ID} " +
                $"scene={runtimeContext.Descriptor.gameObject.scene.name}");
            var type0 = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsSortMode.None)
                .Where(c => c.Type == 0 && (c.ID == 6 || c.ID == 7))
                .OrderBy(c => c.ID)
                .ToList();
            foreach (var controller in type0)
            {
                var marker = controller.GetComponent<ChromaIDMarker>();
                var t = controller.transform;
                Debug.Log($"[SaltyDiag] type0 id={controller.ID} chromaID=" +
                    $"{(marker == null ? "<none>" : marker.ChromaID)} " +
                    $"path={GetPath(t)} pos={t.position} rotQ={t.rotation} color={controller.Color} " +
                    $"kind={controller.Kind} scene={controller.gameObject.scene.name}");
            }
            var id6 = type0.SingleOrDefault(c => c.ID == 6);
            var id7 = type0.SingleOrDefault(c => c.ID == 7);
            Assert.That(id6, Is.Not.Null, "Type0 ID6 controller not found.");
            Assert.That(id7, Is.Not.Null, "Type0 ID7 controller not found.");

            var baseline = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-laser2625-route-baseline.png"));
            var id6Color = id6.Color;
            var id7Color = id7.Color;
            Texture2D trial = null;
            try
            {
                id6.SetColor(Color.clear);
                id7.SetColor(id6Color);
                // No yield: the state update loop cannot overwrite the injected colors.
                trial = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, "salty-laser2625-id7-trial.png"));
            }
            finally
            {
                id6.SetColor(id6Color);
                id7.SetColor(id7Color);
            }

            var baseL = CountRedLaserPixels(baseline, 260, 460);
            var baseR = CountRedLaserPixels(baseline, 564, 764);
            var trialL = CountRedLaserPixels(trial, 260, 460);
            var trialR = CountRedLaserPixels(trial, 564, 764);
            Object.DestroyImmediate(baseline);
            Object.DestroyImmediate(trial);
            Debug.Log($"[SaltyDiag] routing trial red-px baseline L={baseL} R={baseR} " +
                $"| id6->id7 trial L={trialL} R={trialR}");

            // Beat 2.797: which type0 controller carries the map's event lightID 2 response.
            yield return SeekTo(2.797f);
            var lit = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsSortMode.None)
                .Where(c => c.Type == 0 && c.Color.a > 0.01f)
                .Select(c => $"id{c.ID}:{c.name}@{c.transform.position} color={c.Color}")
                .ToList();
            Debug.Log($"[SaltyDiag] beat2.797 lit type0 controllers: " +
                (lit.Count == 0 ? "<none>" : string.Join("; ", lit)));
        }

        // Red laser pixels in the lower half: top-down rows y350..575 (Texture2D is bottom-up),
        // criterion r>=140 && r>=2g && r>=2b so only saturated red counts.
        private static int CountRedLaserPixels(Texture2D texture, int xMin, int xMax)
        {
            var pixels = texture.GetPixels32();
            var count = 0;
            for (var topY = 350; topY <= 575 && topY < 576; topY++)
            {
                var row = 575 - topY;
                for (var x = xMin; x <= xMax && x < 1024; x++)
                {
                    var p = pixels[row * 1024 + x];
                    if (p.r >= 140 && p.r >= 2 * p.g && p.r >= 2 * p.b) count++;
                }
            }
            return count;
        }

        private static string GetPath(Transform t) =>
            t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;

        private static Beatmap.Containers.ObstacleContainer FindWhiteFakeWall()
        {
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(walls, Is.Not.Null, "No ObstacleGridContainer in the loaded scene.");
            var wall = walls.LoadedContainers.Values
                .OfType<Beatmap.Containers.ObstacleContainer>()
                .SingleOrDefault(c => c.ObstacleData.CustomFake
                    && Mathf.Approximately(c.ObstacleData.JsonTime, 221f)
                    && c.ObstacleData.CustomTrack != null
                    && c.ObstacleData.CustomTrack.IsArray
                    && c.ObstacleData.CustomTrack.Children.Any(child =>
                        child.IsString && child.Value == "1/4")
                    && c.ObstacleData.CustomTrack.Children.Any(child =>
                        child.IsString && child.Value == "pee2"));
            Assert.That(wall, Is.Not.Null,
                "No b=221 fakeObstacle with tracks [1/4, pee2] in the loaded containers.");
            return wall;
        }

        private static int CountPixelDelta(Color32[] a, Color32[] b)
        {
            var count = 0;
            for (var i = 0; i < a.Length; i++)
            {
                var delta = Mathf.Abs(a[i].r - b[i].r)
                    + Mathf.Abs(a[i].g - b[i].g)
                    + Mathf.Abs(a[i].b - b[i].b);
                if (delta >= 45) count++;
            }
            return count;
        }

        private Texture2D RenderPixelsAndSave(Camera camera, string path)
        {
            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        // Wall footprint spec from the user's verbal report: x350..680, top-down y0..430,
        // per-pixel delta = sum of abs channel diffs, a row counts when >=3 pixels with delta>=45.
        private static void AppendWallDiff(
            StringBuilder report, string name, Color32[] normalPixels, Color32[] offPixels)
        {
            report.AppendLine($"=== {name} wall diff (normal vs off) ===");
            var changedRows = new List<(int topY, int count, int firstX, int lastX)>();
            for (var topY = 0; topY <= 430; topY++)
            {
                var y = 575 - topY;
                var count = 0;
                var firstX = -1;
                var lastX = -1;
                for (var x = 350; x <= 680; x++)
                {
                    var n = normalPixels[y * 1024 + x];
                    var o = offPixels[y * 1024 + x];
                    var delta = Mathf.Abs(n.r - o.r) + Mathf.Abs(n.g - o.g) + Mathf.Abs(n.b - o.b);
                    if (delta < 45) continue;
                    count++;
                    if (firstX < 0) firstX = x;
                    lastX = x;
                }
                if (count >= 3) changedRows.Add((topY, count, firstX, lastX));
            }

            if (changedRows.Count == 0)
            {
                report.AppendLine("  no changed rows");
                return;
            }
            var groupStart = 0;
            for (var i = 1; i <= changedRows.Count; i++)
            {
                if (i < changedRows.Count && changedRows[i].topY == changedRows[i - 1].topY + 1)
                    continue;
                var rows = changedRows.GetRange(groupStart, i - groupStart);
                report.AppendLine($"  topY {rows[0].topY}..{rows[^1].topY}: " +
                    string.Join(", ", rows.Select(r => $"y{r.topY}={r.count}px x{r.firstX}-{r.lastX}")));
                groupStart = i;
            }
        }

        private void CapturePlayingCamera(string path) =>
            CaptureCamera(cameraManager.CameraControllers[1].Camera, path);

        private void CaptureCamera(Camera camera, string path)
        {
            // Match the deployed frame path: PostProcessRenderingController's BeforeImageEffects
            // command buffer copies an HDR camera target, so an LDR RT would exercise a different
            // bloom source than the live app.
            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var postProcess = Object.FindAnyObjectByType<PostProcessRenderingController>();
            var bloom = Object.FindAnyObjectByType<PyramidBloomController>();
            Debug.Log($"[SaltyDiag] capture {path}: allowHDR={camera.allowHDR} " +
                $"rtFormat={rt.graphicsFormat} " +
                $"beforeImageEffectsBuffers={(postProcess == null ? -1 : camera.GetCommandBuffers(CameraEvent.BeforeImageEffects).Length)} " +
                $"bloomIsReady={(bloom == null ? "none" : bloom.IsReady.ToString())} " +
                $"settingsBloom={Settings.Instance.Bloom}");
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        // User report: notes/chains on tracks `bass`, `dropL`, `dropR` between beats 161-195
        // should follow the authored track color animations (gray/black strobe at 176, base
        // color immediately after the single-point b177 events, 4-beat fade from 189-193)
        // and otherwise show the live ColorScheme base note colors.
        [UnityTest]
        public IEnumerator NormalNotesBeforeAnyColorTrackEvent()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;

            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            foreach (var (beat, side) in new[]
            {
                (160.75f, 0), (160.75f, 1),
                (162.125f, 0), (162.125f, 1),
                (175.25f, 0), (175.25f, 1),
                (188.75f, 0), (188.75f, 1),
            })
            {
                yield return SeekTo(beat);
                var track = side == 0 ? (beat < 161f ? "bass" : "dropL")
                    : beat < 161f ? "bass" : "dropR";
                var expected = side == 0 ? scheme.LeftNoteColor : scheme.RightNoteColor;
                CheckNoteColor(failures, beat, track, side, expected);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Authored b176 events [367]/[368]: 0.125-beat .42/.2 gray -> clear strobe, repeat 6969.
        // The b176 c1 burstSlider (tb=176.25, sc=8) on dropR must color every active chain node.
        // Single-point b177 base-note-color points restore the base color immediately.
        [UnityTest]
        public IEnumerator Beat176FlashCyclesGrayBlackAndColorsChainSegments()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;

            var dropLGray = new Color(0.2f, 0.2f, 0.2f, 0.2f);
            var dropRGray = new Color(0.42f, 0.42f, 0.42f, 0.42f);
            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            foreach (var (beat, left, right) in new[]
            {
                (176.03125f, dropLGray, dropRGray),
                (176.09375f, Color.clear, Color.clear),
                (176.15625f, dropLGray, dropRGray),
                (176.21875f, Color.clear, Color.clear),
            })
            {
                yield return SeekTo(beat);
                CheckNoteColor(failures, beat, "dropL", null, left);
                CheckNoteColor(failures, beat, "dropR", null, right);
                CheckChainColors(failures, beat, 176f, "dropR", right);
            }

            yield return SeekTo(177.375f);
            CheckNoteColor(failures, 177.375f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 177.375f, "dropR", null, scheme.RightNoteColor);

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Authored b189 events [371]/[372]: 4-beat linear fade dropL clear->baseNote0Color,
        // dropR (0.2 gray)->baseNote1Color, finishing at b193.
        [UnityTest]
        public IEnumerator Beat189FadeReturnsBothTracksToBaseByBeat194()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;
            var fadeStartRight = new Color(0.2f, 0.2f, 0.2f, 0.2f);

            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            // At 191.5 both tracks have upcoming notes within the one-beat selection window.
            foreach (var beat in new[] { 188.75f, 189.125f, 190f, 191.5f, 192.5f, 193.125f, 194.5f, 195f })
            {
                yield return SeekTo(beat);
                Color left;
                Color right;
                if (beat < 189f || beat >= 193f)
                {
                    left = scheme.LeftNoteColor;
                    right = scheme.RightNoteColor;
                }
                else
                {
                    var p = (beat - 189f) / 4f;
                    left = Color.LerpUnclamped(Color.clear, scheme.LeftNoteColor, p);
                    right = Color.LerpUnclamped(fadeStartRight, scheme.RightNoteColor, p);
                }
                CheckNoteColor(failures, beat, "dropL", null, left);
                CheckNoteColor(failures, beat, "dropR", null, right);
            }

            // Reverse scrub must not leave a stale held flash/fade value behind.
            yield return SeekTo(175.25f);
            CheckNoteColor(failures, 175.25f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 175.25f, "dropR", null, scheme.RightNoteColor);
            yield return SeekTo(176.09375f);
            CheckNoteColor(failures, 176.09375f, "dropL", null, Color.clear);
            CheckNoteColor(failures, 176.09375f, "dropR", null, Color.clear);
            yield return SeekTo(193.125f);
            CheckNoteColor(failures, 193.125f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 193.125f, "dropR", null, scheme.RightNoteColor);

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator PlayingNoteColorsRemainCorrectAcrossPause()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            var currentSecondsProperty = typeof(AudioTimeSyncController)
                .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
            Assert.That(currentSecondsProperty, Is.Not.Null,
                "AudioTimeSyncController.CurrentSeconds property missing.");

            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            var chainGrid = Object.FindAnyObjectByType<ChainGridContainer>();
            var spawnCheckedNotes = 0;
            var spawnCheckedChains = 0;
            void OnNoteSpawned(Beatmap.Base.BaseObject spawned)
            {
                var now = atsc.CurrentJsonTime;
                if (now < 176f || now >= 177f) return;
                if (!noteGrid.LoadedContainers.TryGetValue(spawned, out var con)
                    || con is not Beatmap.Containers.NoteContainer fresh || fresh.NoteData == null) return;
                if (fresh.NoteData.CustomTrack is not JSONString trackName
                    || (trackName.Value != "dropL" && trackName.Value != "dropR")) return;
                var (left, right) = ExpectedDropColors(now, scheme);
                var expected = trackName.Value == "dropL" ? left : right;
                var actual = fresh.ModelController.MpbController.Mpb.GetColor("_Color");
                if (!ColorMatches(actual, expected))
                    failures.Add($"spawn t={now:F4}: {trackName.Value} note b{fresh.NoteData.JsonTime:F3} " +
                        $"model _Color={actual} expected {expected}.");
                if (TryGetRendererColor(fresh.ModelController, out var rendered)
                    && !ColorMatches(rendered, expected))
                    failures.Add($"spawn t={now:F4}: {trackName.Value} note b{fresh.NoteData.JsonTime:F3} " +
                        $"renderer _Color={rendered} expected {expected}.");
                spawnCheckedNotes++;
            }
            void OnChainSpawned(Beatmap.Base.BaseObject spawned)
            {
                var now = atsc.CurrentJsonTime;
                if (now < 176f || now >= 177f || chainGrid == null) return;
                if (!chainGrid.LoadedContainers.TryGetValue(spawned, out var con)
                    || con is not Beatmap.Containers.ChainContainer fresh || fresh.ChainData == null) return;
                if (!Mathf.Approximately(fresh.ChainData.JsonTime, 176f)
                    || fresh.ChainData.CustomTrack is not JSONString trackName
                    || trackName.Value != "dropR") return;
                var (_, expected) = ExpectedDropColors(now, scheme);
                spawnCheckedChains++;
                foreach (var node in fresh.Nodes.Where(n => n.gameObject.activeSelf))
                {
                    var actual = node.ModelController.MpbController.Mpb.GetColor("_Color");
                    if (!ColorMatches(actual, expected))
                        failures.Add($"spawn t={now:F4}: chain b176 node " +
                            $"model _Color={actual} expected {expected}.");
                    if (TryGetRendererColor(node.ModelController, out var rendered)
                        && !ColorMatches(rendered, expected))
                        failures.Add($"spawn t={now:F4}: chain b176 node " +
                            $"renderer _Color={rendered} expected {expected}.");
                }
            }
            InputTestFixture inputFixture = null;
            Mouse virtualMouse = null;
            try
            {
                inputFixture = new InputTestFixture();
                inputFixture.Setup();
                virtualMouse = InputSystem.AddDevice<Mouse>();

                atsc.MoveToJsonTime(160.75f);
                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                if (!atsc.IsPlaying)
                    failures.Add("setup: TogglePlaying did not enter IsPlaying.");

                foreach (var beat in new[] { 162.125f, 175.25f })
                {
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    CheckDropTrackColors(failures, atsc, beat, scheme);
                }

                atsc.TogglePlaying();
                atsc.StopScheduled = true;
                var pausedBeat = atsc.CurrentJsonTime;
                yield return null;
                yield return null;
                if (atsc.IsPlaying) failures.Add("pause: still IsPlaying after TogglePlaying.");
                var paused = CaptureNoteColors(noteGrid, $"paused@{pausedBeat:F3}");
                CheckNoteColor(failures, pausedBeat, "dropL", null, scheme.LeftNoteColor, 8f);
                CheckNoteColor(failures, pausedBeat, "dropR", null, scheme.RightNoteColor, 8f);

                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                PinClock(atsc, currentSecondsProperty, pausedBeat);
                var playing = CaptureNoteColors(noteGrid, $"playing@{pausedBeat:F3}");

                atsc.TogglePlaying();
                atsc.StopScheduled = true;
                yield return null;
                yield return null;
                var pausedAgain = CaptureNoteColors(noteGrid, $"pausedAgain@{pausedBeat:F3}");

                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                PinClock(atsc, currentSecondsProperty, pausedBeat);
                var resumed = CaptureNoteColors(noteGrid, $"resumed@{pausedBeat:F3}");

                CompareResumedAgainstPaused(
                    failures, "playing", playing, "paused", paused, pausedBeat);
                CompareLoadedNoteColors(failures, "pausedAgain", pausedAgain, "paused", paused);
                CompareResumedAgainstPaused(
                    failures, "resumed", resumed, "paused", paused, pausedBeat);

                var dropLGray = new Color(0.2f, 0.2f, 0.2f, 0.2f);
                var dropRGray = new Color(0.42f, 0.42f, 0.42f, 0.42f);
                var dropLStrobe = new List<(float time, float noteBeat, Color color)>();
                var dropRStrobe = new List<(float time, float noteBeat, Color color)>();
                var dropLRendererStrobe = new List<(float time, float noteBeat, Color color)>();
                var dropRRendererStrobe = new List<(float time, float noteBeat, Color color)>();
                var chainStrobe = new List<(float time, float noteBeat, Color color)>();
                var chainRendererStrobe = new List<(float time, float noteBeat, Color color)>();
                noteGrid.OnContainerSpawned += OnNoteSpawned;
                chainGrid.OnContainerSpawned += OnChainSpawned;
                atsc.TogglePlaying();
                atsc.StopScheduled = true;
                yield return null;
                atsc.MoveToJsonTime(172f);
                yield return null;
                atsc.MoveToJsonTime(176.1f);
                yield return null;
                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                yield return null;
                yield return null;
                for (var scan = 176.1f; scan <= 176.4f; scan += 0.01f)
                {
                    PinClock(atsc, currentSecondsProperty, scan);
                    yield return null;
                    yield return null;
                    var scanTime = atsc.CurrentJsonTime;
                    var dropLNote = NearestLoadedNote("dropL", scanTime, null, 8f);
                    var dropRNote = NearestLoadedNote("dropR", scanTime, null, 8f);
                    if (dropLNote != null)
                    {
                        dropLStrobe.Add((scanTime, dropLNote.NoteData.JsonTime,
                            dropLNote.ModelController.MpbController.Mpb.GetColor("_Color")));
                        if (TryGetRendererColor(dropLNote.ModelController, out var dropLRendered))
                            dropLRendererStrobe.Add((scanTime, dropLNote.NoteData.JsonTime, dropLRendered));
                    }
                    if (dropRNote != null)
                    {
                        dropRStrobe.Add((scanTime, dropRNote.NoteData.JsonTime,
                            dropRNote.ModelController.MpbController.Mpb.GetColor("_Color")));
                        if (TryGetRendererColor(dropRNote.ModelController, out var dropRRendered))
                            dropRRendererStrobe.Add((scanTime, dropRNote.NoteData.JsonTime, dropRRendered));
                    }
                    var strobeChain = chainGrid
                        .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                        .FirstOrDefault(c => Mathf.Approximately(c.ChainData.JsonTime, 176f)
                            && c.ChainData.CustomTrack is JSONString st && st.Value == "dropR");
                    if (strobeChain != null)
                    {
                        foreach (var node in strobeChain.Nodes.Where(n => n.gameObject.activeSelf))
                        {
                            chainStrobe.Add((scanTime, 176f,
                                node.ModelController.MpbController.Mpb.GetColor("_Color")));
                            if (TryGetRendererColor(node.ModelController, out var nodeRendered))
                                chainRendererStrobe.Add((scanTime, 176f, nodeRendered));
                        }
                    }
                }

                CheckStrobeObserved(failures, "dropL note", dropLStrobe, dropLGray);
                CheckStrobeObserved(failures, "dropR note", dropRStrobe, dropRGray);
                CheckStrobeObserved(failures, "dropL note renderer", dropLRendererStrobe, dropLGray);
                CheckStrobeObserved(failures, "dropR note renderer", dropRRendererStrobe, dropRGray);
                if (chainStrobe.Count > 0)
                    CheckStrobeObserved(failures, "dropR chain node", chainStrobe, dropRGray);
                if (chainRendererStrobe.Count > 0)
                    CheckStrobeObserved(failures, "dropR chain node renderer", chainRendererStrobe, dropRGray);
                if (spawnCheckedNotes == 0)
                    failures.Add("strobe window: no dropL/dropR note spawn callback fired.");
                if (spawnCheckedChains == 0)
                    failures.Add("strobe window: no dropR chain b176 spawn callback fired.");

                foreach (var beat in new[]
                {
                    177.375f, 188.75f,
                    189.125f, 191f, 193.125f, 194.5f,
                })
                {
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    CheckDropTrackColors(failures, atsc, beat, scheme);
                }
            }
            finally
            {
                if (noteGrid != null) noteGrid.OnContainerSpawned -= OnNoteSpawned;
                if (chainGrid != null) chainGrid.OnContainerSpawned -= OnChainSpawned;
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
                if (inputFixture != null)
                {
                    virtualMouse = null;
                    inputFixture.TearDown();
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static bool TryGetRendererColor(VisualModelController ctrl, out Color color)
        {
            color = default;
            if (ctrl == null || ctrl.Renderers.Count == 0) return false;
            var renderer = ctrl.Renderers.FirstOrDefault(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                ?? ctrl.Renderers.FirstOrDefault(r => r != null);
            if (renderer == null) return false;
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            if (!mpb.HasColor("_Color")) return false;
            color = mpb.GetColor("_Color");
            return true;
        }

        private static void PinClock(
            AudioTimeSyncController atsc, PropertyInfo currentSecondsProperty, float jsonBeat)
        {
            var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonBeat);
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
        }

        private static void CheckDropTrackColors(
            List<string> failures, AudioTimeSyncController atsc, float targetBeat, ColorSchemeSO scheme)
        {
            var actual = atsc.CurrentJsonTime;
            var (left, right) = ExpectedDropColors(actual, scheme);
            CheckNoteColor(failures, actual, "dropL", null, left, 8f);
            CheckNoteColor(failures, actual, "dropR", null, right, 8f);
            if (actual >= 175.9f && actual < 177.1f)
            {
                var chainLoaded = Object.FindAnyObjectByType<ChainGridContainer>()
                    .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                    .Any(c => Mathf.Approximately(c.ChainData.JsonTime, 176f));
                if (chainLoaded)
                    CheckChainColors(failures, actual, 176f, "dropR", right);
                else
                    Debug.Log($"[SaltyDiag] beat {actual}: chain 176 not loaded while playing; " +
                        "segment check skipped.");
            }
        }

        private static void CheckStrobeObserved(
            List<string> failures, string label,
            List<(float time, float noteBeat, Color color)> observed, Color gray)
        {
            var sawGray = false;
            var sawClear = false;
            foreach (var (time, noteBeat, color) in observed)
            {
                if (ColorMatches(color, gray))
                {
                    sawGray = true;
                }
                else if (ColorMatches(color, Color.clear))
                {
                    sawClear = true;
                }
                else
                {
                    failures.Add($"strobe window: {label} b{noteBeat:F3} at {time:F4} " +
                        $"showed unexpected _Color={color}.");
                }
            }
            if (observed.Count == 0)
                failures.Add($"strobe window: no {label} loaded to sample.");
            if (!sawGray)
                failures.Add($"strobe window: {label} never showed authored gray {gray}.");
            if (!sawClear)
                failures.Add($"strobe window: {label} never showed authored clear.");
        }

        private static (Color left, Color right) ExpectedDropColors(float beat, ColorSchemeSO scheme)
        {
            if (beat >= 176f && beat < 177f)
            {
                var phase = Mathf.Repeat(beat - 176f, 0.125f);
                return phase < 0.0625f
                    ? (new Color(0.2f, 0.2f, 0.2f, 0.2f), new Color(0.42f, 0.42f, 0.42f, 0.42f))
                    : (Color.clear, Color.clear);
            }
            if (beat >= 189f && beat < 193f)
            {
                var p = (beat - 189f) / 4f;
                return (Color.LerpUnclamped(Color.clear, scheme.LeftNoteColor, p),
                    Color.LerpUnclamped(new Color(0.2f, 0.2f, 0.2f, 0.2f), scheme.RightNoteColor, p));
            }
            return (scheme.LeftNoteColor, scheme.RightNoteColor);
        }

        private static Dictionary<Beatmap.Base.BaseObject, Color> CaptureNoteColors(
            NoteGridContainer grid, string phase)
        {
            var colors = new Dictionary<Beatmap.Base.BaseObject, Color>();
            foreach (var pair in grid.LoadedContainers)
            {
                if (pair.Value is not Beatmap.Containers.NoteContainer note || note.NoteData == null)
                    continue;
                colors[pair.Key] = note.ModelController.MpbController.Mpb.GetColor("_Color");
            }
            Debug.Log($"[SaltyDiag] {phase}: loadedNotes={colors.Count}");
            return colors;
        }

        private static void CompareLoadedNoteColors(
            List<string> failures,
            string aLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> a,
            string bLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> b)
        {
            foreach (var key in a.Keys.Union(b.Keys))
            {
                var inA = a.TryGetValue(key, out var ca);
                var inB = b.TryGetValue(key, out var cb);
                if (inA != inB)
                {
                    failures.Add($"{aLabel} vs {bLabel}: note b{key.JsonTime:F3} " +
                        $"present={inA}->{inB}.");
                }
                else if (inA && !ColorMatches(ca, cb))
                {
                    failures.Add($"{aLabel} vs {bLabel}: note b{key.JsonTime:F3} " +
                        $"_Color {ca}->{cb}.");
                }
            }
        }

        private static void CompareResumedAgainstPaused(
            List<string> failures,
            string resumedLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> resumed,
            string pausedLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> paused,
            float pausedBeat)
        {
            foreach (var key in resumed.Keys.Union(paused.Keys))
            {
                var inResumed = resumed.TryGetValue(key, out var cr);
                var inPaused = paused.TryGetValue(key, out var cp);
                if (inResumed && !inPaused)
                {
                    failures.Add($"{resumedLabel} vs {pausedLabel}: note b{key.JsonTime:F3} " +
                        "appeared only on resume.");
                }
                else if (inPaused && !inResumed && key.SongBpmTime <= pausedBeat + 0.001f)
                {
                    failures.Add($"{resumedLabel} vs {pausedLabel}: live note b{key.JsonTime:F3} " +
                        "dropped on resume.");
                }
                else if (inResumed && !ColorMatches(cr, cp))
                {
                    failures.Add($"{resumedLabel} vs {pausedLabel}: note b{key.JsonTime:F3} " +
                        $"_Color {cp}->{cr}.");
                }
            }
        }

        // The user's persisted workspace is EditingMode.BasicEvent (Info.dat mode 4); entering
        // Playing from it deactivates the gameplay object tracks for one frame before the mode
        // switch, the same path that detached the hehe sphere animators. The prior mode is
        // restored in [UnityTearDown], not only inline, so a failure mid-test can't leak it.
        private IEnumerator EnterPlayingFromBasicEventWorkspace()
        {
            editMode = Object.FindAnyObjectByType<EditModeContext>();
            Assert.That(editMode, Is.Not.Null, "No EditModeContext in the loaded scene.");
            previousEditingMode = editMode.EditingMode;
            editingModeChanged = true;
            editMode.EditingMode = EditingMode.BasicEvent;
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
        }

        private static Beatmap.Containers.NoteContainer NearestLoadedNote(
            string track, float beat, int? type, float maxDistance = 1f)
        {
            Beatmap.Containers.NoteContainer best = null;
            var bestDistance = float.MaxValue;
            foreach (var container in Object.FindAnyObjectByType<NoteGridContainer>()
                .LoadedContainers.Values.OfType<Beatmap.Containers.NoteContainer>())
            {
                var data = container.NoteData;
                if (data == null || data.CustomFake) continue;
                if (data.CustomTrack is not JSONString t || t.Value != track) continue;
                if (type.HasValue && data.Type != type.Value) continue;
                var distance = Mathf.Abs(data.JsonTime - beat);
                if (distance > maxDistance) continue;
                if (best == null || distance < bestDistance ||
                    (Mathf.Approximately(distance, bestDistance) && data.JsonTime < best.NoteData.JsonTime))
                {
                    best = container;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool ColorMatches(Color actual, Color expected) =>
            Mathf.Abs(actual.r - expected.r) <= 0.03f
            && Mathf.Abs(actual.g - expected.g) <= 0.03f
            && Mathf.Abs(actual.b - expected.b) <= 0.03f
            && Mathf.Abs(actual.a - expected.a) <= 0.03f;

        private static void CheckNoteColor(
            List<string> failures, float beat, string track, int? type, Color expected,
            float maxDistance = 1f)
        {
            var note = NearestLoadedNote(track, beat, type, maxDistance);
            if (note == null)
            {
                failures.Add($"beat {beat}: no loaded real note on track '{track}'" +
                    (type.HasValue ? $" type {type}" : "") + $" within {maxDistance} beat(s).");
                return;
            }

            var modelMpb = note.ModelController.MpbController.Mpb.GetColor("_Color");
            var containerMpb = note.MpbController.Mpb.GetColor("_Color");
            var renderers = note.ModelController.MpbController.Renderers;
            var rendererColor = (Color?)null;
            string rendererColorText = "<none>";
            if (renderers != null && renderers.Count > 0)
            {
                var block = new MaterialPropertyBlock();
                renderers[0].GetPropertyBlock(block);
                if (!block.isEmpty)
                {
                    rendererColor = block.GetColor("_Color");
                    rendererColorText = rendererColor.Value.ToString();
                }
                else
                {
                    rendererColorText = "<empty>";
                }
            }
            Debug.Log($"[SaltyDiag] beat {beat}: note JsonTime={note.NoteData.JsonTime} " +
                $"track={track} type={note.NoteData.Type} modelMpb={modelMpb} " +
                $"containerMpb={containerMpb} rendererMpb={rendererColorText} expected={expected}");

            if (!ColorMatches(modelMpb, expected))
                failures.Add($"beat {beat}: '{track}' note JsonTime={note.NoteData.JsonTime} " +
                    $"modelMpb _Color={modelMpb} expected={expected}.");
            if (rendererColor.HasValue && !ColorMatches(rendererColor.Value, expected))
                failures.Add($"beat {beat}: '{track}' note JsonTime={note.NoteData.JsonTime} " +
                    $"rendererMpb _Color={rendererColor.Value} expected={expected}.");
        }

        private static void CheckChainColors(
            List<string> failures, float beat, float chainJsonTime, string track, Color expected)
        {
            var chain = Object.FindAnyObjectByType<ChainGridContainer>()
                .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                .FirstOrDefault(c => Mathf.Approximately(c.ChainData.JsonTime, chainJsonTime)
                    && c.ChainData.CustomTrack is JSONString t && t.Value == track);
            if (chain == null)
            {
                var loaded = string.Join(",", Object.FindAnyObjectByType<ChainGridContainer>()
                    .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                    .Select(c => c.ChainData.JsonTime));
                failures.Add($"beat {beat}: chain JsonTime={chainJsonTime} track='{track}' " +
                    $"not loaded (loaded chain times: {loaded}).");
                return;
            }

            var active = chain.Nodes.Where(n => n.gameObject.activeSelf).ToList();
            if (active.Count < 7)
            {
                failures.Add($"beat {beat}: chain JsonTime={chainJsonTime} has {active.Count} " +
                    "active nodes, expected >=7 for sc=8.");
                return;
            }

            foreach (var node in active)
            {
                var color = node.ModelController.MpbController.Mpb.GetColor("_Color");
                if (!ColorMatches(color, expected))
                    failures.Add($"beat {beat}: chain node '{node.name}' _Color={color} " +
                        $"expected={expected}.");
            }
            Debug.Log($"[SaltyDiag] beat {beat}: chain {chainJsonTime} '{track}' " +
                $"activeNodes={active.Count} node0Color={active[0].ModelController.MpbController.Mpb.GetColor("_Color")} expected={expected}");
        }

        // Evidence-only discriminator for the user's live report that pre-event notes look black:
        // MPB _Color reads base-correct at 162.125/175.25 in batch. A/B-disabling each note's body
        // renderers shows what color those pixels actually carry on screen (shader state vs MPB).
        // No parity assertion - the diff counts/means are the evidence.
        [UnityTest]
        public IEnumerator PreFlashNoteRenderedColorDiagnostic()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            var report = new StringBuilder();

            foreach (var beat in new[] { 162.125f, 175.25f, 176.09375f, 177.375f, 194.5f })
            {
                yield return SeekTo(beat);
                report.AppendLine($"=== beat {beat} camera pos={camera.transform.position} " +
                    $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} ===");
                Texture2D baseline = null;
                try
                {
                    baseline = RenderPixelsAndSave(camera,
                        Path.Combine(shotDir, $"salty-color-render-b{beat}-normal.png"));
                    var normalPixels = baseline.GetPixels32();

                    foreach (var track in new[] { "dropL", "dropR" })
                    {
                        var note = NearestLoadedNote(track, beat, track == "dropL" ? 0 : 1);
                        if (note == null)
                        {
                            report.AppendLine($"  {track}: no loaded real note within 1 beat");
                            continue;
                        }

                        var block = new MaterialPropertyBlock();
                        var renderers = note.ModelController.MpbController.Renderers
                            .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                            .ToList();
                        report.AppendLine($"  {track} note JsonTime={note.NoteData.JsonTime} " +
                            $"modelMpb={note.ModelController.MpbController.Mpb.GetColor("_Color")} " +
                            $"enabledActiveRenderers={renderers.Count}");
                        foreach (var r in renderers)
                        {
                            block.Clear();
                            r.GetPropertyBlock(block);
                            report.AppendLine($"    renderer={r.name} mat=" +
                                $"{(r.sharedMaterial == null ? "null" : r.sharedMaterial.name)} " +
                                $"bounds={r.bounds} _Color={block.GetColor("_Color")} " +
                                $"_ColorMultiplier={block.GetFloat("_ColorMultiplier")}");
                        }

                        foreach (var r in renderers) r.enabled = false;
                        Texture2D off = null;
                        try
                        {
                            off = RenderPixelsAndSave(camera, Path.Combine(shotDir,
                                $"salty-color-render-b{beat}-{track}-off.png"));
                        }
                        finally
                        {
                            foreach (var r in renderers) r.enabled = true;
                        }

                        var offPixels = off.GetPixels32();
                        var count = 0;
                        var minX = int.MaxValue; var maxX = int.MinValue;
                        var minTopY = int.MaxValue; var maxTopY = int.MinValue;
                        long sumR = 0, sumG = 0, sumB = 0;
                        byte maxR = 0, maxG = 0, maxB = 0;
                        for (var y = 0; y < 576; y++)
                        for (var x = 0; x < 1024; x++)
                        {
                            var n = normalPixels[y * 1024 + x];
                            var o = offPixels[y * 1024 + x];
                            var delta = Mathf.Abs(n.r - o.r) + Mathf.Abs(n.g - o.g) + Mathf.Abs(n.b - o.b);
                            if (delta < 45) continue;
                            count++;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            var topY = 575 - y;
                            if (topY < minTopY) minTopY = topY;
                            if (topY > maxTopY) maxTopY = topY;
                            sumR += n.r; sumG += n.g; sumB += n.b;
                            if (n.r > maxR) maxR = n.r;
                            if (n.g > maxG) maxG = n.g;
                            if (n.b > maxB) maxB = n.b;
                        }
                        Object.DestroyImmediate(off);
                        if (count == 0)
                        {
                            report.AppendLine($"    {track} note contributed 0 changed pixels " +
                                "(note not visibly rendered).");
                        }
                        else
                        {
                            report.AppendLine($"    {track} note contributorPixels={count} " +
                                $"x[{minX}..{maxX}] topY[{minTopY}..{maxTopY}] " +
                                $"meanRGB=({sumR / count},{sumG / count},{sumB / count}) " +
                                $"maxRGB=({maxR},{maxG},{maxB})");
                        }
                    }
                }
                finally
                {
                    if (baseline != null) Object.DestroyImmediate(baseline);
                }
            }

            Debug.Log($"[SaltyDiag] rendered-note color diagnostic\n{report}");
        }

        // Early-black-note repro on the full map: the user reports notes reading black before
        // the b176 strobe while the preflash diagnostic showed MPB _Color already base-correct.
        // Sweeps the authored order (forward 160.75->194.5, reverse back to 161.25/162.125,
        // optional 188.75), dumps every in-frustum NoteGridContainer container (fake included)
        // with authored beat/track/type and MPB state, then A/B-disables the nearest visible
        // type0/type1 note's model body renderers so the pixels prove what color is actually
        // drawn. Colored-pixel assertions run at the base-color samples (pre-176, pre-189) on
        // BOTH the forward and reverse visits; mid-animation beats are inspect/A-B only.
        [UnityTest]
        public IEnumerator EarlyNoteRenderedColorAcrossReverseSeek()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = PathUtils.Combine(
                "C:/Users/tdrak/AppData/Local/Temp", "devin-salty-early-note-diag");
            Directory.CreateDirectory(shotDir);
            var report = new StringBuilder();
            var failures = new List<string>();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;
            report.AppendLine($"scheme left={scheme.LeftNoteColor} right={scheme.RightNoteColor}");
            if (!UIMode.AnimationMode)
                report.AppendLine("WARNING: UIMode.AnimationMode=false in Playing; " +
                    "track color animation may not evaluate.");

            var assertBeats = new HashSet<float>
                { 160.75f, 161.25f, 162.125f, 165.25f, 175.25f, 188.75f };
            var visits = new Dictionary<float, int>();
            var measurements = new List<NotePixelMeasurement>();

            foreach (var beat in new[]
            {
                160.75f, 162.125f, 165.25f, 175.25f,
                176.09375f, 180f, 194.5f,
                162.125f, 165.25f, 175.25f, 161.25f, 162.125f,
                188.75f,
            })
            {
                yield return SeekTo(beat);
                visits.TryGetValue(beat, out var visit);
                visits[beat] = visit + 1;
                var label = $"b{beat}-v{visit}";
                var candidates = DumpNoteCandidates(report, camera, label);

                var baseline = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, $"salty-earlynote-{label}-on.png"));
                try
                {
                    MeasureNoteBodyContribution(report, failures, measurements, camera,
                        baseline, candidates,
                        PathUtils.Combine(shotDir, $"salty-earlynote-{label}-red-off.png"),
                        beat, visit, label, 0, "red",
                        cand => cand.Container.NoteData.CustomColor ?? scheme.LeftNoteColor,
                        assertBeats.Contains(beat));
                    MeasureNoteBodyContribution(report, failures, measurements, camera,
                        baseline, candidates,
                        PathUtils.Combine(shotDir, $"salty-earlynote-{label}-blue-off.png"),
                        beat, visit, label, 1, "blue",
                        cand => cand.Container.NoteData.CustomColor ?? scheme.RightNoteColor,
                        assertBeats.Contains(beat));
                }
                finally
                {
                    Object.DestroyImmediate(baseline);
                }
            }

            // Reverse-scrub evidence: did the same beat's measured pixel color move between
            // the forward visit and the post-reverse visit while its MPB _Color did not?
            foreach (var group in measurements.GroupBy(m => (m.Beat, m.ColorName))
                .Where(g => g.Count() > 1))
            {
                var visitsText = string.Join(" | ", group.Select(m =>
                    $"v{m.Visit}: changedPx={m.ChangedPixels} baseMean=({m.MeanR:F0},{m.MeanG:F0},{m.MeanB:F0}) " +
                    $"modelMpb={m.ModelColor}"));
                report.AppendLine($"=== revisit b{group.Key.Beat} {group.Key.ColorName}: {visitsText}");
            }

            var reportPath = PathUtils.Combine(shotDir, "salty-early-note-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] early-note color report written to {reportPath}\n{report}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private sealed class NoteCandidate
        {
            public Beatmap.Containers.NoteContainer Container;
            public List<Renderer> BodyRenderers;
            public List<Renderer> AuxRenderers;
            public Bounds Bounds;
        }

        private sealed class NotePixelMeasurement
        {
            public float Beat;
            public int Visit;
            public string ColorName;
            public int ChangedPixels;
            public float MeanR;
            public float MeanG;
            public float MeanB;
            public Color ModelColor;
        }

        // In-frustum loaded note containers that own at least one enabled/active model body
        // renderer (ModelController.MpbController.Renderers). Falls back to every active child
        // renderer only when the container has no model MPB renderer list (e.g. fake notes).
        private static List<NoteCandidate> CollectFrustumNoteCandidates(Camera camera, Plane[] planes)
        {
            var candidates = new List<NoteCandidate>();
            var notes = Object.FindAnyObjectByType<NoteGridContainer>();
            if (notes == null) return candidates;
            foreach (var container in notes.LoadedContainers.Values
                .OfType<Beatmap.Containers.NoteContainer>())
            {
                if (container == null || container.NoteData == null) continue;
                var allActive = container.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
                var model = container.ModelController;
                var mpbController = model == null ? null : model.MpbController;
                var body = mpbController == null
                    ? allActive
                    : mpbController.Renderers
                        .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                        .ToList();
                if (body.Count == 0) continue;
                var bounds = body[0].bounds;
                foreach (var r in body.Skip(1)) bounds.Encapsulate(r.bounds);
                if (!GeometryUtility.TestPlanesAABB(planes, bounds)) continue;
                candidates.Add(new NoteCandidate
                {
                    Container = container,
                    BodyRenderers = body,
                    AuxRenderers = allActive.Where(r => !body.Contains(r)).ToList(),
                    Bounds = bounds,
                });
            }
            return candidates;
        }

        private static List<NoteCandidate> DumpNoteCandidates(
            StringBuilder report, Camera camera, string label)
        {
            var notes = Object.FindAnyObjectByType<NoteGridContainer>();
            var loaded = notes == null
                ? 0
                : notes.LoadedContainers.Values
                    .OfType<Beatmap.Containers.NoteContainer>()
                    .Count(c => c.NoteData != null);
            var candidates = CollectFrustumNoteCandidates(
                camera, GeometryUtility.CalculateFrustumPlanes(camera));
            report.AppendLine($"=== {label} ===");
            report.AppendLine($"  loaded={loaded} inFrustumWithModel={candidates.Count} " +
                $"(fake={candidates.Count(c => c.Container.NoteData.CustomFake)} " +
                $"type0={candidates.Count(c => c.Container.NoteData.Type == 0)} " +
                $"type1={candidates.Count(c => c.Container.NoteData.Type == 1)} " +
                $"other={candidates.Count(c => c.Container.NoteData.Type != 0 && c.Container.NoteData.Type != 1)})");

            // DIAG TEMP: report per-track animator Children multiplicity to find duplicate pushes.
            // CustomTrack.ToString() emits JSON quotes, so read the node value to get the raw name.
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var seenTracks = new Dictionary<string, Beatmap.Animations.TrackAnimator>();
            foreach (var name in candidates
                .Select(c => (string)c.Container.NoteData.CustomTrack)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct())
            {
                var ta = tracksManager == null ? null : tracksManager.GetAnimationTrack(name);
                if (ta != null) seenTracks[name] = ta;
            }
            foreach (var kv in seenTracks)
            {
                var dups = kv.Value.Children
                    .GroupBy(a => a)
                    .Where(g => g.Count() > 1)
                    .Select(g => $"{g.Key.GetHashCode():X8}x{g.Count()}")
                    .ToList();
                report.AppendLine($"  trackAnim[{kv.Key}]: children={kv.Value.Children.Count} " +
                    $"cached={kv.Value.CachedChildren.Length} enabled={kv.Value.enabled} " +
                    $"dupChildren={(dups.Count == 0 ? "<none>" : string.Join(",", dups))} " +
                    $"pushSubs={CountEarlyPushSubscriptions(atsc, kv.Value)}");
            }

            var mpb = new MaterialPropertyBlock();
            foreach (var cand in candidates.OrderBy(c => c.Container.NoteData.JsonTime))
            {
                var data = cand.Container.NoteData;
                var modelMpb = cand.Container.ModelController == null
                    || cand.Container.ModelController.MpbController == null
                    ? null
                    : cand.Container.ModelController.MpbController.Mpb;
                var animator = cand.Container.Animator;
                var trackName = (string)data.CustomTrack;
                var animInfo = "";
                if (animator != null && trackName != null
                    && seenTracks.TryGetValue(trackName, out var trackAnim))
                {
                    var occ = trackAnim.Children.Count(a => ReferenceEquals(a, animator));
                    animInfo = $" animOcc={occ} animEnabled={animator.enabled} " +
                        $"colorsPending={animator.Colors.Count} colorsKeep={animator.Colors.Keep} " +
                        $"animTarget={animator.TargetType} " +
                        $"animProps=[{string.Join(",", animator.AnimatedProperties.Keys)}] " +
                        $"animTracks=[{AnimatorTrackNames(animator)}]";
                }
                report.AppendLine($"  note b={data.JsonTime} " +
                    $"track={(data.CustomTrack == null ? "<none>" : data.CustomTrack.ToString())} " +
                    $"type={data.Type} fake={data.CustomFake} " +
                    $"customColor={(data.CustomColor.HasValue ? data.CustomColor.Value.ToString() : "<none>")} " +
                    $"modelMpb _Color={(modelMpb == null ? "<none>" : modelMpb.GetColor("_Color").ToString())} " +
                    $"_Cutout={(modelMpb == null ? "-" : modelMpb.GetFloat("_Cutout").ToString("F3"))} " +
                    $"proj={ProjectBoundsToPixels(camera, cand.Bounds)}{animInfo}");
                foreach (var r in cand.BodyRenderers)
                {
                    mpb.Clear();
                    r.GetPropertyBlock(mpb);
                    report.AppendLine($"    body={r.name} " +
                        $"mat={(r.sharedMaterial == null ? "null" : r.sharedMaterial.name)} " +
                        $"_Color={(mpb.isEmpty ? "<empty>" : mpb.GetColor("_Color").ToString())} " +
                        $"_Cutout={(mpb.isEmpty ? "-" : mpb.GetFloat("_Cutout").ToString("F3"))} " +
                        $"bounds={r.bounds}");
                }
                foreach (var r in cand.AuxRenderers)
                {
                    report.AppendLine($"    aux={r.name} " +
                        $"mat={(r.sharedMaterial == null ? "null" : r.sharedMaterial.name)} " +
                        $"bounds={r.bounds}");
                }
            }
            return candidates;
        }

        // DIAG TEMP: count how many times a TrackAnimator's PushOnStoppedTimeChanged is subscribed
        // to OnTimeChangedEarly — a double subscription would double-push cached children per seek.
        private static int CountEarlyPushSubscriptions(
            AudioTimeSyncController atsc, Beatmap.Animations.TrackAnimator animator)
        {
            if (atsc == null) return -1;
            var field = typeof(AudioTimeSyncController).GetField(
                "OnTimeChangedEarly",
                System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(atsc) is not System.Action early) return -1;
            return early.GetInvocationList()
                .Count(d => ReferenceEquals(d.Target, animator));
        }

        // DIAG TEMP: reflect the animator's private tracks list to expose multi-track attachments.
        private static string AnimatorTrackNames(Beatmap.Animations.ObjectAnimator animator)
        {
            var field = typeof(Beatmap.Animations.ObjectAnimator)
                .GetField("tracks", System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(animator) is not System.Collections.IEnumerable tracks)
                return "<unreadable>";
            return string.Join(";",
                tracks.Cast<Beatmap.Animations.TrackAnimator>()
                    .Select(t => t == null ? "<null>" : t.name));
        }

        private static string ProjectBoundsToPixels(Camera camera, Bounds bounds)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var allBehind = true;
            for (var cx = 0; cx < 2; cx++)
            for (var cy = 0; cy < 2; cy++)
            for (var cz = 0; cz < 2; cz++)
            {
                var corner = new Vector3(
                    cx == 0 ? bounds.min.x : bounds.max.x,
                    cy == 0 ? bounds.min.y : bounds.max.y,
                    cz == 0 ? bounds.min.z : bounds.max.z);
                var v = camera.WorldToViewportPoint(corner);
                if (v.z > camera.nearClipPlane) allBehind = false;
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            return allBehind
                ? "<behind nearClip>"
                : $"x[{min.x * 1024f:F0}..{max.x * 1024f:F0}] y[{min.y * 576f:F0}..{max.y * 576f:F0}]";
        }

        // Clamped on-screen pixel area of a world bounds; 0 when fully behind the near clip.
        private static float ProjectedViewportArea(Camera camera, Bounds bounds)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var allBehind = true;
            for (var cx = 0; cx < 2; cx++)
            for (var cy = 0; cy < 2; cy++)
            for (var cz = 0; cz < 2; cz++)
            {
                var corner = new Vector3(
                    cx == 0 ? bounds.min.x : bounds.max.x,
                    cy == 0 ? bounds.min.y : bounds.max.y,
                    cz == 0 ? bounds.min.z : bounds.max.z);
                var v = camera.WorldToViewportPoint(corner);
                if (v.z > camera.nearClipPlane) allBehind = false;
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            if (allBehind) return 0f;
            var width = Mathf.Clamp(max.x * 1024f, 0f, 1024f) - Mathf.Clamp(min.x * 1024f, 0f, 1024f);
            var height = Mathf.Clamp(max.y * 576f, 0f, 576f) - Mathf.Clamp(min.y * 576f, 0f, 576f);
            return Mathf.Max(0f, width) * Mathf.Max(0f, height);
        }

        private static int DominantChannel(Color color) =>
            color.r >= color.g
                ? (color.r >= color.b ? 0 : 2)
                : (color.g >= color.b ? 1 : 2);

        // Disables the representative type-matched note's model body renderers and diffs the
        // frame so the changed-pixel baseline color proves what the note actually draws,
        // excluding environment untouched by the A/B. "Representative" = the nearest UPCOMING
        // note of that type with real on-screen area: a note already at/past the strike line
        // (e.g. dropR b162 at 162.125) projects almost entirely off-screen and its body would
        // measure 0 px for purely geometric reasons, not color ones.
        private void MeasureNoteBodyContribution(
            StringBuilder report, List<string> failures,
            List<NotePixelMeasurement> measurements, Camera camera, Texture2D baseline,
            List<NoteCandidate> candidates, string offPath, float beat, int visit, string label,
            int type, string colorName, System.Func<NoteCandidate, Color> expectedColor, bool assert)
        {
            var typed = candidates
                .Where(c => c.Container.NoteData.Type == type)
                .Select(c => new
                {
                    Candidate = c,
                    Delta = c.Container.NoteData.JsonTime - beat,
                    Area = ProjectedViewportArea(camera, c.Bounds),
                })
                .ToList();
            var onScreen = typed.Where(t => t.Area >= 64f).ToList();
            var cand = (onScreen.Where(t => t.Delta >= -0.001f).OrderBy(t => t.Delta).FirstOrDefault()
                ?? onScreen.OrderBy(t => Mathf.Abs(t.Delta)).FirstOrDefault()
                ?? typed.OrderBy(t => Mathf.Abs(t.Delta)).FirstOrDefault())?.Candidate;
            if (cand == null)
            {
                var missing = $"{label}: no in-frustum {colorName} (type {type}) note with " +
                    "active model renderers to isolate.";
                report.AppendLine($"  {missing}");
                if (assert) failures.Add(missing);
                return;
            }

            var data = cand.Container.NoteData;
            var expected = expectedColor(cand);
            var expectedChannel = DominantChannel(expected);
            var modelMpb = cand.Container.ModelController == null
                || cand.Container.ModelController.MpbController == null
                ? null
                : cand.Container.ModelController.MpbController.Mpb;
            var modelColor = modelMpb == null ? Color.clear : modelMpb.GetColor("_Color");

            foreach (var r in cand.BodyRenderers) r.enabled = false;
            Texture2D off = null;
            try
            {
                off = RenderPixelsAndSave(camera, offPath);
            }
            finally
            {
                foreach (var r in cand.BodyRenderers) r.enabled = true;
            }

            var normalPixels = baseline.GetPixels32();
            var offPixels = off.GetPixels32();
            var count = 0;
            var minX = int.MaxValue; var maxX = int.MinValue;
            var minTopY = int.MaxValue; var maxTopY = int.MinValue;
            long sumR = 0, sumG = 0, sumB = 0, sumDR = 0, sumDG = 0, sumDB = 0;
            for (var y = 0; y < 576; y++)
            for (var x = 0; x < 1024; x++)
            {
                var n = normalPixels[y * 1024 + x];
                var o = offPixels[y * 1024 + x];
                var delta = Mathf.Abs(n.r - o.r) + Mathf.Abs(n.g - o.g) + Mathf.Abs(n.b - o.b);
                if (delta < 45) continue;
                count++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                var topY = 575 - y;
                if (topY < minTopY) minTopY = topY;
                if (topY > maxTopY) maxTopY = topY;
                sumR += n.r; sumG += n.g; sumB += n.b;
                sumDR += n.r - o.r; sumDG += n.g - o.g; sumDB += n.b - o.b;
            }
            Object.DestroyImmediate(off);

            var meanR = count == 0 ? 0f : sumR / (float)count;
            var meanG = count == 0 ? 0f : sumG / (float)count;
            var meanB = count == 0 ? 0f : sumB / (float)count;
            var measuredChannel = meanR >= meanG
                ? (meanR >= meanB ? 0 : 2)
                : (meanG >= meanB ? 1 : 2);
            measurements.Add(new NotePixelMeasurement
            {
                Beat = beat, Visit = visit, ColorName = colorName,
                ChangedPixels = count, MeanR = meanR, MeanG = meanG, MeanB = meanB,
                ModelColor = modelColor,
            });
            report.AppendLine($"  A/B {colorName} note b={data.JsonTime} " +
                $"track={(data.CustomTrack == null ? "<none>" : data.CustomTrack.ToString())} " +
                $"fake={data.CustomFake} bodyRenderers={cand.BodyRenderers.Count} " +
                $"changedPx={count} " +
                $"bbox={(count == 0 ? "<none>" : $"x[{minX}..{maxX}] topY[{minTopY}..{maxTopY}]")} " +
                $"baseMean=({meanR:F0},{meanG:F0},{meanB:F0}) " +
                $"deltaMean=({(count == 0 ? 0f : sumDR / (float)count):F0}," +
                $"{(count == 0 ? 0f : sumDG / (float)count):F0}," +
                $"{(count == 0 ? 0f : sumDB / (float)count):F0}) " +
                $"modelMpb={modelColor} expected={expected} " +
                $"expectedDominant={expectedChannel} measuredDominant={measuredChannel}");

            if (!assert) return;
            var dominantMean = measuredChannel == 0 ? meanR : measuredChannel == 1 ? meanG : meanB;
            if (count < 10)
            {
                failures.Add($"{label}: {colorName} note b={data.JsonTime} changed only " +
                    $"{count} px when its body renderers were disabled; the note body is not " +
                    "visibly rendered.");
            }
            else if (dominantMean < 16f)
            {
                // A black body over bright environment still wins its own channel by a few
                // levels (e.g. (5,1,1) is nominally "red"), so dominance alone masks the
                // reported early-black appearance; the dominant channel must be visibly lit.
                failures.Add($"{label}: {colorName} note b={data.JsonTime} pixel contribution " +
                    $"baseMean=({meanR:F0},{meanG:F0},{meanB:F0}) renders near-black " +
                    $"(dominant channel mean {dominantMean:F0}/255), expected {expected} " +
                    "per the note type; early-black repro.");
            }
            else if (measuredChannel != expectedChannel)
            {
                failures.Add($"{label}: {colorName} note b={data.JsonTime} pixel contribution " +
                    $"baseMean=({meanR:F0},{meanG:F0},{meanB:F0}) is channel-{measuredChannel} " +
                    $"dominant, expected {colorName} (channel {expectedChannel}, {expected}) " +
                    "per the note type; early-black repro.");
            }
        }

        // Placement diagnostic for the user's live report: hovering ohnoL/ohnoR note pairs at
        // b217-220.5 sit ~1m apart under the rings in the game while CM shows ~2m + drift, and
        // the authored width-4 fake walls at b221 look too narrow/distant. Logs runtime state
        // only; no parity assertions.
        [UnityTest]
        public IEnumerator Beat219To225NoteAndWallPlacementDiagnostics()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            if (!UIMode.AnimationMode)
                Debug.LogWarning("[SaltyDiag] pos-diag UIMode.AnimationMode=false");
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            var report = new StringBuilder();
            var noteBeats = new HashSet<float> { 217f, 217.5f, 218f, 218.5f, 219f, 219.5f, 220f, 220.5f };
            var pairs = new[]
            {
                ("ohnoR", new[] { 217f, 217.5f }), ("ohnoL", new[] { 218f, 218.5f }),
                ("ohnoR", new[] { 219f, 219.5f }), ("ohnoL", new[] { 220f, 220.5f }),
            };
            var wallBeats = new HashSet<float> { 221.1f, 221.5f, 222.25f, 223.25f, 224.5f };
            var captureBeats = new HashSet<float> { 219.25f, 221.1f, 222.25f, 223.25f };
            var mpb = new MaterialPropertyBlock();
            var captured21925 = false;

            foreach (var beat in new[]
            {
                217f, 218.25f, 218.75f, 219f, 219.125f, 219.25f, 219.5f, 220f, 220.25f,
                220.5f, 221.1f, 221.5f, 222.25f, 223.25f, 224.5f, 225f, 219.25f,
            })
            {
                yield return SeekTo(beat);
                var isReverse21925 = Mathf.Approximately(beat, 219.25f) && captured21925;
                var label = isReverse21925 ? "219.25-reverse" : beat.ToString();
                report.AppendLine($"=== beat {label} camera pos={camera.transform.position} " +
                    $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} " +
                    $"aspect={(camera.targetTexture == null ? Screen.width / (float)Screen.height : camera.targetTexture.width / (float)camera.targetTexture.height)} ===");

                if (captureBeats.Contains(beat))
                {
                    var shot = RenderPixelsAndSave(camera, Path.Combine(shotDir,
                        $"salty-pos-beat{label}.png"));
                    Object.DestroyImmediate(shot);
                    if (Mathf.Approximately(beat, 219.25f)) captured21925 = true;
                }

                // --- hovering note pairs ---
                var loaded = Object.FindAnyObjectByType<NoteGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.NoteContainer>()
                    .Where(c => c.NoteData != null && !c.NoteData.CustomFake
                        && noteBeats.Contains(c.NoteData.JsonTime)
                        && c.NoteData.CustomTrack is JSONString tr
                        && (tr.Value == "ohnoL" || tr.Value == "ohnoR"))
                    .OrderBy(c => c.NoteData.JsonTime)
                    .ToList();
                foreach (var note in loaded)
                {
                    var nd = note.NoteData;
                    var t = note.transform;
                    var animator = note.Animator;
                    var bounds = new Bounds();
                    var anyRenderer = false;
                    var rendererInfo = new StringBuilder();
                    foreach (var r in note.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                        mpb.Clear();
                        r.GetPropertyBlock(mpb);
                        if (!anyRenderer) { bounds = r.bounds; anyRenderer = true; }
                        else bounds.Encapsulate(r.bounds);
                        rendererInfo.Append($"\n      {r.name} mat={(r.sharedMaterial == null ? "null" : r.sharedMaterial.name)}" +
                            $" bounds={r.bounds} _Color={mpb.GetColor("_Color")} _Cutout={mpb.GetFloat("_Cutout")}");
                    }
                    var vp = anyRenderer ? camera.WorldToViewportPoint(bounds.center) : Vector3.zero;
                    report.AppendLine($"  note JsonTime={nd.JsonTime} c={nd.Type} x={nd.PosX} y={nd.PosY}" +
                        $" hjd={nd.HalfJumpDuration} njs={(nd.CustomNoteJumpMovementSpeed == null ? "<default>" : nd.CustomNoteJumpMovementSpeed.ToString())}" +
                        $"\n    worldPos={t.position} localPos={t.localPosition} parent={(t.parent == null ? "<none>" : $"{t.parent.name} pos={t.parent.position} scale={t.parent.lossyScale}")}" +
                        $" worldRot={t.rotation.eulerAngles} worldScale={t.lossyScale}" +
                        $" animatorType={(animator == null ? "<none>" : animator.TargetType.ToString())} animatedTrack={(animator != null && animator.AnimatedTrack)}" +
                        $"\n    boundsCenter={bounds.center} viewport=({vp.x:F3},{vp.y:F3},depth={vp.z:F2}) active={note.gameObject.activeInHierarchy}{rendererInfo}");
                }
                foreach (var (track, members) in pairs)
                {
                    foreach (var member in members)
                    {
                        var hit = loaded.Any(c => c.NoteData.CustomTrack is JSONString mtr
                            && mtr.Value == track && Mathf.Approximately(c.NoteData.JsonTime, member));
                        if (!hit)
                            report.AppendLine($"  pair member not loaded: {track}@{member}");
                    }
                }
                var centroids = new List<(string name, Vector3 center)>();
                foreach (var (track, members) in pairs)
                {
                    var pairContainers = loaded.Where(c => c.NoteData.CustomTrack is JSONString mtr
                        && mtr.Value == track && members.Any(m => Mathf.Approximately(c.NoteData.JsonTime, m))).ToList();
                    if (pairContainers.Count < 2) continue;
                    var ys = pairContainers.Select(c => Mathf.Abs(c.transform.position.y)).ToList();
                    var zs = pairContainers.Select(c => c.transform.position.z).ToList();
                    var centroid = pairContainers.Aggregate(Vector3.zero, (a, c) => a + c.transform.position) / pairContainers.Count;
                    centroids.Add(($"pair {track}@{members[0]}-{members[1]}", centroid));
                    report.AppendLine($"  {centroids[^1].name}: member |dY|={Mathf.Abs(pairContainers[0].transform.position.y - pairContainers[1].transform.position.y):F3}" +
                        $" |dZ|={Mathf.Abs(pairContainers[0].transform.position.z - pairContainers[1].transform.position.z):F3} centroid={centroid}");
                }
                for (var i = 1; i < centroids.Count; i++)
                {
                    report.AppendLine($"  centroidDelta {centroids[i - 1].name} -> {centroids[i].name}:" +
                        $" dY={Mathf.Abs(centroids[i].center.y - centroids[i - 1].center.y):F3}" +
                        $" dZ={Mathf.Abs(centroids[i].center.z - centroids[i - 1].center.z):F3}");
                }

                // --- b221 fake walls ---
                if (wallBeats.Contains(beat))
                {
                    var walls = Object.FindAnyObjectByType<ObstacleGridContainer>()
                        .LoadedContainers.Values
                        .OfType<Beatmap.Containers.ObstacleContainer>()
                        .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f))
                        .ToList();
                    report.AppendLine($"  b221 walls loaded={walls.Count}");
                    foreach (var wall in walls)
                    {
                        var od = wall.ObstacleData;
                        var trackNode = od.CustomTrack;
                        var trackText = trackNode == null ? "<none>" : trackNode.ToString();
                        var isBeatN = trackNode is JSONString s && s.Value.StartsWith("beat");
                        var isPair = trackNode is JSONArray arr &&
                            arr.Children.Any(v => (string)v == "1/4" || (string)v == "pee2");
                        if (!isBeatN && !isPair) continue;
                        var parent = wall.transform.parent;
                        var zb = wall.CoreRenderer != null ? wall.CoreRenderer.bounds : new Bounds();
                        var zc = zb.center.z - camera.transform.position.z;
                        report.AppendLine($"  wall track={trackText} coordinate={od.CustomCoordinate} size={od.CustomSize}" +
                            $" obstacleScale={wall.ObstacleScale}" +
                            $" coreBounds=Center{zb.center} Size{zb.size} zDist={zc:F2}" +
                            $" parent={(parent == null ? "<none>" : $"{parent.name} pos={parent.position} localScale={parent.localScale} lossyScale={parent.lossyScale}")}");
                        foreach (var renderer in wall.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            mpb.Clear();
                            renderer.GetPropertyBlock(mpb);
                            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                            var allBehind = true;
                            var b = renderer.bounds;
                            for (var cx = 0; cx < 2; cx++)
                            for (var cy = 0; cy < 2; cy++)
                            for (var cz = 0; cz < 2; cz++)
                            {
                                var corner = new Vector3(
                                    cx == 0 ? b.min.x : b.max.x,
                                    cy == 0 ? b.min.y : b.max.y,
                                    cz == 0 ? b.min.z : b.max.z);
                                var v = camera.WorldToViewportPoint(corner);
                                if (v.z > camera.nearClipPlane) allBehind = false;
                                min = Vector3.Min(min, v);
                                max = Vector3.Max(max, v);
                            }
                            var proj = allBehind
                                ? "<behind nearClip>"
                                : $"x[{min.x * 1024f:F0}..{max.x * 1024f:F0}] y[{min.y * 576f:F0}..{max.y * 576f:F0}]";
                            report.AppendLine($"    renderer={renderer.name} enabled={renderer.enabled} " +
                                $"active={renderer.gameObject.activeInHierarchy} " +
                                $"_Color={mpb.GetColor("_Color")} _Cutout={mpb.GetFloat("_Cutout")} " +
                                $"viewportProjected={proj}");
                        }
                    }
                }

                // --- ring anchor at 219.25 ---
                if (Mathf.Approximately(beat, 219.25f))
                {
                    var rings = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                        .Where(r => r.name == "Ring" && r.transform.parent != null
                            && r.transform.parent.name.StartsWith("Panels4TrackLaneRing")
                            && r.enabled && r.gameObject.activeInHierarchy
                            && r.bounds.center.z - camera.transform.position.z < 40f)
                        .OrderBy(r => r.bounds.center.z)
                        .ToList();
                    report.AppendLine($"  ring anchors z<40: {rings.Count}");
                    foreach (var ring in rings)
                    {
                        var vc = camera.WorldToViewportPoint(ring.bounds.center);
                        var vmin = camera.WorldToViewportPoint(ring.bounds.min);
                        var vmax = camera.WorldToViewportPoint(ring.bounds.max);
                        report.AppendLine($"    {GetPath(ring.transform)} bounds={ring.bounds} " +
                            $"vpCenter=({vc.x:F3},{vc.y:F3},d={vc.z:F1}) " +
                            $"vpSpan=({(vmax.x - vmin.x):F3},{(vmax.y - vmin.y):F3})");
                    }
                }
            }

            var reportPath = Path.Combine(shotDir, "salty-beats219-225-runtime-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] beats219-225 placement report written to {reportPath}\n{report}");
        }

        // Diagnostic for the actual hovering pair the user means: fake notes b225-229 (c0/c1
        // pairs on tracks [slayN, shitballsN], NJS16 offset4, definitePosition z12 at .5,
        // dissolve 0->1 by .05). Source b0 AnimateTrack slay225-229 carry offsetPosition
        // Y4,2,0,-2,-4 (2-lane authored gap); Heck multiplies track offsets by 0.6 lane
        // distance while CM V3 GameplayObject uses raw offsets - this logs what CM computes.
        [UnityTest]
        public IEnumerator Beat219EarlyFakePairPlacementDiagnostics()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            if (!UIMode.AnimationMode)
                Debug.LogWarning("[SaltyDiag] fake-pair diag UIMode.AnimationMode=false");
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
            var report = new StringBuilder();
            var fakeBeats = new HashSet<float> { 225f, 226f, 227f, 228f, 229f };
            var captureBeats = new HashSet<float> { 219.5f, 220.25f, 222.25f };
            var mpb = new MaterialPropertyBlock();

            foreach (var beat in new[]
            {
                219.25f, 219.5f, 220f, 220.25f, 221.1f, 221.5f, 222.25f, 223.25f, 224.5f,
                225f, 219.5f,
            })
            {
                yield return SeekTo(beat);
                report.AppendLine($"=== beat {beat} camera pos={camera.transform.position} " +
                    $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} ===");
                if (captureBeats.Contains(beat))
                {
                    var shot = RenderPixelsAndSave(camera,
                        Path.Combine(shotDir, $"salty-fake-beat{beat}.png"));
                    Object.DestroyImmediate(shot);
                }

                var fakes = Object.FindAnyObjectByType<NoteGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.NoteContainer>()
                    .Where(c => c.NoteData != null && c.NoteData.CustomFake
                        && fakeBeats.Contains(c.NoteData.JsonTime)
                        && c.NoteData.CustomTrack is JSONArray)
                    .OrderBy(c => c.NoteData.JsonTime).ThenBy(c => c.NoteData.Type)
                    .ToList();
                report.AppendLine($"  loaded fake pair notes: {fakes.Count}");

                var centroids = new List<(string name, Vector3 center)>();
                foreach (var group in fakes.GroupBy(c => c.NoteData.JsonTime).OrderBy(g => g.Key))
                {
                    foreach (var note in group)
                    {
                        var nd = note.NoteData;
                        var t = note.transform;
                        var animator = note.Animator;
                        var trackParent = animator != null && animator.AnimationTrack != null
                            ? animator.AnimationTrack.ObjectParentTransform
                            : null;
                        var rendererInfo = new StringBuilder();
                        var anyBounds = false;
                        var bounds = new Bounds();
                        foreach (var r in note.GetComponentsInChildren<Renderer>(true))
                        {
                            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                            mpb.Clear();
                            r.GetPropertyBlock(mpb);
                            if (!anyBounds) { bounds = r.bounds; anyBounds = true; }
                            else bounds.Encapsulate(r.bounds);
                            rendererInfo.Append($"\n      {r.name} bounds={r.bounds} " +
                                $"_Color={mpb.GetColor("_Color")} _Cutout={mpb.GetFloat("_Cutout")}");
                        }
                        report.AppendLine($"  fake JsonTime={nd.JsonTime} c={nd.Type} x={nd.PosX} y={nd.PosY}" +
                            $" tracks={nd.CustomTrack}" +
                            $"\n    worldPos={t.position} localPos={t.localPosition}" +
                            $" parent={(t.parent == null ? "<none>" : $"{t.parent.name} pos={t.parent.position} scale={t.parent.lossyScale}")}" +
                            $" trackParent={(trackParent == null ? "<none>" : $"{trackParent.name} worldPos={trackParent.position} localPos={trackParent.localPosition}")}" +
                            $"\n    hjd={nd.HalfJumpDuration} njs={(nd.CustomNoteJumpMovementSpeed == null ? "<default>" : nd.CustomNoteJumpMovementSpeed.ToString())} spawnBpm={nd.SpawnSongBpmTime}" +
                            $" animatorType={(animator == null ? "<none>" : animator.TargetType.ToString())}" +
                            $" offsetPos={(animator == null ? "<none>" : $"n={animator.OffsetPosition.Count} v={animator.OffsetPosition.Get()}")}" +
                            $" worldPos={(animator == null ? "<none>" : $"n={animator.WorldPosition.Count} v={animator.WorldPosition.Get()}")}" +
                            $" boundsCenter={(anyBounds ? bounds.center.ToString() : "<none>")} active={note.gameObject.activeInHierarchy}{rendererInfo}");
                    }
                    var members = group.ToList();
                    if (members.Count >= 2)
                    {
                        var centroid = members.Aggregate(Vector3.zero, (a, c) => a + c.transform.position) / members.Count;
                        centroids.Add(($"fake@{group.Key}", centroid));
                        report.AppendLine($"  pair fake@{group.Key}: |dY|=" +
                            $"{Mathf.Abs(members[0].transform.position.y - members[1].transform.position.y):F3}" +
                            $" |dZ|={Mathf.Abs(members[0].transform.position.z - members[1].transform.position.z):F3}" +
                            $" |dX|={Mathf.Abs(members[0].transform.position.x - members[1].transform.position.x):F3} centroid={centroid}");
                    }
                }
                for (var i = 1; i < centroids.Count; i++)
                {
                    report.AppendLine($"  centroidDelta {centroids[i - 1].name} -> {centroids[i].name}:" +
                        $" dY={centroids[i].center.y - centroids[i - 1].center.y:F3}" +
                        $" dZ={centroids[i].center.z - centroids[i - 1].center.z:F3}" +
                        $" dX={centroids[i].center.x - centroids[i - 1].center.x:F3}");
                }

                // slayN track animator state
                var tracksManager = Object.FindAnyObjectByType<TracksManager>();
                foreach (var n in new[] { 225, 226, 227, 228, 229 })
                {
                    var track = tracksManager.GetAnimationTrack($"slay{n}");
                    if (track == null) continue;
                    report.AppendLine($"  track slay{n}: children={track.Children.Count} " +
                        $"proxyPos={(track.Track == null ? "<none>" : track.Track.ObjectParentTransform.position.ToString())}");
                }
            }

            var reportPath = Path.Combine(shotDir, "salty-beats219-225-fake-note-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] fake-note placement report written to {reportPath}\n{report}");
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // A failed assertion must still leave the editor camera and preview mode safe for reload.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
                var editingCamera = cameraManager.CameraControllers[0].Camera.transform;
                if (editingCameraPosition != Vector3.zero)
                {
                    editingCamera.position = editingCameraPosition;
                    editingCamera.rotation = editingCameraRotation;
                }
            }
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
            if (editingModeChanged && editMode != null)
            {
                editMode.EditingMode = previousEditingMode;
                editingModeChanged = false;
            }
            yield break;
        }

        // The large environment scene owns AudioSource references that require a fresh scene on teardown.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
