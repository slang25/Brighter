#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.Logging.Handlers;
using Paramore.Brighter.Policies.Handlers;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Xunit;

namespace Paramore.Brighter.Extensions.Tests;

/// <summary>
/// Pins the invariant that source-generated registration methods rely on: every route into an
/// <see cref="IBrighterBuilder"/> goes through <c>BrighterHandlerBuilder</c>, which scans the loaded
/// <c>Paramore.Brighter*</c> assemblies (and unconditionally appends core Brighter) for handlers.
/// So Brighter's own pipeline handlers — the ones attributes such as <c>[UsePolicy]</c> and
/// <c>[RequestLogging]</c> resolve at runtime — are already in the container before any user
/// registration runs, and generated code does not need to register them again.
/// </summary>
public class FrameworkPipelineHandlerRegistrationTests
{
    public static TheoryData<Type> FrameworkPipelineHandlers() =>
    [
        typeof(ExceptionPolicyHandler<>),
        typeof(ExceptionPolicyHandlerAsync<>),
        typeof(FallbackPolicyHandler<>),
        typeof(FallbackPolicyHandlerRequestHandlerAsync<>),
        typeof(RequestLoggingHandler<>),
        typeof(RequestLoggingHandlerAsync<>),
        typeof(TimeoutPolicyHandler<>),
    ];

    [Theory]
    [MemberData(nameof(FrameworkPipelineHandlers))]
    public void When_Adding_Brighter_The_Framework_Pipeline_Handlers_Are_Registered(Type handlerType)
    {
        var services = new ServiceCollection();

        services.AddBrighter();

        Assert.Contains(services, d => d.ServiceType == handlerType);
    }

    [Fact]
    public void When_Adding_Consumers_The_Framework_Pipeline_Handlers_Are_Registered()
    {
        // AddConsumers is the other public route to a builder; it funnels through the same
        // BrighterHandlerBuilder, so the invariant has to hold there too.
        var services = new ServiceCollection();

        services.AddConsumers();

        Assert.Contains(services, d => d.ServiceType == typeof(ExceptionPolicyHandler<>));
    }
}
