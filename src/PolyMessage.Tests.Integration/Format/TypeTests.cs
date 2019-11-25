using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Integration.Format
{
    public abstract class TypeTests : IntegrationFixture
    {
        private readonly TypeImplementor _implementorInstance;

        protected TypeTests(ITestOutputHelper output) : base(output, services =>
        {
            services.AddSingleton<ITypeContract, TypeImplementor>();
        })
        {
            _implementorInstance = (TypeImplementor) ServiceProvider.GetRequiredService<ITypeContract>();
            Client = CreateClient();

            Client.AddContract<ITypeContract>();
            Host.AddContract<ITypeContract>();
        }

        [Theory]
        [InlineData(
            SByte.MinValue, Int16.MinValue, Int32.MinValue, Int64.MinValue,
            Single.MinValue, Double.MinValue, -123456789123456789,
            Byte.MinValue, UInt16.MinValue, UInt32.MinValue, UInt64.MinValue,
            false, 'a')]
        [InlineData(
            SByte.MaxValue, Int16.MaxValue, Int32.MaxValue, Int64.MaxValue,
            Single.MaxValue, Double.MaxValue, 123456789123456789,
            Byte.MaxValue, UInt16.MaxValue, UInt32.MaxValue, UInt64.MaxValue,
            true, 'Z')]
        public async Task SimpleTypes(
            // signed
            SByte @sbyte, Int16 int16, Int32 int32, Int64 int64,
            // floating point
            Single single, Double @double, Decimal @decimal,
            // unsigned
            Byte @byte, UInt16 uint16, UInt32 uint32, UInt64 uint64,
            // others
            bool @bool, char @char)
        {
            // arrange
            SimpleTypesRequest request = new SimpleTypesRequest();
            // signed
            request.SByte = @sbyte;
            request.Int16 = int16;
            request.Int32 = int32;
            request.Int64 = int64;
            // floating point
            request.Single = single;
            request.Double = @double;
            request.Decimal = @decimal;
            // unsigned
            request.Byte = @byte;
            request.UInt16 = uint16;
            request.UInt32 = uint32;
            request.UInt64 = uint64;
            // others
            request.Bool = @bool;
            request.Char = @char;

            // act
            await StartHost();
            Client.Connect();
            await Client.Get<ITypeContract>().SimpleTypes(request);

            // assert
            _implementorInstance.LastSimpleTypesRequest.Should().BeEquivalentTo(request);
        }

        [Theory]
        [InlineData(CustomEnum.Default, 0, "")]
        [InlineData(CustomEnum.Normal, 12345, "normal")]
        [InlineData(CustomEnum.Extreme, int.MaxValue, "texttexttexttexttexttexttexttexttexttexttexttexttexttexttexttexttexttexttexttexttext")]
        public async Task ValueTypes(CustomEnum @enum, int structID, string structData)
        {
            // arrange
            ValueTypesRequest request = new ValueTypesRequest();
            request.Enum = @enum;
            request.Struct = new CustomStruct {ID = structID, Data = structData};

            // act
            await StartHost();
            Client.Connect();
            await Client.Get<ITypeContract>().ValueTypes(request);

            // assert
            _implementorInstance.LastValueTypesRequest.Should().BeEquivalentTo(request);
        }

        [Fact]
        public async Task Classes()
        {
            // arrange
            ClassesRequest request = new ClassesRequest();
            request.Root = new CustomClassRoot();
            request.Root.Middle1 = new CustomClassMiddle();
            request.Root.Middle2 = new CustomClassMiddle();
            request.Root.Middle1.Leaf = new CustomClassLeaf {ID = 456};
            request.Root.Middle2.Leaf = new CustomClassLeaf {ID = 123};

            // act
            await StartHost();
            Client.Connect();
            await Client.Get<ITypeContract>().Classes(request);

            // assert
            _implementorInstance.LastClassesRequest.Should().BeEquivalentTo(request);
        }

        [Fact]
        public async Task Collections()
        {
            // arrange
            CollectionsRequest request = new CollectionsRequest();
            request.List.AddRange(new[]{int.MaxValue, 123, int.MinValue});
            request.Dictionary.Add(5, new ClassWithID(5));
            request.Dictionary.Add(int.MaxValue, new ClassWithID(int.MaxValue));
            request.Dictionary.Add(int.MinValue, new ClassWithID(int.MinValue));

            // act
            await StartHost();
            Client.Connect();
            await Client.Get<ITypeContract>().Collections(request);

            // assert
            _implementorInstance.LastCollectionsRequest.Should().BeEquivalentTo(request);
        }
    }
}
