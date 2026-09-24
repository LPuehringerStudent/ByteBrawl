export type MenuScreen =
  | 'pause'
  | 'result'
  | 'settings'
  | 'controls'
  | 'credits'
  | null;

export interface PauseCallbacks {
  onResume: () => void;
  onControls: () => void;
  onSettings: () => void;
  onQuit: () => void;
  onTogglePlayerHitboxes?: () => void;
  onToggleAttackHitboxes?: () => void;
}

export interface ResultCallbacks {
  onRematch: () => void;
  onMenu: () => void;
}

export interface BackCallback {
  onBack: () => void;
}

export class MenuManager {
  private overlay: HTMLElement;
  private screens: Map<MenuScreen, HTMLElement> = new Map();
  private activeScreen: MenuScreen = null;

  constructor() {
    this.overlay = document.getElementById('menu-overlay')!;
    this.screens.set('pause', document.getElementById('pause-menu')!);
    this.screens.set('result', document.getElementById('result-menu')!);
    this.screens.set('settings', document.getElementById('settings-menu')!);
    this.screens.set('controls', document.getElementById('controls-menu')!);
    this.screens.set('credits', document.getElementById('credits-menu')!);
  }

  showPause(callbacks: PauseCallbacks): void {
    this.bindOnce('pause-resume', callbacks.onResume);
    this.bindOnce('pause-controls', callbacks.onControls);
    this.bindOnce('pause-settings', callbacks.onSettings);
    this.bindOnce('pause-quit', callbacks.onQuit);

    const playerHitboxButton = document.getElementById('pause-player-hitboxes');
    if (playerHitboxButton) {
      if (callbacks.onTogglePlayerHitboxes) {
        playerHitboxButton.style.display = 'block';
        this.bindOnce('pause-player-hitboxes', callbacks.onTogglePlayerHitboxes);
      } else {
        playerHitboxButton.style.display = 'none';
      }
    }

    const attackHitboxButton = document.getElementById('pause-attack-hitboxes');
    if (attackHitboxButton) {
      if (callbacks.onToggleAttackHitboxes) {
        attackHitboxButton.style.display = 'block';
        this.bindOnce('pause-attack-hitboxes', callbacks.onToggleAttackHitboxes);
      } else {
        attackHitboxButton.style.display = 'none';
      }
    }

    this.show('pause');
  }

  showResult(
    winner: 'p1' | 'p2' | 'draw',
    callbacks: ResultCallbacks,
  ): void {
    const winnerEl = document.getElementById('result-winner')!;
    winnerEl.textContent =
      winner === 'draw'
        ? 'Draw!'
        : winner === 'p1'
          ? 'Player 1 Wins!'
          : 'Player 2 Wins!';
    winnerEl.style.color = winner === 'p1' ? '#00ffff' : '#ff00ff';

    this.bindOnce('result-rematch', callbacks.onRematch);
    this.bindOnce('result-menu-btn', callbacks.onMenu);
    this.show('result');
  }

  showSettings(callbacks: BackCallback): void {
    this.bindOnce('settings-back', callbacks.onBack);
    this.show('settings');
  }

  showControls(callbacks: BackCallback): void {
    this.bindOnce('controls-back', callbacks.onBack);
    this.show('controls');
  }

  showCredits(callbacks: BackCallback): void {
    this.bindOnce('credits-back', callbacks.onBack);
    this.show('credits');
  }

  hide(): void {
    this.overlay.classList.remove('active');
    if (this.activeScreen) {
      const screen = this.screens.get(this.activeScreen);
      if (screen) screen.classList.remove('active');
    }
    this.activeScreen = null;
  }

  isVisible(): boolean {
    return this.overlay.classList.contains('active');
  }

  private show(screen: MenuScreen): void {
    if (this.activeScreen) {
      const current = this.screens.get(this.activeScreen);
      if (current) current.classList.remove('active');
    }

    this.activeScreen = screen;
    const next = this.screens.get(screen);
    if (next) next.classList.add('active');
    this.overlay.classList.add('active');
  }

  private bindOnce(id: string, handler: () => void): void {
    const element = document.getElementById(id);
    if (!element) return;

    // Clone to remove any previous listeners.
    const clone = element.cloneNode(true) as HTMLElement;
    element.parentNode?.replaceChild(clone, element);
    clone.addEventListener('click', (event) => {
      event.stopPropagation();
      event.preventDefault();
      handler();
    });
  }
}
