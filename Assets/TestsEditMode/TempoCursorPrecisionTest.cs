using NUnit.Framework;

namespace TestsEditMode
{
    public class TempoCursorPrecisionTest
    {
        [TestCase(4, 1, false, false, 8)]
        [TestCase(4, -1, false, false, 2)]
        [TestCase(4, 1, false, true, 2)]
        [TestCase(4, -1, false, true, 8)]
        [TestCase(4, 1, true, false, 5)]
        [TestCase(4, -1, true, false, 3)]
        [TestCase(4, 1, true, true, 3)]
        [TestCase(4, -1, true, true, 5)]
        [TestCase(1, -1, false, false, 1)]
        [TestCase(64, 1, false, false, 64)]
        [TestCase(1, -1, true, false, 1)]
        [TestCase(64, 1, true, false, 64)]
        [TestCase(3, -1, false, false, 2)]
        [TestCase(5, -1, false, false, 2)]
        [TestCase(8, 0, false, false, 8)]
        public void SubdivisionRespectsScrollDirectionModifiersAndBounds(int current, float scroll, bool precise, bool invert, int expected)
        {
            var previous = Settings.Instance.InvertPrecisionScroll;
            try
            {
                Settings.Instance.InvertPrecisionScroll = invert;
                Assert.That(TempoCursorPrecision.Step(current, scroll, precise), Is.EqualTo(expected));
            }
            finally { Settings.Instance.InvertPrecisionScroll = previous; }
        }
    }
}
