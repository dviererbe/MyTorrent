using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MyTorrent.DistributionServices.Events
{
    internal static class Utf8JsonReaderHelperExtensions
    {
        internal static void SkipComments(this ref Utf8JsonReader reader, JsonCommentHandling readCommentHandling)
        {
            //read first comment
            if (reader.TokenType == JsonTokenType.Comment)
            {
                if (readCommentHandling == JsonCommentHandling.Disallow)
                    throw new JsonException("Comment found within json input. Comments are specified as not allowed.");
            }
            else
            {
                return;
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.Comment);
        }

        internal static bool ReadSkippingComments(this ref Utf8JsonReader reader, JsonCommentHandling readCommentHandling)
        {
            if (!reader.Read())
                return false;

            if (reader.TokenType == JsonTokenType.Comment)
            {
                if (readCommentHandling == JsonCommentHandling.Disallow)
                    throw new JsonException("Comment found within json input. Comments are specified as not allowed.");
            }
            else
            {
                return true;
            }

            bool read;
            while (read = reader.Read() && reader.TokenType == JsonTokenType.Comment) ;

            return read;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ExpectJsonToken(this ref Utf8JsonReader reader, JsonTokenType expected)
        {
            if (reader.TokenType != expected)
                throw new JsonException($"Expected Token Type {expected}. Actual: {reader.TokenType}");
        }
    }
}
