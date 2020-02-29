using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class ClientJoinDeniedEventJsonConverter : JsonConverter<ClientJoinDeniedEvent>
    {
        public override ClientJoinDeniedEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            bool readReason = false;

            Guid? eventId = null;
            string? clientIdentifier = null;
            ClientJoinDeniedCode? reasonCode = null;
            string? reason = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ClientJoinDeniedEvent(
                        eventId: eventId ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.EventId)} is not defined."),
                        clientIdentifier: clientIdentifier ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.ClientIdentifier)} is not defined."),
                        reasonCode: reasonCode ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.ClientIdentifier)} is not defined."),
                        reason: reason);

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(ClientJoinDeniedEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(ClientJoinDeniedEvent.ClientIdentifier)))
                {
                    if (!(clientIdentifier is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    clientIdentifier = reader.GetString();
                }
                else if (propertyName.Equals(nameof(ClientJoinDeniedEvent.ReasonCode)))
                {
                    if (reasonCode.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.Number);
                    reasonCode = (ClientJoinDeniedCode)reader.GetInt32();

                    if (!Enum.IsDefined(typeof(ClientJoinDeniedCode), reasonCode.Value))
                        throw new JsonException(reasonCode.Value + " is an invalid enum value of " + nameof(ClientJoinDeniedCode));
                }
                else if (propertyName.Equals(nameof(ClientJoinDeniedEvent.Reason)))
                {
                    if (readReason)
                        throw new JsonException("Property defined twice: " + propertyName);

                    readReason = true;

                    reader.ReadSkippingComments(commentHandling);
                    reason = reader.TokenType switch
                    {
                        JsonTokenType.String => reader.GetString(),
                        JsonTokenType.Null => null,
                        _ => throw new JsonException($"Expected Token Type String or Null. Actual: {reader.TokenType}")
                    };
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, ClientJoinDeniedEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(ClientJoinDeniedEvent.EventId), value.EventId);
            writer.WriteString(nameof(ClientJoinDeniedEvent.ClientIdentifier), value.ClientIdentifier);
            writer.WriteNumber(nameof(ClientJoinDeniedEvent.ReasonCode), (int)value.ReasonCode);
            
            if (value.Reason is null)
            {
                if (!options.IgnoreNullValues)
                {
                    writer.WriteNull(nameof(ClientJoinDeniedEvent.Reason));
                }
            }
            else
            {
                writer.WriteString(nameof(ClientJoinDeniedEvent.Reason), value.Reason);
            }
            
            writer.WriteEndObject();
        }
    }
}
