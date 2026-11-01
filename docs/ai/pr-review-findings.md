# Invoice Approval PR Review

## Scope and summary

Reviewed the current `new-ai-demo` branch and working tree against [AGENTS.md](../../AGENTS.md), [approval-limits.md](../specs/approval-limits.md), and [approval-limits-tasks.md](../tasks/approval-limits-tasks.md). Inspected the approval domain service, modified API and repository files, authentication/authorization implementation, all unit tests, and all Playwright tests present in the working tree.

The approval service enforces inclusive Manager and Director limits, requires two distinct Director approvals above 500,000, and rejects invalid approval attempts before updating the invoice. The in-memory repository performs lifecycle decisions within its lock. API approval identity and role come from the authenticated session; the reject endpoint retains its Approver/Admin restriction.

The Playwright test file is present in the working tree but is untracked, so it is not part of the current branch diff. The branch also deletes its CI workflow. No application source files were changed during this review.

## Findings

### Finding PR-001

- **Severity:** Medium
- **File and line:** `.github/workflows/ci.yml:1-18` (deleted on this branch)
- **Description:** The branch removes the CI workflow that built and tested the solution on pushes and pull requests to `main`.
- **Impact:** Changes can merge without the repository's automated build/test gate.
- **Recommended fix:** Restore the workflow or provide an equivalent CI workflow that installs .NET 8, builds the solution, and runs its tests.
- **Verdict:** Deferred
- **Reason for verdict:** The review is report-only; the branch author should confirm whether CI removal was intentional and supply a replacement if needed.

### Finding PR-002

- **Severity:** Medium
- **File and line:** [approval-limit.spec.ts](../../e2e/tests/approval-limit.spec.ts):1-248 (untracked)
- **Description:** The Playwright suite exists in the working tree and passes, but the file is untracked and absent from the branch diff. The `Final Validation` section of [approval-limits-tasks.md](../tasks/approval-limits-tasks.md):21-25 also still leaves “Add Playwright E2E test” unchecked.
- **Impact:** The branch as submitted does not deliver the E2E coverage currently present in this workspace; the task checklist also inaccurately suggests that coverage has not been added.
- **Recommended fix:** Include the test file in the PR and update the task checklist after confirming it belongs in the change.
- **Verdict:** Deferred
- **Reason for verdict:** No files were staged or committed during this review; inclusion in the PR is a human/repository decision.

### Finding PR-003

- **Severity:** Low
- **File and line:** [ApprovalServiceTests.cs:224](../../backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs:224)
- **Description:** The concurrency unit test exercises simultaneous duplicate approvals by the same Director, but not simultaneous approvals by two distinct Directors for one high-value invoice.
- **Impact:** The locking design appears to serialize approval decisions, but the distinct-approver completion path is not directly tested under contention.
- **Recommended fix:** Add a concurrency test that submits approvals from two different Directors concurrently and asserts that the invoice ends approved with exactly two distinct Director entries and no lost or duplicate history.
- **Verdict:** Deferred
- **Reason for verdict:** Existing repository locking and sequential behavior are correct by inspection; additional concurrency coverage was not added during this review.

### Finding PR-004

- **Severity:** Low
- **File and line:** [UC-1-bulk-approval.md:22](../use-cases/UC-1-bulk-approval.md:22)
- **Description:** `git diff --check main...HEAD` reports a new blank line at EOF in this file.
- **Impact:** The branch-level whitespace check fails and adds avoidable diff noise.
- **Recommended fix:** Remove the extra blank line at EOF.
- **Verdict:** Deferred
- **Reason for verdict:** The requested review is report-only; no unrelated documentation edits were made.

## Requirements and coverage

| Requirement | Review result |
|---|---|
| Manager approves exactly 50,000 | Covered by unit and Playwright tests |
| Manager cannot approve 50,001 | Covered; API returns 409 and tests compare the persisted invoice before and after |
| Director approves exactly 500,000 | Covered by unit and Playwright tests |
| First Director approval above 500,000 leaves invoice pending | Covered by unit and Playwright tests |
| Second, distinct Director completes approval | Covered sequentially by unit and Playwright tests |
| Same Director cannot approve twice | Covered, including case-insensitive username comparison; failed attempt checks persisted state |
| Non-Director cannot be second approver | Covered by unit and Playwright tests; failed attempt checks persisted state |
| Rejected attempt does not mutate invoice | Unit helper compares invoice fields, approval history, and repository update count; E2E tests compare the full fetched invoice |
| Authorization and role validation | Approval uses session-derived identity/role and domain policy validation; reject keeps its Approver/Admin restriction |
| Concurrent approval safety | Repository locks the full lifecycle decision; concurrent duplicate by one user is tested, but two distinct concurrent Directors are not |
| Security/error handling | Session middleware rejects missing/invalid bearer tokens; domain policy failures are mapped to HTTP 409 with clear error bodies; no exploitable defect identified in the reviewed approval path |

### Missing test coverage

- Concurrent approval attempts by two distinct Directors for the same high-value invoice.
- Direct API tests for missing/invalid session tokens and for reject-role restrictions.
- Browser/UI interaction coverage. The Playwright cases use `APIRequestContext` against the API rather than driving the web interface.
- The E2E file is not tracked by the current branch, as noted in PR-002.

## Tests and checks actually executed

1. `dotnet test backend/InvoiceApp.sln`
   - **Result:** Aborted before tests ran. Build/restore completed, but the test host requires `Microsoft.NETCore.App 8.0.0`; this environment has only .NET 10.0.13 and 10.0.14.
2. `$env:DOTNET_ROLL_FORWARD = 'Major'; dotnet test backend/InvoiceApp.sln`
   - **Result:** Passed — 23 passed, 0 failed, 0 skipped.
   - **Note:** This workaround used .NET major-version roll-forward to execute the .NET 8 target using the installed runtime.
3. `git diff --check`
   - **Result:** Passed for the tracked working-tree diff. Git emitted a line-ending warning for the pre-existing modification to `docs/ai/AI-HOOK-LOG.jsonl`.
4. `git diff --check main...HEAD`
   - **Result:** Failed — reports a new blank line at EOF in [UC-1-bulk-approval.md:22](../use-cases/UC-1-bulk-approval.md:22) (PR-004).
5. `npm.cmd test -- --reporter=list` (run from `e2e`)
   - **Result:** Passed — 7 passed, 0 failed.

## Remaining risks

- The branch deletes its CI workflow and therefore lacks that branch-level automated build/test gate unless another workflow replaces it.
- The distinct-Director concurrency interleaving lacks direct test coverage.
- Playwright tests exercise API behavior, not browser UI behavior.
- The E2E test file currently exists only as an untracked working-tree file.
- Demo users have static plaintext credentials and sessions/data are process-local; this configuration is for the demo and should not be used as production authentication/storage.

## Final recommendation

**Request Changes.** Restore or replace the deleted CI workflow, include the E2E test in the branch if intended for this PR, and resolve the branch whitespace error. Consider adding distinct-Director contention coverage. The approval policy behavior reviewed here otherwise matches the stated requirements.
