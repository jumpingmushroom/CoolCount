# CoolCount

OmniCC for Valheim. Every mead and food you cannot use yet gets a **countdown and a radial
sweep** right on its slot, in your inventory, in chests and on the hotbar. No more matching the
little status icon in the corner against the bottles in your bag.

- **Meads** count down the effect that blocks them, including exclusion groups: a minor healing
  mead shows the same timer as the medium one that is running.
- **Food** you are digesting counts down to the half-way point where you can eat it again. With
  all three slots full, every other food counts down to the first slot freeing up.
- **Chests** show the same numbers, measured against your own effects.
- Text format, colours by remaining time, sweep colour, optional icon dimming and font size are
  all configurable. The countdown uses the game's own font.

Console: `coolcount` lists what is on cooldown and why; `coolcount effects` and
`coolcount foods` print the state behind the numbers.

Requires BepInEx. Nothing to install on the server; other players are unaffected.
Source and issues: https://github.com/jumpingmushroom/CoolCount
