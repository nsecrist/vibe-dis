using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class RemoveEntityPduTests
{
    [Fact]
    public void RemoveEntityPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new RemoveEntityPdu(0, 0, 0, EntityId.Relative(0), 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 14, pdu.ComputedLength());
    }

    [Fact]
    public void RemoveEntityPdu_RoundTripPreservesValues()
    {
        var original = new RemoveEntityPdu(42, 1, 0, EntityId.Relative(100), 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = RemoveEntityPdu.Deserialize(buffer);

        Assert.Equal(42u, deserialized.RequestId);
        Assert.Equal(1, deserialized.NumberOfParts);
        Assert.Equal(100, deserialized.EntityId.Value);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void RemoveEntityPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;
        Assert.Throws<DisValidationException>(() => RemoveEntityPdu.Deserialize(buffer));
    }

    [Fact]
    public void RemoveEntityPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)12, RemoveEntityPdu.PdTypeValue);
    }
}
