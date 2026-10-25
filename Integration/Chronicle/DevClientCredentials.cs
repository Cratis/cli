// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Cli.Integration.Chronicle;

static class DevClientCredentials
{
    public static async Task Ensure(int mongoDBPort)
    {
        var mongoUrl = new MongoUrl($"mongodb://localhost:{mongoDBPort}/?replicaSet=rs0&directConnection=true");
        var mongoClient = new MongoClient(mongoUrl);
        var database = mongoClient.GetDatabase("chronicle+main");
        var applicationsCollection = database.GetCollection<BsonDocument>("applications");

        var existingApp = await applicationsCollection
            .Find(new BsonDocument("clientId", ChronicleConnectionString.DevelopmentClient))
            .FirstOrDefaultAsync();

        if (existingApp is not null)
        {
            return;
        }

        // Wait for the server to create the applications collection (it may still be initializing).
        var timeout = TimeSpan.FromSeconds(30);
        var tcs = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource(timeout);

        // Try to watch for the dev client application being inserted (in case we're running Debug image).
        var insertWatch = Task.Run(async () =>
        {
            try
            {
                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                    .Match(change =>
                        change.OperationType == ChangeStreamOperationType.Insert &&
                        change.FullDocument["clientId"] == ChronicleConnectionString.DevelopmentClient);

                using var cursor = await applicationsCollection.WatchAsync(pipeline, cancellationToken: cts.Token);
                if (await cursor.MoveNextAsync(cts.Token) && cursor.Current.Any())
                {
                    tcs.TrySetResult(true);
                }
            }
            catch (OperationCanceledException)
            {
                // Timed out waiting — we'll insert manually.
            }
        });

        // Give the Debug server a few seconds to register the dev client automatically.
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token));
        if (completedTask == tcs.Task)
        {
            await cts.CancelAsync();
            return;
        }

        // Re-check in case it was inserted between the initial check and starting the watch.
        existingApp = await applicationsCollection
            .Find(new BsonDocument("clientId", ChronicleConnectionString.DevelopmentClient))
            .FirstOrDefaultAsync();

        if (existingApp is not null)
        {
            await cts.CancelAsync();
            return;
        }

        // Release mode: manually seed the dev client application.
        var passwordHasher = new PasswordHasher<object>();
        var hashedSecret = passwordHasher.HashPassword(null!, ChronicleConnectionString.DevelopmentClientSecret);

        var application = new BsonDocument
        {
            { "_id", Guid.NewGuid().ToString() },
            { "clientId", ChronicleConnectionString.DevelopmentClient },
            { "clientSecret", hashedSecret },
            { "type", "confidential" },
            { "consentType", "implicit" },
            { "permissions", new BsonArray(["ept:token", "gt:client_credentials", "gt:password", "gt:refresh_token"]) }
        };

        await applicationsCollection.InsertOneAsync(application);
        await cts.CancelAsync();
        Console.WriteLine("Seeded dev client credentials for Release-mode server.");
    }
}
