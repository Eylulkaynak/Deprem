"""New local Turkish lines for the physical adventure; synthetic voices, no runtime network."""
import asyncio,hashlib,json,pathlib,re,subprocess,wave,random,math,struct
import edge_tts,imageio_ffmpeg
root=pathlib.Path(__file__).resolve().parents[2];out=root/'Assets/YanYana/Audio/Physical';source=root/'ArtDirection/YanYana/Audio/Physical'
out.mkdir(parents=True,exist_ok=True);source.mkdir(parents=True,exist_ok=True)
lines=set()
for path in (root/'Assets/YanYana/Editor').glob('*.cs'):
 if path.stem in ('YanYanaFlowAuthor','YanYanaWorldAuthor','YanYanaInteractionAuthor','YanYanaConsequencesAuthor'):continue
 for line in re.findall(r'"([^"\r\n]+)"',path.read_text(encoding='utf-8-sig')):
  if re.match(r'^(Ada|Efe|Derya|Emre|Yusuf|İdil|Bora|Komşu|Radyo): ',line) and len(line)<220:lines.add(line)
manifest=[];limit=asyncio.Semaphore(3)
async def one(line):
 key='physical_'+hashlib.sha256(line.encode()).hexdigest()[:12];speaker,spoken=line.split(':',1);voice='tr-TR-AhmetNeural' if speaker in ('Emre','Yusuf','Bora') else 'tr-TR-EmelNeural'
 target=out/(key+'.wav');mp3=source/(key+'.mp3')
 async with limit:
  if not target.exists():
   for attempt in range(3):
    try:
     await edge_tts.Communicate(spoken.strip(),voice,rate='-5%',pitch='+10Hz' if speaker=='Efe' else '+4Hz' if speaker=='Ada' else '+0Hz').save(str(mp3))
     subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-loglevel','error','-i',str(mp3),'-ar','24000','-ac','1',str(target)],check=True);break
    except Exception as e:
     if attempt==2:print('VOICE FAILURE',key,str(e)[:100],flush=True)
     else:await asyncio.sleep(2)
  manifest.append(dict(key=key,line=line,voice=voice,synthetic=True,ready=target.exists()))
async def run():
 await asyncio.gather(*(one(line) for line in sorted(lines)))
 (source/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
 print('PHYSICAL VOICES',sum(x['ready'] for x in manifest),'/',len(manifest),flush=True)
asyncio.run(run())
random.seed(909)
with wave.open(str(out/'radio_static.wav'),'wb') as wav:
 wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(24000)
 wav.writeframes(b''.join(struct.pack('<h',round(3000*random.uniform(-1,1)*(0.65+.35*math.sin(i/24000*82)))) for i in range(48000)))
