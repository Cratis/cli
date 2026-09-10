// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Resolves effective event store and namespace values from an explicitly passed option value,
/// the active context, and the built-in defaults — in that order.
/// An explicitly passed value always wins, even when it equals the built-in default name.
/// </summary>
public static class EventStoreResolution
{
    /// <summary>
    /// Resolves the effective event store name.
    /// </summary>
    /// <param name="option">The event store name passed as an option, or <see langword="null"/> when not passed.</param>
    /// <returns>The resolved event store name and the <see cref="SettingSource"/> it came from.</returns>
    public static ResolvedSetting ResolveEventStore(string? option) =>
        Resolve(option, static ctx => ctx.EventStore, CliDefaults.DefaultEventStoreName);

    /// <summary>
    /// Resolves the effective namespace name.
    /// </summary>
    /// <param name="option">The namespace name passed as an option, or <see langword="null"/> when not passed.</param>
    /// <returns>The resolved namespace name and the <see cref="SettingSource"/> it came from.</returns>
    public static ResolvedSetting ResolveNamespace(string? option) =>
        Resolve(option, static ctx => ctx.Namespace, CliDefaults.DefaultNamespaceName);

    static ResolvedSetting Resolve(string? option, Func<CliContext, string?> contextValue, string defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(option))
        {
            return new(option, SettingSource.Option);
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        var fromContext = contextValue(ctx);
        if (!string.IsNullOrWhiteSpace(fromContext))
        {
            return new(fromContext, SettingSource.Context);
        }

        return new(defaultValue, SettingSource.Default);
    }
}
