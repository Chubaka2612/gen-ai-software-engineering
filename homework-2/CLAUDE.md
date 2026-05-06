# Project: AiTicketHub Support Ticket API

## How to Work on This Project

Before starting any task, read these files in order:
1. `ai-context/CONTEXT.md` — tech stack, layer structure, Result<T> contract, hard constraints
2. `ai-context/PLAN.md` — current phase status, what's done, what's next

## Agents

Match the task type to the correct agent file before generating any output:

| Task type               | Agent file                          |
|-------------------------|-------------------------------------|
| Designing structure     | `ai-context/agents/architect.md`    |
| Writing C# code         | `ai-context/agents/developer.md`    |
| Writing tests           | `ai-context/agents/test-engineer.md`|
| Writing documentation   | `ai-context/agents/technical-writer.md` |

## Skills

Match the task to the correct skill file and follow its STEPS exactly:

| Task                              | Skill file                                   |
|-----------------------------------|----------------------------------------------|
| Bootstrap the solution (run once) | `ai-context/skills/create-project-layout.md` |
| Design a new entity's structure   | `ai-context/skills/create-structure.md`      |
| Implement all methods for an entity | `ai-context/skills/implement-feature.md`   |
| Add one endpoint to an existing entity | `ai-context/skills/create-endpoint.md` |
| Add a file-format parser          | `ai-context/skills/create-parser.md`         |
| Generate tests for an operation   | `ai-context/skills/create-tests.md`          |
| Generate documentation            | `ai-context/skills/create-docs.md`           |

## After Every Task

- Update `ai-context/PLAN.md`: mark completed items `[x]`, in-progress items `[~]`, blocked items `[!]`.
- If a completion check passed, log any non-obvious decisions in the Decision Log table.
- If a contract changed (interface signature, DTO shape, error code), flag it as a breaking change before proceeding.
