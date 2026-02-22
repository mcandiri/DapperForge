# ADR-001: Stored Procedures Only — No Auto-CRUD

**Status:** Accepted
**Date:** 2024-12-15
**Decision Makers:** Core Team

## Context

The initial prototype (v1) included auto-CRUD functionality that generated parameterized SQL on the fly (`INSERT INTO {table} ...`, `SELECT * FROM {table} WHERE ...`). This directly competed with well-established libraries:

- **Dapper.Contrib** — 30M+ NuGet downloads, same attribute-based CRUD
- **Dapper.SimpleCRUD** — 5M+ downloads
- **RepoDb** — full-featured micro-ORM

We had to decide: keep CRUD and compete on all fronts, or drop it and own a niche.

## Decision

**Remove all auto-CRUD and dynamic SQL generation. Focus exclusively on stored procedure execution.**

## Rationale

1. **No differentiation** — Our CRUD was functionally identical to Dapper.Contrib. No reason for anyone to switch.
2. **Expression parsing was half-baked** — Only supported simple binary comparisons (`p => p.IsActive == true`). Real users would hit `NotSupportedException` within minutes (`Contains`, `StartsWith`, `In` — none worked).
3. **SQL injection surface** — Table and column names were interpolated into SQL strings. While sourced from reflection (not user input), this pattern is a red flag in security reviews.
4. **Convention Engine is the real value** — No existing library offers configurable SP naming conventions with schema support, entity mapping, and startup validation. This is where we differentiate.
5. **Smaller surface area = fewer bugs** — Less code to maintain, test, and document.

## Consequences

- Users who need CRUD should pair DapperForge with Dapper.Contrib — we explicitly recommend this in the README
- The library does zero dynamic SQL generation — all database access goes through stored procedures
- Reduced dependency surface — no need for expression tree parsing infrastructure
- Clearer positioning in the ecosystem: "SP toolkit", not "another ORM"

## Alternatives Considered

| Option | Rejected Because |
|---|---|
| Keep CRUD as optional module | Still competes with established libraries, increases maintenance burden |
| Build full expression tree support | Massive scope, essentially building a query provider — not our goal |
| Use Dapper.Contrib internally | Adds dependency, couples us to their design decisions |
