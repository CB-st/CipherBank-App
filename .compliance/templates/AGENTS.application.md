# Application agent contract

- Coordinate one use case per handler/service without owning transport or persistence mechanics.
- Depend on Core ports and domain types.
- Define transaction and idempotency boundaries explicitly.
- Propagate cancellation and stable result/error codes.
- Use decorators for cross-cutting validation, authorization, metrics, caching, or transaction behavior when ordering is explicit.
- Test orchestration with narrow fakes/substitutes; leave provider semantics to integration tests.
