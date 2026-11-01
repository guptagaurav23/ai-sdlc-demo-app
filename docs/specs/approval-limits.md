# Invoice Approval Limits

## Business Rules

1. Managers can approve invoices up to and including 50,000.
2. Directors can approve invoices up to and including 500,000.
3. Managers cannot approve invoices above 50,000.
4. Directors cannot single-approve invoices above 500,000.
5. Rejected approval attempts must not modify invoice state.
6. Above 500,000 will require two distinct approvers.
7. The second approver must be a Director.
8. The same user cannot approve twice.

## Stage 1 Scope

Implement rules 1-5 only.

## Stage 2 Scope

Implement rules 6-8 separately. For invoices above 500,000, the first approval must be made by a Director, followed by a second, distinct Director. The invoice remains PendingApproval after the first approval and becomes Approved only after the second valid approval. Record each approver's username, role, and approval timestamp.

The first-approver role was clarified as Director, followed by a different Director. Managers remain limited to invoices up to 50,000; Directors cannot finalize invoices above 500,000 with a single approval.

## Acceptance Criteria

- Boundary values 50,000 and 500,000 are accepted for their respective roles.
- Amounts above role limits are rejected.
- Unauthorized attempts preserve invoice status and decision fields.
- Existing submit and reject functionality continues working.