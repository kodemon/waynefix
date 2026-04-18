# WayneFix — Reliable Report Notification Service

## Overview

WayneFix is a citizen reporting service for Gotham City, allowing residents to report infrastructure issues such as broken streetlights, potholes, and damaged fire hydrants. This implementation solves a specific problem identified by the operations team: email notifications to case handlers were being silently lost during traffic spikes, with no way to detect or recover from the failure.

The solution decouples email notification from the HTTP request lifecycle using the **Transactional Outbox Pattern**, ensuring that no notification is ever silently lost — even if the email service is down or the application restarts.

AI Disclaimer:

 - Claude Sonnet 4.6 used in:
   - Internal design rubbderduck/discussion.
   - Code Review.
   - Documentation.
   - Test Scaffolding.

---

## Running the Application

### Start

```bash
dotnet run --project src/WayneFix.Api
```

The application will automatically create and initialise the SQLite database on first run using `scripts/schema.sql`. No Docker or external dependencies are required.

The API is available at `https://localhost:5127` with OpenAPI docs at `/openapi`.

### Submitting a Report

```bash
curl -X POST http://localhost:5127/api/reports \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Broken streetlight on Main St",
    "location": "Main St & 5th Ave",
    "recipients": ["handler@gotham.gov"]
  }'
```

---

## Architecture

### Project Structure

```
src/
├── Api/                    # HTTP layer — controllers, contracts, Program.cs
├── Application/            # Use case orchestration — ReportingService
├── Domain/                 # Core business logic — entities, interfaces, value objects
├── Infrastructure/         # External concerns — persistence, email
└── Workers/                # Background processing — OutboxProcessorWorker
```

The project follows a **Clean Architecture** approach where dependency direction always points inward. The Domain layer has no knowledge of infrastructure, persistence, or HTTP concerns. Infrastructure implements interfaces defined in the Domain.

### The Outbox Pattern

The core of the solution is the Transactional Outbox Pattern:

1. When a citizen submits a report, the `ReportingService` creates both a `Report` and an `OutboxMessage` in a **single database transaction**. Either both are saved or neither is — the notification can never be lost at the point of submission.
2. The `OutboxProcessorWorker` runs as a background service, polling for unprocessed outbox messages and attempting to send the email notification.
3. On success, `CompletedAt` is stamped on the message. On failure, the error is recorded and the message remains pending for retry.

This guarantees that even if the email service is down during submission, the message is safely persisted and will be delivered as soon as the service recovers.

### Key Design Decisions

**Domain entities are persistence-ignorant.** Entities (`Report`, `OutboxMessage`) use private setters and constructor-enforced invariants. They have no knowledge of Dapper, SQLite, or any infrastructure concern. Persistence uses dedicated record DTOs (`ReportRecord`, `OutboxMessageRecord`) that Dapper hydrates freely, with mappers translating to domain aggregates.

**Reconstitute factory methods.** Both entities expose a `static Reconstitute(...)` factory method used exclusively for rehydration from the database. This separates creation semantics (with full validation) from reconstitution semantics (restoring existing state), without leaking any persistence concerns into the entity.

**`CorrelationId` over `ReportId`.** The `OutboxMessage` references its source entity via a generic `CorrelationId` rather than a typed `ReportId`. This keeps the outbox mechanism decoupled from any specific domain entity. A `CorrelationType` discriminator would be the natural extension to support routing to different entity fetchers.

**Payload carries delivery metadata only.** The `OutboxEmailPayload` contains only what is needed to deliver the notification — in this case, the recipient list. The content of the notification (report text) is fetched from the `Report` at processing time, keeping the report as the single source of truth and avoiding duplication.

**Atomic transaction boundary in the repository.** `InsertReportAsync` accepts both a `Report` and `OutboxMessage` and inserts them within a single transaction. There is no separate insert method for either entity — they cannot exist independently at creation time.

**Attempts-based retry backoff.** The outbox processor will not retry a message that has failed 3 or more times. The `Attempts` column is derived from the length of the `Errors` JSON array on each update, keeping the domain entity free of persistence-level retry tracking.

---

## What Would Be Different in Production

### Database

SQLite is used here for zero-friction local setup. In production this would be **PostgreSQL**, with proper column types (`UUID`, `TIMESTAMPTZ`, `JSONB`) and the schema managed by a migration tool such as **Fluent Migrator** rather than a raw SQL file executed at startup.

### Distributed Outbox Processing

The current worker fetches all pending messages on each poll. In a distributed deployment with multiple instances, this creates a **competing consumers** problem — two workers could pick up and process the same message simultaneously, resulting in duplicate emails.

The standard solution is `SELECT FOR UPDATE SKIP LOCKED`, which locks rows at the database level so each message is claimed by exactly one worker. SQLite does not support this construct; it is available in PostgreSQL and is the primary reason for the database choice in a production system.

### Dead Letter Handling

Messages that exceed the maximum retry attempts are currently abandoned silently. In production, exhausted messages should be moved to a **dead letter table** for manual inspection and replay. An alert or metric should be configured to notify on-call when dead letters accumulate, ensuring no citizen report is permanently lost without human awareness.

### Email Service

`ConsoleEmailService` is a stub that logs to stdout. A production implementation would use a proper SMTP client or a managed email delivery service such as SendGrid or AWS SES, registered behind the `IEmailService` interface with no changes required elsewhere in the codebase.

### Observability

The current application has no metrics, tracing, or structured logging. Production requirements would include distributed tracing (e.g. OpenTelemetry), structured logs with correlation IDs, and metrics on outbox queue depth and processing latency — particularly important given WayneFix's history of silent failures during traffic spikes.

### Authentication & Authorisation

Not implemented as it is outside the scope of this task. The intended design would use **OIDC** for authentication, issuing **JWT Bearer tokens** consumed by the API via `AddJwtBearer`. 

For authorisation, **Cerbos** would handle granular access control via external policy files rather than hardcoded role checks in the application. This separates policy from code — rules can be updated without redeployment, and policies can evaluate attributes of both the principal (role, district, seniority) and the resource (report status, location, assigned handler).

Two base roles are anticipated: `citizen` for report submission, and `case-handler` for internal report management. Cerbos policies would then express finer-grained rules on top — for example, restricting case handlers to reports within their assigned district, or limiting which report status transitions a junior handler can perform.

### Configuration

Connection strings and other environment-specific values are currently in `appsettings.json`. In production these would be injected via environment variables or a secrets manager, never committed to source control.