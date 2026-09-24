export interface ActionFrame {
  moveX: number; // -1 (left) to 1 (right), 0 if neutral
  moveY: number; // -1 (up) to 1 (down), 0 if neutral
  jumpPressed: boolean;
  jumpHeld: boolean;
  attackLight: boolean;
  attackLightHeld: boolean;
  attackHeavy: boolean;
  attackHeavyHeld: boolean;
  attackSpecial: boolean;
  attackSpecialHeld: boolean;
  gadgetPressed: boolean;
  gadgetHeld: boolean;
  shieldPressed: boolean;
  shieldHeld: boolean;
  grabPressed: boolean;
  grabHeld: boolean;
}

export type HitboxShape = 'box' | 'circle';

export interface ChargeConfig {
  minChargeFrames: number;
  maxChargeFrames: number;
  maxHoldFrames: number;
  damageGrowth: number;
  knockbackGrowth: number;
  hitboxGrowth?: {
    radius?: number;
    width?: number;
    height?: number;
  };
}

export interface AttackData {
  id: string;
  baseDamage: number;
  baseKnockback: number;
  scaling: number;
  direction: { x: number; y: number };
  hitstunFrames: number;
  activeFrames: number;
  shape?: HitboxShape;
  radius?: number;
  charge?: ChargeConfig;
  stages?: AttackStage[];
}

export interface AttackStage extends AttackData {
  spawnFrame: number;
  offsetX: number;
  offsetY: number;
  width: number;
  height: number;
}

export interface WeaponConfig {
  id: string;
  name: string;
  light: AttackData;
  heavy: AttackData;
}

export interface GadgetConfig {
  id: string;
  name: string;
}

export interface FighterConfig {
  name: string;
  maxHealth: number;
  runSpeed: number;
  jumpSpeed: number;
  weight: number;
  spriteKey: string;
  special: AttackData;
  weapon: WeaponConfig;
  gadget: GadgetConfig;
}

export type FighterState =
  | 'idle'
  | 'run'
  | 'jump'
  | 'fall'
  | 'lightAttack'
  | 'heavyAttack'
  | 'special'
  | 'charging'
  | 'shield'
  | 'spotDodge'
  | 'airDodge'
  | 'hitstun'
  | 'recovery';

export interface PlayerConfig {
  id: number;
  startX: number;
  startY: number;
  facing: number;
  keys: {
    left: number;
    right: number;
    up: number;
    down: number;
    light: number;
    heavy: number;
    special: number;
  };
}

export interface StageConfig {
  width: number;
  height: number;
  tileSize: number;
  layout: number[][];
  blastZone: { x: number; y: number; width: number; height: number };
  player1Spawn: { x: number; y: number };
  player2Spawn: { x: number; y: number };
}
