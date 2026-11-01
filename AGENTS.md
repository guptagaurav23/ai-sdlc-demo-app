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

Controllers use routes shaped as `api/[controller]`, inherit `ApiControllerBase`, and use `Handle(...)` to map service errors to HTTP responses. Keep request-body records in their controller. API calls require sign-in. Approval authorization and amount limits are enforced in the domain service so unauthorized roles receive a clear policy error; the reject endpoint retains its Approver/Admin role restriction.

The in-memory repository is process-local, not durable storage. Lifecycle decisions performed through `IInvoiceRepository.ExecuteAtomically(...)` are serialized under the repository lock. Do not assume unrelated multi-step repository operations are atomic.

# 3. Invoice Approval Business Rules

Implement and preserve these approval rules in the domain layer:

- A Manager may approve an invoice with an amount up to and including 50,000.
- A Director may approve an invoice with an amount up to and including 500,000.
- An invoice above 500,000 requires two distinct approvers.
- The first and second approvers for an invoice above 500,000 must be distinct Directors.
- The same user must never count as both approvers.
- When an approval attempt is rejected, return a clear reason that explains why it was rejected.
- A rejected attempt must not change any invoice data or approval history.

Stage 1 implements single-approver Manager/Director amount limits. Stage 2 requires two distinct Director approvals for invoices above 500,000: the invoice remains pending after the first approval and becomes approved after the second. `Invoice.ApprovalHistory` stores each approver's username, role, and timestamp. Failed approval attempts must leave invoice data and approval history unchanged.

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

## Invoice Approval Feature — Agent Instructions

### Business Requirements

Implement role-based invoice approval limits.

1. Managers can approve invoices up to and including 50,000.
2. Directors can approve invoices up to and including 500,000.
3. Invoices above 500,000 require two distinct approvers.
4. For invoices above 500,000, the first and second approvers must both have the Director role.
5. The same user must never count as both approvers.
6. Any rejected approval attempt must return a clear reason.
7. A rejected attempt must not modify any invoice data, approval history, status, or other persisted state.

### Architecture and Layering

- Keep business rules inside the Domain layer.
- Keep API controllers thin.
- Do not duplicate approval rules in the API layer.
- Follow existing repository and dependency injection patterns.
- Avoid introducing unnecessary dependencies.
- Preserve existing functionality.

### Approval Boundary Rules

- An invoice of exactly 50,000 can be approved by a Manager.
- An invoice above 50,000 and up to 500,000 requires a Director.
- An invoice of exactly 500,000 can be approved by one Director.
- An invoice above 500,000 requires two distinct approvers.
- The second approver must be a Director.
- Two approvals by the same user are never sufficient.

### Testing Expectations

- Use the existing test framework and conventions.
- Add unit tests for every business rule.
- Test boundary values explicitly.
- Test duplicate approver rejection.
- Test rejection of a non-director second approver.
- Verify that rejected attempts leave invoice state unchanged.
- Add a Playwright E2E test for rejected over-limit approval.
- Review every AI-generated test for correctness and meaningful assertions.

### Commands

Run the existing test suite using:

`dotnet test backend/InvoiceApp.sln`

Run Playwright tests using the existing E2E configuration and documented commands.

### Code Review Expectations

- Review every AI-generated change.
- Check for business-rule bypasses.
- Check for duplicate approval vulnerabilities.
- Check that failed operations do not mutate state.
- Check concurrency and persistence behavior.
- Do not automatically accept AI review findings.
- Record every review finding as fixed, dismissed with justification, or deferred with justification.

### AI Agent Expectations

- Inspect existing code before implementing.
- Follow the specification and task breakdown.
- Implement features incrementally.
- Do not combine both implementation stages into one change.
- Run tests after each implementation stage.
- Record prompts, responses, tool usage, and human decisions in the AI interaction log.
- Never claim tests pass without executing
