// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Everything the plan of a run with authentication is made from.
/// </summary>
/// <param name="Application">The compiled model.</param>
/// <param name="Environment">The environment's values.</param>
/// <param name="Settings">The options of the run.</param>
/// <param name="ModelRoot">The folder the model's file references are relative to.</param>
/// <param name="RunDirectory">The directory the run writes its files to.</param>
/// <param name="StageName">The name of the Stage container, which the names of the others follow.</param>
/// <param name="MailFolder">The folder onboarding emails are written to.</param>
/// <param name="Attestation">The key pair of this run.</param>
/// <param name="User">The user the containers that read mounted files run as, or null to leave the image's.</param>
public sealed record RunStackInput(
    ApplicationSyntax Application,
    RunEnvironment Environment,
    RunSettings Settings,
    string ModelRoot,
    string RunDirectory,
    string StageName,
    string MailFolder,
    AttestationKey Attestation,
    HostUser? User);
