using System;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;
using Shouldly;

namespace SisoDis.Tests;

public class PduHeaderTests
{
    [Fact]
    public void Constructor_WithValidMagicAndVersion_CreatesInstance()
    {
        var header = new PduHeader(1, 3);
        
        header.Magic.ShouldBe((byte)1);
        header.VersionMajor.ShouldBe((byte)3);
        header.IsIeee.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithInvalidMagic_ThrowsException()
    {
        Action act = () => new PduHeader(0, 3);
        
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("magic");
    }

    [Theory]
    [InlineData((byte)1, (byte)2)] // Not IEEE compliant
    [InlineData((byte)1, (byte)4)] // Future version
    public void Constructor_WithNonIeeeVersion_IsIeeeReturnsFalse(byte magic, byte version)
    {
        var header = new PduHeader(magic, version);
        
        header.IsIeee.ShouldBeFalse();
    }

    [Fact]
    public void Serialize_ProducesCorrectBuffer()
    {
        var header = new PduHeader(1, 3);
        var buffer = new byte[6];
        
        header.Serialize(buffer, 0, (ushort)257); // Type code 0x0101
        
        buffer[0].ShouldBe((byte)1); // magic
        buffer[1].ShouldBe((byte)3); // version
        buffer[2].ShouldBe((byte)0); // reserved high byte
        buffer[3].ShouldBe((byte)0); // reserved low byte
        buffer[4].ShouldBe((byte)1); // type high byte (big-endian)
        buffer[5].ShouldBe((byte)1); // type low byte
    }

    [Fact]
    public void Serialize_BufferTooSmall_ThrowsException()
    {
        var header = new PduHeader();
        var buffer = new byte[3];
        
        Action act = () => header.Serialize(buffer, 0, (ushort)257);
        
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("Buffer too small");
    }

    [Fact]
    public void Deserialize_ReadsCorrectValues()
    {
        var buffer = new byte[] { 1, 3, 0, 0, 1, 1 }; // magic=1, ver=3, type=0x0101
        var header = PduHeader.Deserialize(buffer.AsSpan(), 0);
        
        header.Magic.ShouldBe((byte)1);
        header.VersionMajor.ShouldBe((byte)3);
    }

    [Fact]
    public void Deserialize_BufferTooSmall_ThrowsException()
    {
        var buffer = new byte[] { 1, 3 }; // Incomplete
        
        Action act = () => PduHeader.Deserialize(buffer.AsSpan(), 0);
        
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("Buffer too small");
    }
}

public class EntityIdTests
{
    [Fact]
    public void Constructor_WithValidValue_CreatesInstance()
    {
        var id = new EntityId(42);
        
        id.Value.ShouldBe(42);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(65535)]
    public void Factory_Relative_CreatesValidRelativeId(int value)
    {
        var id = EntityId.Relative(value);
        
        id.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    public void Factory_Relative_WithInvalidValue_ThrowsException(int value)
    {
        Action act = () => EntityId.Relative(value);
        
        act.ShouldThrow<ArgumentOutOfRangeException>()
            .ParamName.ShouldBe("value");
    }

    [Fact]
    public void Factory_Absolute_CreatesAbsoluteIdentifier()
    {
        var id = EntityId.Absolute(12345);
        
        id.Value.ShouldBe(12345);
    }

    [Fact]
    public void DefaultType_ReturnsCorrectConstant()
    {
        EntityId.DefaultType.ShouldBe((byte)2); // Relative identifier type
    }
}
