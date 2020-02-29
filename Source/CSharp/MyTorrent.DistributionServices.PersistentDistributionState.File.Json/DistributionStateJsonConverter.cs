using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    public partial class DistributionState
    {
        public class DistributionStateJsonConverter : JsonConverter<DistributionState>
        {
            public override DistributionState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (typeToConvert != typeof(DistributionState))
                    throw new NotSupportedException("Type to convert json input into is not supported.");

                JsonCommentHandling commentHandling = options.ReadCommentHandling;

                while (reader.TokenType == JsonTokenType.Comment)
                {
                    if (!reader.Read())
                        goto InputEndedUnexpectedError;
                }

                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("Expected Json Token StartObject. Actual: " + reader.TokenType);

                DistributionMetadata? metadata = null;
                IEnumerable<IFragmentedFileInfo>? fileInfos = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.Comment)
                        continue;

                    if (reader.TokenType == JsonTokenType.EndObject)
                        return new DistributionState(
                            metadata ?? throw new JsonException($"Property {nameof(DistributionState.Metadata)} is not defined."),
                            fileInfos ?? throw new JsonException($"Property {nameof(DistributionState.FileInfos)} is not defined."));

                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException("Expected Json Token PropertyName. Actual: " + reader.TokenType);

                    string propertyName = reader.GetString();

                    if (propertyName.Equals(nameof(DistributionState.Metadata)))
                    {
                        if (!(metadata is null))
                            throw new JsonException("Property defined twice: " + propertyName);

                        do
                        {
                            if (!reader.Read())
                                goto InputEndedUnexpectedError;
                        }
                        while (reader.TokenType == JsonTokenType.Comment) ;

                        if (reader.TokenType != JsonTokenType.StartObject)
                            throw new JsonException("Expected Json Token StartObject. Actual: " + reader.TokenType);

                        metadata = JsonSerializer.Deserialize<DistributionMetadata>(ref reader, options);
                    }
                    else if (propertyName.Equals(nameof(DistributionState.FileInfos)))
                    {
                        if (!(fileInfos is null))
                            throw new JsonException("Property defined twice: " + propertyName);

                        do
                        {
                            if (!reader.Read())
                                goto InputEndedUnexpectedError;
                        }
                        while (reader.TokenType == JsonTokenType.Comment);

                        if (reader.TokenType != JsonTokenType.StartArray)
                            throw new JsonException("Expected Json Token StartArray. Actual: " + reader.TokenType);

                        fileInfos = JsonSerializer.Deserialize<IEnumerable<IFragmentedFileInfo>>(ref reader, options);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                InputEndedUnexpectedError:
                throw new JsonException("Json input ended unexpectedly.");
            }

            public override void Write(Utf8JsonWriter writer, DistributionState value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(DistributionState.Metadata));
                JsonSerializer.Serialize(writer, value.Metadata, options);

                writer.WritePropertyName(nameof(DistributionState.FileInfos));
                JsonSerializer.Serialize(writer, value.FileInfos.Values, options);

                writer.WriteEndObject();
            }
        }
    }
}
