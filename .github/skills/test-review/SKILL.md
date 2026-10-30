---
name: test-review
description: Reviews invoice approval tests for meaningful business-rule coverage.
---

# Test Review Skill

Review tests for:

1. Manager approval at 50,000.
2. Manager rejection above 50,000.
3. Director approval at 500,000.
4. Director rejection above 500,000 without a second approval.
5. Two distinct approvers.
6. Duplicate approver rejection.
7. Non-director second approver rejection.
8. Invoice state unchanged after rejected attempts.

Check that tests:
- Assert actual results.
- Assert rejection reasons.
- Assert persisted state.
- Avoid testing implementation details unnecessarily.
- Include boundary values.