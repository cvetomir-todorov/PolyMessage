using System;
using FluentAssertions;
using PolyMessage.Formats.ProtobufNet;
using Xunit;

namespace PolyMessage.Tests.Micro.Format
{
    [PolyMessage(ID = 2)]
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
            target.RegisterMessageTypes(new[] {new MessageInfo(messageType, 2)});
            target.RegisterMessageTypes(new[] {new MessageInfo(messageType, 2)});
            target.RegisterMessageTypes(new[] {new MessageInfo(messageType, 2)});

            int fieldNumber = target.GetFieldNumber(messageType);
            fieldNumber.Should().BeGreaterThan(0);
        }
    }
}
