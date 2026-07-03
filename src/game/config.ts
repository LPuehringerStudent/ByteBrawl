import Phaser from 'phaser';
import { FighterConfig, StageConfig } from '../shared/types';

export const STAGE_CONFIG: StageConfig = {
  width: 320,
  height: 180,
  tileSize: 16,
  layout: [
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, 0, 0, 0, -1, -1, -1, -1, -1, -1, 0, 0, 0, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
    [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
  ],
  blastZone: new Phaser.Geom.Rectangle(-80, -80, 480, 340),
  player1Spawn: { x: 80, y: 120 },
  player2Spawn: { x: 240, y: 120 },
};

export const FIGHTER_P1_CONFIG: FighterConfig = {
  name: 'Byte',
  maxHealth: 100,
  runSpeed: 120,
  jumpSpeed: 380,
  weight: 1,
  spriteKey: 'fighter-p1',
  attacks: {
    light: {
      id: 'light',
      baseDamage: 4,
      baseKnockback: 140,
      scaling: 1.2,
      direction: { x: 1, y: -0.4 },
      hitstunFrames: 12,
      activeFrames: 4,
    },
    heavy: {
      id: 'heavy',
      baseDamage: 10,
      baseKnockback: 220,
      scaling: 1.5,
      direction: { x: 1, y: -0.3 },
      hitstunFrames: 20,
      activeFrames: 6,
    },
    special: {
      id: 'special',
      baseDamage: 7,
      baseKnockback: 180,
      scaling: 1.3,
      direction: { x: 0.8, y: -0.8 },
      hitstunFrames: 16,
      activeFrames: 5,
    },
  },
};

export const FIGHTER_P2_CONFIG: FighterConfig = {
  name: 'Nibble',
  maxHealth: 100,
  runSpeed: 140,
  jumpSpeed: 360,
  weight: 0.9,
  spriteKey: 'fighter-p2',
  attacks: {
    light: {
      id: 'light',
      baseDamage: 3,
      baseKnockback: 130,
      scaling: 1.1,
      direction: { x: 1, y: -0.5 },
      hitstunFrames: 10,
      activeFrames: 4,
    },
    heavy: {
      id: 'heavy',
      baseDamage: 9,
      baseKnockback: 210,
      scaling: 1.4,
      direction: { x: 1, y: -0.4 },
      hitstunFrames: 18,
      activeFrames: 6,
    },
    special: {
      id: 'special',
      baseDamage: 6,
      baseKnockback: 170,
      scaling: 1.2,
      direction: { x: 0.7, y: -0.9 },
      hitstunFrames: 14,
      activeFrames: 5,
    },
  },
};
