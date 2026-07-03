import Phaser from 'phaser';
import { Fighter } from '../fighter/Fighter';

export interface CameraControllerConfig {
  minZoom: number;
  maxZoom: number;
  paddingX: number;
  paddingY: number;
  smoothFactor: number;
}

export class CameraController {
  private config: CameraControllerConfig;

  constructor(
    private camera: Phaser.Cameras.Scene2D.Camera,
    private player1: Fighter,
    private player2: Fighter,
    config?: Partial<CameraControllerConfig>,
  ) {
    this.config = {
      minZoom: 1,
      maxZoom: 3,
      paddingX: 80,
      paddingY: 60,
      smoothFactor: 0.1,
      ...config,
    };
  }

  update(): void {
    const bounds = this.getPlayerBounds();
    const targetZoom = this.calculateTargetZoom(bounds);
    const targetCenter = this.calculateTargetCenter(bounds);

    const newZoom = Phaser.Math.Linear(
      this.camera.zoom,
      targetZoom,
      this.config.smoothFactor,
    );
    const newX = Phaser.Math.Linear(
      this.camera.midPoint.x,
      targetCenter.x,
      this.config.smoothFactor,
    );
    const newY = Phaser.Math.Linear(
      this.camera.midPoint.y,
      targetCenter.y,
      this.config.smoothFactor,
    );

    this.camera.setZoom(newZoom);
    this.camera.centerOn(newX, newY);
  }

  private getPlayerBounds(): Phaser.Geom.Rectangle {
    const minX = Math.min(this.player1.x, this.player2.x);
    const maxX = Math.max(this.player1.x, this.player2.x);
    const minY = Math.min(this.player1.y, this.player2.y);
    const maxY = Math.max(this.player1.y, this.player2.y);

    return new Phaser.Geom.Rectangle(
      minX,
      minY,
      Math.max(maxX - minX, 1),
      Math.max(maxY - minY, 1),
    );
  }

  private calculateTargetZoom(bounds: Phaser.Geom.Rectangle): number {
    const requiredWidth = bounds.width + this.config.paddingX * 2;
    const requiredHeight = bounds.height + this.config.paddingY * 2;

    const zoomX = this.camera.width / requiredWidth;
    const zoomY = this.camera.height / requiredHeight;
    const zoom = Math.min(zoomX, zoomY);

    return Phaser.Math.Clamp(zoom, this.config.minZoom, this.config.maxZoom);
  }

  private calculateTargetCenter(bounds: Phaser.Geom.Rectangle): {
    x: number;
    y: number;
  } {
    return {
      x: bounds.centerX,
      y: bounds.centerY,
    };
  }
}
