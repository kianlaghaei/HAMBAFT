# Scene Director

## Data flow

```text
package-authored semantic state      public or Team-authorized REST projection
                \                    /
                 buildSceneDescriptor
                          |
                  immutable descriptor
                          |
        +-----------------+------------------+
        |                                    |
 lazy PixiJS renderer                 React/SVG fallback
        |                                    |
        +-------- React overlays and controls+
```

`buildSceneDescriptor` is a pure function. Its inputs are `PublicWorld` and, for a Team route only, that team's `TeamExperience`. It does not have an API client, package repository, persistence handle, condition evaluator, effect runner, or clock.

## Descriptor contract

The descriptor contains a deterministic ID (`session:scene`), authoritative visual version, one of six authored time phases, atmosphere/public-event text, 13 location hotspots, semantic business signals, visible connection lines, Team-visible proposal messengers, bounded ambient events, character presence, Avan presence, semantic pressure band, reaction lines and ending state. Rebuilding it after route refresh produces the same descriptor for the same REST payload.

`WorldPresentationState` is the Phase 6B source of meaning. Legacy `scene`, `event`, `time`, `atmosphere`, `mood`, and `presence` tags remain a 0.1/0.2 compatibility path only. React does not map numeric thresholds. Ending visuals are selected only when the corresponding projected ending exists.

## Privacy boundary

The Public Display call never receives `TeamExperience`. Public relationship projections remain empty. Team relationships arrive as localized semantic states after Backend authorization; their numeric values are removed from the Team response for semantic packages. Public agreements come from `PublicWorld.publicAgreements`; party-private agreements come from the Team projection. Ambient events use public semantic tags only.

## Pixi layers

Pixi creates these containers in fixed order:

1. background architecture
2. lighting and time
3. business locations
4. ambient population
5. messengers and movement
6. relationships and agreements
7. event effects
8. foreground atmosphere
9. camera and transitions

The current implementation draws generated vector geometry, so there is no initial asset download beyond the lazy Pixi chunk. Future scene assets must remain lazy and licensed-safe.

## Lifecycle and realtime

`MarketScene` memoizes its descriptor from query data. `PixiMarketCanvas` creates one application per descriptor and destroys the application, display tree, resize listener and canvas on change or unmount. A rejected dynamic import or initialization switches the same React frame to fallback mode. Development-only diagnostics report canvas count, scene ID, bounded ambient count, realtime handler count and fallback state.

SignalR is only a refetch hint. `StateChanged`, narrative, proposal, agreement, behavior, consequence, and ending notifications invalidate the relevant TanStack Query keys. The next authoritative REST result creates the next descriptor. Reconnect invalidates Team, Public World, Admin, and session queries; no scene state is replayed from SignalR messages.

## Tests

The frontend suite covers authored descriptor mapping, privacy filtering, proposal status mapping, agreement visualization, deterministic refresh restoration, fallback output, reduced-motion selection, Pixi layer mount/cleanup, and relevant SignalR invalidations. Backend package tests validate the compiled 0.2.0 package using the same story-package loader as runtime.
