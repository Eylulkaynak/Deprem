import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

import { meshyToGlb } from "../meshy2glb/src/decrypt.js";
import { decompressGlb } from "../meshy2glb/src/decompress.js";
import { MeshoptDecoder } from "../meshy2glb/tests/meshopt_decoder.module.js";

const inputPath = resolve(process.argv[2] ?? ".codex_tmp/abdullah_meshy_pro/model.meshy");
const outputPath = resolve(process.argv[3] ?? ".codex_tmp/abdullah_meshy_pro/AbdullahEkinci_MeshyPro_15K.glb");

const input = await readFile(inputPath);
const sourceBuffer = input.buffer.slice(input.byteOffset, input.byteOffset + input.byteLength);
const encryptedGlb = await meshyToGlb(sourceBuffer);
const standardGlb = await decompressGlb(encryptedGlb, MeshoptDecoder);

await writeFile(outputPath, new Uint8Array(standardGlb));
console.log(JSON.stringify({ inputPath, outputPath, bytes: standardGlb.byteLength }));
