using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class ElectromagneticEmissionPduTests
{
    [Fact]
    public void ElectromagneticEmissionPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ElectromagneticEmissionPdu(
            EntityId.Relative(1),
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 10, pdu.ComputedLength());
    }

    [Fact]
    public void ElectromagneticEmissionPdu_RoundTripPreservesValues()
    {
        var original = new ElectromagneticEmissionPdu(
            EntityId.Relative(42),
            5,
            100,
            1,
            2,
            3,
            4
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ElectromagneticEmissionPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.EntityId.Value);
        Assert.Equal(5, deserialized.EmitterNumber);
        Assert.Equal(100, deserialized.EmitterLocation);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
        Assert.Equal(3, deserialized.NumberOfSystems);
        Assert.Equal(4, deserialized.NumberOfEmissionSystems);
    }

    [Fact]
    public void ElectromagneticEmissionPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => ElectromagneticEmissionPdu.Deserialize(buffer));
    }

    [Fact]
    public void ElectromagneticEmissionPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = ElectromagneticEmissionPdu.Create()
            .WithEntityId(EntityId.Relative(123))
            .WithEmitterNumber(7)
            .WithEmitterLocation(50)
            .WithSimulationFederation(1, 2)
            .WithNumberOfSystems(2)
            .WithNumberOfEmissionSystems(3)
            .Build();

        Assert.Equal(123, pdu.EntityId.Value);
        Assert.Equal(7, pdu.EmitterNumber);
        Assert.Equal(50, pdu.EmitterLocation);
        Assert.Equal(2, pdu.NumberOfSystems);
    }

    [Fact]
    public void ElectromagneticEmissionPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)23, ElectromagneticEmissionPdu.PdTypeValue);
    }
}
