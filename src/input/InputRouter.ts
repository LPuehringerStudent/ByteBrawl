import Phaser from 'phaser';

export class InputRouter {
  private keys: Map<number, Phaser.Input.Keyboard.Key> = new Map();

  constructor(private keyboard: Phaser.Input.Keyboard.KeyboardPlugin) {}

  getKey(keyCode: number): Phaser.Input.Keyboard.Key {
    if (!this.keys.has(keyCode)) {
      this.keys.set(keyCode, this.keyboard.addKey(keyCode));
    }
    return this.keys.get(keyCode)!;
  }

  isDown(keyCode: number): boolean {
    return this.getKey(keyCode).isDown;
  }

  isDownOnce(keyCode: number): boolean {
    const key = this.getKey(keyCode);
    return Phaser.Input.Keyboard.JustDown(key);
  }

  destroy(): void {
    this.keys.clear();
  }
}
