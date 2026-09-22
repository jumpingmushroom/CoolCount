# Changelog

## 0.1.0 — first cut

- Countdown, radial sweep and dimmed icon on meads and food in the inventory grid, container
  grids and hotbar.
- Meads follow the game's own rule: an item must be a Consumable (so mead *bases*, which carry
  the same effect field, are left alone), then it is blocked while the consume effect or any
  effect in its exclusion group runs. Food counts down to the half-way point where it can be eaten again,
  and to the first slot freeing up when all three are full.
- Game or Compact (OmniCC) time format, colour by remaining time, optional tenths.
- `coolcount`, `coolcount effects`, `coolcount foods` console commands.
