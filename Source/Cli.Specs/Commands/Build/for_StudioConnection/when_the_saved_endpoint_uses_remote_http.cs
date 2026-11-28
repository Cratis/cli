// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_StudioConnection;

[Collection(CliSpecsCollection.Name)]
public class when_the_saved_endpoint_uses_remote_http : Cratis.Cli.given.a_temp_config_directory
{
    Exception _error;

    async Task Because()
    {
        var previous = Environment.GetEnvironmentVariable(StudioConnection.BuilderTokenVariable);
        var marker = Environment.GetEnvironmentVariable(BuilderBootstrap.HandoffVariable);
        try
        {
            Environment.SetEnvironmentVariable(StudioConnection.BuilderTokenVariable, null);
            Environment.SetEnvironmentVariable(BuilderBootstrap.HandoffVariable, null);
            CliConfiguration.GetConfigPath().ShouldEqual(Path.Combine(_tempConfigHome, ".cratis", "config.json"));
            new CliConfiguration { Studio = new() { Url = "http://remote.example", Organization = "org", BuilderId = "test" } }.Save();
            _error = await Catch.Exception(() => StudioConnection.Open(CancellationToken.None));
        }
        finally
        {
            Environment.SetEnvironmentVariable(StudioConnection.BuilderTokenVariable, previous);
            Environment.SetEnvironmentVariable(BuilderBootstrap.HandoffVariable, marker);
        }
    }

    [Cratis.Cli.for_DirectFileSecrets.given.unix_only.Fact] void should_refuse_the_saved_connection() => _error.ShouldBeOfExactType<StudioNotConnected>();
}
