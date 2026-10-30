# Implementation Tasks

## Stage 1: Single Approver

- [ ] Pass authenticated user's role to ApprovalService.
- [ ] Enforce Manager limit of 50,000.
- [ ] Enforce Director limit of 500,000.
- [ ] Return clear errors for unauthorized approvals.
- [ ] Add positive and negative unit tests.
- [ ] Run all backend tests.
- [ ] Review generated tests.

## Stage 2: Dual Approval

- [ ] Store first approval details.
- [ ] Require a distinct second approver above 500,000.
- [ ] Require second approver to be Director.
- [ ] Prevent same user from approving twice.
- [ ] Add positive and negative unit tests.
- [ ] Run all backend tests.

## Final Validation

- [ ] Add Playwright E2E test.
- [ ] Perform code review using reviewer agent.
- [ ] Run mutation testing.
- [ ] Complete ASSIGNMENT.md.
- [ ] Record demo video.