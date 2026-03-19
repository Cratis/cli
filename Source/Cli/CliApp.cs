// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chat;
using Cratis.Cli.Commands.Chronicle;
using Cratis.Cli.Commands.Chronicle.Applications;
using Cratis.Cli.Commands.Chronicle.Auth;
using Cratis.Cli.Commands.Chronicle.Diagnose;
using Cratis.Cli.Commands.Chronicle.Events;
using Cratis.Cli.Commands.Chronicle.EventStores;
using Cratis.Cli.Commands.Chronicle.EventTypes;
using Cratis.Cli.Commands.Chronicle.FailedPartitions;
using Cratis.Cli.Commands.Chronicle.Identities;
using Cratis.Cli.Commands.Chronicle.Namespaces;
using Cratis.Cli.Commands.Chronicle.Observers;
using Cratis.Cli.Commands.Chronicle.Projections;
using Cratis.Cli.Commands.Chronicle.ReadModels;
using Cratis.Cli.Commands.Chronicle.Recommendations;
using Cratis.Cli.Commands.Chronicle.Users;
using Cratis.Cli.Commands.Completions;
using Cratis.Cli.Commands.Config;
using Cratis.Cli.Commands.Context;
using Cratis.Cli.Commands.Init;
using Cratis.Cli.Commands.LlmContext;
using Cratis.Cli.Commands.Version;

namespace Cratis.Cli;

/// <summary>
/// Factory for creating a fully configured Cratis CLI <see cref="CommandApp"/>.
/// </summary>
public static class CliApp
{
    /// <summary>
    /// Creates a new <see cref="CommandApp"/> with all Cratis CLI commands registered.
    /// </summary>
    /// <returns>A configured <see cref="CommandApp"/> ready to run.</returns>
    public static CommandApp Create()
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("cratis");
            config.SetApplicationVersion(typeof(CliApp).Assembly.GetName().Version?.ToString() ?? "0.0.0");
            config.SetInterceptor(new EventStoreInterceptor());

