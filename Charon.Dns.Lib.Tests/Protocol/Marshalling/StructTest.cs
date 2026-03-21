// using System;
// using System.Buffers.Binary;
// using Charon.Dns.Lib.Protocol.Marshalling;
// using FluentAssertions;
// using JetBrains.Annotations;
// using Xunit;
//
// namespace Charon.Dns.Lib.Tests.Protocol.Marshalling;
//
// [TestSubject(typeof(Struct))]
// public class StructTest
// {
//     [Theory]
//     [InlineData(0)]
//     [InlineData(0x1000_2000_3000_4000)]
//     [InlineData(0x1000_2000_0000_0000)]
//     [InlineData(0x0000_0000_3000_4000)]
//     [InlineData(ulong.MaxValue)]
//     public void CompareToPrevious(ulong dataValue)
//     {
//         var dvs = dataValue.ToString("x8");
//         
//         // Arrange
//         var data = new byte[8];
//         BinaryPrimitives.WriteUInt64BigEndian(data, dataValue);
//         
//         // Act
//         var oldResult = Struct.GetStruct<TestStruct>(data);
//         var newResult = Struct.GetStruct<TestStruct>(data.AsMemory());
//         
//         // Assert
//         newResult.Field1.Should().Be(oldResult.Field1);
//         newResult.Field2.Should().Be(oldResult.Field2);
//         newResult.Field3.Should().Be(oldResult.Field3);
//         newResult.Field4.Should().Be(oldResult.Field4);
//     }
//     
//     public struct TestStruct
//     {
//         public int Field1;
//         public bool Field2;
//         public byte Field3;
//         public ushort Field4;
//     }
// }
