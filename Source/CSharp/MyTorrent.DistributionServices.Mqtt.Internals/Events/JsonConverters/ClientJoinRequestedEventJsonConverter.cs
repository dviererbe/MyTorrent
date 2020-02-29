using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class ClientJoinRequestedEventJsonConverter : JsonConverter<ClientJoinRequestedEvent>
    {
        public override ClientJoinRequestedEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            bool readFragmentSize = false;

            Guid? eventId = null;
            string? hashAlgorithm = null;
            long? fragmentSize = null;
            IDictionary<string, FileMetadata>? knownFileInfos = null;
            IDictionary<string, FragmentMetadata>? storedFragments = null;
            ISet<Uri>? endpoints = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ClientJoinRequestedEvent(
                        knownFileInfos ?? throw new JsonException($"Property {nameof(ClientJoinRequestedEvent.KnownFileInfos)} is not defined."),
                        storedFragments ?? throw new JsonException($"Property {nameof(ClientJoinRequestedEvent.StoredFragments)} is not defined."),
                        endpoints ?? throw new JsonException($"Property {nameof(ClientJoinRequestedEvent.Endpoints)} is not defined."),
                        hashAlgorithm ?? throw new JsonException($"Property {nameof(ClientJoinRequestedEvent.HashAlgorithm)} is not defined."),
                        fragmentSize,
                        eventId ?? throw new JsonException($"Property {nameof(ClientJoinRequestedEvent.EventId)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(ClientJoinRequestedEvent.EventId)))
                {
                    if (eventId.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    eventId = reader.GetGuid();
                }
                else if (propertyName.Equals(nameof(ClientJoinRequestedEvent.HashAlgorithm)))
                {
                    if (!(hashAlgorithm is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.String);
                    hashAlgorithm = reader.GetString();
                }
                else if (propertyName.Equals(nameof(ClientJoinRequestedEvent.FragmentSize)))
                {
                    if (readFragmentSize)
                        throw new JsonException("Property defined twice: " + propertyName);

                    readFragmentSize = true;

                    reader.ReadSkippingComments(commentHandling);
                    fragmentSize = reader.TokenType switch
                    {
                        JsonTokenType.Number => new long?(reader.GetInt64()),
                        JsonTokenType.Null => null,
                        _ => throw new JsonException($"Expected Token Type Number or Null. Actual: {reader.TokenType}")
                    };
                }
                else if (propertyName.Equals(nameof(ClientJoinRequestedEvent.KnownFileInfos)))
                {
                    if (!(knownFileInfos is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    knownFileInfos = JsonSerializer.Deserialize<IDictionary<string, FileMetadata>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientJoinRequestedEvent.StoredFragments)))
                {
                    if (!(storedFragments is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartObject);
                    storedFragments = JsonSerializer.Deserialize<IDictionary<string, FragmentMetadata>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientJoinRequestedEvent.Endpoints)))
                {
                    if (!(endpoints is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    endpoints = JsonSerializer.Deserialize<ISet<Uri>>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, ClientJoinRequestedEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(ClientJoinRequestedEvent.EventId), value.EventId);

            if (value.FragmentSize.HasValue)
            {
                writer.WriteNumber(nameof(ClientJoinRequestedEvent.FragmentSize), value.FragmentSize.Value);
            }
            else
            {
                if (!options.IgnoreNullValues)
                {
                    writer.WriteNull(nameof(ClientJoinRequestedEvent.FragmentSize));
                }
            }

            writer.WriteString(nameof(ClientJoinRequestedEvent.HashAlgorithm), value.HashAlgorithm);

            writer.WritePropertyName(nameof(ClientJoinRequestedEvent.KnownFileInfos));
            JsonSerializer.Serialize(writer, value.KnownFileInfos, options);

            writer.WritePropertyName(nameof(ClientJoinRequestedEvent.StoredFragments));
            JsonSerializer.Serialize(writer, value.StoredFragments, options);

            writer.WritePropertyName(nameof(ClientJoinRequestedEvent.Endpoints));
            JsonSerializer.Serialize(writer, value.Endpoints, options);

            writer.WriteEndObject();
        }
    }
}
