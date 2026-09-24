import { ActionFrame, AttackData, AttackStage, FighterState } from '../shared/types';
import { AudioManager } from '../shared/AudioManager';
import { Fighter } from './Fighter';
import { HitboxManager } from './HitboxManager';

const DEFAULT_ATTACK_DURATION = 20;
const ATTACK_RECOVERY_FRAMES = 10;
const SPOT_DODGE_FRAMES = 20;
const SPOT_DODGE_TAP_THRESHOLD = 5;
const AIR_DODGE_FRAMES = 20;
const AIR_DODGE_SPEED = 220;
const CHARGE_AIR_DRAG = 0.99;
const CHARGE_GROUND_DRAG = 0.8;
const CHARGE_MAX_FALL_SPEED = 120;
const WALL_SLIDE_SPEED = 60;

export class FighterStateMachine {
  private state: FighterState = 'idle';
  private stateFrames = 0;
  private attackCooldown = 0;
  private activeSequence: {
    stages: AttackStage[];
    nextStageIndex: number;
    totalFrames: number;
  } | null = null;

  private chargeState: {
    attack: AttackData;
    firedState: FighterState;
  } | null = null;

  constructor(
    private fighter: Fighter,
    private hitboxManager: HitboxManager,
    private audio: AudioManager,
  ) {}

  get currentState(): FighterState {
    return this.state;
  }

  update(actions: ActionFrame): void {
    if (this.attackCooldown > 0) this.attackCooldown--;

    this.fighter.update();

    if (
      this.fighter.isAirborne &&
      (this.fighter.body.blocked.left || this.fighter.body.blocked.right)
    ) {
      this.fighter.airJumpsRemaining = this.fighter.maxAirJumps;
      if (this.fighter.body.velocity.y > 0) {
        this.fighter.setVelocityY(WALL_SLIDE_SPEED);
      }
    }

    if (this.fighter.hitstunFrames > 0) {
      this.fighter.deactivateShield();
      this.setState('hitstun');
      this.stateFrames++;
      return;
    }

    if (this.state === 'hitstun' && this.fighter.hitstunFrames === 0) {
      this.setState(this.fighter.isGrounded ? 'idle' : 'fall');
    }

    if (this.isAttackState(this.state)) {
      this.stateFrames++;
      this.tickAttackSequence();
      if (this.stateFrames >= this.getAttackDuration()) {
        this.setState(this.fighter.isGrounded ? 'idle' : 'fall');
      }
      return;
    }

    if (this.state === 'charging') {
      this.stateFrames++;
      this.tickCharge(actions);
      return;
    }

    if (this.state === 'spotDodge') {
      this.stateFrames++;
      this.fighter.setVelocityX(0);

      if (this.stateFrames >= SPOT_DODGE_TAP_THRESHOLD && actions.shieldHeld) {
        this.fighter.invincibleFrames = 0;
        this.fighter.createShieldBubble(this.fighter.sprite.scene);
        this.fighter.activateShield();
        this.setState('shield');
        return;
      }

      if (this.stateFrames >= SPOT_DODGE_FRAMES) {
        this.setState('idle');
      }
      return;
    }

    if (this.state === 'airDodge') {
      this.stateFrames++;
      if (this.stateFrames >= AIR_DODGE_FRAMES) {
        this.fighter.setVelocityX(this.fighter.dodgeDirectionX * 60);
        this.fighter.setVelocityY(80);
        this.setState('fall');
      }
      return;
    }

    if (this.state === 'shield') {
      if (!actions.shieldHeld || this.fighter.shieldHealth <= 0) {
        this.fighter.deactivateShield();
        this.setState(this.fighter.isGrounded ? 'idle' : 'fall');
      } else {
        this.fighter.setVelocityX(0);
        this.fighter.shieldHealth -= 0.1;
      }
      return;
    }

    if (actions.shieldPressed) {
      if (this.fighter.isGrounded) {
        this.setState('spotDodge');
        return;
      } else {
        this.fighter.startDodge(actions.moveX, actions.moveY < 0 ? -1 : actions.moveY > 0 ? 1 : 0);
        this.fighter.setVelocityX(actions.moveX * AIR_DODGE_SPEED);
        this.fighter.setVelocityY(
          actions.moveY < 0
            ? -AIR_DODGE_SPEED
            : actions.moveY > 0
              ? AIR_DODGE_SPEED
              : 0,
        );
        this.setState('airDodge');
        return;
      }
    }

    if (actions.shieldHeld && this.fighter.isGrounded) {
      this.fighter.createShieldBubble(this.fighter.sprite.scene);
      this.fighter.activateShield();
      this.setState('shield');
      return;
    }

    if (actions.jumpPressed && this.fighter.jump()) {
      this.audio.playJump();
    }

    this.fighter.droppingThrough = actions.moveY > 0;

    if (actions.attackLight && this.attackCooldown === 0) {
      this.startAttack('lightAttack', this.fighter.getConfig().weapon.light);
      return;
    }
    if (actions.attackHeavy && this.attackCooldown === 0) {
      const heavy = this.fighter.getConfig().weapon.heavy;
      if (heavy.charge) {
        this.startCharge('heavyAttack', heavy);
      } else {
        this.startAttack('heavyAttack', heavy);
      }
      return;
    }
    if (actions.attackSpecial && this.attackCooldown === 0) {
      const special = this.fighter.getConfig().special;
      if (special.charge) {
        this.startCharge('special', special);
      } else {
        this.startAttack('special', special);
      }
      return;
    }

    if (actions.gadgetPressed) {
      this.activateGadget();
      return;
    }

    const onGround = this.fighter.isGrounded;
    const moving = actions.moveX !== 0;

    if (onGround) {
      if (moving) {
        this.setState('run');
        this.fighter.faceDirection(actions.moveX);
        this.fighter.setVelocityX(actions.moveX * this.fighter.getConfig().runSpeed);
      } else {
        this.setState('idle');
        this.fighter.setVelocityX(0);
      }
    } else {
      if (actions.moveX !== 0) {
        this.fighter.faceDirection(actions.moveX);
        this.fighter.setVelocityX(actions.moveX * this.fighter.getConfig().runSpeed);
      }
      this.setState(this.fighter.body.velocity.y < 0 ? 'jump' : 'fall');
    }

    this.stateFrames++;
  }

