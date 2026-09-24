import Phaser from 'phaser';
import { PlayerInput } from '../input/PlayerInput';
import { InputRouter } from '../input/InputRouter';
import { Fighter } from '../fighter/Fighter';
import { FighterStateMachine } from '../fighter/FighterStateMachine';
import { HitboxManager } from '../fighter/HitboxManager';
import { AudioManager } from '../shared/AudioManager';
import { ActionFrame, FighterConfig } from '../shared/types';
import { DebugRenderer } from './DebugRenderer';
import { CameraController } from './CameraController';
import { GameRules, GameRulesConfig } from './GameRules';
import { OffscreenIndicator } from './OffscreenIndicator';
import { Stage } from './Stage';
import { UIManager } from './UIManager';
import { FIGHTER_P1_CONFIG, FIGHTER_P2_CONFIG, STAGE_CONFIG } from './config';
import { MenuManager } from '../menu/MenuManager';

const P1_KEYS = {
  left: Phaser.Input.Keyboard.KeyCodes.A,
  right: Phaser.Input.Keyboard.KeyCodes.D,
  up: Phaser.Input.Keyboard.KeyCodes.W,
  down: Phaser.Input.Keyboard.KeyCodes.S,
  jump: Phaser.Input.Keyboard.KeyCodes.SPACE,
  light: Phaser.Input.Keyboard.KeyCodes.J,
  heavy: Phaser.Input.Keyboard.KeyCodes.K,
  special: Phaser.Input.Keyboard.KeyCodes.H,
  gadget: Phaser.Input.Keyboard.KeyCodes.C,
  shield: Phaser.Input.Keyboard.KeyCodes.SHIFT,
  grab: Phaser.Input.Keyboard.KeyCodes.L,
};

const P2_KEYS = {
  left: Phaser.Input.Keyboard.KeyCodes.LEFT,
  right: Phaser.Input.Keyboard.KeyCodes.RIGHT,
  up: Phaser.Input.Keyboard.KeyCodes.UP,
  down: Phaser.Input.Keyboard.KeyCodes.DOWN,
  jump: Phaser.Input.Keyboard.KeyCodes.NUMPAD_FIVE,
  light: Phaser.Input.Keyboard.KeyCodes.NUMPAD_ONE,
  heavy: Phaser.Input.Keyboard.KeyCodes.NUMPAD_TWO,
  special: Phaser.Input.Keyboard.KeyCodes.NUMPAD_ZERO,
  gadget: Phaser.Input.Keyboard.KeyCodes.NUMPAD_FOUR,
  shield: Phaser.Input.Keyboard.KeyCodes.NUMPAD_THREE,
  grab: Phaser.Input.Keyboard.KeyCodes.NUMPAD_SIX,
};

const GAME_RULES_CONFIG: GameRulesConfig = {
  startingStocks: 3,
  respawnInvincibilityFrames: 120,
  maxDamage: 999,
};

export class GameScene extends Phaser.Scene {
  private stage!: Stage;
  private player1!: Fighter;
  private player2!: Fighter;
  private inputRouter!: InputRouter;
  private p1Input!: PlayerInput;
  private p2Input?: PlayerInput;
  private p1StateMachine!: FighterStateMachine;
  private p2StateMachine!: FighterStateMachine;
  private hitboxManager!: HitboxManager;
  private gameRules!: GameRules;
  private uiManager!: UIManager;
  private cameraController!: CameraController;
  private offscreenIndicator!: OffscreenIndicator;
  private debugRenderer!: DebugRenderer;
  private gameLayer!: Phaser.GameObjects.Layer;
  private uiLayer!: Phaser.GameObjects.Layer;
  private menuManager!: MenuManager;
  private isTraining = false;
  private isPaused = false;
  private pauseKey!: Phaser.Input.Keyboard.Key;
  private matchFinished = false;
  private p1Config!: FighterConfig;
  private p2Config!: FighterConfig;

  constructor() {
    super({ key: 'GameScene' });
  }

  init(data: {
    mode?: 'versus' | 'training';
    p1Config?: FighterConfig;
    p2Config?: FighterConfig;
  }): void {
    this.isTraining = data.mode === 'training';
    this.isPaused = false;
    this.matchFinished = false;
    this.p1Config = data.p1Config ?? FIGHTER_P1_CONFIG;
    this.p2Config = data.p2Config ?? FIGHTER_P2_CONFIG;
  }

