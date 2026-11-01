# Implementation Tasks

## Stage 1: Single Approver

- [x] Pass authenticated user's role to ApprovalService.
- [x] Enforce Manager limit of 50,000.
- [x] Enforce Director limit of 500,000.
- [x] Return clear errors for unauthorized approvals.
- [x] Add positive and negative unit tests.
- [x] Run all backend tests (23 passed; used .NET 10 major-version roll-forward because the .NET 8 runtime is unavailable).
- [x] Review generated tests.

## Stage 2: Dual Approval

- [x] Store first approval details.
- [x] Require a distinct second approver above 500,000.
- [x] Require the first and second approvers to be Directors.
- [x] Prevent same user from approving twice.
- [x] Add positive and negative unit tests.
- [x] Run all backend tests (23 passed; used .NET 10 major-version roll-forward because the .NET 8 runtime is unavailable).

## Final Validation

- [ ] Add Playwright E2E test.
- [ ] Perform code review using reviewer agent.
- [ ] Run mutation testing.
- [ ] Complete ASSIGNMENT.md.
- [ ] Record demo video.