using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class ServiceRequestPduTests
{
    [Fact]
    public void ServiceRequestPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ServiceRequestPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 19, pdu.ComputedLength());
    }

    [Fact]
    public void ServiceRequestPdu_RoundTripPreservesValues()
    {
        var original = new ServiceRequestPdu(
            EntityId.Relative(42),
            5,
            100,
            123,
            1,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ServiceRequestPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.RequestingEntityId.Value);
        Assert.Equal(5, deserialized.SupplyType);
        Assert.Equal(100u, deserialized.Quantity);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(1, deserialized.ServiceTypeRequested);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void ServiceRequestPdu_Deserialize_InvalidMagic_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 99;

        Assert.Throws<DisValidationException>(() => ServiceRequestPdu.Deserialize(buffer));
    }

    [Fact]
    public void ServiceRequestPdu_Deserialize_InvalidType_ThrowsException()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 1;
        buffer[1] = 3;
        buffer[4] = 99;
        buffer[5] = 0;

        Assert.Throws<DisValidationException>(() => ServiceRequestPdu.Deserialize(buffer));
    }

    [Fact]
    public void ServiceRequestPdu_Builder_CreatesInstanceCorrectly()
    {
        var pdu = ServiceRequestPdu.Create()
            .WithRequestingEntityId(EntityId.Relative(100))
            .WithSupplyType(7)
            .WithQuantity(50)
            .WithRequestId(999)
            .WithServiceTypeRequested(2)
            .WithNumberOfFixedDatum(1)
            .WithNumberOfVariableDatum(2)
            .WithSimulationFederation(1, 1)
            .Build();

        Assert.Equal(100, pdu.RequestingEntityId.Value);
        Assert.Equal(7, pdu.SupplyType);
        Assert.Equal(50u, pdu.Quantity);
        Assert.Equal(999u, pdu.RequestId);
    }

    [Fact]
    public void ServiceRequestPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)5, ServiceRequestPdu.PdTypeValue);
    }
}

public sealed class ResupplyOfferPduTests
{
    [Fact]
    public void ResupplyOfferPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ResupplyOfferPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 20, pdu.ComputedLength());
    }

    [Fact]
    public void ResupplyOfferPdu_RoundTripPreservesValues()
    {
        var original = new ResupplyOfferPdu(
            EntityId.Relative(42),
            5,
            100,
            123,
            3,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ResupplyOfferPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.ReceivingEntityId.Value);
        Assert.Equal(5, deserialized.SupplyType);
        Assert.Equal(100u, deserialized.Quantity);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(3, deserialized.NumberOfSupplyTypes);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void ResupplyOfferPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)6, ResupplyOfferPdu.PdTypeValue);
    }
}

public sealed class ResupplyReceivedPduTests
{
    [Fact]
    public void ResupplyReceivedPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ResupplyReceivedPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 20, pdu.ComputedLength());
    }

    [Fact]
    public void ResupplyReceivedPdu_RoundTripPreservesValues()
    {
        var original = new ResupplyReceivedPdu(
            EntityId.Relative(42),
            5,
            100,
            123,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ResupplyReceivedPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.ReceivingEntityId.Value);
        Assert.Equal(5, deserialized.SupplyType);
        Assert.Equal(100u, deserialized.Quantity);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void ResupplyReceivedPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)7, ResupplyReceivedPdu.PdTypeValue);
    }
}

public sealed class ResupplyCancelPduTests
{
    [Fact]
    public void ResupplyCancelPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ResupplyCancelPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 16, pdu.ComputedLength());
    }

    [Fact]
    public void ResupplyCancelPdu_RoundTripPreservesValues()
    {
        var original = new ResupplyCancelPdu(
            EntityId.Relative(42),
            5,
            123,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ResupplyCancelPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.RequestingEntityId.Value);
        Assert.Equal(5, deserialized.SupplyType);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void ResupplyCancelPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)8, ResupplyCancelPdu.PdTypeValue);
    }
}

public sealed class RepairResponsePduTests
{
    [Fact]
    public void RepairResponsePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new RepairResponsePdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 20, pdu.ComputedLength());
    }

    [Fact]
    public void RepairResponsePdu_RoundTripPreservesValues()
    {
        var original = new RepairResponsePdu(
            EntityId.Relative(42),
            7,
            123,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = RepairResponsePdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.ReceivingEntityId.Value);
        Assert.Equal(7, deserialized.RepairType);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void RepairResponsePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)10, RepairResponsePdu.PdTypeValue);
    }
}

public sealed class RepairCompletePduTests
{
    [Fact]
    public void RepairCompletePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new RepairCompletePdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 20, pdu.ComputedLength());
    }

    [Fact]
    public void RepairCompletePdu_RoundTripPreservesValues()
    {
        var original = new RepairCompletePdu(
            EntityId.Relative(42),
            7,
            123,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = RepairCompletePdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.ReceivingEntityId.Value);
        Assert.Equal(7, deserialized.RepairType);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void RepairCompletePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)9, RepairCompletePdu.PdTypeValue);
    }
}

public sealed class BreakoutRequestPduTests
{
    [Fact]
    public void BreakoutRequestPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new BreakoutRequestPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 16, pdu.ComputedLength());
    }

    [Fact]
    public void BreakoutRequestPdu_RoundTripPreservesValues()
    {
        var original = new BreakoutRequestPdu(
            EntityId.Relative(42),
            123,
            5,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = BreakoutRequestPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.RequestingEntityId.Value);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(5, deserialized.NumberOfRequestedUnits);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void BreakoutRequestPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)51, BreakoutRequestPdu.PdTypeValue);
    }
}

public sealed class BreakoutResponsePduTests
{
    [Fact]
    public void BreakoutResponsePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new BreakoutResponsePdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 16, pdu.ComputedLength());
    }

    [Fact]
    public void BreakoutResponsePdu_RoundTripPreservesValues()
    {
        var original = new BreakoutResponsePdu(
            EntityId.Relative(42),
            123,
            1,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = BreakoutResponsePdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.RespondingEntityId.Value);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(1, deserialized.BreakoutResponseStatus);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void BreakoutResponsePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)52, BreakoutResponsePdu.PdTypeValue);
    }
}

public sealed class BreakoutCancelPduTests
{
    [Fact]
    public void BreakoutCancelPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new BreakoutCancelPdu(
            EntityId.Relative(0),
            0,
            0,
            0,
            0,
            0
        );

        Assert.Equal(PduHeader.HeaderLength + 14, pdu.ComputedLength());
    }

    [Fact]
    public void BreakoutCancelPdu_RoundTripPreservesValues()
    {
        var original = new BreakoutCancelPdu(
            EntityId.Relative(42),
            123,
            2,
            3,
            1,
            2
        );

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = BreakoutCancelPdu.Deserialize(buffer);

        Assert.Equal(42, deserialized.RequestingEntityId.Value);
        Assert.Equal(123u, deserialized.RequestId);
        Assert.Equal(2, deserialized.NumberOfFixedDatum);
        Assert.Equal(3, deserialized.NumberOfVariableDatum);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void BreakoutCancelPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)53, BreakoutCancelPdu.PdTypeValue);
    }
}