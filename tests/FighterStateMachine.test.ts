import { describe, expect, it, vi } from 'vitest';
import { FighterStateMachine } from '../src/fighter/FighterStateMachine';
import { Fighter } from '../src/fighter/Fighter';
import { HitboxManager } from '../src/fighter/HitboxManager';
import { AudioManager } from '../src/shared/AudioManager';
import { ActionFrame, AttackData, FighterConfig } from '../src/shared/types';

const lightAttack: AttackData = {
  id: 'light',
  baseDamage: 10,
  baseKnockback: 100,
  scaling: 1.5,
  direction: { x: 1, y: -0.5 },
  hitstunFrames: 10,
  activeFrames: 4,
};

const heavyAttack: AttackData = {
  id: 'heavy',
  baseDamage: 15,
  baseKnockback: 150,
  scaling: 1.6,
  direction: { x: 1, y: -0.3 },
  hitstunFrames: 14,
  activeFrames: 5,
  charge: {
    minChargeFrames: 5,
    maxChargeFrames: 20,
    maxHoldFrames: 10,
    damageGrowth: 0.5,
    knockbackGrowth: 2,
  },
};

const specialAttack: AttackData = {
  id: 'special',
  baseDamage: 12,
  baseKnockback: 120,
  scaling: 1.4,
  direction: { x: 0.8, y: -0.8 },
  hitstunFrames: 12,
  activeFrames: 4,
};

const config: FighterConfig = {
  name: 'Test',
  maxHealth: 100,
  runSpeed: 120,
  jumpSpeed: 280,
  weight: 1,
  spriteKey: 'test',
  weapon: {
    id: 'test-sword',
    name: 'Test Sword',
    light: lightAttack,
    heavy: heavyAttack,
  },
  special: specialAttack,
  gadget: {
    id: 'shiny-rock',
    name: 'Shiny Rock',
  },
};

function createMockFighter(grounded = true): Fighter {
  return {
    sprite: { scene: {} } as unknown as Phaser.Physics.Arcade.Sprite,
    facing: 1,
    damage: 0,
    stocks: 3,
    invincibleFrames: 0,
    hitstunFrames: 0,
    maxAirJumps: 2,
    airJumpsRemaining: 2,
    droppingThrough: false,
    shieldActive: false,
    shieldHealth: 100,
    maxShieldHealth: 100,
    shieldBubble: null,
    dodgeDirectionX: 0,
    dodgeDirectionY: 0,
    _grounded: grounded,
    get isGrounded() {
      return this._grounded;
    },
    get isAirborne() {
      return !this._grounded;
    },
    get body() {
      return {
        velocity: { x: 0, y: 0 },
        blocked: { left: false, right: false },
        touching: { left: false, right: false },
      } as unknown as Phaser.Physics.Arcade.Body;
    },
    setVelocityX: vi.fn(),
    setVelocityY: vi.fn(),
    jump: vi.fn(() => false),
    faceDirection: vi.fn(),
    applyKnockback: vi.fn(),
    enterHitstun: vi.fn(),
    enterInvincibility: vi.fn(),
    takeDamage: vi.fn(),
    createShieldBubble: vi.fn(),
    activateShield: vi.fn(),
    deactivateShield: vi.fn(),
    damageShield: vi.fn(),
    resetShield: vi.fn(),
    startDodge: vi.fn(),
    loseStock: vi.fn(),
    respawn: vi.fn(),
    update: vi.fn(),
    setChargingFull: vi.fn(),
    getConfig: vi.fn(() => config),
  } as unknown as Fighter;
}

function createMockHitboxManager(): HitboxManager {
  return {
    spawnHitbox: vi.fn(),
    update: vi.fn(),
    checkHits: vi.fn(),
  } as unknown as HitboxManager;
}

function neutralFrame(): ActionFrame {
  return {
    moveX: 0,
    moveY: 0,
    jumpPressed: false,
    jumpHeld: false,
    attackLight: false,
    attackLightHeld: false,
    attackHeavy: false,
    attackHeavyHeld: false,
    attackSpecial: false,
    attackSpecialHeld: false,
    gadgetPressed: false,
    gadgetHeld: false,
    shieldPressed: false,
    shieldHeld: false,
    grabPressed: false,
    grabHeld: false,
  };
}

