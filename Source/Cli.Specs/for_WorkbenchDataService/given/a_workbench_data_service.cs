// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Cli.Commands.Chronicle.Workbench;

namespace Cratis.Cli.for_WorkbenchDataService.given;

public class a_workbench_data_service : Specification
{
    protected IServices _services;
    protected IUsers _users;
    protected IApplications _applications;
    protected WorkbenchDataService _service;
    protected WorkbenchData _data;

    void Establish()
    {
        _users = Substitute.For<IUsers>();
        _applications = Substitute.For<IApplications>();
        _services = Substitute.For<IServices>();
        _services.Users.Returns(_users);
        _services.Applications.Returns(_applications);
        _service = new WorkbenchDataService(_services, new WorkbenchSettings { Server = "chronicle://localhost:35000", EventStore = "es", Namespace = "ns" });
    }

    protected async Task Fetch() => _data = await _service.FetchAsync(null, null, null, CancellationToken.None);
}
