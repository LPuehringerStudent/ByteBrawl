import Phaser from 'phaser';
import { AssetLoader } from '../shared/AssetLoader';

export class BootScene extends Phaser.Scene {
  constructor() {
    super({ key: 'BootScene' });
  }

  create(): void {
    const assetLoader = new AssetLoader(this);
    assetLoader.createPlaceholderTextures();

    this.scene.start('MainMenuScene');
  }
}
