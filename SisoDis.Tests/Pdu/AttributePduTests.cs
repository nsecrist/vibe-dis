using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class AttributePduTests
{
    [Fact]
    public void AttributePdu_WithAttributes_ProducesCorrectLength()
    {
        var attrData = new byte[] { 0x01, 0x02, 0x03 };
        var attributes = new AttributePdu.AttributeData[]
        {
            new(1, attrData),
            new(2, new byte[] { 0xFF })
        };

        var pdu = new AttributePdu(
            EntityId.Relative(1),
            0,
            0,
            attributes
        );

        Assert.True(pdu.ComputedLength() > PduHeader.HeaderLength + 5);
    }

    [Fact]
    public void AttributePdu_RoundTripPreservesValues()
    {
        var attrData = new byte[] { 0x12, 0x34, 0x56 };
        var attributes = new AttributePdu.AttributeData[]
        {
            new(42, attrData)
        };

        var original = new AttributePdu(EntityId.Relative(1), 1, 2, attributes);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = AttributePdu.Deserialize(buffer);

        Assert.Equal(original.EntityId.Value, deserialized.EntityId.Value);
        Assert.Equal((byte)1, deserialized.SimulationReference);
        Assert.Equal((byte)2, deserialized.FederationReference);
        Assert.Single(deserialized.Attributes);
        Assert.Equal((ushort)42, deserialized.Attributes[0].AttributeNumber);
        Assert.Equal(attrData.Length, deserialized.Attributes[0].Data.Length);
    }

    [Fact]
    public void AttributePdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => AttributePdu.Deserialize(buffer));
    }

    [Fact]
    public void AttributePdu_Deserialize_InvalidType_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 1;
        buffer[1] = 3;
        buffer[4] = 9;
        buffer[5] = 0;

        Assert.Throws<DisValidationException>(() => AttributePdu.Deserialize(buffer));
    }

    [Fact]
    public void AttributePdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = AttributePdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithSimulationFederation(1, 2)
            .WithAttribute(42, new byte[] { 0x01, 0x02 })
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.Single(pdu.Attributes);
    }

    [Fact]
    public void AttributePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)19, AttributePdu.PdTypeValue);
    }

    [Fact]
    public void AttributePdu_AttributeData_ContainsExpectedValues()
    {
        var attr = new AttributePdu.AttributeData(100, new byte[] { 0xAA });

        Assert.Equal((ushort)100, attr.AttributeNumber);
        Assert.Single(attr.Data);
        Assert.Equal((byte)0xAA, attr.Data[0]);
    }

    [Fact]
    public void AttributePdu_EmptyAttributes_ProducesMinimalLength()
    {
        var pdu = new AttributePdu(EntityId.Relative(1), 0, 0, Array.Empty<AttributePdu.AttributeData>());

        Assert.Equal(PduHeader.HeaderLength + 5, pdu.ComputedLength());
    }
}
