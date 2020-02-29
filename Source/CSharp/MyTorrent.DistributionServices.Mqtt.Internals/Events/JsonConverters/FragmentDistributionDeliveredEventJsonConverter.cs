using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class FragmentDistributionDeliveredEventJsonConverter : JsonConverter<FragmentDistributionDeliveredEvent>
    {
        public override FragmentDistributionDeliveredEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;
            string? hash = null;
            byte[]? data = null;
            ISet<string>? receivers = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new FragmentDistributionDeliveredEvent(
                        hash ?? throw new JsonException($"Property {nameof(FragmentDistributionDeliveredEvent.Hash)} is not defined."),
                        data ?? throw new JsonException($"Property {nameof(FragmentDistributionDeliveredEvent.Data)} is not defined."),
                        receivers ?? throw new JsonException($"Property {nameof(FragmentDistributionDeliveredEvent.Receivers)} is not defined."),
                        eventId ?? throw new JsonException($"Property {nameof(FragmentDistributionDeliveredEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(FragmentDistributionDeliveredEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionDeliveredEvent.Hash)))
                {
                    if (!(hash is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    hash = reader.GetString();
                }
                else if (propertyName.Equals(nameof(FragmentDistributionDeliveredEvent.Receivers)))
                {
                    if (!(receivers is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    receivers = JsonSerializer.Deserialize<ISet<string>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(FragmentDistributionDeliveredEvent.Data)))
                {
                    if (!(data is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    data = reader.GetBytesFromBase64();
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, FragmentDistributionDeliveredEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(FragmentDistributionDeliveredEvent.EventId), value.EventId);
            writer.WriteString(nameof(FragmentDistributionDeliveredEvent.Hash), value.Hash);
            writer.WritePropertyName(nameof(FragmentDistributionDeliveredEvent.Receivers));
            JsonSerializer.Serialize(writer, value.Receivers, options);
            writer.WriteBase64String(nameof(FragmentDistributionDeliveredEvent.Data), value.Data);
            writer.WriteEndObject();
        }
    }
}
