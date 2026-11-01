// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_checking_event_identities.given;

public class an_existing_event_publication : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    protected string _eventPath = null!;
    protected Dictionary<string, byte[]> _before = null!;
    private protected ArtifactPublicationCheckResult _check = null!;
    protected Exception? _error;

    async Task Establish()
    {
        var plan = await Plan();
        await Publish(plan);
        _eventPath = plan.Artifacts!.Artifacts.Single(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())
            .Contains("public record ProjectRegistered(", StringComparison.Ordinal)).RelativePath;
        _before = Directory.GetFiles(_destination, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, File.ReadAllBytes, StringComparer.Ordinal);
    }

    protected void PinEvent(string storedName) => File.WriteAllText(_projectFile,
        ProjectSource.Replace("event ProjectRegistered", $"event ProjectRegistered\n        id \"{storedName}\"", StringComparison.Ordinal));

    protected bool DestinationIsUnchanged() =>
        Directory.GetFiles(_destination, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .SequenceEqual(_before.Keys.Order(StringComparer.Ordinal)) &&
        _before.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value));
}
