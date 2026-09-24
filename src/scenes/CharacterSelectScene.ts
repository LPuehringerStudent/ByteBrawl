import Phaser from 'phaser';
import { FighterConfig } from '../shared/types';
import { FIGHTER_P1_CONFIG, FIGHTER_P2_CONFIG } from '../game/config';
import { SWORD_CONFIG } from '../game/weapons';
import { SHINY_ROCK_CONFIG } from '../game/gadgets';

type CategoryType = 'character' | 'weapon' | 'gadget';

interface Category {
  type: CategoryType;
  player: 1 | 2;
  label: string;
  x: number;
  y: number;
  color: number;
}

interface CategoryUI {
  border: Phaser.GameObjects.Rectangle;
  nameText: Phaser.GameObjects.Text;
  labelText: Phaser.GameObjects.Text;
}

const CHARACTERS: FighterConfig[] = [FIGHTER_P1_CONFIG, FIGHTER_P2_CONFIG];
const WEAPONS = [SWORD_CONFIG];
const GADGETS = [SHINY_ROCK_CONFIG];

export class CharacterSelectScene extends Phaser.Scene {
  private mode: 'versus' | 'training' = 'versus';
  private categories: Category[] = [];
  private selection: number[] = [];
  private currentCategory = 0;
  private categoryUIs: CategoryUI[] = [];
  private leftArrow!: Phaser.GameObjects.Text;
  private rightArrow!: Phaser.GameObjects.Text;
  private instruction!: Phaser.GameObjects.Text;

  constructor() {
    super({ key: 'CharacterSelectScene' });
  }

  init(data: { mode?: 'versus' | 'training' }): void {
    this.mode = data.mode ?? 'versus';
  }

  create(): void {
    const title = this.mode === 'training' ? 'Training Setup' : 'Choose Fighters';
    this.add
      .text(160, 20, title, {
        fontSize: '20px',
        color: '#ffffff',
        fontStyle: 'bold',
      })
      .setOrigin(0.5);

    this.buildCategories();

    this.categoryUIs = this.categories.map((category) =>
      this.renderCategory(category),
    );

    this.leftArrow = this.add
      .text(0, 0, '<', {
        fontSize: '18px',
        color: '#ffffff',
        fontStyle: 'bold',
      })
      .setOrigin(0.5)
      .setVisible(false);

    this.rightArrow = this.add
      .text(0, 0, '>', {
        fontSize: '18px',
        color: '#ffffff',
        fontStyle: 'bold',
      })
      .setOrigin(0.5)
      .setVisible(false);

    this.instruction = this.add
      .text(160, 170, 'W/S: category  A/D: option  J: select  K: back', {
        fontSize: '10px',
        color: '#aaaaaa',
      })
      .setOrigin(0.5);

    this.updateUI();

    this.input.keyboard!.on('keydown-A', () => this.changeSelection(-1));
    this.input.keyboard!.on('keydown-D', () => this.changeSelection(1));
    this.input.keyboard!.on('keydown-W', () => this.changeCategory(-1));
    this.input.keyboard!.on('keydown-S', () => this.changeCategory(1));
    this.input.keyboard!.on('keydown-J', () => this.confirm());
    this.input.keyboard!.on('keydown-K', () => {
      this.scene.start('MainMenuScene');
    });
    this.input.keyboard!.on('keydown-ESC', () => {
      this.scene.start('MainMenuScene');
    });
  }

  private buildCategories(): void {
    this.categories = [];
    this.selection = [];
    this.currentCategory = 0;

    const p1: Category[] = [
      {
        type: 'character',
        player: 1,
        label: 'P1',
        x: this.mode === 'training' ? 160 : 60,
        y: 70,
        color: 0x00ffff,
      },
      {
        type: 'weapon',
        player: 1,
        label: 'P1 Weapon',
        x: this.mode === 'training' ? 160 : 60,
        y: 120,
        color: 0x00ffff,
      },
      {
        type: 'gadget',
        player: 1,
        label: 'P1 Gadget',
        x: this.mode === 'training' ? 160 : 60,
        y: 150,
        color: 0x00ffff,
      },
    ];

    this.categories.push(...p1);
    this.selection.push(0, 0, 0);

    if (this.mode === 'versus') {
      const p2: Category[] = [
        {
          type: 'character',
          player: 2,
          label: 'P2',
          x: 260,
          y: 70,
          color: 0xff00ff,
        },
        {
          type: 'weapon',
          player: 2,
          label: 'P2 Weapon',
          x: 260,
          y: 120,
          color: 0xff00ff,
        },
        {
          type: 'gadget',
          player: 2,
          label: 'P2 Gadget',
          x: 260,
          y: 150,
          color: 0xff00ff,
        },
      ];

      this.categories.push(...p2);
      this.selection.push(0, 0, 0);
    }
  }

