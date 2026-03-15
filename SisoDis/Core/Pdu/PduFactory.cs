using System;
using System.Collections.Generic;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;
using SisoDis;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Factory for creating PDUs from type codes per IEEE 1278.1-2012 §5.3.3.1 registry pattern.
/// </summary>
public sealed class PduFactory
{
    private static readonly Dictionary<ushort, Type> _pduTypes = new();

    /// <summary>Registers a PDU type with its corresponding type code.</summary>
    public static void RegisterPduType(ushort pduType, Type pduTypeToRegister)
    {
        if (pduType == 0) throw new ArgumentException("PDU type cannot be zero", nameof(pduType));
        if (!typeof(IPdu).IsAssignableFrom(pduTypeToRegister)) 
            throw new ArgumentException("Must implement IPdu interface");

        _pduTypes[pduType] = pduTypeToRegister;
    }

    /// <summary>Creates a PDU instance from the given type code.</summary>
    public static IPdu CreatePdu(ushort pduType)
    {
        if (!_pduTypes.TryGetValue(pduType, out var type)) 
            throw new DisUnknownPduException(pduType);

        try
        {
            return (IPdu)Activator.CreateInstance(type)!;
        }
        catch (Exception ex) when (!(ex is DisUnknownPduException))
        {
            throw new DisValidationException("Failed to create PDU", ex!);
        }
    }

    /// <summary>Checks if a PDU type has been registered.</summary>
    public static bool IsRegistered(ushort pduType) => _pduTypes.ContainsKey(pduType);

    /// <summary>Returns all registered PDU types as a dictionary.</summary>
    public static IReadOnlyDictionary<ushort, Type> GetAllRegisteredPduTypes() 
        => new Dictionary<ushort, Type>(_pduTypes);

