import Phaser from 'phaser';
import { FighterConfig } from '../shared/types';

export class Fighter {
  sprite: Phaser.Physics.Arcade.Sprite;
  facing: number = 1;
  damage: number = 0;
  stocks: number = 3;
  invincibleFrames: number = 0;
  hitstunFrames: number = 0;
  maxAirJumps = 2;
  airJumpsRemaining = 2;
  droppingThrough = false;
  private wasGrounded = false;

  private config: FighterConfig;

  constructor(
    scene: Phaser.Scene,
    x: number,
    y: number,
    texture: string,
    config: FighterConfig,
  ) {
    this.config = config;
    this.sprite = scene.physics.add.sprite(x, y, texture);
    this.sprite.setCollideWorldBounds(false);
    this.sprite.setBounce(0);
    this.sprite.setDrag(0);
    this.sprite.setFriction(0);
    this.sprite.setMaxVelocity(400, 600);
    this.sprite.body!.setSize(12, 20);
    this.sprite.body!.setOffset(2, 4);
    this.facing = 1;
  }

  get x(): number {
    return this.sprite.x;
  }

  get y(): number {
    return this.sprite.y;
  }

  get body(): Phaser.Physics.Arcade.Body {
    return this.sprite.body as Phaser.Physics.Arcade.Body;
  }

  get isGrounded(): boolean {
    return this.body.onFloor();
  }

  get isAirborne(): boolean {
    return !this.isGrounded;
  }

  setVelocityX(value: number): void {
    this.sprite.setVelocityX(value);
  }

  setVelocityY(value: number): void {
    this.sprite.setVelocityY(value);
  }

  jump(): boolean {
    if (this.isGrounded) {
      this.sprite.setVelocityY(-this.config.jumpSpeed);
      this.airJumpsRemaining = this.maxAirJumps;
      return true;
    }

    if (this.airJumpsRemaining > 0) {
      this.sprite.setVelocityY(-this.config.jumpSpeed);
      this.airJumpsRemaining--;
      return true;
    }

    return false;
  }

  faceDirection(dir: number): void {
    if (dir === 0) return;
    this.facing = dir > 0 ? 1 : -1;
    this.sprite.setFlipX(this.facing < 0);
  }

  applyKnockback(vector: { x: number; y: number }): void {
    this.sprite.setVelocity(vector.x, vector.y);
  }

  enterHitstun(frames: number): void {
    this.hitstunFrames = frames;
  }

  enterInvincibility(frames: number): void {
    this.invincibleFrames = frames;
  }

  takeDamage(amount: number): void {
    this.damage += amount;
  }

  loseStock(): void {
    this.stocks -= 1;
    this.damage = 0;
  }

  respawn(x: number, y: number, invincibilityFrames = 120): void {
    this.sprite.setPosition(x, y);
    this.sprite.setVelocity(0, 0);
    this.damage = 0;
    this.hitstunFrames = 0;
    this.enterInvincibility(invincibilityFrames);
  }

  update(): void {
    if (this.hitstunFrames > 0) {
      this.hitstunFrames--;
    }
    if (this.invincibleFrames > 0) {
      this.invincibleFrames--;
      this.sprite.setAlpha(this.invincibleFrames % 12 < 6 ? 0.5 : 1);
    } else {
      this.sprite.setAlpha(1);
    }

    const grounded = this.isGrounded;
    if (grounded && !this.wasGrounded) {
      this.airJumpsRemaining = this.maxAirJumps;
    }
    this.wasGrounded = grounded;
  }

  getConfig(): FighterConfig {
    return this.config;
  }
}
