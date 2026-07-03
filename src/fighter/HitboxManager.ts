import Phaser from 'phaser';
import { AttackData } from '../shared/types';
import { Fighter } from './Fighter';

export interface HitEvent {
  attacker: Fighter;
  defender: Fighter;
  attack: AttackData;
}

export class HitboxManager {
  private activeHitboxes: Map<
    string,
    {
      sprite: Phaser.Physics.Arcade.Sprite;
      attacker: Fighter;
      attack: AttackData;
      framesRemaining: number;
    }
  > = new Map();

  private hitboxIdCounter = 0;

  constructor(private scene: Phaser.Scene) {}

  spawnHitbox(
    attacker: Fighter,
    attack: AttackData,
    offsetX: number,
    offsetY: number,
    width: number,
    height: number,
  ): void {
    const x = attacker.x + offsetX * attacker.facing;
    const y = attacker.y + offsetY;
    const sprite = this.scene.physics.add.sprite(x, y, 'hitbox');
    sprite.setVisible(false);
    sprite.body!.setSize(width, height);
    sprite.body!.setImmovable(true);
    sprite.body!.allowGravity = false;

    const id = `hb-${this.hitboxIdCounter++}`;
    this.activeHitboxes.set(id, {
      sprite,
      attacker,
      attack,
      framesRemaining: attack.activeFrames,
    });
  }

  checkHits(
    playerA: Fighter,
    playerB: Fighter,
    onHit: (event: HitEvent) => void,
  ): void {
    const players = [
      { self: playerA, other: playerB },
      { self: playerB, other: playerA },
    ];

    for (const { self, other } of players) {
      if (other.invincibleFrames > 0) continue;

      for (const [id, data] of this.activeHitboxes) {
        if (data.attacker === self) {
          const hitboxBody = data.sprite.body as Phaser.Physics.Arcade.Body;
          const hitboxRect = new Phaser.Geom.Rectangle(
            hitboxBody.x,
            hitboxBody.y,
            hitboxBody.width,
            hitboxBody.height,
          );
          const otherRect = new Phaser.Geom.Rectangle(
            other.body.x,
            other.body.y,
            other.body.width,
            other.body.height,
          );
          if (Phaser.Geom.Intersects.RectangleToRectangle(hitboxRect, otherRect)) {
            onHit({ attacker: self, defender: other, attack: data.attack });
            // Remove this hitbox after it connects so one attack doesn't multi-hit.
            this.removeHitbox(id);
            break;
          }
        }
      }
    }
  }

  update(): void {
    for (const [id, data] of this.activeHitboxes) {
      data.framesRemaining--;
      if (data.framesRemaining <= 0) {
        this.removeHitbox(id);
      }
    }
  }

  clear(): void {
    for (const id of this.activeHitboxes.keys()) {
      this.removeHitbox(id);
    }
  }

  private removeHitbox(id: string): void {
    const data = this.activeHitboxes.get(id);
    if (data) {
      data.sprite.destroy();
      this.activeHitboxes.delete(id);
    }
  }
}
