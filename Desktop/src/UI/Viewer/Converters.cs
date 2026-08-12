using OpenCvSharp;

namespace System.Text.Json.Serialization
{
    public class Point2fConverter : JsonConverter<Point2f>
    {
        public override Point2f Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                float x = reader.GetSingle();

                reader.Read();
                float y = reader.GetSingle();

                reader.Read(); // EndArray

                return new Point2f(x, y);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                float x = 0, y = 0;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    string prop = reader.GetString()!;
                    reader.Read();

                    if (prop.Equals("x", StringComparison.OrdinalIgnoreCase))
                        x = reader.GetSingle();
                    else if (prop.Equals("y", StringComparison.OrdinalIgnoreCase))
                        y = reader.GetSingle();
                }

                return new Point2f(x, y);
            }

            throw new JsonException("Invalid Point2f format.");
        }

        public override void Write(Utf8JsonWriter writer, Point2f value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
    public class Point3fConverter : JsonConverter<Point3f>
    {
        public override Point3f Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float x = 0, y = 0, z = 0;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Point3f(x, y, z);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string property = reader.GetString();
                    reader.Read();

                    switch (property.ToLower())
                    {
                        case "x":
                            x = reader.GetSingle();
                            break;
                        case "y":
                            y = reader.GetSingle();
                            break;
                        case "z":
                            z = reader.GetSingle();
                            break;
                    }
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Point3f value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("z", value.Z);
            writer.WriteEndObject();
        }
    }
    public class UnixMillisecondsDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long ms = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            long ms = new DateTimeOffset(value).ToUnixTimeMilliseconds();
            writer.WriteNumberValue(ms);
        }
    }
}
