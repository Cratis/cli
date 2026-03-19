// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Cli.Commands.Chronicle.Json;

/// <summary>
/// Converts <see cref="SerializableDateTimeOffset"/> to and from an ISO 8601 string.
/// Without this converter the default serializer emits <c>{"ticks":...,"offsetMinutes":...}</c>
/// which is unreadable for humans and AI agents alike.
/// </summary>
public class SerializableDateTimeOffsetJsonConverter : JsonConverter<SerializableDateTimeOffset>
{
    /// <inheritdoc/>
    public override SerializableDateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var iso = reader.GetString() ?? string.Empty;
        var dto = DateTimeOffset.Parse(iso);
        return new SerializableDateTimeOffset
        {
            Ticks = dto.Ticks,
            OffsetMinutes = dto.Offset.TotalMinutes
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SerializableDateTimeOffset value, JsonSerializerOptions options)
    {
        var dto = new DateTimeOffset(value.Ticks, TimeSpan.FromMinutes(value.OffsetMinutes));
        writer.WriteStringValue(dto.ToString("o"));
    }
}
