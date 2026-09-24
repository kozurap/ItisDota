---
name: dota-team-draft
description: >-
  Drafts balanced Dota 2 5v5 teams from a player list with MMR ranges and role
  priorities (1-5). Use when the user pastes the ItisDota team prompt, asks to
  составить команды, balance Radiant/Dire, or split players into equal games.
---

# Dota Team Draft (ItisDota)

## When this applies

User pastes text starting like «Мы собираемся играть в доту 5 на 5…» plus a player list, or asks to form/rebalance teams. Do **not** modify application code unless asked.

## Role map

| # | Role |
|---|------|
| 1 | Carry (керри) |
| 2 | Mid (мид) |
| 3 | Offlane (сложная) |
| 4 | Soft support |
| 5 | Hard support |

## Effective MMR

For the role a player is assigned in the draft:

| Role priority rank used | Multiplier |
|-------------------------|------------|
| 1st priority | 100% |
| 2nd | 95% |
| 3rd | 90% |
| 4th | 80% |
| 5th | 70% |

Parse MMR:

- `A-B` → midpoint `(A+B)/2`
- `N+` → use `N` (state the assumption)
- single number → use as-is

`effective = nominal * multiplier` for the assigned priority slot.

## Algorithm

1. Count players; must be divisible by 10. If not — say so and stop (or ask whom to drop/add).
2. Number of games = players / 10. Each game = two teams of 5.
3. Prefer assigning each player a role near their top priorities (ideally 1st–2nd).
4. No duplicate roles inside a team; roles 1–5 all present.
5. Balance games: minimize |sum(effective MMR Radiant) − sum(effective MMR Dire)|.
6. Prefer spreading high-MMR players across sides/games when possible.
7. Avoid known one-sided shuffles in `.cursor/rules/dota-team-draft.mdc`. For `@angelbtw` + `@shinraqwe` both in lobby:
   - **Never** same team on main roles (angel mid + shinra carry) — auto-stomp regardless of the other eight.
   - **Never** treat opposite-side 6500 mid vs 6500 carry as equal cores (failed: angel lost mid, shinra stomped safelane).
   - Still give both high role priority; if same team, put `@shinraqwe` on offlane (2nd prio) not carry, and rebalance the rest hard.

## Output structure (required)

1. Draft reasoning per game (roles, effective MMR totals, trade-offs).
2. Explicit explanation: why these teams / matchups.
3. Final block **without tables**, using this shape:

```
*Команда сил света*
Имя, @tag, птс
Имя, @tag, птс
...

*Команда сил тьмы*
Имя, @tag, птс
...
```

- «Силы света» = Radiant, «силы тьмы» = Dire
- Player lines: `Имя, Тег в тг, птс` (nominal MMR string from input)
- Multiple games: repeat under `## Игра N`

Do not end with markdown tables for the final team listing.

## Default prompt (verbatim from app)

Мы собираемся играть в доту 5 на 5. Вот список игроков, их ПТС рейтинг и их приоритеты по ролям (1-керри, 2 - мид, 3 - сложная, 4- софт саппорт, 5 -хард саппорт). Составь команды по 5 человек, где каждый играет на наиболее комфортной роли и поставь их против друг друга так, чтобы игры были равными. Учитывай, что играя на первом приоритете следует считать что игрок обладает 100% птс, второй приоритет - 95%, 3 - 90%, 4 - 80%, 5 - 70%. Дай объяснение почему ты так распределил команды а так же в конце укажи команды не в таблице, а в виде:
*Команда (силы тьмы или света)*
Имя, Тег в тг, птс
Имя, Тег в тг, птс
...
