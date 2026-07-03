import Phaser from 'phaser';

export class AssetLoader {
  constructor(private scene: Phaser.Scene) {}

  preload(): void {
    // Placeholder textures created at runtime in BootScene/GameScene.
    // Real spritesheets and tilemaps can be loaded here later.
  }

  createPlaceholderTextures(): void {
    const graphics = this.scene.add.graphics();

    // Player 1 texture: cyan byte
    graphics.fillStyle(0x00ffff, 1);
    graphics.fillRect(0, 0, 16, 24);
    graphics.generateTexture('fighter-p1', 16, 24);
    graphics.clear();

    // Player 2 texture: magenta nibble
    graphics.fillStyle(0xff00ff, 1);
    graphics.fillRect(0, 0, 16, 24);
    graphics.generateTexture('fighter-p2', 16, 24);
    graphics.clear();

    // Platform texture
    graphics.fillStyle(0x444444, 1);
    graphics.fillRect(0, 0, 16, 16);
    graphics.generateTexture('platform', 16, 16);
    graphics.clear();

    // Hitbox texture (debug)
    graphics.lineStyle(2, 0xff0000, 1);
    graphics.strokeRect(0, 0, 16, 16);
    graphics.generateTexture('hitbox', 16, 16);
    graphics.clear();

    graphics.destroy();
  }
}
