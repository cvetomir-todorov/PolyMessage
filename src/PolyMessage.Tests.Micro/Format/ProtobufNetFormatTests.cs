using System;
using FluentAssertions;
using PolyMessage.Formats.ProtobufNet;
using Xunit;

namespace PolyMessage.Tests.Micro.Format
{
    public sealed class DummyMessage {}

    public class ProtobufNetFormatTests
    {
        [Fact]
        public void RegisterSameTypeMultipleTimes()
        {
            // arrange
            ProtobufNetFormat target = new ProtobufNetFormat();
            Type messageType = typeof(DummyMessage);

            // act & assert
            target.RegisterMessageTypes(new[] {messageType});
            target.RegisterMessageTypes(new[] {messageType});
            target.RegisterMessageTypes(new[] {messageType});

            int fieldNumber = target.GetFieldNumber(messageType);
            fieldNumber.Should().BeGreaterThan(0);
        }
    }
}
