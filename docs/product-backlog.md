# ByteBrawl Product Backlog

## MVP Goal
A playable local 1v1 / training build with:
- **1 Character** (Byte)
- **1 Weapon** (Sword)
- **1 Gadget** (Shiny Rock — placeholder, does nothing)

Everything else is deferred until the core loop feels good.

---

## Character : Byte

### Animations needed
- [ ] Idle
- [ ] Running
- [ ] Jumping
- [ ] Falling
- [ ] Grabbing
- [ ] Being grabbed
- [ ] Shielding
- [ ] Dodging (spot-dodge + air-dodge)
- [ ] Hitstun
- [ ] Light attack pose(s)
- [ ] Heavy attack pose(s)
- [ ] Special attack pose(s)

### Special attacks
- [ ] Neutral Special
- [ ] Up Special (recovery)
- [ ] Down Special
- [ ] Side Special

> Specials can be chargeable **selectively** (not all of them).

---

## Weapon : Sword

### Light attacks
- [ ] Neutral Light (N-Light)
- [ ] Side Light (S-Light)
- [ ] Up Tilt (Up-Light)
- [ ] Down Tilt (Down-Light)
- [ ] Neutral Air (Nair)
- [ ] Side Air (Sair)
- [ ] Down Air (Dair)
- [ ] Up Air (Up-Air)

### Heavy attacks
- [ ] Neutral Heavy (N-Heavy)
- [ ] Up Heavy
- [ ] Down Heavy
- [ ] Side Heavy

> All heavy attacks are chargeable.

### Sword-specific visuals
- [ ] Sword sprite / swing trail for each attack
- [ ] Hitbox sketches with timing, hitstun, active frames per hitbox

### Weapon-class note for MVP
For the MVP the Sword defines the **One-Handed Blade** moveset. Any future weapons in the same class (dagger, katana, shortsword, etc.) are treated as **reskins**: they reuse Byte’s body animations and only change the weapon sprite, hitbox shape, and stats. Truly unique weapon stances are deferred until after MVP.

---

## Gadget : Shiny Rock

- [ ] Visual asset
- [ ] Equip slot support
- [ ] **No gameplay effect** — pure placeholder for MVP

---

## Engine / Systems

- [ ] Directional attack system (neutral / side / up / down, ground vs air)
- [ ] Charge system for heavies and some specials
  - [ ] Minimum charge delay
  - [ ] Damage scaling with charge
  - [ ] Hitbox growth with charge (optional per attack)
  - [ ] No cancel once charging starts
- [ ] Grab system
  - [ ] Whiff / grab hitbox
  - [ ] Pummel / throw directions
  - [ ] Being grabbed state
- [ ] Multi-hit attack support (already in code)
- [ ] Weapon + character sprite decoupling (see approach note below)
- [ ] Stage select expansion (currently only Training Grounds)
- [ ] Audio hooks for attacks, hits, UI
- [ ] Final UI / HUD polish

---

## Approach note : avoiding the animation matrix

A full `characters × weapons × moves` animation matrix is too expensive for a small team. The realistic way out is a **weapon-class / reskin** model:

1. **Weapon class** defines the moveset (body poses, timing, hitbox logic).
   - Example classes: One-Handed Blade, Two-Handed Heavy, Ranged, Brawler.
2. **Character** provides the body animations for each class they can use.
   - In MVP, Byte only uses One-Handed Blade.
3. **Weapon** provides its own sprite/trail and stat tweaks, but reuses the class moveset.
   - Sword, katana, dagger all share the One-Handed Blade class.

### Why the simple “socket a weapon onto one body” idea breaks down

That only works if every weapon shares the same stance and swing. A bow draw, a hammer windup, and a sword slash need different body poses. So the weapon layer is still split, but it is split by **class**, not by individual weapon.

### What this means for art

- Adding a new character = body animations for each class that character can use.
- Adding a new weapon in an existing class = weapon sprite + hitbox/stats.
- Adding a new class = a whole new moveset for every character that can use it.

For MVP there is only one class (One-Handed Blade), so this is invisible until post-MVP.

---

## Things worth adding to the MVP backlog

You did not forget the obvious ones, but here are a few that often get skipped and then block polish:

- [ ] **Grab system** — you listed grab / being-grabbed animations, but the actual throw directions and pummel need design.
- [ ] **Shield health / shield break** — shield damage and break state, plus break stun animation.
- [ ] **Hitstun animation variants** — light hitstun vs heavy hitstun vs tumble.
- [ ] **Ledge / recovery rules** — even if the stage has no ledges yet, Up-Special needs to know what “recovery” means.
- [ ] **Hitstop / screenshake on strong hits** — sells impact without changing balance.
- [ ] **Training dummy controls** — reset position, damage %, etc.
- [ ] **Control rebinding screen** — currently hard-coded.

---

## Post-MVP (do not touch until MVP is solid)

- Additional characters
- Additional weapons
- Gadgets with real effects
- More stages
- Online / netcode
- Cosmetics / unlocks
- Full audio pass
