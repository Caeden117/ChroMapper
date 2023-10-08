using NUnit.Framework;

namespace Tests
{
    public class BeatmapV3OptionalParamTest
    {
        [Test]
        public void DoTheTest()
        {
            // Including EditMode test here in PlayMode so the pipeline runs the tests as well.
            new TestsEditMode.BeatmapV3OptionalParamTestEditMode().TestEverything();
        }
    }
}