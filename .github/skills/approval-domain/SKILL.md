---
name: approval-domain
description: Use when analyzing or implementing invoice approval business rules.
---

# Invoice Approval Domain Skill

Before implementation:

1. Read the invoice domain model.
2. Identify existing approval methods.
3. Understand invoice status transitions.
4. Understand persistence and transaction boundaries.
5. Identify the existing role representation.

Rules:

- Manager limit: 50,000 inclusive.
- Director limit: 500,000 inclusive.
- Above 500,000 requires two distinct approvers.
- The second approver must be a Director.
- Duplicate user approvals must be rejected.
- Rejected operations must not mutate invoice state.

Keep business rules in the Domain layer.
Return clear rejection reasons.
Do not assume implementation details without inspecting the code.