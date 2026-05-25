# Assignment 05 Final2 Documentation

This folder is intentionally focused. Its purpose is to help defend and
maintain the Assignment 05 Final2 modular-monolith project without forcing
developers to read old phase audits that no longer describe the current
system.

Official requirement sources:
- Final1: https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final1
- Final2: https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final2

## Start Here

- [../README.md](../README.md) - runnable assignment overview, URLs, demo users,
  validation snapshot, and high-level feature coverage.
- [final2-architecture.md](final2-architecture.md) - defended modular-monolith
  architecture, module boundaries, persistence state, and risks.
- [final2-traceability.md](final2-traceability.md) - requirement-to-code/test
  evidence matrix for Final2.
- [final1-final2-roadmap.md](final1-final2-roadmap.md) - historical Final1
  baseline and maintenance notes retained for comparison.
- [final1-structure-reset.md](final1-structure-reset.md) - root-level
  architecture reset target, preserved assets, deferred removals, and migration
  checklist.
- [a3-saas-plan.md](a3-saas-plan.md) - current product and scope blueprint for
  the multi-gym SaaS assignment.

## Durable Reference Docs

- [architecture.md](architecture.md) - current runtime surfaces, projects,
  layer boundaries, request flows, and design decisions.
- [reference-architecture-parity.md](reference-architecture-parity.md) - mapping
  from the local LabRent reference zip to the gym implementation, including
  copied UI shell patterns and deliberate architecture differences.
- [module-boundaries.md](module-boundaries.md) - retained status note explaining
  that the earlier module architecture is no longer active.
- [domain-workflows.md](domain-workflows.md) - business workflows and rules by
  domain area.
- [security-and-access.md](security-and-access.md) - auth, roles, tenant
  isolation, CORS, token handling, and security limitations.
- [data-model.md](data-model.md) - ERD and entity notes.
- [api.md](api.md) - public route overview, REST semantics, and error behavior.
- [testing.md](testing.md) - automated commands, coverage summary, manual smoke
  path, and known test gaps.
- [deployment.md](deployment.md) - local runtime, Docker, GitLab CI/CD,
  production variables, and deployment smoke checks.

## Defense Packs

- [final1-defense.md](final1-defense.md) - compact Final1 evidence and defense
  notes.
- [final2-defense.md](final2-defense.md) - compact Final2 defense notes and
  remaining caveats.

## Logs

- [ai-usage.md](ai-usage.md) - assignment-local AI assistance log.
- [../../../../docs/ai-prompts.md](../../../../docs/ai-prompts.md) - repository-level
  AI assistance log.

## Cleanup Policy

The removed docs were mostly one-time phase plans, audits, or narrow contract
notes. Useful current information was consolidated into the durable docs above.

Do not add a new document for every slice. Prefer updating one of these files:
- Final1 development priority -> `final1-final2-roadmap.md`
- architecture boundary change -> `architecture.md`
- domain rule or workflow behavior -> `domain-workflows.md`
- auth, roles, tenant isolation, token, or CORS behavior ->
  `security-and-access.md`
- runtime, CI, Docker, or smoke behavior -> `deployment.md`
- tests or validation evidence -> `testing.md`
- implementation architecture -> `architecture.md`
- external/reference project parity -> `reference-architecture-parity.md`

Only create a new doc when it is expected to remain useful after the current
phase is complete.
