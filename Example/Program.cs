using System.Text;
using System.Text.Json;
using JsonExtensions;
using System.Diagnostics;

namespace ConsoleJsonSample
{
    internal class Program
    {
        // Main is mark as async (even if not needed)
        // to be sure we can call the JsonReader.Read() method from an async scope
        static async Task Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var initialMemory = GC.GetTotalMemory(true);

            var jsonReader = new JsonReader(GetFileStream(), 1024); // test 10 to see buffer increase in debug console

            foreach (var prop in jsonReader.Read())
            {
                switch (prop.TokenType)
                {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        Console.WriteLine($"- ({prop.TokenType})");
                        break;
                    case JsonTokenType.PropertyName:
                        Console.WriteLine($"Property: {prop.Name}");
                        break;
                    default:
                        Console.WriteLine($"Value: {prop.Value}");
                        break;
                }
            }

            stopwatch.Stop();

            var finalMemory = GC.GetTotalMemory(true);

            Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Memory allocated: {finalMemory - initialMemory} bytes");
        }

        static FileStream GetFileStream()
        {
            return new FileStream("Address.json", FileMode.Open);
        }

        static MemoryStream GetMemoryStream()
        {
            const string jsonString =
                /*lang=json*/
                """
                [
                    {
                        "Date": "2019-08-01T00:00:00-07:00",
                        "Temperature": 25,
                        "TemperatureRanges": {
                            "Cold": { "High": 20, "Low": -10.5 },
                            "Hot": { "High": 60, "Low": 20 }
                        },
                        "Summary": "Hot"
                    }, 
                    {
                        "Date": "2019-08-01T00:00:00-07:00",
                        "Temperature": 25,
                        "TemperatureRanges": {
                            "Cold": { "High": 20, "Low": -10 },
                            "Hot": { "High": 60, "Low": 20 }
                        },
                        "Summary": "Hot"
                    }]
                """;

            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
            return new MemoryStream(bytes);
        }
    }
}
