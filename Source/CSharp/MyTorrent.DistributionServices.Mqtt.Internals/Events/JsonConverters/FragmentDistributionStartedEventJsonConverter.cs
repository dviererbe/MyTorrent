using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class FragmentDistributionStartedEventJsonConverter : JsonConverter<FragmentDistributionStartedEvent>
    {
        public override FragmentDistributionStartedEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;
            string? hash = null;
            long? size = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new FragmentDistributionStartedEvent(
                        hash ?? throw new JsonException($"Property {nameof(FragmentDistributionStartedEvent.Hash)} is not defined."),
                        size ?? throw new JsonException($"Property {nameof(FragmentDistributionStartedEvent.Size)} is not defined."),
                        eventId ?? throw new JsonException($"Property {nameof(FragmentDistributionStartedEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(FragmentDistributionStartedEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionStartedEvent.Hash)))
                {
                    if (!(hash is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    hash = reader.GetString();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionStartedEvent.Size)))
                {
                    if (size.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.Number);
                    size = reader.GetInt64();
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, FragmentDistributionStartedEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(FragmentDistributionStartedEvent.EventId), value.EventId);
            writer.WriteString(nameof(FragmentDistributionStartedEvent.Hash), value.Hash);
            writer.WriteNumber(nameof(FragmentDistributionStartedEvent.Size), value.Size);
            writer.WriteEndObject();
        }
    }
}
