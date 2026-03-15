using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class IffPduTests
{
    [Fact]
    public void IffPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new IffPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            null,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 12, pdu.ComputedLength());
    }

    [Fact]
    public void IffPdu_RoundTripPreservesValues()
    {
        var original = new IffPdu(
            EntityId.Relative(42),
            1,
            0,
            0,
            3,
            Array.Empty<IffData>(),
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = IffPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(1, deserialized.EmitterNumber);
        Assert.Equal(0, deserialized.SystemDataCount);
        Assert.Equal(3, deserialized.NumberOfIFFFundamentalParameters);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void IffPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => IffPdu.Deserialize(buffer));
    }

    [Fact]
    public void IffPdu_Deserialize_InvalidType_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 1;
        buffer[1] = 3;
        buffer[4] = 99;
        buffer[5] = 0;

        Assert.Throws<DisValidationException>(() => IffPdu.Deserialize(buffer));
    }

    [Fact]
    public void IffPdu_Builder_CreatesInstanceCorrectly()
    {
        var iffData = new IffData[] 
        {
            new IffData(1, 100, 2, 0, 12345, 0, 0)
        };

        var pdu = IffPdu.Create()
            .WithEntityId(EntityId.Relative(100))
            .WithEmitterNumber(1)
            .WithSystemData(iffData)
            .WithNumberOfIFFFundamentalParameters(2)
            .WithSimulationFederation(1, 1)
            .Build();

        Assert.Equal(100, pdu.EntityId.Value);
        Assert.Equal(1, pdu.EmitterNumber);
        Assert.Equal(1, pdu.SystemData.Length);
    }

    [Fact]
    public void IffPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)28, IffPdu.PdTypeValue);
    }
}