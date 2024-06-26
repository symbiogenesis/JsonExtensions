# JsonParser

Fast Forward reader using `Utf8JsonReader`.

This fast forward reader is useful if you want to iterate all over the **JSON** data with a minimum memory allocation.

This extensions is using the `Utf8JsonReader` that is a high-performance JSON parser that reads from a `ReadOnlySpan<byte>` and is able to read the JSON data without allocating memory for the JSON data.

You can read more about the `Utf8JsonReader` [here](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader).

As the `Utf8JsonReader` is a ref struct, it's complicated to use it to return an `IENumerable<T>` (using `yield`), and even from an `async` method.

This JsonReader class allows you to iterate over the JSON data using a simple `foreach` loop.

``` csharp

var jsonReader = new JsonReader(GetFileStream(), 1024); // test 10 to see buffer increase in debug console

foreach (var prop in jsonReader.Read())
{
    if (prop.TokenType == JsonTokenType.StartObject || prop.TokenType == JsonTokenType.StartArray || prop.TokenType == JsonTokenType.EndObject || prop.TokenType == JsonTokenType.EndArray)
        Console.WriteLine($"- ({prop.TokenType})");
    else if (prop.TokenType == JsonTokenType.PropertyName)
        Console.WriteLine($"Property: {prop.Name}");
    else
        Console.WriteLine($"Value: {prop.Value}");
}
```

``` bash
- (StartObject)
Property: tableName
Value: Address
Property: schemaName
Value: dbo
Property: version
Value: 1
Property: columns
- (StartArray)
- (StartObject)
Property: name
Value: AddressID
Property: type
Value: int
- (EndObject)
- (StartObject)
Property: name
Value: Country
Property: type
Value: string
- (EndObject)
- (EndArray)
- (EndObject)
```