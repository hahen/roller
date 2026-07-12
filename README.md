# D&D Attack Roller

Blazor WebAssembly SPA for rolling D&D attack + damage dice — built for multi-attack turns like *Animate Objects* (10 attacks at once).

## Run

```
dotnet run --project DiceRoller
```

Then open http://localhost:5241.

## Features

- **Attack rolls**: normal / advantage / disadvantage, with a to-hit modifier. Both d20s are shown for adv/dis; the kept die is highlighted.
- **Damage**: any combination of dice groups + modifier, e.g. `1d6 + 2d8 + 4` — add or remove die types per attack with the `+d` button.
- **Crits**: natural 20 crits, natural 1 auto-misses. Crit rule is selectable in the toolbar:
  - *Default (2× dice)* — RAW 5e: roll all damage dice twice, modifier added once.
  - *Perkins (max + roll)* — house rule: normal damage roll + maximized damage dice (e.g. 1d4+4 crit = 1d4 + 4 + 4).
- **Multi-roll (×N)**: each attack has a × count — one card rolls N attacks at once and shows every roll in a log underneath, with a per-card subtotal. E.g. one tiny object (+8, 1d4+4) ×10 covers all of *Animate Objects*.
- **Roll all**: rolls every card; an optional Target AC field shows HIT/MISS per roll plus a summary (hits count, total damage of hits).
- **Presets**: save the current set of attacks under a name and reload it any time (stored in the browser's localStorage, survives page reloads). Saving to an existing name overwrites it; Load replaces the current attacks.
- One-click **Animate Objects preset** (tiny object ×10) on the empty start screen.

## Randomness

All rolls use `System.Security.Cryptography.RandomNumberGenerator.GetInt32` ([DiceService.cs](DiceRoller/Services/DiceService.cs)) — a cryptographically secure RNG with internal rejection sampling. Every die face is exactly equally likely: no modulo bias and no predictable seed, unlike `System.Random`. This is the strongest randomness source available in .NET.
