# Frontend design system

The visual language uses cream paper, deep olive, charcoal, muted gold, ledger lines and lantern glow. All atmosphere is CSS/SVG/typography; there is no external or generated artwork.

Global variables and primitives live in `src/styles/global.css`. Story text has a wide, low-density reading surface and approximately 760px readable measure. Proposals resemble letters, Agreements resemble signed ledger cards, and Endings use a distinct reveal surface. Admin keeps the same typography but favors compact operational panels.

The document root is `lang="fa" dir="rtl"`. Team and Public Display never render raw runtime metric values. Package-specific Persian meaning is resolved by the Backend from hash-locked presentation bands; React receives labels, descriptions, trend and visual tags. Numeric values remain in authoritative state and appear only inside Admin's explicitly technical, collapsed diagnostics.

Keyboard focus is visible. Inputs and buttons are semantic, connection changes use an ARIA live status, contrast does not rely on color alone, and `prefers-reduced-motion` reduces all transitions. Responsive breakpoints cover tablet and narrow screens; the Public Display uses large type and a four-to-two-to-one business grid.

Phase 6B makes the map the primary spatial surface. Hotspots use shape/border/text in addition to color, a location-list equivalent duplicates every meaningful map action, focus moves to the selected context panel, and reduced-motion/fallback retain time, atmosphere, pulse, reactions, choices and Proposal target selection.
