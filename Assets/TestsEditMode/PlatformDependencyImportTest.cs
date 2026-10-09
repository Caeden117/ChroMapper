using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using CompilationPipeline = UnityEditor.Compilation.CompilationPipeline;
using UnityEngine;
using UnityEngine.Rendering;

namespace TestsEditMode
{
    public class PlatformDependencyImportTest
    {
        // Unity resolves private Forms fields during assembly import and stripping, even though
        // file dialogs never use DoubleBuffer or LinkLabel directly. Their drawing types must resolve.
        [Test]
        public void WindowsFormsDrawingFieldsResolve()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("Windows Forms is imported only on Windows.");

            var forms = Assembly.Load("System.Windows.Forms");
            Assert.DoesNotThrow(() =>
            {
                var buffer = forms.GetType("System.Windows.Forms.Control", true)
                    .GetNestedType("DoubleBuffer", BindingFlags.NonPublic);
                var region = buffer.GetField("InvalidRegion", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.That(region.FieldType.FullName, Is.EqualTo("System.Drawing.Region"));
                var style = forms.GetType("System.Windows.Forms.ListViewItem", true)
                    .GetNestedType("ListViewSubItem", BindingFlags.Public | BindingFlags.NonPublic)
                    .GetNestedType("SubItemStyle", BindingFlags.NonPublic);
                Assert.That(style.GetField("font", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FieldType.FullName, Is.EqualTo("System.Drawing.Font"));
            });
        }

        // A Linux or macOS editor can produce a Windows player. Resolve Forms against each
        // available Mono implementation instead of assuming the Windows editor's DLL is special.
        [TestCase("unityjit-win32")]
        [TestCase("unityjit-linux")]
        [TestCase("unityjit-macos")]
        public void MonoDrawingImplementationResolvesForms(string runtime)
        {
            var directory = PathUtils.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/lib/mono", runtime);
            if (!File.Exists(PathUtils.Combine(directory, "System.Drawing.dll")))
                Assert.Ignore($"This Unity installation does not include {runtime}.");
            CheckWindowsPlayerDrawingFields(directory);
        }

        // Resolve compiled field signatures with the player's plugin/profile search paths.
        // An explicit Mono directory takes precedence for the editor-host compatibility cases.
        private static void CheckWindowsPlayerDrawingFields(string drawingDirectory)
        {
            var cecil = Assembly.Load("Mono.Cecil");
            var resolver = Activator.CreateInstance(cecil.GetType("Mono.Cecil.DefaultAssemblyResolver", true));
            using var resolverLifetime = (IDisposable)resolver;
            var resolverType = resolver.GetType();
            resolverType.GetMethod("RemoveSearchDirectory").Invoke(resolver, new object[] { "." });
            resolverType.GetMethod("RemoveSearchDirectory").Invoke(resolver, new object[] { "bin" });
            var addDirectory = resolverType.GetMethod("AddSearchDirectory");
            if (drawingDirectory != null)
                addDirectory.Invoke(resolver, new object[] { drawingDirectory });
            foreach (var importer in PluginImporter.GetAllImporters())
            {
                if (importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64))
                    addDirectory.Invoke(resolver, new object[] { Path.GetDirectoryName(importer.assetPath) });
            }
            var profile = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone);
            foreach (var directory in CompilationPipeline.GetSystemAssemblyDirectories(profile))
                addDirectory.Invoke(resolver, new object[] { directory });

            var settingsType = cecil.GetType("Mono.Cecil.ReaderParameters", true);
            var settings = Activator.CreateInstance(settingsType);
            settingsType.GetProperty("AssemblyResolver").SetValue(settings, resolver);
            var forms = cecil.GetType("Mono.Cecil.AssemblyDefinition", true)
                .GetMethod("ReadAssembly", new[] { typeof(string), settingsType })
                .Invoke(null, new[] { "Assets/StandaloneFileBrowser/Plugins/System.Windows.Forms.dll", settings });
            using var formsLifetime = (IDisposable)forms;
            var region = ResolvePlayerField(forms, "System.Windows.Forms.Control/DoubleBuffer", "InvalidRegion");
            Assert.That(region, Is.Not.Null, "The Windows player cannot resolve System.Drawing.Region.");
            var font = ResolvePlayerField(forms, "System.Windows.Forms.ListViewItem/ListViewSubItem/SubItemStyle", "font");
            Assert.That(font, Is.Not.Null, "The Windows player cannot resolve System.Drawing.Font.");
        }

        // Import diagnostics are deferred until after EditMode tests finish. Compile the real
        // kernel synchronously through Unity's compiler so glcore failures cannot pass unnoticed.
        [TestCase(GraphicsDeviceType.OpenGLCore)]
        [TestCase(GraphicsDeviceType.Direct3D11)]
        public void TextureChannelPackerCompiles(GraphicsDeviceType backend)
        {
            const string path = "Assets/__Scripts/Environments/Components/TextureProcessor3DWriteTextures.compute";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
            Assert.That(shader != null, Is.True);
            var compile = typeof(ShaderUtil).GetMethod("CompileComputeShaderVariant", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(compile, Is.Not.Null, "Unity's synchronous compute compiler must be available.");
            var result = (ShaderData.VariantCompileInfo)compile.Invoke(null, new object[]
            {
                shader, shader.FindKernel("WriteTextures"), Array.Empty<string>(), backend, BuildTarget.StandaloneWindows64
            });
            Assert.That(result.Success, Is.True,
                string.Join("\n", result.Messages.Select(message => $"{message.platform}: {message.message}")));
            Assert.That(result.ShaderData, Is.Not.Empty);
        }

        // Unity excludes its own Cecil assembly from automatic script references. Reflection
        // keeps the check in the existing test assembly while resolving actual player field types.
        private static object ResolvePlayerField(object assembly, string typeName, string fieldName)
        {
            var module = assembly.GetType().GetProperty("MainModule").GetValue(assembly);
            var type = module.GetType().GetMethod("GetType", new[] { typeof(string) }).Invoke(module, new object[] { typeName });
            var fields = (IEnumerable)type.GetType().GetProperty("Fields").GetValue(type);
            var field = fields.Cast<object>().Single(candidate =>
                (string)candidate.GetType().GetProperty("Name").GetValue(candidate) == fieldName);
            var fieldType = field.GetType().GetProperty("FieldType").GetValue(field);
            return fieldType.GetType().GetMethod("Resolve").Invoke(fieldType, null);
        }
    }
}
