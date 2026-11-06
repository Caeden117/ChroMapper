// Empty and malformed GLS filters must fail before iteration so regression tests cannot repeat the billion-iteration stall.
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

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
        [TestCase(0, 1, 0, 0, 10)]
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
        [TestCase(1, 0, 0, 0, 0, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 10)]
        [TestCase(1, 0, 0, 1, 0, new[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }, 10)]
        [TestCase(1, 0, 0, 0, 6, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5)]
        [TestCase(1, 0, 0, 1, 6, new[] { 8, 9, 6, 7, 4, 5, 2, 3, 0, 1 }, 5)]
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
            Assert.That(filter.Select(item => item.Element).ToArray(), Is.EqualTo(expectedElements));
        }

        // For physical lights 0, 1, 4, and 5, chunk indices are 0, 0, 1, 1 and selected-light indices are 0,
        // 1, 2, 3.
        [Test]
        public void ModeBColorDistributionsUseDenseAffectedChunkOrder()
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.StepAndOffset, 0, 2, 0, 4),
                8);
            var entries = filter.ToArray();

            Assert.That(entries.Select(entry => entry.Element).ToArray(), Is.EqualTo(new[] { 0, 1, 4, 5 }));
            Assert.That(entries.Select(entry => entry.AffectedChunkOrder).ToArray(), Is.EqualTo(new[] { 0, 0, 1, 1 }));
            Assert.That(entries.Select(entry => entry.AffectedLightOrder).ToArray(), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.AreEqual(2, filter.VisibleCount);
            Assert.AreEqual(4, filter.AffectedLightCount);
        }

        // OptionalLightProgressFieldAcceptsForwardCompatibleTokensAndTrailingFields protects the optional fourth slot without relaxing required offset/easing validation.
        [Test]
        public void OptionalLightProgressFieldAcceptsForwardCompatibleTokensAndTrailingFields()
        {
            var instructions = GLSColorDistribution.Parse(new[]
            {
                "xg?,0.2,L,l,ignored",
                "r,0.1,L,future,ignored",
                "b,0.3,L,l,ignored,again",
                "h,NaN,L,l",
                "s,0.4,futureEase,l",
                "?,0.5,L,l"
            });

            Assert.AreEqual(3, instructions.Count);
            Assert.That(instructions[0].Targets, Is.EqualTo(GLSColorDistributionTargets.Green));
            Assert.That(instructions[0].Offset, Is.EqualTo(0.2f));
            Assert.That(instructions[1].Targets, Is.EqualTo(GLSColorDistributionTargets.Red));
            Assert.That(instructions[2].Targets, Is.EqualTo(GLSColorDistributionTargets.Blue));
        }

        // OptionalProgressModesRemainTolerantWithoutChangingRequiredFields checks the fourth slot alone controls the mode, and malformed required fields never become active instructions.
        [TestCase("xg?,0.2,L", true, false)]
        [TestCase("xg?,0.2,L,l", true, true)]
        [TestCase("xg?,0.2,L, L ,discard", true, true)]
        [TestCase("xg?,0.2,L,future,l", true, false)]
        [TestCase("xg?,0.2,L,,l", true, false)]
        [TestCase("xg?,0.2,L,ll", true, false)]
        [TestCase("xg?,NaN,L,l", false, false)]
        [TestCase("xg?,Infinity,L,l", false, false)]
        [TestCase("xg?,oops,L,l", false, false)]
        [TestCase("xg?,,L,l", false, false)]
        [TestCase("xg?,0.2,,l", false, false)]
        [TestCase("xg?,0.2,futureEase,l", false, false)]
        [TestCase("xg?,0.2", false, false)]
        [TestCase("x?,0.2,L,l", false, false)]
        [TestCase(null, false, false)]
        public void OptionalProgressModesRemainTolerantWithoutChangingRequiredFields(
            string text, bool valid, bool perLight)
        {
            var instructions = GLSColorDistribution.Parse(new[] { text });
            Assert.That(instructions.Count, Is.EqualTo(valid ? 1 : 0));
            if (!valid)
            {
                return;
            }

            Assert.That(instructions[0].UsesAffectedLightProgress, Is.EqualTo(perLight));
            var color = GLSColorDistribution.Apply(Color.black, instructions, null, 0f, 0.5f);
            Assert.That(color.g, Is.EqualTo(perLight ? 0.1f : 0f).Within(0.0001f));
        }

        // ColorDistributionOptionsUseGlsDisplayAbbreviations ensures authored strings use the exact casing shown on ChroMapper GLS nodes and expose the three distinct Beat Saber curves.
        [Test]
        public void ColorDistributionOptionsUseGlsDisplayAbbreviations()
        {
            var tokens = (string[])typeof(GLSColorDistributionRowView)
                .GetField("EasingTokens", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);

            Assert.That(tokens, Is.EqualTo(new[]
            {
                "L", "I^2", "O^2", "IO^2", "I^3", "O^3", "IO^3", "I^4", "O^4", "IO^4",
                "I^5", "O^5", "IO^5", "ISn", "OSn", "IOSn", "IEx", "OEx", "IOEx", "ICr",
                "OCr", "IOCr", "IBk", "OBk", "IOTBk", "IEl", "OEl", "IOTEl", "IBo", "OBo",
                "IOTBo", "IOBk", "IOEl", "IOBo", "N"
            }));
        }

        // Canonical easing tokens include Beat Saber's InOut variants. Explicit c uses the same chunk
        // progress as an omitted mode.
        [Test]
        public void ColorDistributionParserSupportsCanonicalAndExplicitChunkTokens()
        {
            var standard = GLSColorDistribution.Parse(new[] { "r,1,IOTBk,c", "g,1,IOTEl", "b,1,IOTBo,c" });
            var beatSaber = GLSColorDistribution.Parse(new[] { "r,1,IOBk,c", "g,1,IOEl", "b,1,IOBo,c" });
            var standardColor = GLSColorDistribution.Apply(Color.black, standard, null, 0.25f, 0.75f);
            var beatSaberColor = GLSColorDistribution.Apply(Color.black, beatSaber, null, 0.25f, 0.75f);

            Assert.AreEqual(3, standard.Count);
            Assert.AreEqual(3, beatSaber.Count);
            Assert.That(standard.All(instruction => !instruction.UsesAffectedLightProgress), Is.True);
            Assert.That(beatSaber.All(instruction => !instruction.UsesAffectedLightProgress), Is.True);
            Assert.That(beatSaberColor.r, Is.Not.EqualTo(standardColor.r).Within(0.0001f));
            Assert.That(beatSaberColor.g, Is.Not.EqualTo(standardColor.g).Within(0.0001f));
            Assert.That(beatSaberColor.b, Is.Not.EqualTo(standardColor.b).Within(0.0001f));
        }

        // Reading distributions must preserve authored display abbreviations and progress modes for the next save.
        [Test]
        public void ColorDistributionStringsPreserveAuthoredNamesOnRead()
        {
            var customData = JSON.Parse("{\"colorDistributions\":[\"h,0.1,L\",\"s,0.2,IOTBk,l\",\"v,0.3,IOTEl,c\",\"f,0.4,IOTBo\"]}");

            var values = GLSColorDistribution.ReadStrings(customData, GLSColorDistribution.ColorDistributionsKey);

            Assert.That(values, Is.EqualTo(new[] { "h,0.1,L", "s,0.2,IOTBk,l", "v,0.3,IOTEl,c", "f,0.4,IOTBo" }));
        }

        // AffectedLightCountMatchesPartialReversedAndLimitedChunks protects the per-light denominator before enumeration, including seed-based selection of a short final chunk.
        [TestCase(0, 0, 0f)]
        [TestCase(1, 0, 0f)]
        [TestCase(0, 0, 0.5f)]
        [TestCase(1, 0, 0.5f)]
        [TestCase(0, 1, 0.5f)]
        [TestCase(1, 1, 0.5f)]
        [TestCase(0, 2, 0.5f)]
        [TestCase(1, 2, 0.5f)]
        [TestCase(0, 3, 0.5f)]
        [TestCase(1, 3, 0.5f)]
        public void AffectedLightCountMatchesPartialReversedAndLimitedChunks(int reverse, int random, float limit)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.StepAndOffset, 0, 1, reverse, 4, limit, 0, random, 12),
                10);
            var countBeforeEnumeration = filter.AffectedLightCount;
            var entries = filter.ToArray();

            Assert.That(countBeforeEnumeration, Is.EqualTo(entries.Length));
            Assert.That(filter.AffectedLightCount, Is.EqualTo(countBeforeEnumeration));
            Assert.That(entries.Select(entry => entry.AffectedLightOrder), Is.EqualTo(Enumerable.Range(0, entries.Length)));
            if (random == 0 && limit == 0f)
            {
                Assert.That(entries.Length, Is.EqualTo(10));
            }
            if (random == 0 && limit == 0.5f)
            {
                Assert.That(entries.Length, Is.EqualTo(reverse == 0 ? 6 : 4));
            }
        }

        // PerLightColorDistributionsPreserveHsvHdrAndIndependentF applies different chunk/light coordinates so hue wrapping, saturation clamping, HDR value, f, and box-before-event order cannot regress unnoticed.
        [Test]
        public void PerLightColorDistributionsPreserveHsvHdrAndIndependentF()
        {
            var source = Color.HSVToRGB(0.1f, 0.8f, 2f, true);
            source.a = 0.25f;
            var color = GLSColorDistribution.Apply(
                source,
                GLSColorDistribution.Parse(new[] { "xh,-0.6,L,l", "s,0.6,L,l", "f,0.6,L,l" }),
                GLSColorDistribution.Parse(new[] { "s,-0.4,L,future", "v,2,L,l", "f,0.8,I^2,l" }),
                1f,
                0.5f);
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);

            Assert.That(hue, Is.EqualTo(0.8f).Within(0.0001f));
            // Clamp saturation once after combining box and event offsets: .8 + .6*.5 - .4 = .7. Clamping
            // after each instruction would give .6.
            Assert.That(saturation, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(value, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(color.a, Is.EqualTo(0.75f).Within(0.0001f));
        }

        // FirstColorModelWinsWhileFRemainsIndependent covers rs/sr compatibility, forward-compatible unknown targets, and f composition with a color model.
        [Test]
        public void FirstColorModelWinsWhileFRemainsIndependent()
        {
            var rgbFirst = GLSColorDistribution.Apply(
                new Color(0.2f, 0.3f, 0.4f, 0.5f),
                GLSColorDistribution.Parse(new[] { "xrsf,0.3,L" }),
                null,
                1f);
            var hsvSource = Color.HSVToRGB(0.1f, 0.2f, 0.5f, true);
            var hsvFirst = GLSColorDistribution.Apply(
                hsvSource,
                GLSColorDistribution.Parse(new[] { "xsfr,0.3,L" }),
                null,
                1f);
            Color.RGBToHSV(hsvFirst, out var hue, out var saturation, out var value);

            Assert.That(rgbFirst.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(rgbFirst.g, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(rgbFirst.b, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(rgbFirst.a, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(hue, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(saturation, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(value, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(hsvFirst.a, Is.EqualTo(1.3f).Within(0.0001f));
        }

        // Strobe distributions start from the unshifted base color and apply box then event offsets (f+0.5,
        // b+0.3). Normal offsets (g+0.1, r+0.2) must not leak into this channel.
        [Test]
        public void StrobeDistributionsOwnTheStrobeChannelWithoutExplicitStrobeColor()
        {
            var box = new BaseLightColorEventBox();
            box.SetCustomData(JSON.Parse(
                "{\"colorDistributions\":[\"g,0.1,L\"],\"strobeColorDistributions\":[\"f,0.5,L\"]}"));
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{\"customData\":{\"colorDistributions\":[\"r,0.2,L\"],\"strobeColorDistributions\":[\"b,0.3,L\"]}}"));
            var mainColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);

            var normal = GLSColorDistribution.ApplyNormal(mainColor, box, evt, 1f);
            var strobe = GLSColorDistribution.ApplyStrobe(mainColor, box, evt, 1f);

            Assert.That(normal.r, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(normal.g, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(normal.b, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(normal.a, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(strobe.r, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(strobe.g, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(strobe.b, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(strobe.a, Is.EqualTo(0.9f).Within(0.0001f));
        }

        [Test]
        public void OrderedColorDistributionInstructionsAccumulateWithIndependentEasings()
        {
            var color = GLSColorDistribution.Apply(
                new Color(0.25f, 0.5f, 0.75f, 1f),
                GLSColorDistribution.Parse(new[] { "r,0.2,L", "r,0.4,I^2" }),
                null,
                0.5f);

            Assert.That(color.r, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(color.g, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(color.b, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void InvalidColorDistributionInstructionsAreIgnoredWithoutRejectingRecognizedTargets()
        {
            var instructions = GLSColorDistribution.Parse(new[]
            {
                "xg?,0.2,L",
                "r,NaN,L",
                "b,0.1,futureEase",
                "?,0.5,L"
            });
            var color = GLSColorDistribution.Apply(new Color(0.1f, 0.2f, 0.3f, 1f), instructions, null, 1f);

            Assert.AreEqual(1, instructions.Count);
            Assert.That(color.r, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(color.g, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(color.b, Is.EqualTo(0.3f).Within(0.0001f));
        }
    }
}
