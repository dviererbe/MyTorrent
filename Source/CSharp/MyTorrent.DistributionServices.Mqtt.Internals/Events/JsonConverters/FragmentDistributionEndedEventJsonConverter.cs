using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class FragmentDistributionEndedEventJsonConverter : JsonConverter<FragmentDistributionEndedEvent>
    {
        public override FragmentDistributionEndedEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;
            string? hash = null;
            long? size = null;
            ISet<string>? receivers = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new FragmentDistributionEndedEvent(
                        hash ?? throw new JsonException($"Property {nameof(FragmentDistributionEndedEvent.Hash)} is not defined."),
                        size ?? throw new JsonException($"Property {nameof(FragmentDistributionEndedEvent.Size)} is not defined."),
                        receivers ?? throw new JsonException($"Property {nameof(FragmentDistributionEndedEvent.Receivers)} is not defined."),
                        eventId ?? throw new JsonException($"Property {nameof(FragmentDistributionEndedEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(FragmentDistributionEndedEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionEndedEvent.Hash)))
                {
                    if (!(hash is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    hash = reader.GetString();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionEndedEvent.Size)))
                {
                    if (size.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.Number);
                    size = reader.GetInt64();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionEndedEvent.Receivers)))
                {
                    if (!(receivers is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    receivers = JsonSerializer.Deserialize<ISet<string>>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, FragmentDistributionEndedEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(FragmentDistributionEndedEvent.EventId), value.EventId);
            writer.WriteString(nameof(FragmentDistributionEndedEvent.Hash), value.Hash);
            writer.WriteNumber(nameof(FragmentDistributionEndedEvent.Size), value.Size);
            writer.WritePropertyName(nameof(FragmentDistributionEndedEvent.Receivers));
            JsonSerializer.Serialize(writer, value.Receivers, options);
            writer.WriteEndObject();
        }
    }
}
