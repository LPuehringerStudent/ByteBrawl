import { describe, expect, it } from 'vitest';
import { GameRules } from '../src/game/GameRules';
import { AttackData } from '../src/shared/types';
import { Fighter } from '../src/fighter/Fighter';
import { AudioManager } from '../src/shared/AudioManager';

function createFighter(): Fighter {
  return {
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
    damageShield(amount: number) {
      this.shieldHealth -= amount;
    },
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
  id: 'light',
  baseDamage: 10,
  baseKnockback: 100,
  scaling: 1.5,
  direction: { x: 1, y: -0.5 },
  hitstunFrames: 10,
  activeFrames: 4,
};

const config = {
  startingStocks: 3,
  respawnInvincibilityFrames: 120,
  maxDamage: 999,
};

describe('GameRules', () => {
  it('increases defender damage on hit', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.applyHit(p1, p2, attack);
    expect(p2.damage).toBe(10);
  });

  it('scales knockback with defender damage', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    p2.damage = 50;
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.applyHit(p1, p2, attack);
    const expectedKnockbackX = 1 * (100 + 50 * 1.5);
    expect(p2.x).toBeCloseTo(expectedKnockbackX);
  });

  it('declares p1 winner when p2 runs out of stocks', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    p2.stocks = 1;
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.checkRingOut(p2, () => true, { x: 0, y: 0 });
    expect(rules.matchState).toBe('p1Win');
    expect(p2.stocks).toBe(0);
  });

  it('declares p2 winner when p1 runs out of stocks', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    p1.stocks = 1;
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.checkRingOut(p1, () => true, { x: 0, y: 0 });
    expect(rules.matchState).toBe('p2Win');
    expect(p1.stocks).toBe(0);
  });

  it('respawns player with remaining stocks after ring out', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    p2.stocks = 2;
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.checkRingOut(p2, () => true, { x: 50, y: 100 });
    expect(rules.matchState).toBe('active');
    expect(p2.stocks).toBe(1);
    expect(p2.x).toBe(50);
    expect(p2.y).toBe(100);
    expect(p2.damage).toBe(0);
  });

  it('ignores hits while defender is invincible', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    p2.invincibleFrames = 5;
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.applyHit(p1, p2, attack);
    expect(p2.damage).toBe(0);
    expect(p2.x).toBe(0);
  });

  it('damages shield instead of player when shield is active', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    p2.shieldActive = true;
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.applyHit(p1, p2, attack);
    expect(p2.damage).toBe(0);
    expect(p2.shieldHealth).toBe(70);
  });

  it('breaks shield when shield damage exceeds remaining health', () => {
    const p1 = createFighter();
    const p2 = createFighter();
    p2.shieldActive = true;
    p2.shieldHealth = 5;
    const rules = new GameRules(config, new AudioManager(), p1, p2);
    rules.applyHit(p1, p2, attack);
    expect(p2.shieldHealth).toBeLessThanOrEqual(0);
    expect(p2.damage).toBe(0);
  });
});