    /// <summary>Initializes the factory with all standard DIS PDU type registrations per IEEE 1278.1-2012.</summary>
    static PduFactory()
    {
        // Register Fire PDU (Type code = 2) - IEEE §5.3.3 Table 5-4
        if (!IsRegistered(FirePdu.PdTypeValue))
            RegisterPduType(FirePdu.PdTypeValue, typeof(FirePdu));

        // Register Detonation PDU (Type code = 3) - IEEE §5.3.4 Table 5-4
        if (!IsRegistered(DetonationPdu.PdTypeValue))
            RegisterPduType(DetonationPdu.PdTypeValue, typeof(DetonationPdu));

        // Register EntityStatePDU (Type code = 1) - IEEE §5.3.3.1 Table 5-4
        if (!IsRegistered(EntityStatePdu.PdTypeValue))
            RegisterPduType(EntityStatePdu.PdTypeValue, typeof(EntityStatePdu));

        // Register Collision PDU (Type code = 4) - IEEE §5.3.4 Table 5-4
        if (!IsRegistered(CollisionPdu.PdTypeValue))
            RegisterPduType(CollisionPdu.PdTypeValue, typeof(CollisionPdu));

        // Register Collision-Elastic PDU (Type code = 5) - IEEE §5.3.5 Table 5-4
        if (!IsRegistered(CollisionElasticPdu.PdTypeValue))
            RegisterPduType(CollisionElasticPdu.PdTypeValue, typeof(CollisionElasticPdu));

        // Register Entity State Update PDU (Type code = 6) - IEEE §5.3.6 Table 5-4
        if (!IsRegistered(EntityStateUpdatePdu.PdTypeValue))
            RegisterPduType(EntityStateUpdatePdu.PdTypeValue, typeof(EntityStateUpdatePdu));

        // Register Attribute PDU (Type code = 7) - IEEE §5.3.7 Table 5-4
        if (!IsRegistered(AttributePdu.PdTypeValue))
            RegisterPduType(AttributePdu.PdTypeValue, typeof(AttributePdu));

        // Register Munition PDU (Type code = 20) - IEEE §5.3.10 Table 5-4
        if (!IsRegistered(MunitionPdu.PdTypeValue))
            RegisterPduType(MunitionPdu.PdTypeValue, typeof(MunitionPdu));

        // Register Designator PDU (Type code = 21) - IEEE §5.3.11 Table 5-4
        if (!IsRegistered(DesignatorPdu.PdTypeValue))
            RegisterPduType(DesignatorPdu.PdTypeValue, typeof(DesignatorPdu));

        // Register Electromagnetic Emission PDU (Type code = 22) - IEEE §5.3.12 Table 5-4
        if (!IsRegistered(ElectromagneticEmissionPdu.PdTypeValue))
            RegisterPduType(ElectromagneticEmissionPdu.PdTypeValue, typeof(ElectromagneticEmissionPdu));

        // Register Create Entity PDU (Type code = 23) - IEEE §5.3.6.1 Table 5-4
        if (!IsRegistered(CreateEntityPdu.PdTypeValue))
            RegisterPduType(CreateEntityPdu.PdTypeValue, typeof(CreateEntityPdu));

        // Register Remove Entity PDU (Type code = 24) - IEEE §5.3.6.2 Table 5-4
        if (!IsRegistered(RemoveEntityPdu.PdTypeValue))
            RegisterPduType(RemoveEntityPdu.PdTypeValue, typeof(RemoveEntityPdu));

        // Register Start/Resume PDU (Type code = 25) - IEEE §5.3.6.3 Table 5-4
        if (!IsRegistered(StartResumePdu.PdTypeValue))
            RegisterPduType(StartResumePdu.PdTypeValue, typeof(StartResumePdu));

        // Register Stop/Freeze PDU (Type code = 26) - IEEE §5.3.6.4 Table 5-4
        if (!IsRegistered(StopFreezePdu.PdTypeValue))
            RegisterPduType(StopFreezePdu.PdTypeValue, typeof(StopFreezePdu));

        // Register Acknowledge PDU (Type code = 27) - IEEE §5.3.6.5 Table 5-4
        if (!IsRegistered(AcknowledgePdu.PdTypeValue))
            RegisterPduType(AcknowledgePdu.PdTypeValue, typeof(AcknowledgePdu));

        // Register Action Request PDU (Type code = 28) - IEEE §5.3.6.6 Table 5-4
        if (!IsRegistered(ActionRequestPdu.PdTypeValue))
            RegisterPduType(ActionRequestPdu.PdTypeValue, typeof(ActionRequestPdu));

        // Register Action Response PDU (Type code = 29) - IEEE §5.3.6.7 Table 5-4
        if (!IsRegistered(ActionResponsePdu.PdTypeValue))
            RegisterPduType(ActionResponsePdu.PdTypeValue, typeof(ActionResponsePdu));

        // Register Data Query PDU (Type code = 30) - IEEE §5.3.6.8 Table 5-4
        if (!IsRegistered(DataQueryPdu.PdTypeValue))
            RegisterPduType(DataQueryPdu.PdTypeValue, typeof(DataQueryPdu));

        // Register Service Request PDU (Type code = 40) - IEEE §5.3.8.1 Table 5-4
        if (!IsRegistered(ServiceRequestPdu.PdTypeValue))
            RegisterPduType(ServiceRequestPdu.PdTypeValue, typeof(ServiceRequestPdu));

        // Register Resupply Offer PDU (Type code = 41) - IEEE §5.3.8.2 Table 5-4
        if (!IsRegistered(ResupplyOfferPdu.PdTypeValue))
            RegisterPduType(ResupplyOfferPdu.PdTypeValue, typeof(ResupplyOfferPdu));

        // Register Resupply Received PDU (Type code = 42) - IEEE §5.3.8.3 Table 5-4
        if (!IsRegistered(ResupplyReceivedPdu.PdTypeValue))
            RegisterPduType(ResupplyReceivedPdu.PdTypeValue, typeof(ResupplyReceivedPdu));

        // Register Resupply Cancel PDU (Type code = 43) - IEEE §5.3.8.4 Table 5-4
        if (!IsRegistered(ResupplyCancelPdu.PdTypeValue))
            RegisterPduType(ResupplyCancelPdu.PdTypeValue, typeof(ResupplyCancelPdu));

        // Register Repair Response PDU (Type code = 44) - IEEE §5.3.8.5 Table 5-4
        if (!IsRegistered(RepairResponsePdu.PdTypeValue))
            RegisterPduType(RepairResponsePdu.PdTypeValue, typeof(RepairResponsePdu));

        // Register Repair Complete PDU (Type code = 45) - IEEE §5.3.8.6 Table 5-4
        if (!IsRegistered(RepairCompletePdu.PdTypeValue))
            RegisterPduType(RepairCompletePdu.PdTypeValue, typeof(RepairCompletePdu));

        // Register Breakout Request PDU (Type code = 46) - IEEE §5.3.8.7 Table 5-4
        if (!IsRegistered(BreakoutRequestPdu.PdTypeValue))
            RegisterPduType(BreakoutRequestPdu.PdTypeValue, typeof(BreakoutRequestPdu));

        // Register Breakout Response PDU (Type code = 47) - IEEE §5.3.8.8 Table 5-4
        if (!IsRegistered(BreakoutResponsePdu.PdTypeValue))
            RegisterPduType(BreakoutResponsePdu.PdTypeValue, typeof(BreakoutResponsePdu));

        // Register Breakout Cancel PDU (Type code = 48) - IEEE §5.3.8.9 Table 5-4
        if (!IsRegistered(BreakoutCancelPdu.PdTypeValue))
            RegisterPduType(BreakoutCancelPdu.PdTypeValue, typeof(BreakoutCancelPdu));
    }
}
