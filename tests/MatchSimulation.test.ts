import { describe, expect, it } from 'vitest';
import { GameRules } from '../src/game/GameRules';
import { AttackData } from '../src/shared/types';
import { Fighter } from '../src/fighter/Fighter';
import { AudioManager } from '../src/shared/AudioManager';

function createFighter(name: string): Fighter {
  return {
    name,
    x: 0,
    y: 0,
    damage: 0,
    stocks: 3,
    facing: 1,
    invincibleFrames: 0,
    shieldActive: false,
    shieldHealth: 100,
    takeDamage(amount: number) {
      this.damage += amount;
    },
    applyKnockback(vec: { x: number; y: number }) {
      this.x += vec.x;
      this.y += vec.y;
    },
    enterHitstun(_frames: number) {},
    loseStock() {
      this.stocks -= 1;
    },
    respawn(x: number, y: number, _invincibility?: number) {
      this.x = x;
      this.y = y;
      this.damage = 0;
    },
  } as unknown as Fighter;
}

const attack: AttackData = {
  id: 'heavy',
  baseDamage: 20,
  baseKnockback: 120,
  scaling: 1.2,
  direction: { x: 1, y: -0.5 },
  hitstunFrames: 15,
  activeFrames: 6,
};

const config = {
  startingStocks: 3,
  respawnInvincibilityFrames: 120,
  maxDamage: 999,
};

describe('Match simulation', () => {
  it('plays a full match ending in p1 victory', () => {
    const p1 = createFighter('p1');
    const p2 = createFighter('p2');
    const rules = new GameRules(config, new AudioManager(), p1, p2);

    // P1 hits P2 repeatedly until damage is high enough for ring-out.
    for (let i = 0; i < 5; i++) {
      rules.applyHit(p1, p2, attack);
    }

    // Knock P2 off stage three times.
    for (let stock = 0; stock < 3; stock++) {
      // Knock p2 far enough to be out of bounds.
      p2.x = 500;
      p2.y = 300;
      rules.checkRingOut(p2, () => true, { x: 160, y: 100 });
    }

    expect(rules.matchState).toBe('p1Win');
    expect(p2.stocks).toBe(0);
  });

  it('respawns a player after ring out until stocks run out', () => {
    const p1 = createFighter('p1');
    const p2 = createFighter('p2');
    const rules = new GameRules(config, new AudioManager(), p1, p2);

    p2.x = 500;
    p2.y = 300;
    rules.checkRingOut(p2, () => true, { x: 160, y: 100 });
    expect(p2.stocks).toBe(2);
    expect(p2.x).toBe(160);
    expect(p2.y).toBe(100);
    expect(p2.damage).toBe(0);
    expect(rules.matchState).toBe('active');
  });
});
