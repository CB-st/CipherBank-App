# Cognitive Complexity refactor example

`00-Legacy.cs` mixes validation, approval policy, and untyped string outcomes in a deeply nested function. `01-Refactored.cs` preserves the decision order while using guard clauses, a named validation decision, and an explicit result. `02-Tests.cs` demonstrates branch and boundary evidence.

## Translation method

1. Write a decision table from the legacy return order. Earlier checks matter because multiple inputs may be invalid at once.
2. Add characterization tests for every current return code and boundary.
3. Replace nesting with guards without changing check order.
4. Extract a function only when its statements form a nameable concept.
5. Introduce the typed result after comparison tests show the same external code/status mapping.
6. Run the exact Sonar C# analyzer and confirm S3776 is resolved. Do not claim a score from visual inspection.

This example is copied as source guidance, not added to the receiving solution automatically. Adapt namespaces and contracts, compile it in the real project, and keep any intentional rule change in a separate review.
