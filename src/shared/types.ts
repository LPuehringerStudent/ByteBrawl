export interface ActionFrame {
  moveX: number; // -1 (left) to 1 (right), 0 if neutral
  moveY: number; // -1 (up) to 1 (down), 0 if neutral
  jumpPressed: boolean;
  jumpHeld: boolean;
  attackLight: boolean;
  attackHeavy: boolean;
  attackSpecial: boolean;
}

export interface AttackData {
  id: string;
  baseDamage: number;
  baseKnockback: number;
  scaling: number;
  direction: { x: number; y: number };
  hitstunFrames: number;
  activeFrames: number;
}

export interface FighterConfig {
  name: string;
  maxHealth: number;
  runSpeed: number;
  jumpSpeed: number;
  weight: number;
  spriteKey: string;
  attacks: Record<string, AttackData>;
}

export type FighterState =
  | 'idle'
  | 'run'
  | 'jump'
  | 'fall'
  | 'lightAttack'
  | 'heavyAttack'
  | 'special'
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
