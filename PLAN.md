# CoolCount — Technical Plan

**Idea:** an OmniCC-style mod for Valheim: draw the remaining cooldown of consumables directly on
their inventory-grid and hotbar slots (countdown text, dimmed icon, optional radial sweep), so you
never have to cross-reference the status-effect list in the top-left.

**Target build:** Valheim 1.0.14. Findings below come from `assembly_valheim.dll` (the copy in
`../Recount/lib`, dated 2026-09-17) decompiled with ILSpy 9.1 (`~/.dotnet/tools/ilspycmd`, needs
`DOTNET_ROOT=$HOME/.dotnet`).

---

## 1. Prior art (searched Thunderstore, Nexus, Steam forums, 2026-09-22)

Nothing found that shows a cooldown on the item slot itself. What exists:

- **Cooldown *changers*:** PotionCooldownReduction (Nexus 2139), EasyLiving, BetterConsumables,
  UsefulTankards, FeastMaster. They shorten or remove the cooldown; none display it on the item.
- **HUD replacements:** HudRevizualized, Carturs UI HUD, Inject0rHUD. They redraw the guardian-power
  cooldown and food timers on the HUD, not on inventory or hotbar items.
- **Timer mods:** Hearthwait, Atze's Timer Display, GumshoeTimerSuite, MyLittleUI. Hover timers for
  fermenters, smelters, cooking; nothing on consumables in the bag.
- **Vanilla:** the active mead effect is a status-effect icon top-left with a `TimeText` countdown
  and a `Cooldown` overlay child shown when `StatusEffect.m_cooldownIcon` is set. That is the only
  in-game indication, and it does not tell you *which items in your bag* are blocked.

## 2. How the game actually gates consumables

### 2.1 Meads have no separate cooldown timer

`Player.CanConsumeItem(item)` (Player.cs:6033):

```csharp
if (item.m_shared.m_consumeStatusEffect) {
    var se = item.m_shared.m_consumeStatusEffect;
    if (m_seman.HaveStatusEffect(se.NameHash()) || m_seman.HaveStatusEffectCategory(se.m_category))
        { Message("$msg_cantconsume"?); return false; }
}
```

`Player.CanConsumeItem` calls `base.CanConsumeItem` first, and `Humanoid.CanConsumeItem`
(Humanoid.cs:1035) rejects anything whose `m_itemType` is not `Consumable` before any of this
runs. That gate matters: **mead bases are `Material` and still carry the finished mead's
`m_consumeStatusEffect`** (which is why their tooltip names the effect), so matching on that
field alone marks the base as gated. Found in testing, 2026-09-22.

