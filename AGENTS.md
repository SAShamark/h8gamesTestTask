# AGENTS.md

Repository instructions for AI coding agents working on this Unity project.

## Core Behavior

- Think before coding. State assumptions when the task is ambiguous.
- Prefer the smallest change that solves the requested problem.
- Touch only files directly related to the task.
- Match the existing C# and Unity project style.
- Do not refactor adjacent code unless the user asks for it or the change is required.
- Remove only unused code introduced by your own changes.

## Unity Project Rules

- Treat `Assets/`, `Packages/`, and `ProjectSettings/` as the meaningful project sources.
- Do not edit generated or machine-local folders such as `Library/`, `Temp/`, `obj/`, `Logs/`, `.idea/`, or `UserSettings/` unless explicitly requested.
- Do not manually edit generated `.csproj` or `.sln` files unless the task is specifically about IDE/project generation.
- Preserve `.meta` files. When adding, moving, or deleting Unity assets, keep the related `.meta` files consistent.
- Be careful with scenes, prefabs, ScriptableObjects, and serialized assets. Small text changes can affect Unity serialization.
- Avoid changing third-party plugin code under `Assets/Plugins/` unless the task is specifically about that plugin.

## C# Gameplay Code

- Keep gameplay logic readable and direct.
- Prefer explicit names over clever abstractions.
- Avoid adding new managers, service locators, singletons, or global state unless the existing codebase already uses that pattern for the same problem.
- Use Unity lifecycle methods intentionally: `Awake` for local setup, `Start` for cross-object setup, `Update` only when per-frame work is required.
- Cache component references when they are used repeatedly.
- Guard against missing serialized references when failure would be hard to diagnose.

## Verification

- After code changes, check for compile errors when practical.
- For gameplay changes, describe the manual Unity Editor test path if automated tests are not available.
- If changing prefabs, scenes, or serialized assets, mention what should be checked in the Unity Editor.

## Communication

- If multiple interpretations are possible, ask or state the chosen assumption.
- If a simpler approach exists, mention it.
- If unrelated issues are noticed, report them separately instead of fixing them silently.
- Summarize changed files and verification performed at the end of the task.
