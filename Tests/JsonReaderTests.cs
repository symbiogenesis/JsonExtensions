using JsonExtensions;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace Tests
{
    public class JsonReaderTests
    {
        private const string jsonInvalid =
            """
            {
                "a":::::::,,,,,,
            }
            """;

        private const string jsonUnbalancedObject =
            """
            {
                "a": 12,
                "b": 12,
                "c": 12
            """;

        private const string jsonUnbalancedArray =
            """
            [10,20,30
            """;

        private const string jsonSmallObject =
            /*lang=json,strict*/
            """
            {
                "a": 12,
                "b": 12,
                "c": 12
            }
            """;

        private const string jsonSmallArray =
            /*lang=json,strict*/
            """[12,12,12]""";

        private const string jsonArray =
            /*lang=json,strict*/
            """
            [{
                "Date": "2019-08-01T00:00:00-07:00",
                "Temperature": 25,
                "TemperatureRanges": {
                    "Cold": { "High": 20, "Low": -10.5 },
                    "Hot": { "High": 60, "Low": 20 }
                },
                "Summary": "Hot",
                "IsHot": true
            },
            {
                "Date": "2019-08-01T00:00:00-07:00",
                "Temperature": 25,
                "TemperatureRanges": {
                    "Cold": { "High": 20, "Low": -10 },
                    "Hot": { "High": 60, "Low": 20 }
                },
                "Summary": "Hot",
                "IsHot": false
            }]
            """;

        private readonly ITestOutputHelper output;

        public JsonReaderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(jsonInvalid)]
        [InlineData(jsonUnbalancedObject)]
        [InlineData(jsonUnbalancedArray)]
        public async Task InvalidJson_ShouldThrow(string json)
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var jsonReader = new JsonReader(stream, 10);

            Assert.ThrowsAny<JsonException>(() =>
            {
                foreach(var v in jsonReader.Read())
                {
                    output.WriteLine($"{v.TokenType}");
                }
            });
        }

        [Fact]
        public async Task LargeGap_ShouldThrow()
        {
            var largeGapJson = $"{{ \"a\": {new String(' ', 2 * 1024 * 1024)}10 }}";
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeGapJson));
            var jsonReader = new JsonReader(stream, 10);

            Assert.ThrowsAny<JsonException>(() =>
            {
                foreach(var v in jsonReader.Read())
                {
                    output.WriteLine($"{v.TokenType}");
                }
            });
        }

        [Theory]
        [InlineData(jsonSmallObject, new[] { JsonTokenType.StartObject, JsonTokenType.PropertyName, JsonTokenType.Number, JsonTokenType.PropertyName, JsonTokenType.Number, JsonTokenType.PropertyName, JsonTokenType.Number, JsonTokenType.EndObject })]
        [InlineData(jsonSmallArray, new[] { JsonTokenType.StartArray, JsonTokenType.Number, JsonTokenType.Number, JsonTokenType.Number, JsonTokenType.EndArray })]
        public async Task Json_ShouldContainsAllTokens(string json, JsonTokenType[] expectedTokens)
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Select(x => x.TokenType).ToList();

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task JsonArray_ShouldContainsValidStringTypes()
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Where(x => x.TokenType == JsonTokenType.String).Select(v => v.Value.ToString()).ToList();

            Assert.Equal(["2019-08-01T00:00:00-07:00", "Hot", "2019-08-01T00:00:00-07:00", "Hot"], tokens);
        }

        [Fact]
        public async Task JsonArray_ShouldContainsValidNumberTypes()
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Where(x => x.TokenType == JsonTokenType.Number).Select(v => v.Value.Deserialize<double>()).ToList();

            Assert.Equal([25, 20, -10.5, 60, 20, 25, 20, -10, 60, 20], tokens);
        }

        [Fact]
        public async Task JsonArray_ShouldContainsValidBooleanTypes()
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Where(x => x.TokenType == JsonTokenType.False || x.TokenType == JsonTokenType.True).Select(v => v.Value.Deserialize<bool>()).ToList();

            Assert.Equal([true, false], tokens);
        }

        [Fact]
        public async Task NullValue_ShouldReturnNullToken()
        {
            const string jsonWithNull = "{ \"a\": null }";
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonWithNull));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Select(x => x.TokenType).ToList();

            Assert.Equal([JsonTokenType.StartObject, JsonTokenType.PropertyName, JsonTokenType.Null, JsonTokenType.EndObject], tokens);
        }

        [Fact]
        public async Task EmptyObject_ShouldReturnStartAndEndObjectTokens()
        {
            const string emptyObject = "{}";
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyObject));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Select(x => x.TokenType).ToList();

            Assert.Equal([JsonTokenType.StartObject, JsonTokenType.EndObject], tokens);
        }

        [Fact]
        public async Task EmptyArray_ShouldReturnStartAndEndArrayTokens()
        {
            const string emptyArray = "[]";
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyArray));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Select(x => x.TokenType).ToList();

            Assert.Equal([JsonTokenType.StartArray, JsonTokenType.EndArray], tokens);
        }

        [Fact]
        public async Task JsonWithSpecialCharacters_ShouldReturnCorrectTokens()
        {
            const string jsonWithSpecialChars = "{ \"a\": \"hello\\nworld\", \"b\": \"\\\"quoted\\\"\" }";
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonWithSpecialChars));
            var jsonReader = new JsonReader(stream, 10);
            var tokens = jsonReader.Read().Select(x => x.TokenType).ToList();

            Assert.Equal([JsonTokenType.StartObject, JsonTokenType.PropertyName, JsonTokenType.String, JsonTokenType.PropertyName, JsonTokenType.String, JsonTokenType.EndObject], tokens);
        }
    }
}