  create(): void {
    this.menuManager = this.registry.get('menuManager') as MenuManager;
    this.menuManager.hide();

    this.physics.world.setBounds(0, 0, STAGE_CONFIG.width, STAGE_CONFIG.height);
    this.physics.world.gravity.y = 800;

    this.gameLayer = this.add.layer();
    this.uiLayer = this.add.layer();

    this.stage = new Stage(this, STAGE_CONFIG);

    this.player1 = this.createFighter(
      STAGE_CONFIG.player1Spawn.x,
      STAGE_CONFIG.player1Spawn.y,
      this.p1Config,
    );
    this.player2 = this.createFighter(
      STAGE_CONFIG.player2Spawn.x,
      STAGE_CONFIG.player2Spawn.y,
      this.p2Config,
    );

    this.resetFighters();

    this.physics.add.collider(
      this.player1.sprite,
      this.stage.getPlatformLayer(),
      undefined,
      this.handlePlatformCollision,
      this,
    );
    this.physics.add.collider(
      this.player2.sprite,
      this.stage.getPlatformLayer(),
      undefined,
      this.handlePlatformCollision,
      this,
    );

    this.inputRouter = new InputRouter(this.input.keyboard!);
    this.p1Input = new PlayerInput(this.inputRouter, P1_KEYS);
    if (!this.isTraining) {
      this.p2Input = new PlayerInput(this.inputRouter, P2_KEYS);
    }

    this.hitboxManager = new HitboxManager(this, this.gameLayer);

    const audio = new AudioManager();
    this.p1StateMachine = new FighterStateMachine(
      this.player1,
      this.hitboxManager,
      audio,
    );
    this.p2StateMachine = new FighterStateMachine(
      this.player2,
      this.hitboxManager,
      audio,
    );

    this.gameRules = new GameRules(
      GAME_RULES_CONFIG,
      audio,
      this.player1,
      this.player2,
    );

    this.uiManager = new UIManager(
      this.player1,
      this.player2,
      this.gameRules,
    );
    this.uiManager.show();

    const camera = this.cameras.main;
    camera.setBounds(-200, -240, 720, 620);
    camera.centerOn(STAGE_CONFIG.width / 2, STAGE_CONFIG.height / 2);

    const deadzone = new Phaser.Geom.Rectangle(-80, -100, 480, 380);
    this.cameraController = new CameraController(
      camera,
      this.player1,
      this.player2,
      { deadzone },
    );

    this.offscreenIndicator = new OffscreenIndicator(this, this.uiLayer);
    this.debugRenderer = new DebugRenderer(
      this,
      this.player1,
      this.player2,
      this.hitboxManager,
    );

    const uiCamera = this.cameras.add(0, 0, STAGE_CONFIG.width, STAGE_CONFIG.height);
    uiCamera.ignore(this.gameLayer);
    uiCamera.ignore(this.stage.getPlatformLayer());
    uiCamera.ignore(this.debugRenderer.getGraphics());
    camera.ignore(this.uiLayer);

    this.pauseKey = this.input.keyboard!.addKey(
      Phaser.Input.Keyboard.KeyCodes.ESC,
    );
    this.pauseKey.on('down', () => this.togglePause());
  }

  update(_time: number, delta: number): void {
    this.cameraController.update();
    this.offscreenIndicator.update(this.cameraController.getOffscreenPlayers());
    this.uiManager.update();

    if (this.isPaused || this.matchFinished) return;

    if (this.gameRules.matchState !== 'active') {
      this.handleMatchEnd();
      return;
    }

    const p1Actions = this.p1Input.getActions();
    const p2Actions = this.p2Input
      ? this.p2Input.getActions()
      : this.getDummyActions();

    this.p1StateMachine.update(p1Actions);
    this.p2StateMachine.update(p2Actions);

    this.hitboxManager.update();
    this.hitboxManager.checkHits(this.player1, this.player2, (event) => {
      this.gameRules.applyHit(event.attacker, event.defender, event.attack);
    });

    if (this.isTraining) {
      this.handleTrainingRespawn(this.player1, this.stage.getPlayer1Spawn());
      this.handleTrainingRespawn(this.player2, this.stage.getPlayer2Spawn());
    } else {
      this.gameRules.checkRingOut(
        this.player1,
        (x, y) => this.stage.isOutOfBounds(x, y),
        this.stage.getPlayer1Spawn(),
      );
      this.gameRules.checkRingOut(
        this.player2,
        (x, y) => this.stage.isOutOfBounds(x, y),
        this.stage.getPlayer2Spawn(),
      );
    }

    if (!this.isTraining) {
      this.gameRules.update(delta);
    }

    this.debugRenderer.update();
  }

  private getDummyActions(): ActionFrame {
    return {
      moveX: 0,
      moveY: 0,
      jumpPressed: false,
      jumpHeld: false,
      attackLight: false,
      attackLightHeld: false,
      attackHeavy: false,
      attackHeavyHeld: false,
      attackSpecial: false,
      attackSpecialHeld: false,
      gadgetPressed: false,
      gadgetHeld: false,
      shieldPressed: false,
      shieldHeld: false,
      grabPressed: false,
      grabHeld: false,
    };
  }

