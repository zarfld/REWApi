using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace REWApi.Domain.JsonConverters
{
    /// <summary>
    /// JSON converter that handles empty strings by converting them to doubles.
    /// Used for REW API fields that may return empty strings when values are not set.
    /// </summary>
    public class EmptyStringToDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                
                // Handle empty string or "null" string
                if (string.IsNullOrWhiteSpace(stringValue) || stringValue.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return 0.0;
                }
                
                // Try to parse the string as a double
                if (double.TryParse(stringValue, out double result))
                {
                    return result;
                }
                
                throw new JsonException($"Unable to convert \"{stringValue}\" to Double.");
            }
            
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDouble();
            }
            
            if (reader.TokenType == JsonTokenType.Null)
            {
                return 0.0;
            }
            
            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Double.");
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
