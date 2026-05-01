// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502, CA1034 // Marker types: intentionally empty and nested for branch hierarchy

namespace Cratis.Cli.Registration;

/// <summary>
/// Chronicle server commands branch. Contains all sub-branches for event stores,
/// observers, events, etc.
/// </summary>
[CliBranch("chronicle", "Commands for interacting with Cratis Chronicle")]
public static class ChronicleBranch
{
    /// <summary>Event store management.</summary>
    [CliBranch("event-stores", "Manage event stores")]
    public static class EventStores { }

    /// <summary>Namespace management within an event store.</summary>
    [CliBranch("namespaces", "Manage namespaces within an event store")]
    public static class Namespaces { }

    /// <summary>Event type inspection.</summary>
    [CliBranch("event-types", "Manage event types")]
    public static class EventTypes { }

    /// <summary>Event querying and inspection.</summary>
    [CliBranch("events", "Query and inspect events")]
    public static class Events { }

    /// <summary>Observer management (reactors, reducers, projections).</summary>
    [CliBranch("observers", "Manage observers (reactors, reducers, projections)")]
    public static class Observers { }

    /// <summary>Failed partition inspection.</summary>
    [CliBranch("failed-partitions", "Inspect failed observer partitions")]
    public static class FailedPartitions { }

    /// <summary>System recommendations.</summary>
    [CliBranch("recommendations", "Manage system recommendations")]
    public static class Recommendations { }

    /// <summary>Background job management.</summary>
    [CliBranch("jobs", "Manage background jobs")]
    public static class Jobs { }

    /// <summary>Identity inspection.</summary>
    [CliBranch("identities", "Inspect identities")]
    public static class Identities { }

    /// <summary>Projection management.</summary>
    [CliBranch("projections", "Manage projections")]
    public static class Projections { }

    /// <summary>Read model data inspection.</summary>
    [CliBranch("read-models", "Inspect read model data")]
    public static class ReadModels { }

    /// <summary>Authentication management.</summary>
    [CliBranch("auth", "Authentication management")]
    public static class Auth { }

    /// <summary>User management.</summary>
    [CliBranch("users", "Manage Chronicle users")]
    public static class Users { }

    /// <summary>OAuth application management.</summary>
    [CliBranch("applications", "Manage OAuth client applications")]
    public static class Applications { }
}
