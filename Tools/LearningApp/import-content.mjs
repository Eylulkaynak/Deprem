// Export existing authored content and art into native Unity assets. No web runtime.
import { readFile, writeFile, mkdir, copyFile } from 'node:fs/promises';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';
const source = process.argv[2] || 'C:/Users/ataor/Desktop/Deprem/portrait-puzzle-demo';
const project = fileURLToPath(new URL('../../', import.meta.url));
const output = path.join(project, 'Assets/LearningApp/Resources/LearningApp');
await mkdir(path.join(output, 'Art'), { recursive: true });
let seed = 20260930;
const deterministicMath = Object.create(Math);
deterministicMath.random = () => ((seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0) / 4294967296);
const context = vm.createContext({ Phaser: { Scene: class {} }, Math: deterministicMath });
async function evaluate(file, names, prefixOnly = false) {
  let code = await readFile(path.join(source, 'src', file), 'utf8');
  code = code.replace(/^import .*?;\s*$/gm, '').replace(/\bexport /g, '').replace('import.meta.env.BASE_URL', "''");
  if (prefixOnly) code = code.substring(0, code.indexOf('class '));
  return vm.runInContext(`(() => { ${code}\nreturn {${names.join(',')}}; })()`, context, { timeout: 3000 });
}
const assets = await evaluate('data/assets.js', ['TEXTURE_KEYS', 'ASSET_MANIFEST', 'QUIZ_CHOICE_ICON_KEYS']);
Object.assign(context, assets);
Object.assign(context, await evaluate('data/gameModes.js', ['GAME_MODE_KEYS', 'normalizeGameMode']));
const child = (await evaluate('scenes/EducationScene.js', ['UNITS'], true)).UNITS;
const adult = (await evaluate('scenes/AdultEducationScene.js', ['COURSE_ITEMS'], true)).COURSE_ITEMS;
const guides = await evaluate('scenes/GuideScene.js', ['GUIDE_CARDS', 'ADULT_GUIDE_CARDS'], true);
const plans = await evaluate('scenes/PersonalPlanScene.js', ['PLAN_FIELDS', 'CHECKLIST_ITEMS'], true);
const levels = await evaluate('data/levels.js', ['LEVELS', 'ITEM_LIBRARY', 'getLevel']);
context.LEVELS = levels.LEVELS;
const badges = (await evaluate('systems/achievements.js', ['ACHIEVEMENT_DEFS'])).ACHIEVEMENT_DEFS.map(({ id, title, description }) => ({ id, title, description }));
const normalizeExercise = e => ({
  kind: e.type === 'info' ? 'info' : 'question', title: e.title || '', prompt: e.prompt || '',
  text: e.text || e.learn || '', tip: e.tip || '', success: e.success || '', icon: e.iconKey || '',
  choices: (e.choices || []).map(c => ({ label: c.label, correct: c.correct, icon: c.iconKey || assets.QUIZ_CHOICE_ICON_KEYS[c.id] || '', feedback: c.feedback || '' })),
});
const courses = child.map((u, i) => ({ id: 'child-' + i, title: u.title, subtitle: u.subtitle, icon: u.iconKey, adult: false, exercises: u.exercises.map(normalizeExercise) }));
adult.forEach((item, i) => courses.push({
  id: 'adult-' + i, title: item.title, subtitle: item.section, icon: item.iconKey || 'item-emergency-bag', adult: true,
  exercises: [item.type === 'lesson'
    ? { kind: 'info', title: item.title, text: [item.summary, ...(item.bullets || []).map(b => '• ' + b), item.action].filter(Boolean).join('\n\n'), choices: [] }
    : { kind: 'question', prompt: item.question, text: '', choices: item.options.map(c => ({ label: c.label, correct: c.correct, feedback: c.feedback })) }],
}));
const art = [];
for (const asset of assets.ASSET_MANIFEST) {
  const extension = path.extname(asset.url);
  if (extension === '.svg') continue;
  await copyFile(path.join(source, 'public', asset.url), path.join(output, 'Art', asset.key + extension));
  art.push({ key: asset.key, resource: 'LearningApp/Art/' + asset.key });
}
const nativeLevels = [];
for (const adultMode of [false, true]) {
  for (let i = 0; i < levels.LEVELS.length; i++) {
    const level = levels.getLevel(i, adultMode ? 'adult' : 'child');
    const definition = { ...levels.LEVELS[i], ...(adultMode ? levels.LEVELS[i].adult : {}) };
    nativeLevels.push({ ...level, adult: adultMode, rounds: (level.rounds || []).map(steps => ({ steps })), questions: (definition.questions || []).map(normalizeExercise) });
  }
}
const data = {
  version: 1, courses, art, badges,
  guides: [...guides.GUIDE_CARDS.map(g => ({ ...g, adult: false })), ...guides.ADULT_GUIDE_CARDS.map(g => ({ ...g, adult: true }))],
  planFields: plans.PLAN_FIELDS, checklist: plans.CHECKLIST_ITEMS,
  items: Object.entries(levels.ITEM_LIBRARY).map(([id, item]) => ({ id, ...item })), levels: nativeLevels,
};
// Keep the reviewed child narration text when reimporting the original web content.
const narrationCopy = JSON.parse(await readFile(new URL('./narration-copy.json', import.meta.url), 'utf8'));
for (const [id, text] of Object.entries(narrationCopy.courseInfo)) {
  data.courses.find(course => course.id === id).exercises[0].text = text;
}
for (const patch of narrationCopy.exercisePatches) {
  const exercise = data.courses.find(course => course.id === patch.course).exercises[patch.index];
  for (const [key, value] of Object.entries(patch)) {
    if (key === 'choiceLabels') value.forEach((label, index) => { exercise.choices[index].label = label; });
    else if (key !== 'course' && key !== 'index') exercise[key] = value;
  }
}
for (const level of data.levels) {
  if (!level.adult) level.instruction = narrationCopy.gameInstructions[level.id];
}
await writeFile(path.join(output, 'catalog.json'), JSON.stringify(data, null, 2));
await mkdir(path.join(output, 'Audio'), { recursive: true });
await copyFile(path.join(source, 'public/assets/sounds/gamemusic.wav'), path.join(output, 'Audio/music.wav'));
console.log(JSON.stringify({ courses: courses.length, child: child.length, adult: adult.length, levels: nativeLevels.length, art: art.length, guides: data.guides.length }));
