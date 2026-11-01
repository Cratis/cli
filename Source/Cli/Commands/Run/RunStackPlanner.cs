// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Plans the containers and files of a run whose model declares <c language="screenplay">authentication</c>: the Stage behind AuthProxy and, when the model
/// declares <c language="screenplay">onboarding</c>, the application's own Ante beside it.
/// </summary>
/// <remarks>
/// Planning is pure: it decides what is written and which <c language="shell">docker run</c> arguments start each container, and does neither. A secret
/// only ever becomes the content of a <see cref="PlannedFile"/>; the arguments carry paths, names and values that are not secret.
/// </remarks>
public static partial class RunStackPlanner
{
    /// <summary>
    /// The image AuthProxy runs from.
    /// </summary>
    public const string AuthProxyImage = "cratis/authproxy";

    /// <summary>
    /// The image Ante runs from.
    /// </summary>
    public const string AnteImage = "ghcr.io/cratis/ante";

    /// <summary>
    /// The image the database Ante keeps its exchange state in runs from.
    /// </summary>
    public const string MongoImage = "mongo:8";

    /// <summary>
    /// The name of the event store the application's events are appended to, shared with Ante as its host store.
    /// </summary>
    public const string AppStore = "Application";

    /// <summary>
    /// The name the other containers reach the Stage by.
    /// </summary>
    public const string StageAlias = "stage";

    /// <summary>
    /// The name Ante reaches its database by.
    /// </summary>
    public const string MongoAlias = "mongo";

    /// <summary>
    /// The port AuthProxy listens on inside its container.
    /// </summary>
    public const int AuthProxyPort = 8080;

    /// <summary>
    /// The port Ante listens on. It shares AuthProxy's network stack, so AuthProxy reaches it on loopback and nothing else can.
    /// </summary>
    public const int AntePort = 5002;

    /// <summary>
    /// Where the directory holding the environment's values is mounted inside a container.
    /// </summary>
    public const string SecretsPath = "/run/cratis/secrets";

    /// <summary>
    /// Plans a run.
    /// </summary>
    /// <param name="input">What the plan is made from.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the model refers to a file that does not exist.</exception>
    public static RunStackPlan Plan(RunStackInput input)
    {
        var id = input.StageName[StageContainer.NamePrefix.Length..];
        var network = $"cratis-run-{id}";
        var onboarding = input.Application.Onboarding;
        var stage = PlanStage(input, network, onboarding is not null);

        var directories = new List<string> { "stage", "stage/secrets", "authproxy", "authproxy/secrets" };
        var files = new List<PlannedFile>
        {
            new("stage/chronicle-connection-string", $"chronicle://localhost:{StageContainer.WorkbenchPort}/?auth=none")
        };
        files.AddRange(StageSecrets(input));

        var containers = new List<PlannedContainer>();
        var services = new List<RunService>();
        var entry = $"http://localhost:{input.Settings.AuthPort}";

        if (onboarding is not null)
        {
            containers.Add(PlanMongo(id, network));
        }

        containers.Add(PlanAuthProxy(input, id, network, onboarding, directories, files));
        services.Add(new("Application (AuthProxy)", entry));
        if (onboarding is not null)
        {
            containers.Add(PlanAnte(input, id, onboarding, directories, files));
            services.Add(new("Onboarding lobby", LobbyAddress(input.Settings.LobbyPort)));
        }

        var mailFolder = onboarding is not null && !input.Environment.HasMailServer ? input.MailFolder : null;
        return new(network, stage, directories, files, containers, services, entry, mailFolder);
    }

    /// <summary>
    /// Gets the address of the onboarding lobby from the host.
    /// </summary>
    /// <param name="lobbyPort">The host port the lobby is reached on.</param>
    /// <returns>The address, on the same host name as the application.</returns>
    public static string LobbyAddress(int lobbyPort) => $"http://localhost:{lobbyPort}";

    static StageRuntimeOptions PlanStage(RunStackInput input, string network, bool onboarding)
    {
        List<KeyValuePair<string, string>> environment =
        [
            new("STAGE_EVENT_STORE", AppStore),
            new("CRATIS_ENVIRONMENT", input.Environment.Name),
            new("CRATIS_SECRETS_DIRECTORY", SecretsPath),
            new("CRATIS_CHRONICLE_CONNECTION_STRING_FILE", "/run/cratis/chronicle-connection-string")
        ];
        List<ContainerMount> mounts = [new(Path.Combine(input.RunDirectory, "stage", "secrets"), SecretsPath), new(Path.Combine(input.RunDirectory, "stage", "chronicle-connection-string"), "/run/cratis/chronicle-connection-string")];
        if (onboarding && !input.Environment.HasMailServer)
        {
            environment.Add(new("CRATIS_MAIL_PICKUP_FOLDER", "/run/cratis-mail"));
            mounts.Add(new(input.MailFolder, "/run/cratis-mail", ReadOnly: false));
        }

        return new(network, StageAlias, "127.0.0.1", environment, mounts);
    }

    static IEnumerable<PlannedFile> StageSecrets(RunStackInput input) =>
        input.Environment.Values
            .Where(pair => pair.Key.StartsWith(RunEnvironment.MailPrefix, StringComparison.Ordinal))
            .Select(pair => new PlannedFile($"stage/secrets/{pair.Key}", pair.Value));

    static PlannedContainer PlanMongo(string id, string network)
    {
        var name = $"cratis-mongo-{id}";
        return new("mongo", name, MongoImage, ["run", "--rm", "--name", name, "--network", network, "--network-alias", MongoAlias, MongoImage]);
    }
}
