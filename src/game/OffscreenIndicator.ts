import Phaser from 'phaser';
import { Fighter } from '../fighter/Fighter';
import { OffscreenPlayer } from './CameraController';

const INDICATOR_RADIUS = 6;

export class OffscreenIndicator {
  private indicators: Map<Fighter, Phaser.GameObjects.Graphics> = new Map();

  constructor(
    private scene: Phaser.Scene,
    private layer?: Phaser.GameObjects.Layer,
  ) {}

  update(offscreenPlayers: OffscreenPlayer[]): void {
    const seen = new Set<Fighter>();

    for (const { fighter, screenX, screenY } of offscreenPlayers) {
      seen.add(fighter);
      const color = fighter.sprite.texture.key === 'fighter-p1'
        ? 0x00ffff
        : 0xff00ff;

      let indicator = this.indicators.get(fighter);
      if (!indicator) {
        indicator = this.scene.add.graphics();
        if (this.layer) {
          this.layer.add(indicator);
        }
        this.indicators.set(fighter, indicator);
      }

      indicator.clear();
      indicator.fillStyle(color, 1);
      indicator.fillCircle(screenX, screenY, INDICATOR_RADIUS);
      indicator.setDepth(100);
    }

    for (const [fighter, indicator] of this.indicators) {
      if (!seen.has(fighter)) {
        indicator.destroy();
        this.indicators.delete(fighter);
      }
    }
  }

  destroy(): void {
    for (const indicator of this.indicators.values()) {
      indicator.destroy();
    }
    this.indicators.clear();
  }
}
