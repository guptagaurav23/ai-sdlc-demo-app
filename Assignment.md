# AI-SDLC Assignment – Invoice Approval System

video link :  https://nagarro-my.sharepoint.com/:v:/p/gaurav_gupta14/IQCFefc-PgMZSbCJRTWWvAHRAThvljtoEsoKVTi63_qP98M?e=nAeFaA
## 1. Project Overview

This project extends an existing Invoice Approval System using an AI-assisted Software Development Life Cycle (AI-SDLC).

**Repository:** https://github.com/guptagaurav23/ai-sdlc-demo-app/tree/new-ai-demo

**Technology:** .NET 8, C#, xUnit, Playwright, and GitHub Copilot.

## 2. Business Requirements

The following invoice approval rules were defined:

| User Role | Approval Limit |
|---|---:|
| Manager | 50,000 |
| Director | 500,000 |

Additional rules:

- A Manager can approve invoices up to and including 50,000.
- A Director can approve invoices up to and including 500,000.
- An invoice above 500,000 requires two different Directors.
- The first Director approval must not complete the invoice.
- A second, different Director must approve the invoice.
- The same user cannot approve the same invoice twice.
- A non-Director cannot provide the second approval.
- Rejected approval attempts must return a clear error and must not change the invoice state.

## 3. Custom AI Agents

The following custom GitHub Copilot agents were created:

| Orchestrator | Coordinates the overall AI-SDLC workflow |
| Specification | Helps define requirements and specifications |
| Implementation | Implements the approval business rules |
| Testing | Helps create and execute tests |
| Reviewer | Reviews implementation and identifies potential issues |

Agent configuration files are stored in:

- `.github/agents/orchestrator.agent.md`
- `.github/agents/specification.agent.md`
- `.github/agents/implementation.agent.md`
- `.github/agents/testing.agent.md`
- `.github/agents/reviewer.agent.md`

## 4. Custom Agent Skills

The following custom skills were created:

**Approval Domain Skill**

Location: `.github/skills/approval-domain/SKILL.md`

Provides domain-specific guidance for invoice approval rules.

**Test Review Skill**

Location: `.github/skills/test-review/SKILL.md`

Provides guidance for reviewing test cases and test coverage.

**PR Review Skill**

Location: `.github/skills/pr-review/SKILL.md`

Provides guidance for reviewing code changes and documenting findings.

## 5. AI Hooks and Audit Logging

AI hook and audit logging files were configured and updated.

Relevant files:

- `.github/hooks/ai-audit.json`
- `.github/hooks/log-ai-event.cjs`
- `docs/ai/AI-HOOK-LOG.jsonl`

## 5. Unit Testing

The project uses xUnit for backend unit testing.

Unit tests were updated to cover the approval rules, including role-based approval limits and the two-Director approval workflow.

The intended validation includes:

- Manager approval limit.
- Director approval limit.
- Rejection of over-limit approvals.
- First and second Director approvals.
- Duplicate Director approval rejection.
- Non-Director second approval rejection.
- Invoice state remaining unchanged after rejected attempts.

Test command:

```powershell
dotnet test backend/InvoiceApp.sln
```

## 6. Playwright End-to-End Testing

Playwright was used to implement an API-based E2E test.

Test file:

`e2e/tests/approval-limit.spec.ts`

The confirmed test scenario:

1. Sign in using the demo Manager account.
2. Create a 50,001 invoice.
3. Submit the invoice.
4. Attempt approval as Manager.
5. Verify that the API returns HTTP 409.
6. Verify the expected approval policy error.
7. Verify that the invoice state remains unchanged.

The existing Playwright configuration starts the API automatically.

The HTML report was also opened to inspect the Chromium test result.

## 7. PR Review

A custom Reviewer Agent was created to review the invoice approval implementation.

The review checklist includes:

- Business rule correctness.
- Authorization and role validation.
- Duplicate approver protection.
- Repository updates and concurrency handling.
- Unit test coverage.
- Playwright E2E coverage.
- Error handling.
- Invoice state consistency.

The review findings report is intended to be stored at:

`docs/ai/pr-review-findings.md`

Final review findings and the Approve or Request Changes recommendation must be added after the actual review has been completed.

