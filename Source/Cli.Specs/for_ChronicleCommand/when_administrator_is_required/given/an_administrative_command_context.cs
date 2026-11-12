// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Security;
using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Cli.Commands.Chronicle.Applications;
using Cratis.Cli.Commands.Chronicle.Jobs;
using Cratis.Cli.Commands.Chronicle.Users;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required.given;

[Collection(CliSpecsCollection.Name)]
public class an_administrative_command_context : Specification
{
    protected IServices _services;
    protected IUsers _users;
    protected IApplications _applications;
    protected IJobs _jobs;
    protected StringWriter _output;
    protected StringWriter _error;
    TextWriter _previousOutput;
    TextWriter _previousError;

    void Establish()
    {
        _users = Substitute.For<IUsers>();
        _applications = Substitute.For<IApplications>();
        _jobs = Substitute.For<IJobs>();
        _services = Substitute.For<IServices>();
        _services.Users.Returns(_users);
        _services.Applications.Returns(_applications);
        _services.Jobs.Returns(_jobs);
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
    }

    protected void AddUserAnswers(CommandResult result) =>
        _users.AddUser(Arg.Any<AddUserRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(result);

    protected void RemoveApplicationAnswers(CommandResult result) =>
        _applications.RemoveApplication(Arg.Any<RemoveApplicationRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(result);

    protected void AllUsersAnswers(QueryResult<IEnumerable<UserResponse>> result) =>
        _users.AllUsers(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Observable.Return(result));

    protected void AllApplicationsAnswers(QueryResult<IEnumerable<ApplicationResponse>> result) =>
        _applications.AllApplications(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Observable.Return(result));

    protected Task<int> AddUser() =>
        new AddUserCommandForSpecs().Execute(_services, new AddUserSettings { Username = "alice", Email = "alice@example.com", Password = "secret" });

    protected Task<int> RemoveApplication() =>
        new RemoveApplicationCommandForSpecs().Execute(_services, new RemoveApplicationSettings { AppId = Guid.NewGuid() });

    protected Task<int> ListUsers() => new ListUsersCommandForSpecs().Execute(_services, new EventStoreSettings());

    protected Task<int> ListApplications() => new ListApplicationsCommandForSpecs().Execute(_services, new EventStoreSettings());

    protected Task<int> DeleteJob() =>
        new DeleteJobCommandForSpecs().Execute(_services, new JobCommandSettings { EventStore = "es", Namespace = "ns", JobId = Guid.NewGuid().ToString() });

    public class AddUserCommandForSpecs : AddUserCommand
    {
        public Task<int> Execute(IServices services, AddUserSettings settings) => ExecuteCommandAsync(services, settings, "json");
    }

    public class RemoveApplicationCommandForSpecs : RemoveApplicationCommand
    {
        public Task<int> Execute(IServices services, RemoveApplicationSettings settings) => ExecuteCommandAsync(services, settings, "json");
    }

    public class ListUsersCommandForSpecs : ListUsersCommand
    {
        public Task<int> Execute(IServices services, EventStoreSettings settings) => ExecuteCommandAsync(services, settings, "json");
    }

    public class ListApplicationsCommandForSpecs : ListApplicationsCommand
    {
        public Task<int> Execute(IServices services, EventStoreSettings settings) => ExecuteCommandAsync(services, settings, "json");
    }

    public class DeleteJobCommandForSpecs : DeleteJobCommand
    {
        public Task<int> Execute(IServices services, JobCommandSettings settings) => ExecuteCommandAsync(services, settings, "json");
    }
}
