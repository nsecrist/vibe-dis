using System;
using Xunit;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

namespace SisoDis.Tests.Pdu;

public sealed class StartResumePduTests
{
    [Fact]
    public void StartResumePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new StartResumePdu(0, 0, 0, 0, 0, 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 20, pdu.ComputedLength());
    }

    [Fact]
    public void StartResumePdu_RoundTripPreservesValues()
    {
        var original = new StartResumePdu(1, 1000, 500, 2, 0, 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = StartResumePdu.Deserialize(buffer);

        Assert.Equal(1u, deserialized.RequestId);
        Assert.Equal(1000u, deserialized.RealTime);
        Assert.Equal(500u, deserialized.SimulationTime);
        Assert.Equal(2, deserialized.Level);
        Assert.Equal(1, deserialized.SimulationReference);
        Assert.Equal(2, deserialized.FederationReference);
    }

    [Fact]
    public void StartResumePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)13, StartResumePdu.PdTypeValue);
    }
}

public sealed class StopFreezePduTests
{
    [Fact]
    public void StopFreezePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new StopFreezePdu(0, 0, 0, 0, 0, 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 20, pdu.ComputedLength());
    }

    [Fact]
    public void StopFreezePdu_RoundTripPreservesValues()
    {
        var original = new StopFreezePdu(1, 1000, 500, 3, 0, 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = StopFreezePdu.Deserialize(buffer);

        Assert.Equal(1u, deserialized.RequestId);
        Assert.Equal(3, deserialized.Reason);
        Assert.Equal(1, deserialized.SimulationReference);
    }

    [Fact]
    public void StopFreezePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)14, StopFreezePdu.PdTypeValue);
    }
}

public sealed class AcknowledgePduTests
{
    [Fact]
    public void AcknowledgePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new AcknowledgePdu(0, 0, 0, 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 12, pdu.ComputedLength());
    }

    [Fact]
    public void AcknowledgePdu_RoundTripPreservesValues()
    {
        var original = new AcknowledgePdu(42, 1, 2, 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = AcknowledgePdu.Deserialize(buffer);

        Assert.Equal(42u, deserialized.RequestId);
        Assert.Equal((ushort)1, deserialized.ResponseFlag);
        Assert.Equal((ushort)2, deserialized.AcknowledgeFlag);
    }

    [Fact]
    public void AcknowledgePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)15, AcknowledgePdu.PdTypeValue);
    }
}

public sealed class ActionRequestPduTests
{
    [Fact]
    public void ActionRequestPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ActionRequestPdu(0, 0, 0, 0, 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 14, pdu.ComputedLength());
    }

    [Fact]
    public void ActionRequestPdu_RoundTripPreservesValues()
    {
        var original = new ActionRequestPdu(42, 100, 5, 3, 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ActionRequestPdu.Deserialize(buffer);

        Assert.Equal(42u, deserialized.RequestId);
        Assert.Equal((ushort)100, deserialized.ActionId);
        Assert.Equal((ushort)5, deserialized.NumberOfFixedDatum);
    }

    [Fact]
    public void ActionRequestPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)16, ActionRequestPdu.PdTypeValue);
    }
}

public sealed class ActionResponsePduTests
{
    [Fact]
    public void ActionResponsePdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new ActionResponsePdu(0, 0, 0, 0, 0, 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 16, pdu.ComputedLength());
    }

    [Fact]
    public void ActionResponsePdu_RoundTripPreservesValues()
    {
        var original = new ActionResponsePdu(42, 100, 1, 5, 3, 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = ActionResponsePdu.Deserialize(buffer);

        Assert.Equal(42u, deserialized.RequestId);
        Assert.Equal((ushort)100, deserialized.ActionId);
        Assert.Equal((ushort)1, deserialized.ResponseFlag);
    }

    [Fact]
    public void ActionResponsePdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)17, ActionResponsePdu.PdTypeValue);
    }
}

public sealed class DataQueryPduTests
{
    [Fact]
    public void DataQueryPdu_DefaultValues_ProducesCorrectLength()
    {
        var pdu = new DataQueryPdu(0, 0, 0, 0, 0, 0);
        Assert.Equal(PduHeader.HeaderLength + 14, pdu.ComputedLength());
    }

    [Fact]
    public void DataQueryPdu_RoundTripPreservesValues()
    {
        var original = new DataQueryPdu(42, 100, 5, 3, 1, 2);

        byte[] buffer = new byte[original.ComputedLength()];
        original.Serialize(buffer);

        var deserialized = DataQueryPdu.Deserialize(buffer);

        Assert.Equal(42u, deserialized.RequestId);
        Assert.Equal((ushort)100, deserialized.TimeInterval);
        Assert.Equal((ushort)5, deserialized.NumberOfFixedDatum);
    }

    [Fact]
    public void DataQueryPdu_PduTypeValue_IsCorrect()
    {
        Assert.Equal((ushort)18, DataQueryPdu.PdTypeValue);
    }
}
