using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class FragmentDistributionRequestedEventJsonConverter : JsonConverter<FragmentDistributionRequestedEvent>
    {
        public override FragmentDistributionRequestedEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;
            string? hash = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new FragmentDistributionRequestedEvent(
                        hash ?? throw new JsonException($"Property {nameof(FragmentDistributionObtainedEvent.Hash)} is not defined."),
                        eventId ?? throw new JsonException($"Property {nameof(FragmentDistributionObtainedEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(FragmentDistributionObtainedEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionObtainedEvent.Hash)))
                {
                    if (!(hash is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    hash = reader.GetString();
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, FragmentDistributionRequestedEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(FragmentDistributionRequestedEvent.EventId), value.EventId);
            writer.WriteString(nameof(FragmentDistributionRequestedEvent.Hash), value.Hash);
            writer.WriteEndObject();
        }
    }
}
