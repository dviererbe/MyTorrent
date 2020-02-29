using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices
{
    public class FragmentedFileInfoJsonConverter : JsonConverter<IFragmentedFileInfo>
    {
        public override IFragmentedFileInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!typeToConvert.Equals(typeof(IFragmentedFileInfo)) && !typeToConvert.Equals(typeof(FragmentedFileInfo)))
                throw new NotSupportedException("Type to convert json input into is not supported.");

            JsonCommentHandling commentHandling = options.ReadCommentHandling;

            while (reader.TokenType == JsonTokenType.Comment)
            {
                if (!reader.Read())
                    goto InputEndedUnexpectedError;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected Json Token StartObject. Actual: " + reader.TokenType);
            
            string? hash = null;
            long? size = null;
            IEnumerable<string>? fragmentSequence = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    continue;

                if (reader.TokenType == JsonTokenType.EndObject)
                    return new FragmentedFileInfo(
                        fileHash: hash ?? throw new JsonException($"Property {nameof(FragmentedFileInfo.Hash)} is not defined."),
                        fileSize: size ?? throw new JsonException($"Property {nameof(FragmentedFileInfo.Size)} is not defined."),
                        fragmentSequence ?? throw new JsonException($"Property {nameof(FragmentedFileInfo.FragmentSequence)} is not defined."));

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected Json Token PropertyName. Actual: " + reader.TokenType);
                
                string propertyName = reader.GetString();

                if (propertyName.Equals(nameof(FragmentedFileInfo.Hash)))
                {
                    if (!(hash is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    while (reader.TokenType == JsonTokenType.Comment)
                    {
                        if (!reader.Read())
                            goto InputEndedUnexpectedError;
                    }

                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException("Expected Json Token String. Actual: " + reader.TokenType);

                    hash = reader.GetString();
                }
                else if (propertyName.Equals(nameof(FragmentedFileInfo.Size)))
                {
                    if (size.HasValue)
                        throw new JsonException("Property defined twice: " + propertyName);

                    while (reader.TokenType == JsonTokenType.Comment)
                    {
                        if (!reader.Read())
                            goto InputEndedUnexpectedError;
                    }

                    if (reader.TokenType != JsonTokenType.Number)
                        throw new JsonException("Expected Json Token Number. Actual: " + reader.TokenType);

                    size = reader.GetInt64();
                }
                else if (propertyName.Equals(nameof(FragmentedFileInfo.FragmentSequence)))
                {
                    if (!(fragmentSequence is null))
                        throw new JsonException("Property defined twice: " + propertyName);

                    while (reader.TokenType == JsonTokenType.Comment)
                    {
                        if (!reader.Read())
                            goto InputEndedUnexpectedError;
                    }

                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected Json Token StartArray. Actual: " + reader.TokenType);

                    fragmentSequence = JsonSerializer.Deserialize<IEnumerable<string>>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }

            InputEndedUnexpectedError:
            throw new JsonException("Json input ended unexpectedly.");
        }

        public override void Write(Utf8JsonWriter writer, IFragmentedFileInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(IFragmentedFileInfo.Hash), value.Hash);
            writer.WriteNumber(nameof(IFragmentedFileInfo.Size), value.Size);

            writer.WritePropertyName(nameof(IFragmentedFileInfo.FragmentSequence));
            JsonSerializer.Serialize(writer, value.FragmentSequence, options);

            writer.WriteEndObject();
        }
    }
}