So a mead is blocked exactly while it is a Consumable and (a) its own status effect is still
running, or (b) any status effect sharing its `m_category` string is running ("exclusion
groups" in wiki terms). The "cooldown"
is just the remaining `m_ttl - m_time` of that effect (`StatusEffect.GetRemaningTime()`, sic).
`StatusEffect.m_cooldown` exists but is only used for guardian powers.

Consequence: the number to draw on a mead slot is
`max over active SE where (SE.NameHash == item SE hash || SE.m_category == item SE category) of GetRemaningTime()`.
Utility meads whose effect has an empty category and short TTL are simply free once the effect ends.

### 2.2 Food has an effective cooldown too

`Player.CanEat` → `Food.CanEatAgain()` = `m_time < m_foodBurnTime / 2`. You can re-eat a food
only once it has passed half its burn time. So a food slot can show
`m_foodBurnTime/2 - food.m_time` while positive. (Different slot: eating a *different* food is
allowed while any slot is past half, or while fewer than 3 foods are active.)

### 2.3 Other `m_consumeStatusEffect` users

`Attack.cs:1037` (weapons that apply a consume SE on use), `Feast.cs:97` (feasts), `ItemDrop.cs:1667`
(use-on-pickup). Same gating rule applies wherever `CanConsumeItem` is called, so the same overlay
logic covers them.

### 2.4 Guardian power

`Player.m_guardianPowerCooldown` counts down in seconds; it is not an inventory item. Out of scope
unless we want a config toggle to also show it somewhere.

## 3. Where to draw

- **Inventory grid:** `InventoryGrid.UpdateGui(Player, ItemDrop.ItemData dragItem)` (private,
  InventoryGrid.cs:245) runs every frame the inventory is open. Each slot is an `InventoryElement`
  MonoBehaviour with public `m_icon` (Image), `m_amount`, `m_quality` (TMP_Text), `m_food` (Image),
  `m_durability` (GuiBar). Elements are re-instantiated when the grid resizes (line 262).
- **Hotbar:** `HotkeyBar.UpdateIcons(Player)` (private) with a private `ElementData` list
  (`m_go`, `m_icon`, `m_amount`, `m_durability`). Elements are rebuilt whenever the bound-item
  count changes.

Plan: Harmony postfix on both. For each element with a gated item, find-or-create a child
`CoolCount` GameObject holding a TMP_Text (copy font/material from `m_amount`) and an `Image`
with `fillMethod = Radial360` for the sweep; tint `m_icon.color` while blocked; clear when not.
Publicize `assembly_valheim` at build time (same `BepInEx.AssemblyPublicizer.MSBuild` setup as
Recount) to reach `m_elements` and `ElementData`.

Cheap: one dictionary lookup per slot per frame; no networking, purely client-side.

## 4. Implementation (0.1.0)

- **No Jotunn.** Nothing here needs a custom canvas or asset; the overlay lives inside the
  game's own slot prefabs. Dependency is BepInEx alone; `ConfigurationManagerAttributes` is
  declared locally so ConfigurationManager still gets ordering and advanced flags.
- **`Core/Cooldowns.cs`** is the only place that decides whether an item is blocked. It mirrors
  `Humanoid.CanConsumeItem` (Consumable item type, else no cooldown at all) and then
  `Player.CanConsumeItem` (hash or category match against `SEMan.GetStatusEffects()`, an empty
  category matching nothing, exactly as `HaveStatusEffectCategory` does) and `Player.CanEat`
  (`Food.m_time` counts *down* from `m_foodBurnTime`; re-eating opens once it is under half, so
  the countdown is `m_time - burn/2`). A blocking effect with no TTL yields
  `Remaining = +Infinity`: the icon dims, no text, no sweep.
- **`UI/SlotOverlay.cs`** is a MonoBehaviour on a child of the slot's icon `Image`, stretched
  to it. Children: `sweep`, a filled `Image` (Radial360, origin Top, counter-clockwise fill so
  the dark region shrinks clockwise like WoW's) over a 4x4 white sprite created at runtime, and
  `time`, an `Instantiate` of the slot's own `m_amount` TMP label so font, material and outline
  match. Both have `raycastTarget = false`. Dimming writes `m_icon.color` and restores white on
  hide; the inventory grid rewrites that colour every frame anyway, the hotbar never does.
- **`Patches/SlotPatches.cs`**: postfixes on `InventoryGrid.UpdateGui(Player, ItemData)` and
  `HotkeyBar.UpdateIcons(Player)`. Each clears overlays on elements the game marked unused,
  then walks the same item list the game just drew (`m_inventory.GetAllItems()` /
  `m_items`) and paints or hides per slot. Container grids arrive with `player == null`; the
  local player is substituted (`General.ShowInContainers`). Private members reached through
  build-time publicizing, as in Recount.
- **Console**: `coolcount`, `coolcount effects`, `coolcount foods` for checking numbers from the
  BepInEx log without a screenshot.

## 5. Open questions

- Untested in game as of the first build. Things to look at on the rig: label size against the
  ~64 px slot, whether the sweep sits under the equipped/food/durability siblings acceptably,
  and that the instantiated `m_amount` copy carries no stray component (a Localize script, say)
  that rewrites its text.
- Compatibility: Equipment & Quick Slots / Extended Inventory add extra grids; they instantiate
  the same `InventoryElement` prefab so the postfix should just work, but verify.
- Item tooltip line ("Ready in 1:20") would be a cheap addition via `ItemDrop.ItemData.GetTooltip`.
- Guardian power is not an item; a HUD countdown for it is out of scope unless asked.
