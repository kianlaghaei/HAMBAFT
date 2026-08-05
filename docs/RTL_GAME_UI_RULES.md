# HAMBAFT RTL game UI rules

## Direction

- Put `dir="rtl"` on the application shell.
- Persian narrative and player controls use RTL.
- Keep IDs, code, hashes, versions, and technical tokens LTR-isolated.
- Prefer CSS logical properties: `margin-inline`, `padding-inline`, `inset-inline`, and `border-inline`.
- Avoid hardcoded `left`/`right` unless the rule describes genuinely spatial behavior.

## Text

- Never reverse technical identifiers.
- Use `dir="ltr"` or `<bdi>` for code, versions, and numeric technical strings.
- Do not mix untranslated English labels into player UI.
- Prefer Persian numbers in player-facing content.
- Keep Story text concise enough for a fixed gameplay panel.

## Layout

At 1366×768 and 1440×900:

- Do not allow document-level vertical scrolling during gameplay.
- Keep the map, current event, and active action visible together.
- Provide one dominant active action.
- Open messages and Pacts in drawers or overlays.
- Do not let overlays enlarge document height.

## Map

- PixiJS owns spatial world rendering.
- React owns panels, text, forms, and accessibility.
- Do not bake Persian labels into PNG assets.
- Do not use the reference screenshot as a static page background.
- Do not show every hotspot simultaneously.

## Accessibility

- Locations must be keyboard-selectable.
- Use semantic buttons and visible focus states.
- Keep a functional fallback renderer.
- Provide a reduced-motion equivalent for animated states.

## Visual QA

Verify at 1366×768, 1440×900, 1920×1080, and a tablet viewport:

- RTL text direction and readable Persian wrapping.
- No horizontal or vertical overflow.
- No clipped active action.
- No private-data leakage on Public Display.
- Map, event, and action remain visible in the intended gameplay state.
