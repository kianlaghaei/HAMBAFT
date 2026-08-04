# Authored behavior runtime

Uncontrolled Entities use `ControllerType.AuthoredBehavior` and a Package Behavior Profile. No model call, script evaluation or arbitrary code is involved.

At checkpoint resolution the resolver:

1. sorts active authored Entities by Definition ID then Entity ID;
2. loads and validates the Entity profile;
3. filters rules by repeat policy and typed conditions;
4. keeps the highest priority;
5. sorts remaining rules by ID;
6. applies explicit rule and Difficulty weight multipliers;
7. selects with SHA-256 material containing Package hash, seed, Difficulty, event-derived version/checkpoint, Entity and candidate weights;
8. uses the authored fallback if no rule remains;
9. emits `AuthoredBehaviorActionSelected` before applying Action effects.

The same Package hash, seed, Difficulty and history selects the same Action. File order and dictionary order are irrelevant. Different seeds may choose different weighted Actions. Repeat policies are `OncePerSession`, `OncePerCheckpoint` and `Repeatable`.

The sample Credit Provider uses cooperative and opportunistic rules. Hard multiplies the opportunistic rule by 3 and cooperative by 0.5; tests verify both exact modifiers and a higher opportunistic selection frequency. Public authored narrative routes only identifiers through SignalR and clients refetch projections.