  private startAttack(state: FighterState, attack: AttackData): void {
    this.fighter.deactivateShield();
    this.setState(state);
    this.activeSequence = null;

    if (attack.stages && attack.stages.length > 0) {
      const stages = [...attack.stages].sort(
        (a, b) => a.spawnFrame - b.spawnFrame,
      );
      const lastStage = stages[stages.length - 1];
      const totalFrames =
        lastStage.spawnFrame + lastStage.activeFrames + ATTACK_RECOVERY_FRAMES;

      this.activeSequence = { stages, nextStageIndex: 0, totalFrames };
      this.attackCooldown = totalFrames;
      return;
    }

    this.attackCooldown = DEFAULT_ATTACK_DURATION;
    this.hitboxManager.spawnHitbox(
      this.fighter,
      attack,
      14,
      -2,
      12,
      16,
    );
  }

  private getAttackDuration(): number {
    return this.activeSequence?.totalFrames ?? DEFAULT_ATTACK_DURATION;
  }

  private tickAttackSequence(): void {
    if (!this.activeSequence) return;

    while (
      this.activeSequence.nextStageIndex < this.activeSequence.stages.length &&
      this.stateFrames >=
        this.activeSequence.stages[this.activeSequence.nextStageIndex].spawnFrame
    ) {
      const stage = this.activeSequence.stages[this.activeSequence.nextStageIndex];
      this.hitboxManager.spawnHitbox(
        this.fighter,
        stage,
        stage.offsetX,
        stage.offsetY,
        stage.width,
        stage.height,
      );
      this.activeSequence.nextStageIndex++;
    }
  }

