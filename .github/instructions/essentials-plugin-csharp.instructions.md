---
description: "Use when editing or creating C# plugin code in this repository. Enforces Essentials plugin patterns, naming, and formatting for Bluesound API implementation files."
applyTo: "src/**/*.cs"
---

# Essentials Plugin C# Conventions

- Prefer preserving the existing plugin architecture and base types.
- Keep device/controller classes aligned with Essentials patterns used in this repo:
  - `EssentialsBridgeableDevice` inheritance for bridgeable third-party devices
  - Factory-based instantiation for config-driven creation
  - Join map classes for bridge joins

# Formatting And Naming

- Use tab indentation (size 4) in C# files.
- Name private fields in camelCase with no underscore prefix.
- Prefer keeping existing public API names and signatures unchanged unless the task explicitly requires API changes.

# Communication And Feedback Patterns

- Reuse existing communication abstractions (`IBasicCommunication`, queue-driven receive handling, and gather/event callbacks) instead of introducing parallel patterns.
- Prefer extending existing feedback objects and event flows over adding duplicate state tracking paths.

# Safety Rules For Changes

- Prefer minimal, focused edits that preserve current behavior unless behavior changes are explicitly requested.
- Preserve XML documentation on public members when touching existing code.
- When adding new configuration values, place them in config classes and annotate for JSON serialization consistently with existing properties.

# Validation

- After edits, validate for compile/lint issues in changed files and resolve regressions introduced by the change.