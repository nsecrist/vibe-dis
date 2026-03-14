# SisoDis.NET – Agent Guidance

## Quick Reference

```bash
# Build
dotnet build                    # All projects, release mode
dotnet build ./SisoDis.Tests    # Test project only
dotnet clean -v quiet           # Clean build artifacts

# Test (most common for agents)
dotnet test                     # Run all tests with output
dotnet test --verbosity quiet   # Fast run (CI mode)
dotnet test -v n                # Normal verbosity  
dotnet test --filter "FullyQualifiedName~SomeTestNamespace"  # Single test by name/pattern

# Dev workflow
dotnet watch build              # Auto-build on file change
dotnet format --verify-no-changes  # Verify formatting compliance

# Linting/style checks
roslynator-dotnet analyze   # Roslynator static analysis
```

## Build System

### Project Configuration
- **Target Framework**: .NET 10+ LTS (specified in `.csproj` file)
- **LangVersion**: `latest` (C# 12+)
- **Nullable**: `enable` in project files
- **ImplicitUsings**: `true` for standard .NET types

### Build Artifacts Location
- Release DLLs: `{RepoPath}/bin/Release/net10.0/SisoDis.dll`
- Debug DLLs: `{RepoPath}/bin/Debug/net10.0/SisoDis.dll`
- Test artifacts in separate folder under bin directory

## Code Style Guidelines

### File Structure & Types
- One public type per file; one class or record struct unless combined logically (e.g., `MyClass+Builder`)
- No partial classes except generated code (marked with `[Generated]` attribute)
- Keep files focused: <200 lines for simple types, up to 500 lines with multiple related components

### Imports Order (strict)
```cs
using System;                           // Base framework (most common first, then others)
using System.Collections.Generic;       // Only if this file uses collection types
using System.Buffers;                   // For serialization helpers in Pdu files
using SisoDis.Core.Common;              // Project-specific, sorted alphabetically
```

Avoid wildcard imports (`*`); specify each namespace explicitly and keep count low.

### Coding Patterns

| Pattern | Usage Example |
|---------|---------------|
| Records for immutability | `public record EntityStatePdu : PduBase` |
| Record structs for small values | `record struct EntityId(int Id)` |
| Span<byte> over arrays | Parameter in Serialize/DeserializeBody methods |
| Primary constructors | `public class Result(T Value) { ... }` |

### Naming Conventions

- **Public classes**: PascalCase (`FireEventPdu`, `NetworkBufferManager`)
- **Internal/private fields**: camelCase (`_position`, `_serialNumber`)
- **Constants**: ALL_CAPS_WITH_UNDERSCORES (rare; prefer immutable records)
- **Exception types**: XxxException suffix, e.g., `DisValidationException`

Avoid abbreviations except PDU/ID/Vector per industry standard.

### Serialization & Performance Rules

Always use `Span<byte>`, `ReadOnlySpan<byte>` in hot paths (serialize/deserialize). Allocate only when necessary using MemoryPool or per-request pattern for networking code.

Use BinaryPrimitives for fixed-size fields: always big-endian as per DIS spec. Avoid allocating arrays internally unless unavoidable.

```cs
public void Serialize(ReadOnlySpan<byte> buffer, int offset)
{
    // No allocations in hot path
    Buffer.BlockCopy(_id.Value, 0, buffer.AsSpan(), offset, 2);
}
```

### Exception Handling Strategy

Define two exception types: `DisValidationException` for invalid field values and `DisUnknownPduException` for deserialization mismatches. Always throw on validation failures (out-of-range fields, missing required data).

### Documentation Requirements

Every public type has XML docs with `<summary>`, `<param>`, `<returns>` where parameters exist. Include IEEE 1278.1-2012 section references like "Per IEEE 1278.1-2012 §5.3.3.1" for exact spec mapping.

```cs
/// <summary>
/// Represents entity state information per DIS Entity State PDU (IEEE 1278.1-2012 §5.3.3.1).
/// </summary>
public record EntityStatePdu : IPdu;
```

### Builder Pattern

For PDUs with >4 parameters, create a builder: `EntityStatePdu.Create().WithId(...).Build()`. Use implicit or explicit factory methods where constructors feel clunky.

## Unit Testing Rules

Use xunit test framework with Shouldly for assertion syntax (preferred) or FluentAssertions as alternative. Target 90%+ coverage on public API and all serialization paths.

Test must do round-trip (build → serialize → deserialize → deep equal comparison always), validation (invalid ranges throw exceptions), edge cases (boundary values, articulated parts = 0..N). Naming pattern: `[Fact] public void EntityId_RoundTripPreservesValues()`

## Extension Development Template

When implementing a new PDU per IEEE 1278.1-2 maintain: record definition with XML docs, builder if complexity warrants (>4 params), ComputedLength method, Serialize/DeserializeBody methods using Span<byte>, validation logic for known invalid ranges (null checks, range bounds). Add unit tests covering round-trip paths and validation edge cases. Register in PduFactory with appropriate handler mapping.

## Cursor/Copilot Integration

This repository supports Cursor rules at `.cursor/rules/` directory and GitHub Copilot via `.github/copilot-instructions.md`. These files would contain custom LSP rules for IntelliSense, formatting preferences beyond standard C# style rules, CI integration notes, security scanning configuration like .editorconfig references.

## Current Milestone Overview

Core infrastructure includes common types (EntityId, Vector3Double, entityType enums), PduHeader, IPdu interface, PduBase abstract class, PduFactory registry pattern, and DisSerializer. Next phase: complete EntityStatePdu serialization with tests, implement Fire/Detonation/Packet PDUs. Future work: UDP multicast network client, documentation build script.

## Dependencies Reference

Core uses System.Buffers and System.Memory (standard .NET). Test project adds xunit for testing framework, Shouldly assertion library for readable expectations (preferred), coverlet.collector for code coverage instrumentation. Use dotnet add package command to reference NuGet packages per project needs.

## Notes

See `agents.md` for detailed architecture blueprint, folder structure diagram, and milestone roadmap context. This file focuses on operational commands, coding constraints, and AI-assisted development workflows.
