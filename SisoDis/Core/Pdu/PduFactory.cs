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
}
