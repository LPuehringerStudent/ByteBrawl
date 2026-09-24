import Phaser from 'phaser';
import { MenuManager } from '../menu/MenuManager';

interface MenuItem {
  label: string;
  action: () => void;
}

export class MainMenuScene extends Phaser.Scene {
  private menuManager!: MenuManager;
  private selectedIndex = 0;
  private itemTexts: Phaser.GameObjects.Text[] = [];
  private enterKey!: Phaser.Input.Keyboard.Key;

  constructor() {
    super({ key: 'MainMenuScene' });
  }

  create(): void {
    this.menuManager = this.registry.get('menuManager') as MenuManager;
    this.menuManager.hide();

    this.add
      .text(160, 40, 'BYTE BRAWL', {
        fontSize: '32px',
        color: '#ffffff',
        fontStyle: 'bold',
      })
      .setOrigin(0.5);

    const items: MenuItem[] = [
      { label: 'Local Versus', action: () => this.scene.start('CharacterSelectScene') },
      { label: 'Training', action: () => this.startTraining() },
      { label: 'Controls', action: () => this.showControls() },
      { label: 'Settings', action: () => this.showSettings() },
      { label: 'Credits', action: () => this.showCredits() },
      {
        label: 'Quit',
        action: () => {
          this.add
            .text(160, 150, 'Thanks for playing!', {
              fontSize: '12px',
              color: '#aaaaaa',
            })
            .setOrigin(0.5);
        },
      },
    ];

    this.itemTexts = items.map((item, index) => {
      const text = this.add
        .text(160, 80 + index * 16, item.label, {
          fontSize: '14px',
          color: index === 0 ? '#00ffff' : '#ffffff',
        })
        .setOrigin(0.5)
        .setInteractive();

      text.on('pointerover', () => {
        this.selectedIndex = index;
        this.updateSelection();
      });

      text.on('pointerdown', () => {
        this.selectedIndex = index;
        this.updateSelection();
        items[index].action();
      });

      return text;
    });

    this.enterKey = this.input.keyboard!.addKey(
      Phaser.Input.Keyboard.KeyCodes.ENTER,
    );

    this.input.keyboard!.on('keydown-UP', () => this.moveSelection(-1));
    this.input.keyboard!.on('keydown-DOWN', () => this.moveSelection(1));
    this.input.keyboard!.on('keydown-W', () => this.moveSelection(-1));
    this.input.keyboard!.on('keydown-S', () => this.moveSelection(1));
    this.input.keyboard!.on('keydown-J', () => items[this.selectedIndex].action());
    this.enterKey.on('down', () => items[this.selectedIndex].action());

    this.updateSelection();
  }

  private moveSelection(delta: number): void {
    this.selectedIndex =
      (this.selectedIndex + delta + this.itemTexts.length) %
      this.itemTexts.length;
    this.updateSelection();
  }

  private updateSelection(): void {
    this.itemTexts.forEach((text, index) => {
      text.setColor(index === this.selectedIndex ? '#00ffff' : '#ffffff');
      text.setScale(index === this.selectedIndex ? 1.1 : 1);
    });
  }

  private startTraining(): void {
    this.scene.start('CharacterSelectScene', { mode: 'training' });
  }

  private showControls(): void {
    this.input.enabled = false;
    this.menuManager.showControls({
      onBack: () => {
        this.menuManager.hide();
        this.input.enabled = true;
      },
    });
  }

  private showSettings(): void {
    this.input.enabled = false;
    this.menuManager.showSettings({
      onBack: () => {
        this.menuManager.hide();
        this.input.enabled = true;
      },
    });
  }

  private showCredits(): void {
    this.input.enabled = false;
    this.menuManager.showCredits({
      onBack: () => {
        this.menuManager.hide();
        this.input.enabled = true;
      },
    });
  }
}
