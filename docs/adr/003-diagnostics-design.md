# ADR-003: Diagnostics as First-Class Feature

**Status:** Accepted
**Date:** 2025-01-10
**Decision Makers:** Core Team

## Context

In production, the most common SP-related incidents are:

1. **Slow queries** — an SP that was fast starts degrading under load
2. **Silent failures** — SP executes but returns unexpected results
3. **Missing SPs** — someone deploys code that references an SP not yet created in the target database

We needed a diagnostics system that catches all three without requiring external APM tools.

## Decision

**Build a three-layer diagnostics system:**

1. **Structured logging via `ILogger`** — integrates with any logging provider (Serilog, NLog, etc.)
2. **`OnQueryExecuted` callback** — programmatic access to every execution event for custom metrics, alerting, or tracing
3. **`ValidateSpOnStartup`** — hosted service that logs all expected SP names on application start

All diagnostics are **opt-in** via `EnableDiagnostics = true`. Zero overhead when disabled.

## Rationale

### Why `ILogger` and not custom logging?

Every .NET project already has `ILogger` configured. Using a custom logging interface would force users to write adapters. Structured logging with named parameters (`{SpName}`, `{ElapsedMs}`) works correctly with all major sinks.

### Why a callback and not events/observables?

`Action<QueryEvent>` is the simplest integration point. Users can bridge to any system:

```csharp
options.OnQueryExecuted = e => metrics.RecordSpExecution(e.SpName, e.Duration);
options.OnQueryExecuted = e => activitySource.StartActivity(e.SpName)?.SetTag("duration", e.Duration);
```

Events or `IObservable<T>` would add complexity without meaningful benefit for this use case.

### Why no emoji in log messages?

The v2 prompt initially used emoji (`⚠ SLOW`, `✗ FAILED`). We removed them because:
- Structured logging sinks (Seq, Elasticsearch) index on fields, not message content
- Some terminals and log viewers don't render emoji correctly
- `LogLevel.Warning` and `LogLevel.Error` already convey severity

### Why validate on startup instead of runtime?

Runtime validation (checking if SP exists before each call) adds a round-trip per execution. Startup validation logs all expected SPs once, letting teams cross-reference against their database. This is a development aid, not a runtime guard.

## Consequences

- `QueryEvent` is the single data model for all diagnostic scenarios (success, failure, slow)
- The callback fires even when `EnableDiagnostics = false` — this is intentional so custom metrics always work regardless of logging config
- `ValidateSpOnStartup` currently only logs expected names — it does not query the database to verify existence (this requires an open connection during startup, which has its own complications)

## Future Considerations

- **OpenTelemetry integration** — expose `ActivitySource` for distributed tracing
- **Metric counters** — execution count, error rate, p99 latency per SP
