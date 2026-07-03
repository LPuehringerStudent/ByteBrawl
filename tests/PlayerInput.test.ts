import { describe, expect, it } from 'vitest';
import { PlayerInput } from '../src/input/PlayerInput';
import { InputRouter } from '../src/input/InputRouter';

function createRouter(state: Record<number, { down: boolean; downOnce: boolean }>): InputRouter {
  return {
    isDown: (code: number) => state[code]?.down ?? false,
    isDownOnce: (code: number) => state[code]?.downOnce ?? false,
  } as InputRouter;
}

const KEYS = {
  left: 1,
  right: 2,
  up: 3,
  down: 4,
  light: 5,
  heavy: 6,
  special: 7,
};

describe('PlayerInput', () => {
  it('returns neutral move when no directional keys are pressed', () => {
    const router = createRouter({});
    const input = new PlayerInput(router, KEYS);
    const actions = input.getActions();
    expect(actions.moveX).toBe(0);
  });

  it('returns -1 when only left is pressed', () => {
    const router = createRouter({ [KEYS.left]: { down: true, downOnce: false } });
    const input = new PlayerInput(router, KEYS);
    expect(input.getActions().moveX).toBe(-1);
  });

  it('returns 1 when only right is pressed', () => {
    const router = createRouter({ [KEYS.right]: { down: true, downOnce: false } });
    const input = new PlayerInput(router, KEYS);
    expect(input.getActions().moveX).toBe(1);
  });

  it('returns 0 when both left and right are pressed', () => {
    const router = createRouter({
      [KEYS.left]: { down: true, downOnce: false },
      [KEYS.right]: { down: true, downOnce: false },
    });
    const input = new PlayerInput(router, KEYS);
    expect(input.getActions().moveX).toBe(0);
  });

  it('detects jump press on first frame only', () => {
    const router = createRouter({ [KEYS.up]: { down: true, downOnce: true } });
    const input = new PlayerInput(router, KEYS);
    expect(input.getActions().jumpPressed).toBe(true);
    expect(input.getActions().jumpHeld).toBe(true);
  });

  it('detects light attack once per press', () => {
    const router = createRouter({ [KEYS.light]: { down: true, downOnce: true } });
    const input = new PlayerInput(router, KEYS);
    expect(input.getActions().attackLight).toBe(true);
  });
});
