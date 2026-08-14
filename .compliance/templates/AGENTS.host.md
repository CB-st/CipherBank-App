# Host/API/UI agent contract

- Own startup, dependency composition, middleware/UI lifecycle, transport models, and error mapping.
- Keep handlers/controllers/view models thin and delegate policy.
- Validate inputs before domain execution and return consistent errors.
- Propagate request/window cancellation and perform bounded shutdown.
- Keep authentication and authorization distinct from validation.
- Test middleware/routing or UI behavior through the real host boundary.
- Do not query database contexts directly when an application port exists.
- When views/view models live in a separate project, also apply `AGENTS.ui.md` there; keep this file focused on composition, startup, and transport mapping.