            config.AddBranch("chronicle", chronicle =>
            {
                chronicle.SetDescription("Commands for interacting with Cratis Chronicle");

                chronicle.AddBranch("event-stores", eventStores =>
                {
                    eventStores.SetDescription("Manage event stores");
                    eventStores.AddCommand<ListEventStoresCommand>("list")
                        .WithDescription("List all event stores")
                        .WithExample("chronicle", "event-stores", "list");
                });

                chronicle.AddBranch("namespaces", namespaces =>
                {
                    namespaces.SetDescription("Manage namespaces within an event store");
                    namespaces.AddCommand<ListNamespacesCommand>("list")
                        .WithDescription("List namespaces in an event store")
                        .WithExample("chronicle", "namespaces", "list")
                        .WithExample("chronicle", "namespaces", "list", "-e", "MyStore");
                });

                chronicle.AddBranch("event-types", eventTypes =>
                {
                    eventTypes.SetDescription("Manage event types");
                    eventTypes.AddCommand<ListEventTypesCommand>("list")
                        .WithDescription("List registered event types")
                        .WithExample("chronicle", "event-types", "list");
                    eventTypes.AddCommand<ShowEventTypeCommand>("show")
                        .WithDescription("Show an event type registration with its JSON schema")
                        .WithExample("chronicle", "event-types", "show", "UserRegistered")
                        .WithExample("chronicle", "event-types", "show", "UserRegistered+1", "-o", "json");
                });

                chronicle.AddBranch("events", events =>
                {
                    events.SetDescription("Query and inspect events");
                    events.AddCommand<GetEventsCommand>("get")
                        .WithDescription("Get events from an event sequence")
                        .WithExample("chronicle", "events", "get", "-o", "plain")
                        .WithExample("chronicle", "events", "get", "--from", "100", "--to", "200")
                        .WithExample("chronicle", "events", "get", "--event-type", "UserRegistered");
                    events.AddCommand<CountEventsCommand>("tail")
                        .WithDescription("Get the highest used sequence number (tail). Not a total count — gaps may exist in the sequence.")
                        .WithExample("chronicle", "events", "tail");
                    events.AddCommand<HasEventsCommand>("has")
                        .WithDescription("Check if events exist for an event source ID")
                        .WithExample("chronicle", "events", "has", "abc-123");
                });

                chronicle.AddBranch("observers", observers =>
                {
                    observers.SetDescription("Manage observers (reactors, reducers, projections)");
                    observers.AddCommand<ListObserversCommand>("list")
                        .WithDescription("List observers")
                        .WithExample("chronicle", "observers", "list")
                        .WithExample("chronicle", "observers", "list", "--type", "reactor");
                    observers.AddCommand<ShowObserverCommand>("show")
                        .WithDescription("Show detailed information about a specific observer")
                        .WithExample("chronicle", "observers", "show", "550e8400-e29b-41d4-a716-446655440000");
                    observers.AddCommand<ReplayObserverCommand>("replay")
                        .WithDescription("Replay an observer from the beginning")
                        .WithExample("chronicle", "observers", "replay", "550e8400-e29b-41d4-a716-446655440000");
                    observers.AddCommand<ReplayPartitionCommand>("replay-partition")
                        .WithDescription("Replay a specific partition of an observer")
                        .WithExample("chronicle", "observers", "replay-partition", "550e8400-e29b-41d4-a716-446655440000", "my-partition");
                    observers.AddCommand<RetryPartitionCommand>("retry-partition")
                        .WithDescription("Retry a failed partition")
                        .WithExample("chronicle", "observers", "retry-partition", "550e8400-e29b-41d4-a716-446655440000", "my-partition");
                });

                chronicle.AddBranch("failed-partitions", failedPartitions =>
                {
                    failedPartitions.SetDescription("Inspect failed observer partitions");
                    failedPartitions.AddCommand<ListFailedPartitionsCommand>("list")
                        .WithDescription("List failed partitions")
                        .WithExample("chronicle", "failed-partitions", "list")
                        .WithExample("chronicle", "failed-partitions", "list", "--observer", "550e8400-e29b-41d4-a716-446655440000");
                    failedPartitions.AddCommand<ShowFailedPartitionCommand>("show")
                        .WithDescription("Show detailed information about a specific failed partition")
                        .WithExample("chronicle", "failed-partitions", "show", "550e8400-e29b-41d4-a716-446655440000", "my-partition");
                });

                chronicle.AddBranch("recommendations", recommendations =>
                {
                    recommendations.SetDescription("Manage system recommendations");
                    recommendations.AddCommand<ListRecommendationsCommand>("list")
                        .WithDescription("List recommendations")
                        .WithExample("chronicle", "recommendations", "list");
                    recommendations.AddCommand<PerformRecommendationCommand>("perform")
                        .WithDescription("Perform a recommendation")
                        .WithExample("chronicle", "recommendations", "perform", "550e8400-e29b-41d4-a716-446655440000");
                    recommendations.AddCommand<IgnoreRecommendationCommand>("ignore")
                        .WithDescription("Ignore a recommendation")
                        .WithExample("chronicle", "recommendations", "ignore", "550e8400-e29b-41d4-a716-446655440000");
                });

                chronicle.AddBranch("identities", identities =>
                {
                    identities.SetDescription("Inspect identities");
                    identities.AddCommand<ListIdentitiesCommand>("list")
                        .WithDescription("List known identities")
                        .WithExample("chronicle", "identities", "list", "-o", "plain");
                });

                chronicle.AddBranch("projections", projections =>
                {
                    projections.SetDescription("Manage projections");
                    projections.AddCommand<ListProjectionsCommand>("list")
                        .WithDescription("List projection definitions")
                        .WithExample("chronicle", "projections", "list");
                    projections.AddCommand<ShowProjectionCommand>("show")
                        .WithDescription("Show a projection declaration")
                        .WithExample("chronicle", "projections", "show", "MyProjection", "-o", "json");
                });

                chronicle.AddBranch("read-models", readModels =>
                {
                    readModels.SetDescription("Inspect read model data");
                    readModels.AddCommand<ListReadModelsCommand>("list")
                        .WithDescription("List read model definitions")
                        .WithExample("chronicle", "read-models", "list");
                    readModels.AddCommand<GetReadModelInstancesCommand>("instances")
                        .WithDescription("List read model instances")
                        .WithExample("chronicle", "read-models", "instances", "MyReadModel")
                        .WithExample("chronicle", "read-models", "instances", "MyReadModel", "--page", "2");
                    readModels.AddCommand<GetReadModelByKeyCommand>("get")
                        .WithDescription("Get a single read model instance by key")
                        .WithExample("chronicle", "read-models", "get", "MyReadModel", "abc-123");
                    readModels.AddCommand<GetReadModelOccurrencesCommand>("occurrences")
                        .WithDescription("List read model occurrences (replay history)")
                        .WithExample("chronicle", "read-models", "occurrences", "MyReadModelType");
                    readModels.AddCommand<GetReadModelSnapshotsCommand>("snapshots")
                        .WithDescription("Get snapshots for a read model instance by key")
                        .WithExample("chronicle", "read-models", "snapshots", "MyReadModel", "abc-123");
                });

                chronicle.AddBranch("auth", auth =>
                {
                    auth.SetDescription("Authentication management");
                    auth.AddCommand<AuthStatusCommand>("status")
                        .WithDescription("Show current authentication status")
                        .WithExample("chronicle", "auth", "status");
                });

                chronicle.AddCommand<LoginCommand>("login")
                    .WithDescription("Log in as a user via the password grant flow")
                    .WithExample("chronicle", "login", "admin");

                chronicle.AddCommand<LogoutCommand>("logout")
                    .WithDescription("Clear the cached login session")
                    .WithExample("chronicle", "logout");

                chronicle.AddBranch("users", users =>
                {
                    users.SetDescription("Manage Chronicle users");
                    users.AddCommand<ListUsersCommand>("list")
                        .WithDescription("List all users")
                        .WithExample("chronicle", "users", "list");
                    users.AddCommand<AddUserCommand>("add")
                        .WithDescription("Add a new user")
                        .WithExample("chronicle", "users", "add", "alice", "alice@example.com", "P@ssw0rd!");
                    users.AddCommand<RemoveUserCommand>("remove")
                        .WithDescription("Remove a user")
                        .WithExample("chronicle", "users", "remove", "550e8400-e29b-41d4-a716-446655440000");
                });

                chronicle.AddCommand<DiagnoseCommand>("diagnose")
                    .WithDescription("Run a health check against the Chronicle server and show a diagnostic report")
                    .WithExample("chronicle", "diagnose")
                    .WithExample("chronicle", "diagnose", "-o", "json")
                    .WithExample("chronicle", "diagnose", "--watch")
                    .WithExample("chronicle", "diagnose", "--watch", "--interval", "2");

                chronicle.AddBranch("applications", applications =>
                {
                    applications.SetDescription("Manage OAuth client applications");
                    applications.AddCommand<ListApplicationsCommand>("list")
                        .WithDescription("List all applications")
                        .WithExample("chronicle", "applications", "list");
                    applications.AddCommand<AddApplicationCommand>("add")
                        .WithDescription("Add a new application")
                        .WithExample("chronicle", "applications", "add", "my-app", "my-secret");
                    applications.AddCommand<RemoveApplicationCommand>("remove")
                        .WithDescription("Remove an application")
                        .WithExample("chronicle", "applications", "remove", "550e8400-e29b-41d4-a716-446655440000");
                    applications.AddCommand<RotateSecretCommand>("rotate-secret")
                        .WithDescription("Rotate an application's client secret")
                        .WithExample("chronicle", "applications", "rotate-secret", "550e8400-e29b-41d4-a716-446655440000", "new-secret");
                });
            });

