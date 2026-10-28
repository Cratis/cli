// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_render_acceptance_command : a_check_command
{
    private protected Func<ScreenplayRenderPlan, bool, string?> _acceptance = null!;
    protected const string AcceptanceFailure = "Mock host rejected degraded render acceptance";
    protected string _artifactPath = null!;
    protected string _reportPath = null!;

    void Establish()
    {
        _settings.Check = false;
        _artifactPath = Path.Combine(_destination, "allowed.cs");
        _reportPath = Path.Combine(_destination, "mock-render-report.json");
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenplayRenderPlan(
                1,
                [new(ScreenplayDiagnosticSeverity.Warning, "MOCK-DEGRADED", "Mock host degradation", null)],
                _artifactPlan));
        _acceptance = Substitute.For<Func<ScreenplayRenderPlan, bool, string?>>();
        _acceptance(Arg.Any<ScreenplayRenderPlan>(), Arg.Any<bool>())
            .Returns(call => call.ArgAt<bool>(1) ? AcceptanceFailure : null);
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // Mock-host outputs prove CLI ordering, not a published Stage report contract.
                Directory.CreateDirectory(_destination);
                File.WriteAllText(_artifactPath, "allowed artifact");
                File.WriteAllText(_reportPath, "mock degradation report");
                return new ArtifactPublicationResult(2, 0, 0, new([], new(null, "manifest-hash")), false);
            });
        _publication.Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ArtifactPublicationCheckResult(2, 0, 0, 0, false, new([], new(null, "manifest-hash"))));
        _command = new RenderCommand(_planning, _publication, _acceptance);
    }
}
