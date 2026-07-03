import { Fighter } from '../fighter/Fighter';
import { GameRules, MatchState } from './GameRules';

export class UIManager {
  private p1NameEl: HTMLElement;
  private p1DamageEl: HTMLElement;
  private p1StocksEl: HTMLElement;
  private p2NameEl: HTMLElement;
  private p2DamageEl: HTMLElement;
  private p2StocksEl: HTMLElement;
  private timerEl: HTMLElement;
  private winMessageEl: HTMLElement;

  constructor(
    private player1: Fighter,
    private player2: Fighter,
    private gameRules: GameRules,
  ) {
    this.p1NameEl = document.getElementById('p1-name')!;
    this.p1DamageEl = document.getElementById('p1-damage')!;
    this.p1StocksEl = document.getElementById('p1-stocks')!;
    this.p2NameEl = document.getElementById('p2-name')!;
    this.p2DamageEl = document.getElementById('p2-damage')!;
    this.p2StocksEl = document.getElementById('p2-stocks')!;
    this.timerEl = document.getElementById('timer')!;
    this.winMessageEl = document.getElementById('win-message')!;

    this.p1NameEl.textContent = this.player1.getConfig().name;
    this.p2NameEl.textContent = this.player2.getConfig().name;

    const p1Icon = document.getElementById('p1-icon')!;
    const p2Icon = document.getElementById('p2-icon')!;
    p1Icon.style.backgroundColor = '#00ffff';
    p2Icon.style.backgroundColor = '#ff00ff';
  }

  update(): void {
    this.p1DamageEl.textContent = `${Math.floor(this.player1.damage)}%`;
    this.p2DamageEl.textContent = `${Math.floor(this.player2.damage)}%`;
    this.p1StocksEl.textContent = `Stocks: ${this.player1.stocks}`;
    this.p2StocksEl.textContent = `Stocks: ${this.player2.stocks}`;

    const minutes = Math.floor(this.gameRules.matchTimer / 60);
    const seconds = this.gameRules.matchTimer % 60;
    this.timerEl.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

    if (this.gameRules.matchState !== 'active') {
      this.showWinOverlay(this.gameRules.matchState);
    }
  }

  private showWinOverlay(state: MatchState): void {
    if (this.winMessageEl.style.display === 'block') return;
    this.winMessageEl.textContent =
      state === 'p1Win' ? 'PLAYER 1 WINS!' : 'PLAYER 2 WINS!';
    this.winMessageEl.style.display = 'block';
  }

  destroy(): void {
    this.winMessageEl.style.display = 'none';
  }
}
