// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Configuration;

/// <summary>
/// Strict JSON reading helpers for template manifests. Every read either produces a value of the
/// expected type or a named <see cref="InvalidTemplateManifest"/> error — malformed manifests never
/// fall through to default values.
/// </summary>
static class Json
{
    /// <summary>
    /// Gets the property names present on an object.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <returns>All property names.</returns>
    public static IEnumerable<string> PropertyNames(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().Select(p => p.Name)
            : [];

    /// <summary>
    /// Throws for any property on the element that is not in the known set.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="context">JSON path context for error messages.</param>
    /// <param name="known">The known property names.</param>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static void RejectUnknownProperties(JsonElement element, string context, params string[] known)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var unknown = PropertyNames(element)
            .Where(name => name != "$schema" && !known.Contains(name, StringComparer.Ordinal))
            .ToArray();
        if (unknown.Length > 0)
        {
            throw new InvalidTemplateManifest(
                $"{context}: unknown propert{(unknown.Length == 1 ? "y" : "ies")} '{string.Join("', '", unknown)}'. " +
                $"Known properties are: {string.Join(", ", known.Order(StringComparer.Ordinal))}.");
        }
    }

    /// <summary>
    /// Reads a string property, or null when absent.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The string value, or null when absent.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString() ?? throw new InvalidTemplateManifest($"Property '{name}' must be a string.")
            : null;

    /// <summary>
    /// Reads a boolean property, or null when absent.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The boolean value, or null when absent.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static bool? GetBool(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        throw new InvalidTemplateManifest($"Property '{name}' must be a boolean.");
    }

    /// <summary>
    /// Reads an integer property, or null when absent.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The integer value, or null when absent.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static int? GetInt(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        throw new InvalidTemplateManifest($"Property '{name}' must be an integer.");
    }

    /// <summary>
    /// Reads a string-array property, or empty when absent.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The string values.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyList<string> GetStringArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
        {
            throw new InvalidTemplateManifest($"Property '{name}' must be an array of strings.");
        }

        return [.. value.EnumerateArray().Select(item => item.GetString()!)];
    }

    /// <summary>
    /// Reads an object property as an enumerable of named properties, or empty when absent.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>Named property pairs.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IEnumerable<(string Name, JsonElement Value)> GetObjectProperties(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidTemplateManifest($"Property '{name}' must be an object.");
        }

        return value.EnumerateObject().Select(property => (property.Name, property.Value));
    }

    /// <summary>
    /// Reads a nested object property, or null when absent.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The object element, or null when absent.</returns>
    public static JsonElement? GetObject(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;

    /// <summary>
    /// Reads an array property's elements, or empty when absent.
    /// </summary>
    /// <param name="element">The object element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The array elements.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyList<JsonElement> GetArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidTemplateManifest($"Property '{name}' must be an array.");
        }

        return [.. value.EnumerateArray()];
    }
}