            config.AddBranch("context", ctx =>
            {
                ctx.SetDescription("Manage named connection contexts");
                ctx.AddCommand<ListContextsCommand>("list")
                    .WithDescription("List all contexts")
                    .WithExample("context", "list");
                ctx.AddCommand<CreateContextCommand>("create")
                    .WithDescription("Create a new context")
                    .WithExample("context", "create", "dev", "--server", "chronicle://localhost:35000/?disableTls=true")
                    .WithExample("context", "create", "prod", "--server", "chronicle://prod:35000", "-e", "production");
                ctx.AddCommand<SetContextCommand>("set")
                    .WithDescription("Switch to a context")
                    .WithExample("context", "set", "prod");
                ctx.AddCommand<ShowContextCommand>("show")
                    .WithDescription("Show current context details")
                    .WithExample("context", "show");
                ctx.AddCommand<DeleteContextCommand>("delete")
                    .WithDescription("Delete a context")
                    .WithExample("context", "delete", "old-dev");
                ctx.AddCommand<RenameContextCommand>("rename")
                    .WithDescription("Rename a context")
                    .WithExample("context", "rename", "dev", "development");
            });

            config.AddBranch("config", configCmd =>
            {
                configCmd.SetDescription("Manage CLI configuration");
                configCmd.AddCommand<ShowConfigCommand>("show")
                    .WithDescription("Show current configuration")
                    .WithExample("config", "show");
                configCmd.AddCommand<SetConfigCommand>("set")
                    .WithDescription("Set a configuration value")
                    .WithExample("config", "set", "server", "chronicle://myhost:35000");
                configCmd.AddCommand<ConfigPathCommand>("path")
                    .WithDescription("Print configuration file path")
                    .WithExample("config", "path");
            });

