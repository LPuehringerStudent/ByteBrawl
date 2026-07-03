import Phaser from 'phaser';
import { Fighter } from '../fighter/Fighter';

export interface CameraControllerConfig {
  minZoom: number;
  maxZoom: number;
  paddingX: number;
  paddingY: number;
  smoothFactor: number;
  deadzone: Phaser.Geom.Rectangle;
}

export interface OffscreenPlayer {
  fighter: Fighter;
  screenX: number;
  screenY: number;
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
      minZoom: 0.7,
      maxZoom: 3,
      paddingX: 80,
      paddingY: 60,
      smoothFactor: 0.1,
      deadzone: new Phaser.Geom.Rectangle(-80, -100, 480, 380),
      ...config,
    };
  }

  update(): void {
    const bounds = this.getPlayerBounds();
    const targetZoom = this.calculateTargetZoom(bounds);
    const targetCenter = this.calculateTargetCenter(bounds, targetZoom);

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

  getOffscreenPlayers(): OffscreenPlayer[] {
    const visible = this.getVisibleRect();
    const result: OffscreenPlayer[] = [];

    for (const fighter of [this.player1, this.player2]) {
      if (!visible.contains(fighter.x, fighter.y)) {
        result.push({
          fighter,
          screenX: this.clamp(fighter.x, visible.left, visible.right),
          screenY: this.clamp(fighter.y, visible.top, visible.bottom),
        });
      }
    }

    return result;
  }

  private getVisibleRect(): Phaser.Geom.Rectangle {
    const halfWidth = this.camera.width / (2 * this.camera.zoom);
    const halfHeight = this.camera.height / (2 * this.camera.zoom);
    return new Phaser.Geom.Rectangle(
      this.camera.midPoint.x - halfWidth,
      this.camera.midPoint.y - halfHeight,
      halfWidth * 2,
      halfHeight * 2,
    );
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
    const fitZoom = Math.min(zoomX, zoomY);

    const deadzoneZoomX = this.camera.width / this.config.deadzone.width;
    const deadzoneZoomY = this.camera.height / this.config.deadzone.height;
    const maxZoomOut = Math.max(deadzoneZoomX, deadzoneZoomY);

    const minZoom = Math.max(this.config.minZoom, maxZoomOut);
    return Phaser.Math.Clamp(fitZoom, minZoom, this.config.maxZoom);
  }

  private calculateTargetCenter(
    bounds: Phaser.Geom.Rectangle,
    zoom: number,
  ): { x: number; y: number } {
    const halfWidth = this.camera.width / (2 * zoom);
    const halfHeight = this.camera.height / (2 * zoom);

    const minCenterX = this.config.deadzone.left + halfWidth;
    const maxCenterX = this.config.deadzone.right - halfWidth;
    const minCenterY = this.config.deadzone.top + halfHeight;
    const maxCenterY = this.config.deadzone.bottom - halfHeight;

    return {
      x: Phaser.Math.Clamp(bounds.centerX, minCenterX, maxCenterX),
      y: Phaser.Math.Clamp(bounds.centerY, minCenterY, maxCenterY),
    };
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, value));
  }
}
