---
name: balance-check
description: Scan card data for balance issues — damage per step, enemy scaling, archetype distribution
disable-model-invocation: false
---

# Balance Check for Hanoi Game

Analyze current game balance and output a report.

## Check Items

1. **Damage per step** — For each card level, calculate `(baseDamage * LayerScale + ATK) / OptimalSteps`:
   - Read `EffectManager.LayerScale()` multipliers
   - Read `CardData.EffectPool` for all card values
   ```bash
   grep -A1 "LayerScale" Assets/Scripts/Core/EffectManager.cs
   ```

2. **Enemy HP vs card damage** — Check if enemies at each stage can be killed in a reasonable number of completions:
   ```bash
   grep -E "baseHP|scaling|baseATK" Assets/Scripts/Core/Enemy.cs
   ```

3. **Archetype card counts** — Count cards per element per level:
   ```bash
   grep -c "Element\.Pyro\|Element\.Cryo\|Element\.Hydro\|Element\.Electro\|Element\.Anemo\|Element\.Geo\|Element\.Dendro" Assets/Scripts/Core/CardData.cs
   ```

4. **Fatigue math** — Verify fatigue values:
   ```bash
   grep -A5 "GetFatigueMultiplier" Assets/Scripts/Core/BattleManager.cs
   ```

## Output Format
```
=== Balance Report ===
Per-step efficiency by level:
  L3: X damage/step
  L4: X damage/step
  L5: X damage/step
  L6: X damage/step
  L7: X damage/step

Enemy HP range per stage:
  Stage 0: XXX-XXX HP
  Stage 1: XXX-XXX HP
  Stage 2: XXX-XXX HP

Cards per element: Pyro=X Cryo=X Hydro=X Electro=X Anemo=X Geo=X Dendro=X

Issues found: ...
```
