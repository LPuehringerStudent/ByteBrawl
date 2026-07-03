import { ActionFrame } from '../shared/types';
import { InputRouter } from './InputRouter';

export interface PlayerInputConfig {
  left: number;
  right: number;
  up: number;
  down: number;
  light: number;
  heavy: number;
  special: number;
}

export class PlayerInput {
  constructor(private router: InputRouter, private config: PlayerInputConfig) {}

  getActions(): ActionFrame {
    const left = this.router.isDown(this.config.left);
    const right = this.router.isDown(this.config.right);

    let moveX = 0;
    if (left && !right) moveX = -1;
    else if (right && !left) moveX = 1;

    return {
      moveX,
      jumpPressed: this.router.isDownOnce(this.config.up),
      jumpHeld: this.router.isDown(this.config.up),
      attackLight: this.router.isDownOnce(this.config.light),
      attackHeavy: this.router.isDownOnce(this.config.heavy),
      attackSpecial: this.router.isDownOnce(this.config.special),
    };
  }
}
