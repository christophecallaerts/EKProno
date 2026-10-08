---
name: interview-via-parent
description: When story-mapping runs as a subagent, AskUserQuestion is unavailable — relay interview questions through the parent agent instead
metadata:
  type: feedback
---

When the story-mapping skill/agent is launched as a **subagent**, the `AskUserQuestion` tool is not in its toolset (and is not fetchable via `ToolSearch`). Plain text written by a subagent is also not shown to the user.

**Why:** The interview process mandates one-question-at-a-time via `AskUserQuestion`, but a subagent has no channel to the user at all, so attempting the interview silently stalls or leads to inventing answers.

**How to apply:** Do not guess the user's answers. Immediately `SubagentHandback` with the next interview question (plus suggested options) and ask the parent agent to relay it and send the answer back via `SendMessage`. Alternatively, recommend the user invoke the story-mapping skill directly in the main conversation, where `AskUserQuestion` works.
