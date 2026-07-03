import Phaser from 'phaser';
import { Fighter } from '../fighter/Fighter';
import { GameRules, MatchState } from './GameRules';

export class UIManager {
  private p1Icon: Phaser.GameObjects.Image;
  private p1NameText: Phaser.GameObjects.Text;
  private p1DamageText: Phaser.GameObjects.Text;
  private p1StocksText: Phaser.GameObjects.Text;
  private p2Icon: Phaser.GameObjects.Image;
  private p2NameText: Phaser.GameObjects.Text;
  private p2DamageText: Phaser.GameObjects.Text;
  private p2StocksText: Phaser.GameObjects.Text;
  private timerText: Phaser.GameObjects.Text;
  private winText: Phaser.GameObjects.Text | null = null;

  constructor(
    private scene: Phaser.Scene,
    private layer: Phaser.GameObjects.Layer,
    private player1: Fighter,
    private player2: Fighter,
    private gameRules: GameRules,
  ) {
    const width = scene.scale.width;

    const textStyle = {
      fontFamily: 'Arial, sans-serif',
      color: '#ffffff',
    };

    const nameStyle = { ...textStyle, fontSize: '10px' };
    const damageStyle = { ...textStyle, fontSize: '16px', fontStyle: 'bold' };
    const stocksStyle = { ...textStyle, fontSize: '9px' };

    // Player 1 panel (top left).
    this.p1Icon = this.createIcon(14, 14, this.player1.sprite.texture.key, false);
    this.p1NameText = scene.add.text(34, 10, this.player1.getConfig().name, {
      ...nameStyle,
      color: '#00ffff',
    });
    this.p1DamageText = scene.add.text(34, 24, '0%', {
      ...damageStyle,
      color: '#00ffff',
    });
    this.p1StocksText = scene.add.text(34, 42, 'Stocks: 3', stocksStyle);

    // Player 2 panel (top right).
    this.p2Icon = this.createIcon(width - 14, 14, this.player2.sprite.texture.key, true);
    this.p2NameText = scene.add.text(width - 34, 10, this.player2.getConfig().name, {
      ...nameStyle,
      color: '#ff00ff',
    }).setOrigin(1, 0);
    this.p2DamageText = scene.add.text(width - 34, 24, '0%', {
      ...damageStyle,
      color: '#ff00ff',
    }).setOrigin(1, 0);
    this.p2StocksText = scene.add.text(width - 34, 42, 'Stocks: 3', stocksStyle).setOrigin(1, 0);

    // Timer (top center).
    this.timerText = scene.add.text(width / 2, 12, '3:00', {
      ...textStyle,
      fontSize: '16px',
    }).setOrigin(0.5, 0);

    // Add every HUD element to the UI layer so the dedicated UI camera renders it.
    for (const obj of [
      this.p1Icon,
      this.p1NameText,
      this.p1DamageText,
      this.p1StocksText,
      this.p2Icon,
      this.p2NameText,
      this.p2DamageText,
      this.p2StocksText,
      this.timerText,
    ]) {
      this.layer.add(obj);
      obj.setDepth(100);
    }
  }

  update(): void {
    this.p1DamageText.setText(`${Math.floor(this.player1.damage)}%`);
    this.p2DamageText.setText(`${Math.floor(this.player2.damage)}%`);
    this.p1StocksText.setText(`Stocks: ${this.player1.stocks}`);
    this.p2StocksText.setText(`Stocks: ${this.player2.stocks}`);

    const minutes = Math.floor(this.gameRules.matchTimer / 60);
    const seconds = this.gameRules.matchTimer % 60;
    this.timerText.setText(`${minutes}:${seconds.toString().padStart(2, '0')}`);

    if (this.gameRules.matchState !== 'active' && !this.winText) {
      this.showWinOverlay(this.gameRules.matchState);
    }
  }

  private createIcon(
    x: number,
    y: number,
    texture: string,
    rightAligned: boolean,
  ): Phaser.GameObjects.Image {
    const icon = this.scene.add.image(x, y, texture);
    icon.setScale(1.5);
    icon.setOrigin(rightAligned ? 1 : 0, 0);
    return icon;
  }

  private showWinOverlay(state: MatchState): void {
    const width = this.scene.scale.width;
    const height = this.scene.scale.height;
    const message = state === 'p1Win' ? 'PLAYER 1 WINS!' : 'PLAYER 2 WINS!';

    this.winText = this.scene.add.text(width / 2, height / 2, message, {
      fontSize: '20px',
      color: '#ffff00',
      fontFamily: 'Arial, sans-serif',
    });
    this.winText.setOrigin(0.5, 0.5).setDepth(100);
    this.layer.add(this.winText);
  }

  destroy(): void {
    this.p1Icon.destroy();
    this.p1NameText.destroy();
    this.p1DamageText.destroy();
    this.p1StocksText.destroy();
    this.p2Icon.destroy();
    this.p2NameText.destroy();
    this.p2DamageText.destroy();
    this.p2StocksText.destroy();
    this.timerText.destroy();
    this.winText?.destroy();
  }
}
