using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class ClientRegisteredEventJsonConverter : JsonConverter<ClientRegisteredEvent>
    {
        public override ClientRegisteredEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;
            string? clientIdentifier = null;
            ClientMetadata? info = null;
            IDictionary<string, FileMetadata>? addedFiles = null;
            IDictionary<string, FragmentMetadata>? addedFragments = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ClientRegisteredEvent(
                        clientIdentifier ?? throw new JsonException($"Property {nameof(ClientRegisteredEvent.ClientIdentifier)} is not defined."),
                        info ?? throw new JsonException($"Property {nameof(ClientRegisteredEvent.Info)} is not defined."),
                        addedFiles ?? throw new JsonException($"Property {nameof(ClientRegisteredEvent.AddedFiles)} is not defined."),
                        addedFragments ?? throw new JsonException($"Property {nameof(ClientRegisteredEvent.AddedFragments)} is not defined."),
                        eventId ?? throw new JsonException($"Property {nameof(ClientRegisteredEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(ClientRegisteredEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(ClientRegisteredEvent.ClientIdentifier)))
                {
                    if (clientIdentifier != null)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    clientIdentifier = reader.GetString();
                }
                else if (propertyName.Equals(nameof(ClientRegisteredEvent.Info)))
                {
                    if (!(info is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    info = JsonSerializer.Deserialize<ClientMetadata>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientRegisteredEvent.AddedFiles)))
                {
                    if (!(addedFiles is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    addedFiles = JsonSerializer.Deserialize<IDictionary<string, FileMetadata>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientRegisteredEvent.AddedFragments)))
                {
                    if (!(addedFragments is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    addedFragments = JsonSerializer.Deserialize<IDictionary<string, FragmentMetadata>>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, ClientRegisteredEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(ClientRegisteredEvent.EventId), value.EventId);

            writer.WriteString(nameof(ClientRegisteredEvent.ClientIdentifier), value.ClientIdentifier);

            writer.WritePropertyName(nameof(ClientRegisteredEvent.Info));
            JsonSerializer.Serialize(writer, value.Info, options);

            writer.WritePropertyName(nameof(ClientRegisteredEvent.AddedFiles));
            JsonSerializer.Serialize(writer, value.AddedFiles, options);

            writer.WritePropertyName(nameof(ClientRegisteredEvent.AddedFragments));
            JsonSerializer.Serialize(writer, value.AddedFragments, options);

            writer.WriteEndObject();
        }
    }
}
