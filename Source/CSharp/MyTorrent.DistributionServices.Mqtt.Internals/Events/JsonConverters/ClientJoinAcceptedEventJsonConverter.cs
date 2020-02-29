using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class ClientJoinAcceptedEventJsonConverter : JsonConverter<ClientJoinAcceptedEvent>
    {
        public override ClientJoinAcceptedEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            Guid? eventId = null;
            string? clientIdentifier = null;
            long? fragmentSize = null;
            IDictionary<string, FileMetadata>? addFileInfos = null;
            IDictionary<string, FragmentMetadata>? addFragmentInfos = null;
            ISet<string>? removeFileInfos = null;
            ISet<string>? removeFragments = null;
            IDictionary<string, ClientMetadata>? clients = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ClientJoinAcceptedEvent(
                        eventId: eventId ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.EventId)} is not defined."),
                        clientIdentifier: clientIdentifier ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.ClientIdentifier)} is not defined."),
                        fragmentSize: fragmentSize ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.FragmentSize)} is not defined."),
                        addFileInfos: addFileInfos ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.AddFileInfos)} is not defined."),
                        addFragmentInfos: addFragmentInfos ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.AddFragmentInfos)} is not defined."),
                        removeFileInfos: removeFileInfos ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.RemoveFileInfos)} is not defined."),
                        removeFragmentInfos: removeFragments ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.RemoveFragmentInfos)} is not defined."),
                        clients: clients ?? throw new JsonException($"Property {nameof(ClientJoinAcceptedEvent.Clients)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.ClientIdentifier)))
                {
                    if (!(clientIdentifier is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    clientIdentifier = reader.GetString();
                }
                else if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.FragmentSize)))
                {
                    if (fragmentSize.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.Number);
                    fragmentSize = reader.GetInt64();
                }
                else if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.AddFileInfos)))
                {
                    if (!(addFileInfos is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    addFileInfos = JsonSerializer.Deserialize<IDictionary<string, FileMetadata>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.AddFragmentInfos)))
                {
                    if (addFragmentInfos != null)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    addFragmentInfos = JsonSerializer.Deserialize<IDictionary<string, FragmentMetadata>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.RemoveFileInfos)))
                {
                    if (!(removeFileInfos is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    removeFileInfos = JsonSerializer.Deserialize<ISet<string>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.RemoveFragmentInfos)))
                {
                    if (!(removeFragments is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    removeFragments = JsonSerializer.Deserialize<ISet<string>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientJoinAcceptedEvent.Clients)))
                {
                    if (!(clients is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    clients = JsonSerializer.Deserialize<IDictionary<string, ClientMetadata>>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, ClientJoinAcceptedEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(ClientJoinAcceptedEvent.EventId), value.EventId);
            writer.WriteString(nameof(ClientJoinAcceptedEvent.ClientIdentifier), value.ClientIdentifier);
            writer.WriteNumber(nameof(ClientJoinAcceptedEvent.FragmentSize), value.FragmentSize);
            writer.WritePropertyName(nameof(ClientJoinAcceptedEvent.AddFileInfos));
            JsonSerializer.Serialize(writer, value.AddFileInfos, options);
            writer.WritePropertyName(nameof(ClientJoinAcceptedEvent.AddFragmentInfos));
            JsonSerializer.Serialize(writer, value.AddFragmentInfos, options);
            writer.WritePropertyName(nameof(ClientJoinAcceptedEvent.RemoveFileInfos));
            JsonSerializer.Serialize(writer, value.RemoveFileInfos, options);
            writer.WritePropertyName(nameof(ClientJoinAcceptedEvent.RemoveFragmentInfos));
            JsonSerializer.Serialize(writer, value.RemoveFragmentInfos, options);
            writer.WritePropertyName(nameof(ClientJoinAcceptedEvent.Clients));
            JsonSerializer.Serialize(writer, value.Clients, options);
            writer.WriteEndObject();
        }
    }
}
