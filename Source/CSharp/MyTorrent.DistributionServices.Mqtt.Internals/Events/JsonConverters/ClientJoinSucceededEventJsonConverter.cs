using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class ClientJoinSucceededEventJsonConverter : JsonConverter<ClientJoinSucceededEvent>
    {
        public override ClientJoinSucceededEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ClientJoinSucceededEvent(eventId ?? throw new JsonException($"Property {nameof(ClientJoinSucceededEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(ClientJoinSucceededEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, ClientJoinSucceededEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(ClientJoinSucceededEvent.EventId), value.EventId);
            writer.WriteEndObject();
        }
    }
}
