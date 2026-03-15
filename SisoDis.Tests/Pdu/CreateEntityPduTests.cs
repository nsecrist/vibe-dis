using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class CreateEntityPduTests
{
    [Fact]
    public void CreateEntityPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new CreateEntityPdu(0, 0, 0, EntityId.Relative(0), 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 14, pdu.ComputedLength());
    }

    [Fact]
    public void CreateEntityPdu_RoundTripPreservesValues()
    {
        var original = new CreateEntityPdu(42, 1, 0, EntityId.Relative(100), 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = CreateEntityPdu.Deserialize(buffer);

        Assert.Equal(42u, deserialized.RequestId);
        Assert.Equal(1, deserialized.NumberOfParts);
        Assert.Equal(0, deserialized.PartParameterIndex);
        Assert.Equal(100, deserialized.EntityId.Value);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void CreateEntityPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;
        Assert.Throws<DisValidationException>(() => CreateEntityPdu.Deserialize(buffer));
    }

    [Fact]
    public void CreateEntityPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = CreateEntityPdu.Create()
            .WithRequestId(123)
            .WithNumberOfParts(2)
            .WithPartParameterIndex(1)
            .WithEntityId(EntityId.Relative(456))
            .WithSimulationFederation(1, 2)
            .Build();

        Assert.Equal(123u, pdu.RequestId);
        Assert.Equal(2, pdu.NumberOfParts);
        Assert.Equal(1, pdu.PartParameterIndex);
        Assert.Equal(456, pdu.EntityId.Value);
    }

    [Fact]
    public void CreateEntityPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)11, CreateEntityPdu.PdTypeValue);
    }
}
