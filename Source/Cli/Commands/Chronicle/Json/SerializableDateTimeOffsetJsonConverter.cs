// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Cli.Commands.Chronicle.Json;

/// <summary>
/// Converts <see cref="SerializableDateTimeOffset"/> to and from an ISO 8601 string.
/// </summary>
public class SerializableDateTimeOffsetJsonConverter : JsonConverter<SerializableDateTimeOffset>
{
    /// <inheritdoc/>
    public override SerializableDateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var iso = reader.GetString() ?? string.Empty;
        return new SerializableDateTimeOffset { Value = iso };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SerializableDateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
