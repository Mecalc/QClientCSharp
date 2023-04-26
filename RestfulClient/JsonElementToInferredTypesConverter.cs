// -------------------------------------------------------------------------
// Copyright (c) Mecalc (Pty) Limited. All rights reserved.
// -------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace QClient.RestfulClient
{
    /// <summary>
    /// This class will provide the necessary methods to convert QServer elements to C# types.
    /// </summary>
    public class JsonElementToInferredTypesConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader,
                                     Type typeToConvert,
                                     JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    return null;
                case JsonTokenType.EndObject:
                    return null;
                case JsonTokenType.StartArray:
                    return null;
                case JsonTokenType.EndArray:
                    return null;
                case JsonTokenType.PropertyName:
                    return null;
                case JsonTokenType.Comment:
                    return null;

                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long intValue))
                    {
                        return intValue;
                    }

                    return reader.GetDouble();

                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return null;

                default:
                    return JsonDocument.ParseValue(ref reader).RootElement.Clone();
            }
        }

        public override void Write(Utf8JsonWriter writer,
                                   object objectToWrite,
                                   JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
        }
    }
}
