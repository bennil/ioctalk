using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using BSAG.IOCTalk.Serialization.Json;
using Xunit;
using Xunit.Abstractions;

namespace BSAG.IOCTalk.Common.Test
{
    /// <summary>
    /// Regression: the deserialization of large arrays grew quadratically with the payload size because of
    /// culture-sensitive string.IndexOf calls (no ASCII fast path for e.g. de-DE) and because of key lookups
    /// scanning the whole remaining json for properties missing in the json.
    /// 2369 objects / 1.8 MB took 136 s with de-DE before the fix (0.6 s with the invariant culture).
    /// </summary>
    public class JsonSerializerLargePayloadTest
    {
        private const int ItemCount = 3000;

        /// <summary>
        /// Generous limit - the quadratic implementation needed minutes for this payload
        /// </summary>
        private static readonly TimeSpan MaxDeserializeTime = TimeSpan.FromSeconds(10);

        private readonly ITestOutputHelper output;

        public JsonSerializerLargePayloadTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("")]
        public void TestMethodLargeArrayDeserializationIsLinear(string cultureName)
        {
            var oldCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);

                var serializer = new JsonObjectSerializer();
                LargePayloadItem[] items = CreateItems();
                string json = serializer.Serialize(items, null);

                var sw = Stopwatch.StartNew();
                var result = (LargePayloadItem[])serializer.Deserialize(json, typeof(LargePayloadItem[]), null);
                sw.Stop();
                output.WriteLine($"[{cultureName}] {ItemCount} items, {json.Length / 1024} KB: {sw.ElapsedMilliseconds} ms");

                Assert.True(sw.Elapsed < MaxDeserializeTime, $"Deserialization took {sw.Elapsed}");
                AssertItemsEqual(items, result);
            }
            finally
            {
                CultureInfo.CurrentCulture = oldCulture;
            }
        }

        [Fact]
        public void TestMethodLargeArrayDeserializationWithMissingProperties()
        {
            var oldCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                var serializer = new JsonObjectSerializer();
                LargePayloadItem[] items = CreateItems();
                string json = serializer.Serialize(items, null);

                // target type has additional properties not contained in the json
                var sw = Stopwatch.StartNew();
                var result = (LargePayloadItemExtended[])serializer.Deserialize(json, typeof(LargePayloadItemExtended[]), null);
                sw.Stop();
                output.WriteLine($"[de-DE, missing properties] {ItemCount} items, {json.Length / 1024} KB: {sw.ElapsedMilliseconds} ms");

                Assert.True(sw.Elapsed < MaxDeserializeTime, $"Deserialization took {sw.Elapsed}");
                Assert.Equal(items.Length, result.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    Assert.Equal(items[i].Id, result[i].Id);
                    Assert.Equal(items[i].Name, result[i].Name);
                    Assert.Equal(items[i].Utc, result[i].Utc);
                    Assert.Equal(items[i].Amount, result[i].Amount);
                    Assert.Equal(items[i].Comment, result[i].Comment);
                    Assert.Null(result[i].ExtraBefore);
                    Assert.Null(result[i].ExtraAfter);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = oldCulture;
            }
        }

        private static LargePayloadItem[] CreateItems()
        {
            var start = new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);
            return Enumerable.Range(1, ItemCount).Select(i => new LargePayloadItem
            {
                Id = i,
                Name = $"Tankstelle Süd {i} – Zapfsäule \"{i % 4}\"",
                ExternalId = $"2050710-{i}-2611021926",
                Utc = start.AddMinutes(-i),
                OptionalUtc = i % 3 == 0 ? (DateTime?)null : start.AddMinutes(-i - 5),
                Quantity = i % 5 == 0 ? (double?)null : i * 1.25,
                Amount = i * 0.37m,
                OptionalId = i % 2 == 0 ? (int?)null : i * 7,
                Key = new Guid(i, 0, 0, new byte[8]),
                Duration = TimeSpan.FromSeconds(i),
                Flag = i % 2 == 0,
                Comment = i % 4 == 0 ? null : $"Größe {i} äöü ß € Ünicode",
            }).ToArray();
        }

        private static void AssertItemsEqual(LargePayloadItem[] expected, LargePayloadItem[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                var e = expected[i];
                var a = actual[i];
                Assert.Equal(e.Id, a.Id);
                Assert.Equal(e.Name, a.Name);
                Assert.Equal(e.ExternalId, a.ExternalId);
                Assert.Equal(e.Utc, a.Utc);
                Assert.Equal(e.OptionalUtc, a.OptionalUtc);
                Assert.Equal(e.Quantity, a.Quantity);
                Assert.Equal(e.Amount, a.Amount);
                Assert.Equal(e.OptionalId, a.OptionalId);
                Assert.Equal(e.Key, a.Key);
                Assert.Equal(e.Duration, a.Duration);
                Assert.Equal(e.Flag, a.Flag);
                Assert.Equal(e.Comment, a.Comment);
            }
        }
    }

    public class LargePayloadItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ExternalId { get; set; }
        public DateTime Utc { get; set; }
        public DateTime? OptionalUtc { get; set; }
        public double? Quantity { get; set; }
        public decimal Amount { get; set; }
        public int? OptionalId { get; set; }
        public Guid Key { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Flag { get; set; }
        public string Comment { get; set; }
    }

    /// <summary>
    /// Like <see cref="LargePayloadItem"/> with additional properties that are missing in the json
    /// </summary>
    public class LargePayloadItemExtended
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ExtraBefore { get; set; }
        public string ExternalId { get; set; }
        public DateTime Utc { get; set; }
        public DateTime? OptionalUtc { get; set; }
        public double? Quantity { get; set; }
        public decimal Amount { get; set; }
        public int? OptionalId { get; set; }
        public Guid Key { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Flag { get; set; }
        public string Comment { get; set; }
        public string ExtraAfter { get; set; }
    }
}
