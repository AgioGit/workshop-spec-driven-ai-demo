---
name: lessons-apply
description: Before planning a new feature, search all existing lessons.md files across docs/features/ and surface the most relevant past lessons as planning context. Use this immediately before /feature-plan to catch known pitfalls before implementation.
---

## Parameters

- **featurePath**: <!-- required — repo-root-relative path to the feature folder, e.g. docs/features/134500_pcm_api_getequipment -->

---

## Task

Search all existing lessons across completed features and surface the most relevant ones as context for planning `{featurePath}`.

This skill does **not** write any files. It outputs a prioritised list of relevant lessons directly into the conversation for the planner to use.

---

## Step 1 — Read the feature specification

Read `{featurePath}/specification.md` (fallback: `{featurePath}/instructions.md`) to understand:

- Feature type (endpoint / sync / event / domain-model)
- Which domain entities are involved (borehole, ring, drift, production-area, etc.)
- Which layers are touched (GironAdapter, EF Core, FastEndpoints, MediatR, RabbitMQ)
- Any specific patterns mentioned (dual-write, outbox, incremental sync, upsert)

Extract **search terms** from this analysis: entity names, layer names, and pattern names.

---

## Step 2 — Collect all lessons

Find all lessons files:

```powershell
Get-ChildItem -Recurse -Path docs/features -Filter "lessons.md" | Select-Object FullName
```

Read each file and parse the individual lessons (each starts with `## L{NNN} ·`).

---

## Step 3 — Score and rank relevance

For each lesson, compute a relevance score based on how many of the search terms from Step 1 appear in the lesson's **Tags**, **Title**, or **Summary** fields.

Scoring:
- Tag match on domain entity (e.g. `borehole`): +3 points
- Tag match on layer (e.g. `ef-core`, `gironadapter`): +2 points
- Tag match on problem type (e.g. `tracking-conflict`, `outbox`): +2 points
- Severity 🔴 Critical or 🟠 Major: +2 bonus points
- Severity 🟡 Medium: +1 bonus point

---

## Step 4 — Output the top lessons

Output the **top 10 most relevant lessons** (or all if fewer than 10 qualify with score > 0), grouped by severity (Critical first).

For each lesson output:

```
### {FeatureName} — {L-ID}: {Title}
Severity: {emoji} {Level} | Tags: {tags}
Summary: {one-sentence summary}
Rule: {the generalised rule}
```

Then conclude with a short paragraph:

> **Planning note:** Based on past lessons, the highest-risk areas for this feature are: [X, Y, Z]. Pay particular attention to [specific pattern/layer] when planning.

---

## Requirements

- Read actual lessons files — do not invent lessons
- If no lessons files exist yet, say so and suggest the planner proceed with extra caution in Giron/Oracle/EF Core areas
- Output only lessons with score > 0 unless fewer than 3 qualify (in that case include the top 3 regardless)
- Do not write any files — this skill is read-only output only
