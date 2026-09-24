import Phaser from 'phaser';
import { Fighter } from '../fighter/Fighter';
import { HitboxManager } from '../fighter/HitboxManager';

export interface DebugRenderOptions {
  playerHitboxes: boolean;
  attackHitboxes: boolean;
}

export class DebugRenderer {
  private graphics: Phaser.GameObjects.Graphics;
  private options: DebugRenderOptions = {
    playerHitboxes: false,
    attackHitboxes: false,
  };

  constructor(
    scene: Phaser.Scene,
    private player1: Fighter,
    private player2: Fighter,
    private hitboxManager: HitboxManager,
  ) {
    this.graphics = scene.add.graphics();
    this.graphics.setDepth(1000);
  }

  setOptions(options: Partial<DebugRenderOptions>): void {
    this.options = { ...this.options, ...options };
    if (!this.options.playerHitboxes && !this.options.attackHitboxes) {
      this.graphics.clear();
    }
  }

  getOptions(): DebugRenderOptions {
    return { ...this.options };
  }

  update(): void {
    const shouldRender =
      this.options.playerHitboxes || this.options.attackHitboxes;
    if (!shouldRender) return;

    this.graphics.clear();

    if (this.options.playerHitboxes) {
      this.drawFighterBody(this.player1, 0x00ff00);
      this.drawFighterBody(this.player2, 0x00ff00);
    }

    if (this.options.attackHitboxes) {
      this.drawHitboxes();
    }
  }

  destroy(): void {
    this.graphics.destroy();
  }

  getGraphics(): Phaser.GameObjects.Graphics {
    return this.graphics;
  }

  private drawFighterBody(fighter: Fighter, color: number): void {
    const body = fighter.body;
    this.graphics.fillStyle(color, 0.25);
    this.graphics.fillRect(body.x, body.y, body.width, body.height);
    this.graphics.lineStyle(1, color, 0.7);
    this.graphics.strokeRect(body.x, body.y, body.width, body.height);
  }

  private drawHitboxes(): void {
    const hitboxes = this.hitboxManager.getActiveHitboxes();
    for (const hb of hitboxes) {
      const body = hb.sprite.body as Phaser.Physics.Arcade.Body;
      if (body.isCircle) {
        this.graphics.fillStyle(0xff0000, 0.25);
        this.graphics.fillCircle(body.center.x, body.center.y, body.width / 2);
        this.graphics.lineStyle(1, 0xff0000, 0.7);
        this.graphics.strokeCircle(
          body.center.x,
          body.center.y,
          body.width / 2,
        );
      } else {
        this.graphics.fillStyle(0xff0000, 0.25);
        this.graphics.fillRect(body.x, body.y, body.width, body.height);
        this.graphics.lineStyle(1, 0xff0000, 0.7);
        this.graphics.strokeRect(body.x, body.y, body.width, body.height);
      }

      this.drawKnockbackVector(body, hb.attacker.facing, hb.attack);
    }
  }

  private drawKnockbackVector(
    body: Phaser.Physics.Arcade.Body,
    facing: number,
    attack: { direction: { x: number; y: number }; baseKnockback: number },
  ): void {
    const startX = body.center.x;
    const startY = body.center.y;

    const vectorLength = 20;
    const magnitude = Math.sqrt(
      attack.direction.x ** 2 + attack.direction.y ** 2,
    );
    const scale = magnitude === 0 ? 0 : vectorLength / magnitude;

    const endX = startX + attack.direction.x * facing * scale;
    const endY = startY + attack.direction.y * scale;

    this.graphics.lineStyle(1, 0xffff00, 0.9);
    this.graphics.lineBetween(startX, startY, endX, endY);
  }
}
