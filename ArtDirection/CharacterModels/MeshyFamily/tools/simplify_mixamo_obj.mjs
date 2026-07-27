import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

function argument(name) {
  const index = process.argv.indexOf(name);
  if (index < 0 || index + 1 >= process.argv.length) {
    throw new Error(`Missing ${name}`);
  }
  return process.argv[index + 1];
}

const inputPath = path.resolve(argument("--input"));
const outputPath = path.resolve(argument("--output"));
const targetFaces = Number(argument("--target-faces"));
const poseArms = process.argv.includes("--t-pose-arms");
const widthScaleIndex = process.argv.indexOf("--rig-width-scale");
const rigWidthScale =
  widthScaleIndex >= 0 ? Number(process.argv[widthScaleIndex + 1]) : 1;
const meshoptimizerFolder = path.resolve(argument("--meshoptimizer"));
const moduleUrl = pathToFileURL(
  path.join(meshoptimizerFolder, "meshopt_simplifier.js"),
).href;
const { MeshoptSimplifier } = await import(moduleUrl);
await MeshoptSimplifier.ready;

const source = await fs.readFile(inputPath, "utf8");
const sourcePositions = [null];
const sourceUvs = [null];
const sourceNormals = [null];
const outputPositions = [];
const outputUvs = [];
const outputNormals = [];
const indices = [];
const vertexMap = new Map();
let materialLibrary = "";
let objectName = "Character";
let materialName = "Character_Material";

function resolveIndex(index, length) {
  const numeric = Number(index);
  return numeric < 0 ? length + numeric : numeric;
}

function vertexFor(token) {
  let existing = vertexMap.get(token);
  if (existing !== undefined) return existing;

  const [positionText, uvText, normalText] = token.split("/");
  const position = sourcePositions[
    resolveIndex(positionText, sourcePositions.length)
  ];
  const uv = sourceUvs[resolveIndex(uvText, sourceUvs.length)] ?? [0, 0];
  const normal = sourceNormals[
    resolveIndex(normalText, sourceNormals.length)
  ] ?? [0, 1, 0];

  existing = outputPositions.length;
  vertexMap.set(token, existing);
  outputPositions.push(position);
  outputUvs.push(uv);
  outputNormals.push(normal);
  return existing;
}

for (const rawLine of source.split(/\r?\n/)) {
  const line = rawLine.trim();
  if (!line || line.startsWith("#")) continue;
  const parts = line.split(/\s+/);
  switch (parts[0]) {
    case "mtllib":
      materialLibrary = parts.slice(1).join(" ");
      break;
    case "o":
      objectName = parts.slice(1).join("_");
      break;
    case "usemtl":
      materialName = parts.slice(1).join("_");
      break;
    case "v":
      sourcePositions.push(parts.slice(1, 4).map(Number));
      break;
    case "vt":
      sourceUvs.push(parts.slice(1, 3).map(Number));
      break;
    case "vn":
      sourceNormals.push(parts.slice(1, 4).map(Number));
      break;
    case "f": {
      const corners = parts.slice(1).map(vertexFor);
      for (let corner = 1; corner + 1 < corners.length; corner += 1) {
        indices.push(corners[0], corners[corner], corners[corner + 1]);
      }
      break;
    }
  }
}

if (poseArms) {
  const pivotX = 31;
  const pivotY = 10;
  const directionX = 0.37;
  const directionY = -0.929;
  const normalX = 0.929;
  const normalY = 0.37;
  const angle = (68 * Math.PI) / 180;
  const cosine = Math.cos(angle);
  const sine = Math.sin(angle);

  for (let index = 0; index < outputPositions.length; index += 1) {
    const position = outputPositions[index];
    const side = Math.sign(position[0]);
    if (side === 0) continue;

    const x = Math.abs(position[0]);
    const dx = x - pivotX;
    const dy = position[1] - pivotY;
    const along = dx * directionX + dy * directionY;
    const across = dx * normalX + dy * normalY;
    if (
      along < -4 ||
      along > 47 ||
      Math.abs(across) > 14.5 ||
      x < 27
    ) {
      continue;
    }

    const weight = Math.max(0, Math.min(1, (along + 4) / 7));
    const weightedAngle = angle * weight;
    const weightedCosine = weight >= 0.999 ? cosine : Math.cos(weightedAngle);
    const weightedSine = weight >= 0.999 ? sine : Math.sin(weightedAngle);
    const rotatedX = weightedCosine * dx - weightedSine * dy + pivotX;
    const rotatedY = weightedSine * dx + weightedCosine * dy + pivotY;
    position[0] = rotatedX * side;
    position[1] = rotatedY;
  }
}

if (rigWidthScale !== 1) {
  for (const position of outputPositions) {
    position[0] *= rigWidthScale;
  }
}

const positions = new Float32Array(outputPositions.flat());
const attributes = new Float32Array(
  outputPositions.flatMap((_, index) => [
    ...outputNormals[index],
    ...outputUvs[index],
  ]),
);
const inputIndices = new Uint32Array(indices);
const [simplifiedIndices, error] = MeshoptSimplifier.simplifyWithAttributes(
  inputIndices,
  positions,
  3,
  attributes,
  5,
  [0.25, 0.25, 0.25, 8.0, 8.0],
  null,
  targetFaces * 3,
  0.08,
  ["Permissive", "RegularizeLight"],
);

const remap = new Map();
const used = [];
const compactIndices = new Uint32Array(simplifiedIndices.length);
for (let index = 0; index < simplifiedIndices.length; index += 1) {
  const oldIndex = simplifiedIndices[index];
  let newIndex = remap.get(oldIndex);
  if (newIndex === undefined) {
    newIndex = used.length;
    remap.set(oldIndex, newIndex);
    used.push(oldIndex);
  }
  compactIndices[index] = newIndex;
}

const lines = [
  "# Texture-preserving Meshoptimizer simplification for Mixamo.",
  `# faces=${simplifiedIndices.length / 3} error=${error}`,
  `mtllib ${materialLibrary}`,
  `o ${objectName}`,
  `usemtl ${materialName}`,
];

for (const index of used) {
  const value = outputPositions[index];
  lines.push(`v ${value[0]} ${value[1]} ${value[2]}`);
}
for (const index of used) {
  const value = outputUvs[index];
  lines.push(`vt ${value[0]} ${value[1]}`);
}
for (const index of used) {
  const value = outputNormals[index];
  lines.push(`vn ${value[0]} ${value[1]} ${value[2]}`);
}
for (let index = 0; index < compactIndices.length; index += 3) {
  const a = compactIndices[index] + 1;
  const b = compactIndices[index + 1] + 1;
  const c = compactIndices[index + 2] + 1;
  lines.push(`f ${a}/${a}/${a} ${b}/${b}/${b} ${c}/${c}/${c}`);
}

await fs.writeFile(outputPath, `${lines.join("\n")}\n`, "utf8");
process.stdout.write(
  JSON.stringify({
    inputFaces: inputIndices.length / 3,
    outputFaces: simplifiedIndices.length / 3,
    outputVertices: used.length,
    error,
  }),
);