  private startCharge(firedState: FighterState, attack: AttackData): void {
    this.fighter.deactivateShield();
    this.chargeState = { attack, firedState };
    this.setState('charging');
    this.attackCooldown =
      (attack.charge?.maxChargeFrames ?? 0) +
      (attack.charge?.maxHoldFrames ?? 0) +
      DEFAULT_ATTACK_DURATION;
  }

  private tickCharge(actions: ActionFrame): void {
    if (!this.chargeState) return;

    // Slow down existing momentum while charging instead of killing it instantly.
    // Use stronger drag on the ground so the fighter stops sliding, but keep
    // air momentum for controllable aerial charges.
    const body = this.fighter.body;
    const horizontalDrag = this.fighter.isGrounded
      ? CHARGE_GROUND_DRAG
      : CHARGE_AIR_DRAG;
    this.fighter.setVelocityX(body.velocity.x * horizontalDrag);

    if (!this.fighter.isGrounded) {
      let newVelocityY = body.velocity.y * CHARGE_AIR_DRAG;
      if (newVelocityY > CHARGE_MAX_FALL_SPEED) {
        newVelocityY = CHARGE_MAX_FALL_SPEED;
      }
      this.fighter.setVelocityY(newVelocityY);
    }

    const charge = this.chargeState.attack.charge!;
    const released = !this.isChargeButtonHeld(actions);
    const minReached = this.stateFrames >= charge.minChargeFrames;
    const fullCharge = this.stateFrames >= charge.maxChargeFrames;
    const autoRelease =
      this.stateFrames >= charge.maxChargeFrames + charge.maxHoldFrames;

    this.fighter.setChargingFull(fullCharge);

    if (autoRelease || (minReached && released && !fullCharge)) {
      this.fireChargedAttack(Math.min(this.stateFrames, charge.maxChargeFrames));
    } else if (fullCharge && released) {
      this.fireChargedAttack(charge.maxChargeFrames);
    }
  }

  private isChargeButtonHeld(actions: ActionFrame): boolean {
    if (!this.chargeState) return false;
    if (this.chargeState.firedState === 'heavyAttack') {
      return actions.attackHeavyHeld;
    }
    return actions.attackSpecialHeld;
  }

  private fireChargedAttack(chargeFrames: number): void {
    if (!this.chargeState) return;

    const { attack, firedState } = this.chargeState;
    const chargedAttack = this.buildChargedAttack(attack, chargeFrames);
    this.chargeState = null;
    this.startAttack(firedState, chargedAttack);
  }

  private buildChargedAttack(
    attack: AttackData,
    chargeFrames: number,
  ): AttackData {
    const charge = attack.charge!;
    const frames = Math.max(0, Math.min(chargeFrames, charge.maxChargeFrames));

    const charged: AttackData = {
      ...attack,
      id: `${attack.id}-charged`,
      baseDamage: attack.baseDamage + frames * charge.damageGrowth,
      baseKnockback: attack.baseKnockback + frames * charge.knockbackGrowth,
    };

    return charged;
  }

  private activateGadget(): void {
    const gadget = this.fighter.getConfig().gadget;
    if (gadget.id === 'shiny-rock') {
      // Placeholder: Shiny Rock does nothing.
      return;
    }
  }

  private setState(newState: FighterState): void {
    if (this.state !== newState) {
      this.state = newState;
      this.stateFrames = 0;
      if (!this.isAttackState(newState)) {
        this.activeSequence = null;
      }
      if (newState !== 'charging') {
        this.chargeState = null;
        this.fighter.setChargingFull(false);
      }
      if (newState === 'spotDodge') {
        this.fighter.enterInvincibility(SPOT_DODGE_FRAMES);
      }
    }
  }

  private isAttackState(state: FighterState): boolean {
    return (
      state === 'lightAttack' ||
      state === 'heavyAttack' ||
      state === 'special'
    );
  }
}
