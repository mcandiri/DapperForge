# ADR-002: Convention-Based SP Name Resolution

**Status:** Accepted
**Date:** 2024-12-20
**Decision Makers:** Core Team

## Context

In SP-heavy codebases, teams typically follow a naming pattern:

```
Get_Students, Save_Students, Remove_Students
sel_Students, up_Students, del_Students
sp_GetStudents, sp_SaveStudents, sp_DeleteStudents
dbo.usp_Student_Select, dbo.usp_Student_Upsert, dbo.usp_Student_Delete
```

Every team has their own convention, but the pattern is always the same: `{prefix}{separator}{entity}` with an optional schema. Yet every SP call hardcodes the full name as a magic string.

## Decision

**Implement a convention engine that resolves SP names from entity types using a configurable pattern: `{schema}.{prefix}{separator}{entity}`.**

Provide three levels of customization:
1. **Default convention** — `Get_`, `Save_`, `Remove_` with no schema
2. **ConventionBuilder** — configure prefix, separator, and schema globally
3. **MapEntity<T>** — override entity name for specific types

## Rationale

1. **Eliminates magic strings** — SP names are derived from types, not scattered across the codebase
2. **Team consistency** — configure once, enforce everywhere. New developers can't accidentally use the wrong prefix
3. **Refactor-safe** — rename an entity, and all SP references update automatically
4. **Testable** — convention resolution is a pure function: `Type → string`. Easy to unit test
5. **Progressive complexity** — defaults work for 80% of cases, builder for 15%, per-entity mapping for 5%

## Design: Entity Name Resolution Order

```
1. MapEntity<T>("CustomName")     → highest priority, explicit override
2. EntityNameResolver(type)       → global custom function
3. Default: type.Name + "s"       → fallback convention
```

This allows teams to handle irregular pluralization (e.g., `Person` → `People`) without abandoning the convention system entirely.

## Consequences

- Every entity type implicitly maps to 3 SP names (select, upsert, delete)
- Teams must ensure their actual SPs match the convention — `ValidateSpOnStartup` helps catch mismatches
- Default pluralization (`+ "s"`) is naive — doesn't handle `Child` → `Children`. This is intentional: we avoid pulling in a pluralization library for an edge case that `MapEntity` solves

## Alternatives Considered

| Option | Rejected Because |
|---|---|
| Attribute-based (`[SpName("Get_Students")]`) | Requires decorating every entity — defeats the purpose of conventions |
| Pluralization library (Humanizer) | Heavy dependency for a feature `MapEntity` already solves |
| No convention, always explicit SP names | That's just raw Dapper with extra steps |
