import { ActionFrame, FighterState } from '../shared/types';
import { AudioManager } from '../shared/AudioManager';
import { Fighter } from './Fighter';
import { HitboxManager } from './HitboxManager';

const ATTACK_COOLDOWN_FRAMES = 20;

export class FighterStateMachine {
  private state: FighterState = 'idle';
  private stateFrames = 0;
  private attackCooldown = 0;

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

    if (this.fighter.hitstunFrames > 0) {
      this.setState('hitstun');
      this.stateFrames++;
      return;
    }

    if (this.state === 'hitstun' && this.fighter.hitstunFrames === 0) {
      this.setState(this.fighter.isGrounded ? 'idle' : 'fall');
    }

    if (this.isAttackState(this.state)) {
      this.stateFrames++;
      if (this.stateFrames >= ATTACK_COOLDOWN_FRAMES) {
        this.setState(this.fighter.isGrounded ? 'idle' : 'fall');
      }
      return;
    }

    if (actions.jumpPressed && this.fighter.jump()) {
      this.audio.playJump();
    }

    if (actions.attackLight && this.attackCooldown === 0) {
      this.startAttack('lightAttack', 'light');
      return;
    }
    if (actions.attackHeavy && this.attackCooldown === 0) {
      this.startAttack('heavyAttack', 'heavy');
      return;
    }
    if (actions.attackSpecial && this.attackCooldown === 0) {
      this.startAttack('special', 'special');
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

  private startAttack(state: FighterState, attackId: string): void {
    this.setState(state);
    this.attackCooldown = ATTACK_COOLDOWN_FRAMES;
    const attack = this.fighter.getConfig().attacks[attackId];
    if (attack) {
      this.hitboxManager.spawnHitbox(
        this.fighter,
        attack,
        14,
        -2,
        12,
        16,
      );
    }
  }

  private setState(newState: FighterState): void {
    if (this.state !== newState) {
      this.state = newState;
      this.stateFrames = 0;
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
