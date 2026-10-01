// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.given;

namespace Cratis.Cli.for_EventStoreInterceptor;

[Collection(CliSpecsCollection.Name)]
public class when_diagnosing_all_event_stores : a_temp_config_directory
{
    InteractiveInterceptor _interceptor;

    void Establish()
    {
        new CliConfiguration { ActiveContext = "default", Contexts = new Dictionary<string, CliContext> { ["default"] = new() { Server = "chronicle://localhost:35000" } } }.Save();
        _interceptor = new InteractiveInterceptor();
    }

    void Because() => _interceptor.Intercept(
        new CommandContext([], Substitute.For<IRemainingArguments>(), "diagnose", null),
        new DiagnoseSettings { AllEventStores = true });

    [Fact] void should_bypass_the_interactive_selection_path() => _interceptor.InteractiveChecks.ShouldEqual(0);
    [Fact] void should_not_persist_a_default_event_store() => string.IsNullOrEmpty(CliConfiguration.Load().GetCurrentContext().EventStore).ShouldBeTrue();

    /// <summary>
    /// Detects entry to the default-store selection path in an interactive terminal.
    /// </summary>
    public sealed class InteractiveInterceptor : EventStoreInterceptor
    {
        /// <summary>
        /// Gets the number of times interactive selection was considered.
        /// </summary>
        public int InteractiveChecks { get; private set; }

        /// <inheritdoc/>
        protected override bool IsInteractive()
        {
            InteractiveChecks++;
            return true;
        }
    }
}
