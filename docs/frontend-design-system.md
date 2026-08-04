# Frontend design system

The visual language uses cream paper, deep olive, charcoal, muted gold, ledger lines and lantern glow. All atmosphere is CSS/SVG/typography; there is no external or generated artwork.

Global variables and primitives live in `src/styles/global.css`. Story text has a wide, low-density reading surface and approximately 760px readable measure. Proposals resemble letters, Agreements resemble signed ledger cards, and Endings use a distinct reveal surface. Admin keeps the same typography but favors compact operational panels.

The document root is `lang="fa" dir="rtl"`. Display numbers use Persian digits where presentation benefits, while request values remain machine numbers. Package-specific Persian labels live in a presentation adapter, not generic metric components. Generic metric cards accept label/value/min/max and expose semantic meter attributes.

Keyboard focus is visible. Inputs and buttons are semantic, connection changes use an ARIA live status, contrast does not rely on color alone, and `prefers-reduced-motion` reduces all transitions. Responsive breakpoints cover tablet and narrow screens; the Public Display uses large type and a four-to-two-to-one business grid.
