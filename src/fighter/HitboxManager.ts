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
      hasHit: boolean;
    }
  > = new Map();

  private hitboxIdCounter = 0;

  constructor(
    private scene: Phaser.Scene,
    private layer?: Phaser.GameObjects.Layer,
  ) {}

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

    const body = sprite.body as Phaser.Physics.Arcade.Body;
    const shape = attack.shape ?? 'circle';
    if (shape === 'box') {
      body.setSize(width, height);
    } else {
      const radius = attack.radius ?? Math.round(width / 2);
      body.setCircle(radius);
    }

    body.setImmovable(true);
    body.allowGravity = false;

    if (this.layer) {
      this.layer.add(sprite);
    }

    const id = `hb-${this.hitboxIdCounter++}`;
    this.activeHitboxes.set(id, {
      sprite,
      attacker,
      attack,
      framesRemaining: attack.activeFrames,
      hasHit: false,
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

      for (const data of this.activeHitboxes.values()) {
        if (data.attacker === self && !data.hasHit) {
          const hitboxBody = data.sprite.body as Phaser.Physics.Arcade.Body;
          const otherRect = new Phaser.Geom.Rectangle(
            other.body.x,
            other.body.y,
            other.body.width,
            other.body.height,
          );

          const hit = hitboxBody.isCircle
            ? Phaser.Geom.Intersects.CircleToRectangle(
                new Phaser.Geom.Circle(
                  hitboxBody.center.x,
                  hitboxBody.center.y,
                  hitboxBody.width / 2,
                ),
                otherRect,
              )
            : Phaser.Geom.Intersects.RectangleToRectangle(
                new Phaser.Geom.Rectangle(
                  hitboxBody.x,
                  hitboxBody.y,
                  hitboxBody.width,
                  hitboxBody.height,
                ),
                otherRect,
              );

          if (hit) {
            onHit({ attacker: self, defender: other, attack: data.attack });
            // Mark as hit so one attack doesn't multi-hit, but keep it visible
            // for its remaining active frames.
            data.hasHit = true;
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

  getActiveHitboxes(): Array<{
    sprite: Phaser.Physics.Arcade.Sprite;
    attacker: Fighter;
    attack: AttackData;
    framesRemaining: number;
    hasHit: boolean;
  }> {
    return Array.from(this.activeHitboxes.values());
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