describe('FighterStateMachine shield and dodge states', () => {
  it('enters shield state when shield is held on the ground', () => {
    const fighter = createMockFighter(true);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({ ...neutralFrame(), shieldHeld: true });

    expect(fighter.activateShield).toHaveBeenCalled();
    expect(fighter.createShieldBubble).toHaveBeenCalled();
    expect(fsm.currentState).toBe('shield');
  });

  it('decays shield health while shield is held', () => {
    const fighter = createMockFighter(true);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({ ...neutralFrame(), shieldHeld: true });
    const healthAfterOneFrame = fighter.shieldHealth;
    fsm.update({ ...neutralFrame(), shieldHeld: true });

    expect(fighter.shieldHealth).toBeLessThan(healthAfterOneFrame);
  });

  it('exits shield state when shield is released', () => {
    const fighter = createMockFighter(true);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({ ...neutralFrame(), shieldHeld: true });
    expect(fsm.currentState).toBe('shield');

    fsm.update({ ...neutralFrame(), shieldHeld: false });
    expect(fighter.deactivateShield).toHaveBeenCalled();
    expect(fsm.currentState).toBe('idle');
  });

  it('performs a spot-dodge when shield is tapped on the ground', () => {
    const fighter = createMockFighter(true);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({ ...neutralFrame(), shieldPressed: true, shieldHeld: true });
    expect(fsm.currentState).toBe('spotDodge');

    fsm.update(neutralFrame());
    expect(fighter.enterInvincibility).toHaveBeenCalled();
    expect(fsm.currentState).toBe('spotDodge');
  });

  it('returns to idle after spot-dodge ends', () => {
    const fighter = createMockFighter(true);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({ ...neutralFrame(), shieldPressed: true });
    for (let i = 0; i < 20; i++) {
      fsm.update(neutralFrame());
    }

    expect(fsm.currentState).toBe('idle');
  });

  it('transitions from spot-dodge startup to shield when shield is held', () => {
    const fighter = createMockFighter(true);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({ ...neutralFrame(), shieldPressed: true, shieldHeld: true });
    expect(fsm.currentState).toBe('spotDodge');

    for (let i = 0; i < 5; i++) {
      fsm.update({ ...neutralFrame(), shieldHeld: true });
    }

    expect(fighter.activateShield).toHaveBeenCalled();
    expect(fsm.currentState).toBe('shield');
  });

  it('performs a directional air-dodge when shield is tapped in the air', () => {
    const fighter = createMockFighter(false);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({
      ...neutralFrame(),
      shieldPressed: true,
      moveX: 1,
      moveY: -1,
    });

    expect(fighter.startDodge).toHaveBeenCalledWith(1, -1);
    expect(fsm.currentState).toBe('airDodge');
  });

  it('sets air-dodge velocity based on direction input', () => {
    const fighter = createMockFighter(false);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({
      ...neutralFrame(),
      shieldPressed: true,
      moveX: -1,
      moveY: 1,
    });

    expect(fighter.setVelocityX).toHaveBeenCalledWith(-220);
    expect(fighter.setVelocityY).toHaveBeenCalledWith(220);
  });

  it('falls after air-dodge ends', () => {
    const fighter = createMockFighter(false);
    const fsm = new FighterStateMachine(
      fighter,
      createMockHitboxManager(),
      new AudioManager(),
    );

    fsm.update({
      ...neutralFrame(),
      shieldPressed: true,
      moveX: 0,
      moveY: 0,
    });
    for (let i = 0; i < 20; i++) {
      fsm.update(neutralFrame());
    }

    expect(fsm.currentState).toBe('fall');
  });
});

describe('FighterStateMachine charging', () => {
  it('enters charging state when a chargeable heavy is pressed', () => {
    const fighter = createMockFighter(true);
    const hitboxes = createMockHitboxManager();
    const fsm = new FighterStateMachine(fighter, hitboxes, new AudioManager());

    fsm.update({ ...neutralFrame(), attackHeavy: true, attackHeavyHeld: true });

    expect(fsm.currentState).toBe('charging');
    expect(hitboxes.spawnHitbox).not.toHaveBeenCalled();
  });

  it('fires a charged heavy when released after the minimum charge', () => {
    const fighter = createMockFighter(true);
    const hitboxes = createMockHitboxManager();
    const fsm = new FighterStateMachine(fighter, hitboxes, new AudioManager());

    fsm.update({ ...neutralFrame(), attackHeavy: true, attackHeavyHeld: true });
    for (let i = 0; i < 5; i++) {
      fsm.update({ ...neutralFrame(), attackHeavyHeld: true });
    }
    fsm.update({ ...neutralFrame(), attackHeavyHeld: false });

    expect(fsm.currentState).toBe('heavyAttack');
    expect(hitboxes.spawnHitbox).toHaveBeenCalled();
    const charged = (hitboxes.spawnHitbox as ReturnType<typeof vi.fn>).mock
      .calls[0][1] as AttackData;
    expect(charged.baseDamage).toBeGreaterThan(heavyAttack.baseDamage);
  });

  it('auto-fires at max charge while the button is still held', () => {
    const fighter = createMockFighter(true);
    const hitboxes = createMockHitboxManager();
    const fsm = new FighterStateMachine(fighter, hitboxes, new AudioManager());

    fsm.update({ ...neutralFrame(), attackHeavy: true, attackHeavyHeld: true });
    for (let i = 0; i < 31; i++) {
      fsm.update({ ...neutralFrame(), attackHeavyHeld: true });
    }

    expect(fsm.currentState).toBe('heavyAttack');
    expect(hitboxes.spawnHitbox).toHaveBeenCalled();
  });
});