            config.AddCommand<LlmContextCommand>("llm-context")
                .WithDescription("Output CLI capabilities as JSON for AI agent consumption")
                .WithExample("llm-context");

            config.AddCommand<VersionCommand>("version")
                .WithDescription("Show CLI and server version information and contracts compatibility")
                .WithExample("version")
                .WithExample("version", "-o", "json");

            config.AddCommand<SelfUpdateCommand>("update")
                .WithDescription("Update the Cratis CLI to the latest version")
                .WithExample("update")
                .WithExample("update", "--version", "1.2.3");

            config.AddCommand<InitCommand>("init")
                .WithDescription("Generate CHRONICLE.md and configure AI tools for the current project")
                .WithExample("init")
                .WithExample("init", "--tool", "claude")
                .WithExample("init", "--force", "--no-commands");

            config.AddBranch("completions", completions =>
            {
                completions.SetDescription("Generate and install shell completion scripts");
                completions.AddCommand<PrintCompletionCommand>("bash")
                    .WithDescription("Print the bash completion script to stdout")
                    .WithExample("completions", "bash");
                completions.AddCommand<PrintCompletionCommand>("zsh")
                    .WithDescription("Print the zsh completion script to stdout")
                    .WithExample("completions", "zsh");
                completions.AddCommand<PrintCompletionCommand>("fish")
                    .WithDescription("Print the fish completion script to stdout")
                    .WithExample("completions", "fish");
                completions.AddCommand<CompletionsInstallCommand>("install")
                    .WithDescription("Automatically install completions for the current shell (run once after installing cratis)")
                    .WithExample("completions", "install")
                    .WithExample("completions", "install", "--shell", "zsh");
            });

            config.AddCommand<ChatCommand>("chat")
                .WithDescription("Chat with an AI assistant about your Chronicle system")
                .WithExample("chat")
                .WithExample("chat", "\"what observers are failing?\"")
                .WithExample("chat", "--provider", "ollama", "--model", "llama3.1");
        });

        return app;
    }
}
