import Phaser from 'phaser';
import { PlayerInput } from '../input/PlayerInput';
import { InputRouter } from '../input/InputRouter';
import { Fighter } from '../fighter/Fighter';
import { FighterStateMachine } from '../fighter/FighterStateMachine';
import { HitboxManager } from '../fighter/HitboxManager';
import { AssetLoader } from '../shared/AssetLoader';
import { AudioManager } from '../shared/AudioManager';
import { CameraController } from './CameraController';
import { GameRules, GameRulesConfig } from './GameRules';
import { OffscreenIndicator } from './OffscreenIndicator';
import { Stage } from './Stage';
import { UIManager } from './UIManager';
import { FIGHTER_P1_CONFIG, FIGHTER_P2_CONFIG, STAGE_CONFIG } from './config';

const P1_KEYS = {
  left: Phaser.Input.Keyboard.KeyCodes.A,
  right: Phaser.Input.Keyboard.KeyCodes.D,
  up: Phaser.Input.Keyboard.KeyCodes.W,
  down: Phaser.Input.Keyboard.KeyCodes.S,
  light: Phaser.Input.Keyboard.KeyCodes.Z,
  heavy: Phaser.Input.Keyboard.KeyCodes.X,
  special: Phaser.Input.Keyboard.KeyCodes.SPACE,
};

const P2_KEYS = {
  left: Phaser.Input.Keyboard.KeyCodes.LEFT,
  right: Phaser.Input.Keyboard.KeyCodes.RIGHT,
  up: Phaser.Input.Keyboard.KeyCodes.UP,
  down: Phaser.Input.Keyboard.KeyCodes.DOWN,
  light: Phaser.Input.Keyboard.KeyCodes.NUMPAD_ONE,
  heavy: Phaser.Input.Keyboard.KeyCodes.NUMPAD_TWO,
  special: Phaser.Input.Keyboard.KeyCodes.NUMPAD_ZERO,
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
  private p2Input!: PlayerInput;
  private p1StateMachine!: FighterStateMachine;
  private p2StateMachine!: FighterStateMachine;
  private hitboxManager!: HitboxManager;
  private gameRules!: GameRules;
  private uiManager!: UIManager;
  private cameraController!: CameraController;
  private offscreenIndicator!: OffscreenIndicator;

  constructor() {
    super({ key: 'GameScene' });
  }

  create(): void {
    this.physics.world.setBounds(0, 0, STAGE_CONFIG.width, STAGE_CONFIG.height);
    this.physics.world.gravity.y = 800;

    const assetLoader = new AssetLoader(this);
    assetLoader.createPlaceholderTextures();

    this.stage = new Stage(this, STAGE_CONFIG);

    this.player1 = new Fighter(
      this,
      STAGE_CONFIG.player1Spawn.x,
      STAGE_CONFIG.player1Spawn.y,
      FIGHTER_P1_CONFIG.spriteKey,
      FIGHTER_P1_CONFIG,
    );
    this.player2 = new Fighter(
      this,
      STAGE_CONFIG.player2Spawn.x,
      STAGE_CONFIG.player2Spawn.y,
      FIGHTER_P2_CONFIG.spriteKey,
      FIGHTER_P2_CONFIG,
    );

    this.player1.stocks = GAME_RULES_CONFIG.startingStocks;
    this.player2.stocks = GAME_RULES_CONFIG.startingStocks;

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
    this.p2Input = new PlayerInput(this.inputRouter, P2_KEYS);

    this.hitboxManager = new HitboxManager(this);

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
      this,
      this.player1,
      this.player2,
      this.gameRules,
    );

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

    this.offscreenIndicator = new OffscreenIndicator(this);
  }

  update(_time: number, delta: number): void {
    this.cameraController.update();
    this.offscreenIndicator.update(this.cameraController.getOffscreenPlayers());

    if (this.gameRules.matchState !== 'active') {
      this.uiManager.update();
      return;
    }

    const p1Actions = this.p1Input.getActions();
    const p2Actions = this.p2Input.getActions();

    this.p1StateMachine.update(p1Actions);
    this.p2StateMachine.update(p2Actions);

    this.hitboxManager.update();
    this.hitboxManager.checkHits(this.player1, this.player2, (event) => {
      this.gameRules.applyHit(event.attacker, event.defender, event.attack);
    });

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

    this.gameRules.update(delta);
    this.uiManager.update();
  }

  destroy(): void {
    this.stage.destroy();
    this.uiManager.destroy();
    this.offscreenIndicator.destroy();
    this.inputRouter.destroy();
  }

  private handlePlatformCollision(
    playerSprite: unknown,
    tile: unknown,
  ): boolean {
    // TODO: The small platforms are fundamentally different to the base stage.
    // You cannot phase through the base stage.
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
