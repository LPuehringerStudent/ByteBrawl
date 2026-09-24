import Phaser from 'phaser';
import { FighterConfig } from '../shared/types';

export class StageSelectScene extends Phaser.Scene {
  private p1Config!: FighterConfig;
  private p2Config!: FighterConfig;

  constructor() {
    super({ key: 'StageSelectScene' });
  }

  init(data: {
    p1Config?: FighterConfig;
    p2Config?: FighterConfig;
  }): void {
    this.p1Config = data.p1Config!;
    this.p2Config = data.p2Config!;
  }

  create(): void {
    this.add
      .text(160, 20, 'Choose Stage', {
        fontSize: '20px',
        color: '#ffffff',
        fontStyle: 'bold',
      })
      .setOrigin(0.5);

    const rect = this.add.rectangle(160, 80, 180, 80, 0x1a1a2e);
    rect.setStrokeStyle(2, 0x444444);

    this.add
      .text(160, 80, 'Training Grounds', {
        fontSize: '14px',
        color: '#ffffff',
      })
      .setOrigin(0.5);

    const instruction = this.add
      .text(160, 150, 'J: fight     K: back', {
        fontSize: '12px',
        color: '#aaaaaa',
      })
      .setOrigin(0.5);

    this.tweens.add({
      targets: instruction,
      alpha: 0.4,
      duration: 600,
      yoyo: true,
      repeat: -1,
    });

    const startGame = () => {
      this.scene.start('GameScene', {
        mode: 'versus',
        p1Config: this.p1Config,
        p2Config: this.p2Config,
      });
    };

    this.input.keyboard!.on('keydown-ENTER', startGame);
    this.input.keyboard!.on('keydown-J', startGame);

    const goBack = () => {
      this.scene.start('CharacterSelectScene', { mode: 'versus' });
    };

    this.input.keyboard!.on('keydown-ESC', goBack);
    this.input.keyboard!.on('keydown-K', goBack);
  }
}
