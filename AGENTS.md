# 1. Project Overview

This repository contains a small invoice approval demo application. The backend is an ASP.NET Core 8 Web API targeting .NET 8, the web interface is plain HTML, CSS, and JavaScript served by the API, and invoice data and sign-in sessions are stored in memory. The repository also contains an xUnit unit-test project and a Playwright end-to-end test setup.

The application supports creating and submitting invoices, then approving or rejecting pending invoices. The approval policy described below is required behavior for this project; do not assume every rule is already implemented in the current service.

# 2. Architecture

- `backend/src/InvoiceApp.Domain` contains the domain model, repository interface, and business rules. `ApprovalService` owns invoice lifecycle operations. Keep approval policy and state-transition rules here, not in controllers or `Program.cs`.
- `backend/src/InvoiceApp.Api` contains the ASP.NET Core API, authentication, and in-memory persistence. Controllers are in `Controllers/`; sign-in and role enforcement are in `Auth/`; `InMemoryInvoiceRepository` implements the domain repository interface. The UI is served from `wwwroot/`.
- `backend/src/InvoiceApp.Api/Program.cs` configures dependency injection and the HTTP middleware pipeline. Register services here, but do not put business logic here.
- `backend/tests/InvoiceApp.Tests` is the xUnit unit-test project. Use `FakeInvoiceRepository` when testing domain behavior. Follow the existing `<ClassUnderTest>Tests` naming pattern.
- `e2e` contains the Playwright configuration and test/page folders. Add browser-level tests there when a feature needs end-to-end coverage.
- `docs/use-cases` contains short briefs for potential features.

Controllers use routes shaped as `api/[controller]`, inherit `ApiControllerBase`, and use `Handle(...)` to map service errors to HTTP responses. Keep request-body records in their controller. Protect endpoints with the existing sign-in and role mechanisms; approval actions currently use `RequireRole("Approver", "Admin")`. The requested Manager/Director approval policy does not currently match those role names, so resolve role mapping explicitly when implementing it; do not silently assume the roles are interchangeable.

The in-memory repository is process-local, not durable storage. Although it uses locks for individual repository operations, do not assume a complete approval decision is atomic or protected from concurrent updates.

# 3. Invoice Approval Business Rules

Implement and preserve these approval rules in the domain layer:

- A Manager may approve an invoice with an amount up to and including 50,000.
- A Director may approve an invoice with an amount up to and including 500,000.
- An invoice above 500,000 requires two distinct approvers.
- The second approver for an invoice above 500,000 must be a Director.
- The same user must never count as both approvers.
- When an approval attempt is rejected, return a clear reason that explains why it was rejected.
- A rejected attempt must not change any invoice data or approval history.

The current `Invoice` model records a single decision (`DecidedBy`, `DecidedAt`, and `DecisionNote`); it does not currently model a separate approval history. Do not describe the rules above as already implemented. If approval history is introduced, ensure failed/rejected attempts leave both the invoice and its history unchanged.

# 4. Coding Guidelines

- Follow the existing project boundaries, naming, formatting, and patterns.
- Put business rules in `InvoiceApp.Domain`; keep controllers focused on HTTP concerns.
- Use the existing repository interface and inject dependencies through the existing registration pattern.
- Use clear, descriptive names. Avoid one-letter variables and `flag` parameters.
- Avoid unnecessary dependencies and unrelated refactoring.
- Do not modify unrelated files.

# 5. Testing Guidelines

The unit-test project is `backend/tests/InvoiceApp.Tests/InvoiceApp.Tests.csproj`. From the repository root, run the repository's documented test command:

```text
dotnet test backend/InvoiceApp.sln
```

Add or update unit tests for every approval rule, including negative cases. Cover the 50,000 and 500,000 boundaries and amounts immediately above them; unauthorized roles and amounts; missing or duplicate approvers; a non-Director second approver; clear rejection reasons; and assertions that rejected attempts leave invoice data and approval history unchanged. Use `FakeInvoiceRepository` for domain tests.

# 6. Review Expectations

Review changes for boundary conditions, duplicate approvers, authorization, valid state transitions, unchanged data after rejected attempts, and concurrency or race conditions. Repository-level locks do not by themselves guarantee that a multi-step approval decision is atomic.

Never assume generated code or tests are correct: inspect the implementation and test the required behavior. Require human review before committing.
