// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_CheckReleaseCommand.given;

public class a_release_target : Specification
{
    protected IServices _services;
    protected ReleaseDescription _release;
    protected CompatibilityResult _result;
    protected CommandResult<CheckEventTypeRegistrationsResponse> _schemaResult;

    protected CompatibilityCheck Kernel => _result.Checks.Single(_ => _.Check == CompatibilityCheck.Kernel);
    protected CompatibilityCheck Contracts => _result.Checks.Single(_ => _.Check == CompatibilityCheck.Contracts);
    protected CompatibilityCheck Schemas => _result.Checks.Single(_ => _.Check == CompatibilityCheck.Schemas);

    void Establish()
    {
        _services = Substitute.For<IServices>();
        _release = new ReleaseDescription(
            "v3.2.0",
            "Cratis/Direct",
            "direct",
            "production",
            "dotnet",
            "19.181.3",
            1,
            new byte[] { 1, 2, 3 },
            "19.157.0",
            [new EventTypeRegistration { Type = new EventType { Id = "BookAdded", Generation = 1 }, Schema = "{}" }]);
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = "19.181.3", ProtocolVersion = "1" });
        _services.Connections.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns(new CompatibilityResponse { IsCompatible = true, ServerVersion = "19.181.3", ServerProtocolVersion = "1" });
        _schemaResult = new CommandResult<CheckEventTypeRegistrationsResponse>
        {
            Response = new CheckEventTypeRegistrationsResponse { Results = [new EventTypeCheckResult { EventType = "BookAdded", Generation = 1, Accepted = true }] }
        };
        _services.EventTypes.CheckEventTypeRegistrations(Arg.Any<CheckEventTypeRegistrationsRequest>(), Arg.Any<CallContext>()).Returns(_ => Task.FromResult(_schemaResult));
    }

    protected async Task Check() => _result = await ReleaseChecker.Check(_services, _release, "Shop", DateTimeOffset.UtcNow);
}