  private renderCategory(category: Category): CategoryUI {
    const isCharacter = category.type === 'character';
    const width = isCharacter ? 80 : 90;
    const height = isCharacter ? 80 : 32;

    const border = this.add.rectangle(category.x, category.y, width, height, 0x1a1a2e);
    border.setStrokeStyle(2, category.color);

    if (isCharacter) {
      this.add.rectangle(category.x, category.y - 8, 32, 32, category.color);
    }

    const nameText = this.add
      .text(category.x, category.y + (isCharacter ? 28 : 2), this.getItemName(category), {
        fontSize: isCharacter ? '12px' : '10px',
        color: '#ffffff',
      })
      .setOrigin(0.5);

    const labelText = this.add
      .text(category.x, category.y + (isCharacter ? 42 : 14), category.label, {
        fontSize: '9px',
        color: '#888888',
      })
      .setOrigin(0.5);

    return { border, nameText, labelText };
  }

  private getItemName(category: Category): string {
    const index = this.selection[this.categories.indexOf(category)];
    switch (category.type) {
      case 'character':
        return CHARACTERS[index].name;
      case 'weapon':
        return WEAPONS[index].name;
      case 'gadget':
        return GADGETS[index].name;
      default:
        return '';
    }
  }

  private getList(category: Category): unknown[] {
    switch (category.type) {
      case 'character':
        return CHARACTERS;
      case 'weapon':
        return WEAPONS;
      case 'gadget':
        return GADGETS;
      default:
        return [];
    }
  }

  private changeSelection(delta: number): void {
    const category = this.categories[this.currentCategory];
    const list = this.getList(category);
    const index = this.currentCategory;
    this.selection[index] =
      (this.selection[index] + delta + list.length) % list.length;
    this.updateUI();
  }

  private changeCategory(delta: number): void {
    this.currentCategory =
      (this.currentCategory + delta + this.categories.length) %
      this.categories.length;
    this.updateUI();
  }

  private confirm(): void {
    this.currentCategory++;
    if (this.currentCategory >= this.categories.length) {
      this.startNextScene();
      return;
    }
    this.updateUI();
  }

  private startNextScene(): void {
    if (this.mode === 'training') {
      this.scene.start('GameScene', {
        mode: 'training',
        p1Config: this.buildConfig(1),
      });
    } else {
      this.scene.start('StageSelectScene', {
        p1Config: this.buildConfig(1),
        p2Config: this.buildConfig(2),
      });
    }
  }

  private buildConfig(player: 1 | 2): FighterConfig {
    const charIndex = this.categories.findIndex(
      (c) => c.player === player && c.type === 'character',
    );
    const weaponIndex = this.categories.findIndex(
      (c) => c.player === player && c.type === 'weapon',
    );
    const gadgetIndex = this.categories.findIndex(
      (c) => c.player === player && c.type === 'gadget',
    );

    const base = CHARACTERS[this.selection[charIndex]];
    return {
      ...base,
      weapon: WEAPONS[this.selection[weaponIndex]],
      gadget: GADGETS[this.selection[gadgetIndex]],
    };
  }

  private updateUI(): void {
    this.categories.forEach((category, index) => {
      const ui = this.categoryUIs[index];
      const isActive = index === this.currentCategory;
      const completed = index < this.currentCategory;

      ui.border.setStrokeStyle(isActive ? 3 : 2, isActive ? 0xffffff : category.color);
      ui.nameText.setText(this.getItemName(category));

      if (completed) {
        ui.nameText.setColor('#aaaaaa');
      } else if (isActive) {
        ui.nameText.setColor('#ffffff');
      } else {
        ui.nameText.setColor('#888888');
      }
    });

    const active = this.categories[this.currentCategory];
    const ui = this.categoryUIs[this.currentCategory];
    const bounds = ui.border.getBounds();

    this.leftArrow.setPosition(bounds.left - 10, active.y).setVisible(true);
    this.rightArrow.setPosition(bounds.right + 10, active.y).setVisible(true);

    this.instruction.setText(
      this.currentCategory >= this.categories.length - 1
        ? 'W/S: category  A/D: option  J: start  K: back'
        : 'W/S: category  A/D: option  J: select  K: back',
    );
  }
}
