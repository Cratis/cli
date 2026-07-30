// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageEndpoints;

public class when_resolving_for_host_ports : Specification
{
    StageEndpoints _endpoints;

    void Because() => _endpoints = StageEndpoints.For(9191, 35001);

    [Fact] void should_serve_the_api_over_http_on_the_host_port() => _endpoints.Api.ShouldEqual("http://localhost:9191");
    [Fact] void should_point_the_api_reference_at_the_same_host_port() => _endpoints.ApiReference.ShouldEqual("http://localhost:9191/scalar/v1");
    [Fact] void should_serve_the_workbench_over_https_on_its_own_host_port() => _endpoints.Workbench.ShouldEqual("https://localhost:35001");
}
