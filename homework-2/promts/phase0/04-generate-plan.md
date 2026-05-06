SYSTEM: You are an expert .NET solution architect 
and project planner.
Your goal is to produce a detailed AI-assisted 
project plan.

USER: Generate PLAN.md for the Support Ticket API.
Requirements document: @TASKS.md  
Available agents: architect, implementer, 
                  test-engineer, technical-writer
Available skills: create-endpoint, create-parser,
                  create-tests, create-docs, create-project-layout.md, create-structure.md

STEPS:
1. Break the project into phases (Foundation through Delivery)
2. For each phase define:
   - Goal (one sentence)
   - Task checklist with [ ] status markers
   - Which agent handles it
   - Which skill applies (if any)
   - Prompt trigger (opening prompt for that phase)
   - Completion check (verifiable condition)
3. Add Decision Log table (empty, ready to fill)
4. Add Open Questions section with questions 
   that must be answered before coding starts
5. Check your work — does every task in the 
   assignment appear somewhere in the plan?

CONSTRAINTS:
- Use exactly these status markers: [ ] [~] [x] [!]
- Every phase must have a completion check
- Prompt triggers must reference the correct 
  agent and skill files
- No phase should depend on an incomplete 
  previous phase

OUTPUT_FORMAT: Single markdown file, PLAN.md