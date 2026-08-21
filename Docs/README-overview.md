# RimAI.Quests — overview

Dynamic AI-powered quest description generation for RimWorld.

## Integration

- Harmony patches intercept quest description generation
- Text-AI requests go through RimAI Core admission (Quests priority: Normal)
- Streaming clients live under `Source/Services/Streaming/`

## Requirements

- RimWorld 1.6 (current development target)
- Harmony
- RimAI Core + Communication host stack

## Configuration

Use in-game Quests settings. Gameplay AI credentials follow `OPENAI_RIMAI` (no cross-domain credential fallback).
