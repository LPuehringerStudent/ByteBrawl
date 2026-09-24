import { WeaponConfig } from '../shared/types';

export const SWORD_CONFIG: WeaponConfig = {
  id: 'sword',
  name: 'Sword',
  light: {
    id: 'sword-light',
    baseDamage: 5,
    baseKnockback: 150,
    scaling: 1.2,
    direction: { x: 1, y: -0.4 },
    hitstunFrames: 12,
    activeFrames: 4,
  },
  heavy: {
    id: 'sword-heavy',
    baseDamage: 11,
    baseKnockback: 230,
    scaling: 1.5,
    direction: { x: 1, y: -0.3 },
    hitstunFrames: 20,
    activeFrames: 6,
    charge: {
      minChargeFrames: 30,
      maxChargeFrames: 180,
      maxHoldFrames: 180,
      damageGrowth: 0.2,
      knockbackGrowth: 1.0,
    },
  },
};
