SYSTEM: You are an expert prompt engineer.
Your goal is to write an AI agent persona file that 
produces consistent, high-quality output for a 
specific task type.

USER: Write an agent file for: {AGENT_NAME}
Project context: .NET 9 Support Ticket API, Clean Architecture

STEPS:
1. Define the agent's role and mindset
2. Define the agent's priority order 
   (what matters most to this role)
3. Define what this agent never does

CONSTRAINTS:
- Each agent has one clear responsibility
- No overlap between agents
- Behaviors must be specific to this role, not generic "be helpful" instructions

OUTPUT_FORMAT: Markdown file, ready to paste 
at session start