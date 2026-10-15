using System.Globalization;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class JSONNumberFormattingTest
    {
        [TestCase(9972654)]
        [TestCase(10043470)]
        [TestCase(10090096)]
        [TestCase(999999872)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void SampleIndicesSerializeAsExactIntegerTokens(int sampleIndex)
        {
            var node = new JSONNumber(sampleIndex);
            var output = node.ToString();

            Assert.AreEqual(sampleIndex.ToString(CultureInfo.InvariantCulture), output);
            Assert.True(int.TryParse(output, NumberStyles.Integer, CultureInfo.InvariantCulture, out var savedIndex));
            Assert.AreEqual(sampleIndex, savedIndex);
        }

        [TestCase(9007199254740991L)]
        [TestCase(1000000000000000000L)]
        [TestCase(9223372036854774784L)]
        [TestCase(long.MinValue)]
        public void ExactlyRepresentableLongsSerializeAsIntegerTokens(long value)
        {
            var node = new JSONNumber(value);

            Assert.AreEqual(value.ToString(CultureInfo.InvariantCulture), node.ToString());
            Assert.AreEqual(value, JSON.Parse(node.ToString()).AsLong);
        }

        [TestCase(1e20)]
        [TestCase(-1e20)]
        [TestCase(1e-7)]
        [TestCase(-1e-7)]
        [TestCase(1.2345678901234567e100)]
        [TestCase(-1.2345678901234567e-100)]
        [TestCase(9007199254740991d)]
        [TestCase(double.MaxValue)]
        [TestCase(double.MinValue)]
        [TestCase(double.Epsilon)]
        [TestCase(-double.Epsilon)]
        [TestCase(0.6822871999174)]
        [TestCase(9223372036854775808d)]
        [TestCase(-9223372036854777856d)]
        public void DoublesRoundTripWithoutLosingPrecision(double value)
        {
            var node = new JSONNumber(value);

            Assert.AreEqual(value, JSON.Parse(node.ToString()).AsDouble);
            Assert.AreEqual(value, JSON.Parse(node.ToString(2)).AsDouble);
        }

        [TestCase(0.0000001f, "1.00000001E-07")]
        [TestCase(-0.0000001f, "-1.00000001E-07")]
        [TestCase(10043470f, "10043470")]
        [TestCase(999999872f, "999999872")]
        [TestCase(0.1f, "0.100000001")]
        [TestCase(-9.305975f, "-9.30597496")]
        [TestCase(2.999999999999999997f, "3")]
        [TestCase(2.9999998f, "2.99999976")]
        public void FloatNumbersKeepTheirPrecision(float value, string expected)
        {
            JSONNumber[] nodes = { new JSONNumber(value), new JSONFloatNumber(value) };
            foreach (var node in nodes)
            {
                Assert.AreEqual(expected, node.Value);
                Assert.AreEqual(value, JSON.Parse(node.ToString()).AsFloat);
            }
        }

        [TestCase(float.MaxValue)]
        [TestCase(float.MinValue)]
        [TestCase(float.Epsilon)]
        [TestCase(-float.Epsilon)]
        public void ExtremeFloatsRoundTrip(float value)
        {
            JSONNumber[] nodes = { new JSONNumber(value), new JSONFloatNumber(value) };
            foreach (var node in nodes)
            {
                Assert.AreEqual(value, JSON.Parse(node.ToString()).AsFloat);
            }
        }

        [TestCase(float.MaxValue, "3.40282347E+38")]
        [TestCase(float.MinValue, "-3.40282347E+38")]
        [TestCase(10000000000f, "1E+10")]
        public void ExplicitFloatNumbersUseG9(float value, string expected)
        {
            var node = new JSONFloatNumber(value);

            Assert.AreEqual(expected, node.Value);
            Assert.AreEqual(value, JSON.Parse(node.ToString()).AsFloat);
        }

        [Test]
        public void FloatNumbersKeepFloatFormattingAfterValueChanges()
        {
            const double value = 0.12345678901234567;
            var expected = new JSONFloatNumber((float)value).Value;
            var node = new JSONFloatNumber(0);
            node.AsDouble = value;
            Assert.AreEqual(expected, node.Value);

            node.Value = value.ToString("G17", CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, node.Value);
        }

        [TestCase("2.999999999999999997", 3d)]
        [TestCase("2.9999999999999996", 2.9999999999999996d)]
        [TestCase("3.0000000000000004", 3.0000000000000004d)]
        public void ValuesNearIntegersKeepDoublePrecision(string input, double expected)
        {
            var output = JSON.Parse(input).ToString();

            Assert.AreEqual(expected, JSON.Parse(output).AsDouble);
            if (expected == 3d)
                Assert.AreEqual("3", output);
        }

        [TestCase(1e20, 3, "1E+20")]
        [TestCase(-1e20, 3, "-1E+20")]
        [TestCase(0.0000001, 9, "1E-07")]
        [TestCase(1.234567, 3, "1.235")]
        [TestCase(2.9999999999999996, 3, "3")]
        public void BeatRoundingRetainsSelectedPrecision(double value, int precision, string expected)
        {
            var node = new JSONNumberWithOverridenRounding(value, precision);

            Assert.AreEqual(expected, node.Value);
            Assert.AreEqual(expected, node.ToString());
        }

        [TestCase("en-US")]
        [TestCase("de-DE")]
        public void AudioSampleIndicesStayIntegerTokensInSerializedObjects(string cultureName)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var region = new JSONObject { ["si"] = 10043470, ["ei"] = 10090096, ["sb"] = 0.0000001 };

                var output = region.ToString();
                Assert.That(output, Does.Contain("\"si\":10043470,"));
                Assert.That(output, Does.Contain("\"ei\":10090096,"));
                var saved = JSON.Parse(region.ToString(2));
                Assert.AreEqual(10043470, saved["si"].AsInt);
                Assert.AreEqual(10090096, saved["ei"].AsInt);
                Assert.AreEqual(0.0000001, saved["sb"].AsDouble);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
