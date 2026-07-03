import Phaser from 'phaser';
import { Fighter } from '../fighter/Fighter';
import { GameRules, MatchState } from './GameRules';

export class UIManager {
  private p1DamageText: Phaser.GameObjects.Text;
  private p2DamageText: Phaser.GameObjects.Text;
  private p1StocksText: Phaser.GameObjects.Text;
  private p2StocksText: Phaser.GameObjects.Text;
  private timerText: Phaser.GameObjects.Text;
  private winText: Phaser.GameObjects.Text | null = null;

  constructor(
    private scene: Phaser.Scene,
    private player1: Fighter,
    private player2: Fighter,
    private gameRules: GameRules,
  ) {
    const width = scene.scale.width;

    this.p1DamageText = scene.add.text(16, 16, '', {
      fontSize: '18px',
      color: '#00ffff',
      fontFamily: 'Arial, sans-serif',
    });

    this.p2DamageText = scene.add.text(width - 16, 16, '', {
      fontSize: '18px',
      color: '#ff00ff',
      fontFamily: 'Arial, sans-serif',
    }).setOrigin(1, 0);

    this.p1StocksText = scene.add.text(16, 40, '', {
      fontSize: '10px',
      color: '#ffffff',
      fontFamily: 'Arial, sans-serif',
    });

    this.p2StocksText = scene.add.text(width - 16, 40, '', {
      fontSize: '10px',
      color: '#ffffff',
      fontFamily: 'Arial, sans-serif',
    }).setOrigin(1, 0);

    this.timerText = scene.add.text(width / 2, 16, '', {
      fontSize: '18px',
      color: '#ffffff',
      fontFamily: 'Arial, sans-serif',
    }).setOrigin(0.5, 0);
  }

  update(): void {
    this.p1DamageText.setText(`${Math.floor(this.player1.damage)}%`);
    this.p2DamageText.setText(`${Math.floor(this.player2.damage)}%`);
    this.p1StocksText.setText(`P1: ${this.player1.stocks}`);
    this.p2StocksText.setText(`P2: ${this.player2.stocks}`);

    const minutes = Math.floor(this.gameRules.matchTimer / 60);
    const seconds = this.gameRules.matchTimer % 60;
    this.timerText.setText(`${minutes}:${seconds.toString().padStart(2, '0')}`);

    if (this.gameRules.matchState !== 'active' && !this.winText) {
      this.showWinOverlay(this.gameRules.matchState);
    }
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
    this.winText.setOrigin(0.5, 0.5);
  }

  destroy(): void {
    this.p1DamageText.destroy();
    this.p2DamageText.destroy();
    this.p1StocksText.destroy();
    this.p2StocksText.destroy();
    this.timerText.destroy();
    this.winText?.destroy();
  }
}
