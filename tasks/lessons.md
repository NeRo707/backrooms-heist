# Lessons

- Before proposing changes to level spawning, inspect existing scene components and ask whether the level already has an authored spawn-point workflow. Preserve that workflow unless the user explicitly requests a replacement.
- When a user asks for a cleaner Unity interaction implementation, prefer a simple, component-driven approach over ad-hoc raycast/overlap logic and keep behavior explicit and maintainable.
