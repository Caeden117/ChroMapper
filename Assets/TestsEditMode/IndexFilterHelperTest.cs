// Empty and malformed GLS filters must fail before iteration so regression tests cannot repeat the billion-iteration stall.
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;

namespace TestsEditMode
{
    public class IndexFilterHelperTest
    {
        // The rotation and translation axis fixtures select missing environment groups with chunked, reversed Division filters.
        [TestCase(1, 4, 2, 1, 3)]
        [TestCase(1, 5, 3, 1, 2)]
        [TestCase(1, 4, 2, 0, 3)]
        [TestCase(1, 1, 0, 0, 0)]
        [TestCase(1, 1, 0, 1, 0)]
        [TestCase(2, 0, 1, 0, 3)]
        [TestCase(2, 0, 1, 1, 3)]
        [TestCase(2, 0, 0, 0, 0)]
        public void EmptyGroupReturnsNoFilter(int type, int param0, int param1, int reverse, int chunks)
        {
            var filter = IndexFilterHelper.Convert(new BaseIndexFilter(type, param0, param1, reverse, chunks), 0);

            // NUnit formats IEnumerable values on failure; assert a boolean so reporting cannot enumerate the broken filter.
            Assert.That(filter == null, Is.True, "A missing environment group must be rejected before chunk division or enumeration.");
        }

        // Invalid parameters share the empty-group failure mode: they must never become huge or backwards ranges.
        [TestCase(0, 0, 0, 0, 10)]
        [TestCase(-1, 0, 0, 0, 10)]
        [TestCase(2, -1, 0, 0, 10)]
        [TestCase(2, 2, 0, 0, 10)]
        [TestCase(2, int.MaxValue, 1, 0, 10)]
        [TestCase(1, 0, 0, -1, 10)]
        [TestCase(1, 0, 0, 0, -1)]
        public void InvalidDivisionParametersReturnNoFilter(int sections, int sectionId, int reverse, int chunks, int groupSize)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.Division, sections, sectionId, reverse, chunks), groupSize);

            // Failure output must not enumerate an invalid filter while explaining that it should have been rejected.
            Assert.That(filter == null, Is.True, "Invalid Division parameters must not create an iterable range.");
        }

        // A legal section number can still lie beyond the available chunks; returning a reversed range invents lights.
        [TestCase(0)]
        [TestCase(1)]
        public void EmptyTrailingDivisionSectionReturnsNoFilter(int reverse)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.Division, 5, 3, reverse, 2), 10);

            // Failure output must not enumerate the invalid range it is reporting.
            Assert.That(filter == null, Is.True, "A section beyond the available chunks must not select neighbouring or negative IDs.");
        }

        // Negative offsets can overflow the range size, and negative steps cannot represent forward offset selection.
        [TestCase(-1, 1)]
        [TestCase(int.MinValue, 1)]
        [TestCase(0, -1)]
        [TestCase(10, 1)]
        public void InvalidStepAndOffsetParametersReturnNoFilter(int offset, int step)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.StepAndOffset, offset, step, 0), 10);

            // Failure output must not enumerate an invalid or negative-sized range.
            Assert.That(filter == null, Is.True, "Invalid StepAndOffset parameters must not create an iterable range.");
        }

        // Guarding invalid input must preserve normal chunk expansion, reverse order, and a partially filled final chunk.
        [TestCase(1, 3, 2, 0, 0, new[] { 8, 9 }, 2)]
        [TestCase(1, 3, 2, 1, 0, new[] { 1, 0 }, 2)]
        [TestCase(1, 2, 0, 0, 3, new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, 2)]
        [TestCase(1, 2, 0, 1, 3, new[] { 8, 9, 4, 5, 6, 7 }, 2)]
        [TestCase(2, 1, 1, 0, 3, new[] { 4, 5, 6, 7, 8, 9 }, 2)]
        [TestCase(2, 1, 1, 1, 3, new[] { 4, 5, 6, 7, 0, 1, 2, 3 }, 2)]
        [TestCase(2, 2, 0, 0, 0, new[] { 2 }, 1)]
        [TestCase(2, 2, 0, 1, 0, new[] { 7 }, 1)]
        public void ValidFiltersPreserveSelectedElements(
            int type, int param0, int param1, int reverse, int chunks, int[] expectedElements, int expectedCount)
        {
            var filter = IndexFilterHelper.Convert(new BaseIndexFilter(type, param0, param1, reverse, chunks), 10);

            Assert.That(filter, Is.Not.Null);
            Assert.That(filter.Count, Is.EqualTo(expectedCount), "Check the bound before enumerating so a regression fails promptly.");
            Assert.That(filter.VisibleCount, Is.EqualTo(expectedCount));
            Assert.That(filter.Select(item => item.element).ToArray(), Is.EqualTo(expectedElements));
        }
    }
}
