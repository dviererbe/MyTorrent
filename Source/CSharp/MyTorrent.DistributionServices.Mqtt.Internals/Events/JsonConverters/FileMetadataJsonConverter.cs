using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class FileMetadataJsonConverter : JsonConverter<FileMetadata>
    {
        public override FileMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            long? size = null;
            IEnumerable<string>? fragmentSequence = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new FileMetadata(
                        size ?? throw new JsonException($"Property {nameof(FileMetadata.Size)} is not defined."),
                        fragmentSequence ?? throw new JsonException($"Property {nameof(FileMetadata.FragmentSequence)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(FileMetadata.Size)))
                {
                    if (size.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.Number);
                    size = reader.GetInt64();
                }
                else if (propertyName.Equals(nameof(FileMetadata.FragmentSequence)))
                {
                    if (!(fragmentSequence is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    fragmentSequence = JsonSerializer.Deserialize<IEnumerable<string>>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, FileMetadata value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber(nameof(FileMetadata.Size), value.Size);
            
            writer.WritePropertyName(nameof(FileMetadata.FragmentSequence));
            JsonSerializer.Serialize(writer, value.FragmentSequence, options);
            
            writer.WriteEndObject();
        }
    }
}
