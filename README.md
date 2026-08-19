# This is Blast! - Core Mechanics & Gameplay Overview

A polished mobile hybrid-casual puzzle mechanics implementation built in Unity 6000 (URP) inspired by *This is Blast!* by Voodoo.

---

## 🎬 Gameplay Showcase

▶️ **[Watch Full Gameplay Video on YouTube](https://youtu.be/E3WGpKQwxHo)**

---

## 🎮 Core Game Mechanics

### 1. Board & Grid System
* **Grid Layout:** A deterministic, grid-based board composed of colored block items and durable obstacles (e.g., multi-hit blocks).
* **Block Accessibility:** Firing targets are evaluated dynamically. Cannons can only attack accessible blocks from the front/exposed edges matching their color.

### 2. Cannon Queue & Active Slots
* **Queue Structure:** An incoming queue of Cannon/Launcher units with varying ammo capacities and color designations.
* **Active Slots (5 Max):** Tapping an available cannon in the queue moves it into one of 5 active target slots.
* **Slot Capacity Limit:** If all 5 active slots are occupied by cannons without available targets, no further queue picks can be made.

### 3. Firing & Auto-Targeting Loop
* **Automatic Target Match:** Once in an active slot, a cannon instantly queries the board for accessible blocks matching its color.
* **Projectile Burst:** Fires projectiles rapidly, reducing its ammo counter per hit and destroying target blocks upon contact.
* **Slot Evacuation:** When ammo reaches zero (`Ammo = 0`), the cannon completes its recoil cycle, despawns, and frees the active slot for incoming units.
* **Idle/Wait State:** If no accessible matching block exists on the board, the cannon remains safely in its slot until state changes unlock a target.

### 4. Obstacles & Durability
* **Multi-Hit Obstacles:** Special block structures requiring multiple projectile hits to break, adding strategic depth to line-of-sight clearing.

### 5. Level Flow & Win / Loss Conditions
* **Win Condition:** Complete clearance of all destroyable target blocks on the board.
* **Loss Condition:** All 5 active slots filled with cannons that have no valid targets available on the board (deadlock).
