using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class FileInfoPublishedEventJsonConverter : JsonConverter<FileInfoPublishedEvent>
    {
        public override FileInfoPublishedEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;
            string? hash = null;
            long? size = null;
            IEnumerable<string>? fragmentSequence = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new FileInfoPublishedEvent(
                        hash ?? throw new JsonException($"Property {nameof(FileInfoPublishedEvent.Hash)} is not defined."),
                        size ?? throw new JsonException($"Property {nameof(FileInfoPublishedEvent.Size)} is not defined."),
                        fragmentSequence ?? throw new JsonException($"Property {nameof(FileInfoPublishedEvent.FragmentSequence)} is not defined."),
                        eventId ?? throw new JsonException($"Property {nameof(FileInfoPublishedEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(FileInfoPublishedEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(FileInfoPublishedEvent.Hash)))
                {
                    if (!(hash is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    hash = reader.GetString();
                }
                else if (propertyName.Equals(nameof(FileInfoPublishedEvent.Size)))
                {
                    if (size.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.Number);
                    size = reader.GetInt64();
                }
                else if (propertyName.Equals(nameof(FileInfoPublishedEvent.FragmentSequence)))
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

        public override void Write(Utf8JsonWriter writer, FileInfoPublishedEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(FileInfoPublishedEvent.EventId), value.EventId);
            writer.WriteString(nameof(FileInfoPublishedEvent.Hash), value.Hash);
            writer.WriteNumber(nameof(FileInfoPublishedEvent.Size), value.Size);
            writer.WritePropertyName(nameof(FileInfoPublishedEvent.FragmentSequence));
            JsonSerializer.Serialize(writer, value.FragmentSequence, options);
            writer.WriteEndObject();
        }
    }
}
