import Phaser from 'phaser';
import { StageConfig } from '../shared/types';

export class Stage {
  private platformLayer: Phaser.Tilemaps.TilemapLayer | null = null;
  private blastZone: Phaser.Geom.Rectangle;
  private player1Spawn: { x: number; y: number };
  private player2Spawn: { x: number; y: number };

  constructor(
    private scene: Phaser.Scene,
    config: StageConfig,
  ) {
    this.blastZone = new Phaser.Geom.Rectangle(
      config.blastZone.x,
      config.blastZone.y,
      config.blastZone.width,
      config.blastZone.height,
    );
    this.player1Spawn = config.player1Spawn;
    this.player2Spawn = config.player2Spawn;
    this.createTilemap(config);
  }

  private createTilemap(config: StageConfig): void {
    const map = this.scene.make.tilemap({
      data: config.layout,
      tileWidth: config.tileSize,
      tileHeight: config.tileSize,
      width: config.layout[0]?.length ?? 0,
      height: config.layout.length,
    });

    const tileset = map.addTilesetImage('platform', 'platform');
    if (!tileset) {
      throw new Error('Failed to create tileset from platform texture');
    }

    this.platformLayer = map.createLayer(0, tileset, 0, 0);
    if (!this.platformLayer) {
      throw new Error('Failed to create platform layer');
    }

    this.platformLayer.setCollisionByExclusion([-1]);
  }

  getPlatformLayer(): Phaser.Tilemaps.TilemapLayer {
    if (!this.platformLayer) {
      throw new Error('Platform layer not initialized');
    }
    return this.platformLayer;
  }

  isOutOfBounds(x: number, y: number): boolean {
    return !this.blastZone.contains(x, y);
  }

  getPlayer1Spawn(): { x: number; y: number } {
    return { ...this.player1Spawn };
  }

  getPlayer2Spawn(): { x: number; y: number } {
    return { ...this.player2Spawn };
  }

  getBounds(): Phaser.Geom.Rectangle {
    return this.blastZone;
  }

  destroy(): void {
    this.platformLayer?.destroy();
  }
}
