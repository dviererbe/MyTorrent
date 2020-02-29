using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events.JsonConverters
{
    public class ClientMetadataJsonConverter : JsonConverter<ClientMetadata>
    {
        public override ClientMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            reader.SkipComments(commentHandling);
            reader.ExpectJsonToken(JsonTokenType.StartObject);

            ISet<Uri>? endpoints = null;
            ISet<string>? storedFragments = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ClientMetadata(
                        endpoints ?? throw new JsonException($"Property {nameof(ClientMetadata.Endpoints)} is not defined."),
                        storedFragments ?? throw new JsonException($"Property {nameof(ClientMetadata.StoredFragments)} is not defined."));

                reader.ExpectJsonToken(JsonTokenType.PropertyName);
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(ClientMetadata.Endpoints)))
                {
                    if (!(endpoints is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    endpoints = JsonSerializer.Deserialize<ISet<Uri>>(ref reader, options);
                }
                else if (propertyName.Equals(nameof(ClientMetadata.StoredFragments)))
                {
                    if (!(storedFragments is null))
                        throw new JsonException("Property defined twice: " + propertyName);
                        
                    reader.ReadSkippingComments(commentHandling);
                    reader.ExpectJsonToken(JsonTokenType.StartArray);
                    storedFragments = JsonSerializer.Deserialize<ISet<string>>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, ClientMetadata value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName(nameof(ClientMetadata.Endpoints));
            JsonSerializer.Serialize(writer, value.Endpoints, options);
            
            writer.WritePropertyName(nameof(ClientMetadata.StoredFragments));
            JsonSerializer.Serialize(writer, value.StoredFragments, options);
            
            writer.WriteEndObject();
        }
    }
}
