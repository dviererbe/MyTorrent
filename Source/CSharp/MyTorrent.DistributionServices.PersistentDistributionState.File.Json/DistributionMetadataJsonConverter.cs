using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    public class DistributionMetadataJsonConverter : JsonConverter<DistributionMetadata>
    {
        public override DistributionMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(DistributionMetadata))
                throw new NotSupportedException("Type to convert json input into is not supported.");

            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            while (reader.TokenType == JsonTokenType.Comment)
            {
                if (!reader.Read())
                    goto InputEndedUnexpectedError;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected Json Token StartObject. Actual: " + reader.TokenType);

            string? hashAlgorithm = null;
            long? fragmentSize = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new DistributionMetadata(
                        hashAlgorithm ?? throw new JsonException($"Property {nameof(DistributionMetadata.HashAlgorithm)} is not defined."),
                        fragmentSize ?? throw new JsonException($"Property {nameof(DistributionMetadata.FragmentSize)} is not defined."));

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected Json Token PropertyName. Actual: " + reader.TokenType);

                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(DistributionMetadata.HashAlgorithm)))
                {
                    if (!(hashAlgorithm is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    do
                    {
                        if (!reader.Read())
                            goto InputEndedUnexpectedError;
                    } while (reader.TokenType == JsonTokenType.Comment);

                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException("Expected Json Token String. Actual: " + reader.TokenType);

                    hashAlgorithm = reader.GetString();
                }
                else if (propertyName.Equals(nameof(DistributionMetadata.FragmentSize)))
                {
                    if (fragmentSize.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    do
                    {
                        if (!reader.Read())
                            goto InputEndedUnexpectedError;
                    }
                    while (reader.TokenType == JsonTokenType.Comment);

                    if (reader.TokenType != JsonTokenType.Number)
                        throw new JsonException("Expected Json Token Number. Actual: " + reader.TokenType);

                    fragmentSize = reader.GetInt64();
                }
                else
                {
                    reader.Skip();
                }
            }

            InputEndedUnexpectedError:
            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, DistributionMetadata value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(DistributionMetadata.HashAlgorithm), value.HashAlgorithm);
            writer.WriteNumber(nameof(DistributionMetadata.FragmentSize), value.FragmentSize);

            writer.WriteEndObject();
        }
    }
}
