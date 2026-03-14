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
    }
}
