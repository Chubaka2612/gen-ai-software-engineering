# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Purpose

Course homework repository for **GenAI and Agentic AI for Software Engineering**. Each assignment lives in its own folder (`homework-1/` through `homework-6/`). Work happens on a dedicated branch (`homework-X-submission`) and is submitted as a PR to `main` on the personal fork.

## Submission Workflow

- Branch per homework: `homework-X-submission`
- PR must include: what was implemented, AI tools and prompts used, how to verify, and screenshots embedded or linked from `docs/screenshots/`
- PRs without detailed descriptions are rejected by the instructor
- Reviewer: `Alexey-Popov`

## Homework Folder Layout

Each `homework-N/` folder follows this shape:

```
homework-N/
├── src/          # source code
├── docs/
│   └── screenshots/
├── demo/         # run.bat / run.sh, sample-requests.http, sample-data.json
├── README.md     # solution overview and AI tools used
├── HOWTORUN.md   # step-by-step run instructions
└── TASKS.md      # assignment spec (do not modify)
```

Each homework has its own `.claude/` subfolder with stack-specific conventions and build hooks.