  destroy(): void {
    this.stage.destroy();
    this.uiManager.destroy();
    this.offscreenIndicator.destroy();
    this.debugRenderer.destroy();
    this.inputRouter.destroy();
  }

  private createFighter(x: number, y: number, config: FighterConfig): Fighter {
    const fighter = new Fighter(
      this,
      x,
      y,
      config.spriteKey,
      config,
      this.gameLayer,
    );
    this.gameLayer.add(fighter.sprite);
    return fighter;
  }

  private resetFighters(): void {
    this.player1.stocks = GAME_RULES_CONFIG.startingStocks;
    this.player2.stocks = GAME_RULES_CONFIG.startingStocks;
    this.player1.respawn(
      STAGE_CONFIG.player1Spawn.x,
      STAGE_CONFIG.player1Spawn.y,
      0,
    );
    this.player2.respawn(
      STAGE_CONFIG.player2Spawn.x,
      STAGE_CONFIG.player2Spawn.y,
      0,
    );
  }

  private handleTrainingRespawn(
    player: Fighter,
    spawn: { x: number; y: number },
  ): void {
    if (this.stage.isOutOfBounds(player.x, player.y)) {
      player.respawn(spawn.x, spawn.y, 0);
    }
  }

  private togglePause(): void {
    if (this.matchFinished) return;

    this.isPaused = !this.isPaused;

    if (this.isPaused) {
      this.menuManager.showPause(this.buildPauseCallbacks());
    } else {
      this.menuManager.hide();
    }
  }

  private buildPauseCallbacks() {
    const hitboxOptions = this.debugRenderer.getOptions();
    const playerHitboxBtn = document.getElementById('pause-player-hitboxes');
    if (playerHitboxBtn) {
      playerHitboxBtn.textContent = hitboxOptions.playerHitboxes
        ? 'Hide Player Hitboxes'
        : 'Show Player Hitboxes';
    }
    const attackHitboxBtn = document.getElementById('pause-attack-hitboxes');
    if (attackHitboxBtn) {
      attackHitboxBtn.textContent = hitboxOptions.attackHitboxes
        ? 'Hide Attack Hitboxes'
        : 'Show Attack Hitboxes';
    }

    const callbacks = {
      onResume: () => {
        this.isPaused = false;
        this.menuManager.hide();
      },
      onControls: () => {
        this.menuManager.showControls({
          onBack: () => this.menuManager.showPause(this.buildPauseCallbacks()),
        });
      },
      onSettings: () => {
        this.menuManager.showSettings({
          onBack: () => this.menuManager.showPause(this.buildPauseCallbacks()),
        });
      },
      onQuit: () => {
        this.isPaused = false;
        this.menuManager.hide();
        this.scene.start('MainMenuScene');
      },
    };

    if (this.isTraining) {
      return {
        ...callbacks,
        onTogglePlayerHitboxes: () => {
          this.debugRenderer.setOptions({
            playerHitboxes: !hitboxOptions.playerHitboxes,
          });
          this.menuManager.showPause(this.buildPauseCallbacks());
        },
        onToggleAttackHitboxes: () => {
          this.debugRenderer.setOptions({
            attackHitboxes: !hitboxOptions.attackHitboxes,
          });
          this.menuManager.showPause(this.buildPauseCallbacks());
        },
      };
    }

    return callbacks;
  }

  private handleMatchEnd(): void {
    if (this.matchFinished) return;
    this.matchFinished = true;

    const winner = this.gameRules.matchState === 'p1Win' ? 'p1' : 'p2';
    this.menuManager.showResult(winner, {
      onRematch: () => {
        this.menuManager.hide();
        this.scene.restart({
          mode: this.isTraining ? 'training' : 'versus',
          p1Config: this.p1Config,
          p2Config: this.p2Config,
        });
      },
      onMenu: () => {
        this.menuManager.hide();
        this.scene.start('MainMenuScene');
      },
    });
  }

  private handlePlatformCollision(
    playerSprite: unknown,
    tile: unknown,
  ): boolean {
    const sprite = playerSprite as Phaser.Physics.Arcade.Sprite;
    const tileObj = tile as Phaser.Tilemaps.Tile;
    const fighter =
      sprite === this.player1.sprite ? this.player1 : this.player2;
    const body = sprite.body as Phaser.Physics.Arcade.Body;

    const isBaseStage = tileObj.getTop() >= 160;
    if (isBaseStage) return true;

    if (fighter.droppingThrough) return false;
    if (body.velocity.y < 0) return false;

    return body.bottom <= tileObj.getTop() + 4;
  }
}
