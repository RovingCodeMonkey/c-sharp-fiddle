# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 console application used as a C# scratch pad / fiddle environment for experimenting with C# language features, LINQ, and data structures.

## Commands

```bash
# Build
dotnet build

# Run
dotnet run --project c-sharp-fiddle/c-sharp-fiddle.csproj

# Build and run (from solution root)
dotnet run --project c-sharp-fiddle
```

## Structure

- `c-sharp-fiddle.slnx` — Solution file (new `.slnx` format)
- `c-sharp-fiddle/Program.cs` — Entry point; this is where experimentation happens
- `c-sharp-fiddle/Models/` — Model classes used in experiments (e.g., `Tink`)

## Notes

- Target framework: `net10.0`
- Implicit usings and nullable reference types are enabled
- The `Models/` folder holds supporting types; `Program.cs` is the main scratch area
- `ITink` interface in `tink.cs` is currently empty — it's a placeholder pattern
