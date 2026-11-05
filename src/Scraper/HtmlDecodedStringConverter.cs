using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HtmlDecodedStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options) =>
        WebUtility.HtmlDecode(reader.GetString());

    public override void Write(Utf8JsonWriter writer, string value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}