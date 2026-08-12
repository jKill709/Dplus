using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dplus_Desktop.Config
{
    public class FlexibleDateTimeConverter : JsonConverter<DateTime?>
    {
        private static readonly string[] formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "MM-dd-yyyy hh:mm:sstt",
            "M-d-yyyy h:mm:sstt",
            "yyyy-MM-ddTHH:mm:ss",
        };

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (DateTime.TryParseExact(str, formats, null, System.Globalization.DateTimeStyles.None, out DateTime dt))
                {
                    return dt;
                }
            }
            return null; // fallback if invalid
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            else
                writer.WriteNullValue();
        }
    }
}
