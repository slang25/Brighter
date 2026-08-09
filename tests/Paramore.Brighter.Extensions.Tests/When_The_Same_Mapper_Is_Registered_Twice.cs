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
using Microsoft.Extensions.DependencyInjection;
using Paramore.Brighter.Extensions.DependencyInjection;
using Xunit;

namespace Paramore.Brighter.Extensions.Tests;

/// <summary>
/// Mapper registration is a set, not a list — the same rule <see cref="SubscriberRegistry"/> follows
/// for handlers. Two mechanisms can legitimately both cover a mapper (<c>AutoFromAssemblies()</c>
/// alongside a source-generated registration method, or one generated method called from two
/// composition paths); the caller means "register the union", not "throw at startup". Two
/// <em>different</em> mappers for one message type are still a genuine conflict.
/// </summary>
public class DuplicateMapperRegistrationTests
{
    [Fact]
    public void When_The_Same_Mapper_Is_Registered_Twice_It_Is_Not_A_Conflict()
    {
        var builder = new ServiceCollectionMessageMapperRegistryBuilder(new ServiceCollection());

        builder.Add(typeof(MyEvent), typeof(MyMapper));
        builder.Add(typeof(MyEvent), typeof(MyMapper));

        Assert.Equal(typeof(MyMapper), builder.Mappers[typeof(MyEvent)]);
        Assert.Single(builder.Mappers);
    }

    [Fact]
    public void When_The_Same_Async_Mapper_Is_Registered_Twice_It_Is_Not_A_Conflict()
    {
        var builder = new ServiceCollectionMessageMapperRegistryBuilder(new ServiceCollection());

        builder.AddAsync(typeof(MyEvent), typeof(MyMapper));
        builder.AddAsync(typeof(MyEvent), typeof(MyMapper));

        Assert.Equal(typeof(MyMapper), builder.AsyncMappers[typeof(MyEvent)]);
        Assert.Single(builder.AsyncMappers);
    }

    [Fact]
    public void When_A_Different_Mapper_Is_Registered_For_The_Same_Message_It_Throws()
    {
        var builder = new ServiceCollectionMessageMapperRegistryBuilder(new ServiceCollection());
        builder.Add(typeof(MyEvent), typeof(MyMapper));

        var exception = Assert.Throws<ArgumentException>(
            () => builder.Add(typeof(MyEvent), typeof(MyOtherMapper)));

        Assert.Contains("are in conflict", exception.Message);
    }

    [Fact]
    public void When_A_Different_Async_Mapper_Is_Registered_For_The_Same_Message_It_Throws()
    {
        var builder = new ServiceCollectionMessageMapperRegistryBuilder(new ServiceCollection());
        builder.AddAsync(typeof(MyEvent), typeof(MyMapper));

        var exception = Assert.Throws<ArgumentException>(
            () => builder.AddAsync(typeof(MyEvent), typeof(MyOtherMapper)));

        Assert.Contains("are in conflict", exception.Message);
    }

    // Deliberately NOT implementing IAmAMessageMapper<>: this test assembly's name starts with
    // "Paramore.Brighter", so AddBrighter's built-in assembly sweep would find these types. Add()
    // doesn't validate the mapper type, so plain classes exercise the same code path.
    public class MyEvent : Event
    {
        public MyEvent() : base(Guid.NewGuid()) { }
    }

    public class MyMapper;

    public class MyOtherMapper;
}
