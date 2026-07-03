import { AttackData } from '../shared/types';
import { AudioManager } from '../shared/AudioManager';
import { Fighter } from '../fighter/Fighter';

export interface GameRulesConfig {
  startingStocks: number;
  respawnInvincibilityFrames: number;
  maxDamage: number;
}

export type MatchState = 'active' | 'p1Win' | 'p2Win';

export class GameRules {
  matchState: MatchState = 'active';
  matchTimer = 180; // seconds
  private elapsed = 0;

  constructor(
    private config: GameRulesConfig,
    private audio: AudioManager,
    private player1: Fighter,
    private player2: Fighter,
  ) {}

  applyHit(
    _attacker: Fighter,
    defender: Fighter,
    attack: AttackData,
  ): void {
    if (this.matchState !== 'active') return;

    const preDamage = defender.damage;
    defender.takeDamage(attack.baseDamage);

    const knockbackX =
      attack.direction.x *
      (attack.baseKnockback + preDamage * attack.scaling);
    const knockbackY =
      attack.direction.y *
      (attack.baseKnockback + preDamage * attack.scaling);

    defender.applyKnockback({ x: knockbackX, y: knockbackY });
    defender.enterHitstun(attack.hitstunFrames);
    this.audio.playHit();
  }

  checkRingOut(
    player: Fighter,
    isOutOfBounds: (x: number, y: number) => boolean,
    respawnPoint: { x: number; y: number },
  ): void {
    if (this.matchState !== 'active') return;

    if (isOutOfBounds(player.x, player.y)) {
      player.loseStock();
      if (player.stocks <= 0) {
        if (player === this.player1) {
          this.matchState = 'p2Win';
        } else if (player === this.player2) {
          this.matchState = 'p1Win';
        }
      } else {
        player.respawn(
          respawnPoint.x,
          respawnPoint.y,
          this.config.respawnInvincibilityFrames,
        );
      }
    }
  }

  update(dt: number): void {
    if (this.matchState !== 'active') return;
    this.elapsed += dt;
    if (this.elapsed >= 1000) {
      this.matchTimer--;
      this.elapsed -= 1000;
      if (this.matchTimer <= 0) {
        this.matchTimer = 0;
        this.matchState = 'p1Win'; // Default tie-breaker for MVP.
      }
    }
  }

  resetTimer(): void {
    this.matchTimer = 180;
    this.elapsed = 0;
  }
}
