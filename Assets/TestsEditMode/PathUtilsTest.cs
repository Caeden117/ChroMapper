using System.IO;
using NUnit.Framework;

namespace TestsEditMode
{
    public class PathUtilsTest
    {
        // Unity project paths must remain stable regardless of the host operating system.
        [Test]
        public void CombineUnityAssetPathUsesForwardSlashes()
        {
            Assert.AreEqual(
                "Assets/__Scenes/Environments/Data/ColliderEnvironment.json",
                PathUtils.Combine(
                    "Assets",
                    "__Scenes\\Environments",
                    "Data",
                    "ColliderEnvironment.json"));
        }

        // Windows filesystem paths remain valid while avoiding Unity-unsafe separators.
        [Test]
        public void CombineWindowsPathUsesForwardSlashes()
        {
            Assert.AreEqual("C:/Beat Saber/UserData/file.json", PathUtils.Combine("C:\\Beat Saber", "UserData", "file.json"));
        }

        // URL schemes must retain their double slash when paths are combined.
        [Test]
        public void CombineUrlPreservesScheme()
        {
            Assert.AreEqual("https://example.com/api/maps", PathUtils.Combine("https://example.com/api", "maps"));
        }

        // UNC roots must retain their leading double slash.
        [Test]
        public void CombineUncPathPreservesRoot()
        {
            Assert.AreEqual("//server/share/maps", PathUtils.Combine("\\\\server\\share", "maps"));
        }

        // A native absolute child replaces the parent on both Windows and Linux.
        [Test]
        public void CombineRootedChildDiscardsEarlierSegments()
        {
            var tempDirectory = Path.GetTempPath().Replace('\\', '/').TrimEnd('/');
            var parent = $"{tempDirectory}/PathUtilsTest/Maps/Bookmarks";
            var child = $"{tempDirectory}/PathUtilsTest/bookmarks.dat";

            Assert.AreEqual(child, PathUtils.Combine(parent, child));
        }
    }
}
