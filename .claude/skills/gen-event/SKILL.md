---
name: gen-event
description: Generate and insert a new event into the game's event pool with proper formatting
disable-model-invocation: false
---

# Generate New Event

Creates a new event entry in `Assets/Scripts/Core/GameManager.cs`.

## Arguments

The user should describe: event text, choices (label + effect each).

## Insertion Point

Find the event pool end marker and insert before it:
```bash
grep -n "var e = events\[Random" Assets/Scripts/Core/GameManager.cs
```

## Template Format

```csharp
("EVENT_TEXT", new (string, System.Action)[] {
    ("CHOICE_1_LABEL", () => { EFFECT_CODE_1 }),
    ("CHOICE_2_LABEL", () => { EFFECT_CODE_2 }),
}),
```

## Common Effect Codes
- `permanentATKBonus += N;` — +ATK
- `stepMultiplier += 0.0Xf;` — step multiplier
- `persistentHP = Mathf.Min(...)` — heal
- `Deck.AddCard(Deck.GenerateRewardChoices(currentStage)[0]);` — +1 card
- `mora += N;` — +mora
- `mora = Mathf.Max(0, mora - N);` — -mora

## After Insertion

Rebuild scene and build:
```bash
/home/liuhan/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity -quit -batchmode -nographics \
  -projectPath /home/liuhan/hanoi-game \
  -executeMethod HanoiGame.SceneBuilder.Build
```
