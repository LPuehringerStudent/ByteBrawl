// Extract collision coordinates from Soldier pose metadata. No image files are read.
const fs = require('node:fs');

const [hurtboxPath, posePath, outputPath] = process.argv.slice(2);
if (!hurtboxPath || !posePath || !outputPath) {
  throw new Error('Usage: node tools/extract-soldier-collision.cjs <hurtboxes.json> <poses.json> <output.json>');
}
const hurtboxes = JSON.parse(fs.readFileSync(hurtboxPath, 'utf8'));
const poses = JSON.parse(fs.readFileSync(posePath, 'utf8'));
if (hurtboxes.frames.length !== poses.frames.length || hurtboxes.frames.length !== 57) {
  throw new Error('Expected matching 57-frame Soldier pose and hurtbox sources');
}
const active = [
  [25, 'jab', 'far_hand', 2],
  [32, 'cross', 'near_hand', 3],
  [33, 'cross', 'near_hand', 3],
  [47, 'kick', 'far_foot', 4],
  [48, 'kick', 'far_foot', 4],
  [49, 'kick', 'far_foot', 4],
];
const attacks = active.map(([frame, attack, part, damage]) => {
  const point = poses.frames[frame].parts[part].Start;
  return { frame, attack, x: point.X - 64, y: point.Y - 121, radius: 9, damage };
});
const profile = {
  schema: 1,
  origin: { x: 64, y: 121 },
  frames: hurtboxes.frames,
  attacks,
};
for (const frame of profile.frames) {
  if (frame.parts.length !== 15 || frame.parts.some(part => part.id === 'backpack')) {
    throw new Error(`Expected 15 damage-receiving parts and no backpack in ${frame.id}`);
  }
}
fs.writeFileSync(outputPath, JSON.stringify(profile, null, 2) + '\n');
console.log(`Wrote ${profile.frames.length} collision frames and ${attacks.length} active attack circles`);
