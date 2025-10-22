using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace REWApi.Domain.JsonConverters
{
    /// <summary>
    /// JSON converter that handles empty strings and floating point numbers by converting them to integers.
    /// Used for REW API fields that may return empty strings or decimal numbers when values are not set or are fractional.
    /// </summary>
    public class EmptyStringToIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                
                // Handle empty string or "null" string
                if (string.IsNullOrWhiteSpace(stringValue) || stringValue.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
                
                // Try to parse the string as an integer
                if (int.TryParse(stringValue, out int result))
                {
                    return result;
                }
                
                throw new JsonException($"Unable to convert \"{stringValue}\" to Int32.");
            }
            
            if (reader.TokenType == JsonTokenType.Number)
            {
                // Try to get as int32 first
                if (reader.TryGetInt32(out int intValue))
                {
                    return intValue;
                }
                
                // If it's a floating point number, truncate to integer
                if (reader.TryGetDouble(out double doubleValue))
                {
                    return (int)doubleValue;
                }
                
                throw new JsonException($"Unable to convert number to Int32.");
            }
            
            if (reader.TokenType == JsonTokenType.Null)
            {
                return 0;
            }
            
            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Int32.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
