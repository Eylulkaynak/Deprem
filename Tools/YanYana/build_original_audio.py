"""Author local interaction sounds and Turkish synthetic character voices.

Effects are synthesized from original oscillators/noise. Dialogue is synthetic TTS,
not a recording of an actor. The game ships local audio and makes no online calls.
"""
import asyncio, json, math, pathlib, random, struct, subprocess, wave
import edge_tts
import imageio_ffmpeg

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT/'Assets/YanYana/Audio'
SOURCE = ROOT/'ArtDirection/YanYana/Audio'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
RATE=24000
random.seed(29317)

def sound(name, duration, fn):
    with wave.open(str(OUT/(name+'.wav')), 'wb') as wav:
        wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(RATE)
        wav.writeframes(b''.join(struct.pack('<h', round(max(-.9,min(.9,fn(i/RATE)))*32767)) for i in range(round(duration*RATE))))

sound('complete', .56, lambda t: .18*math.exp(-7*t)*(math.sin(2*math.pi*(660 if t<.18 else 880)*t)+.4*math.sin(2*math.pi*1320*t)))
sound('zipper', .65, lambda t: random.uniform(-1,1)*.13*(.4+.6*math.sin(2*math.pi*70*t)**2)*math.sin(math.pi*t/.65))
sound('footstep', .24, lambda t: random.uniform(-1,1)*.15*math.exp(-20*t)+.07*math.sin(2*math.pi*110*t)*math.exp(-25*t))
sound('click', .2, lambda t: .23*math.sin(2*math.pi*550*t)*math.exp(-40*t))
sound('water', 4, lambda t: random.uniform(-1,1)*.17*(.72+.28*math.sin(2*math.pi*7*t)))
sound('rumble', 5, lambda t: (.06*math.sin(2*math.pi*37*t)+.045*math.sin(2*math.pi*53*t)+random.uniform(-.015,.015))*(.5+.5*math.sin(math.pi*t/5)))
def atmosphere(t):
    base=.018*math.sin(2*math.pi*130.81*t)+.012*math.sin(2*math.pi*196*t)
    phrase=t%4.4
    bird=.055*math.sin(2*math.pi*(1300+330*math.sin(phrase*9))*t)*math.exp(-10*phrase) if phrase<.8 else 0
    return base+bird+random.uniform(-.006,.006)
sound('neighborhood', 22, atmosphere)

async def voices():
    data=json.loads((ROOT/'Assets/YanYana/Content/Campaign.json').read_text(encoding='utf-8'))
    semaphore=asyncio.Semaphore(3)
    manifest=[]
    async def one(beat):
        line=beat['line']
        if not line:return
        speaker=line.split(':',1)[0] if ':' in line else beat['role']
        text=line.split(':',1)[-1].strip()
        male=speaker in ('Emre','Yusuf','Bora')
        voice='tr-TR-AhmetNeural' if male else 'tr-TR-EmelNeural'
        pitch='+12Hz' if speaker=='Efe' else '+5Hz' if speaker=='Ada' else '-4Hz' if speaker=='Yusuf' else '+0Hz'
        mp3=SOURCE/(beat['id']+'.mp3'); target=OUT/(beat['id']+'.wav')
        async with semaphore:
            if not target.exists():
                for attempt in range(3):
                    try:
                        await edge_tts.Communicate(text,voice,rate='-8%',pitch=pitch).save(str(mp3))
                        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-loglevel','error','-i',str(mp3),'-ar',str(RATE),'-ac','1',str(target)],check=True)
                        break
                    except Exception as exc:
                        if attempt==2: print('VOICE FAILED',beat['id'],type(exc).__name__,str(exc)[:150],flush=True)
                        await asyncio.sleep(2)
            manifest.append(dict(id=beat['id'],speaker=speaker,text=text,voice=voice,synthetic=True,ready=target.exists()))
            print('VOICE',beat['id'],target.exists(),flush=True)
    await asyncio.gather(*(one(b) for b in data['beats']))
    (SOURCE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print('AUDIO COMPLETE',sum(x['ready'] for x in manifest),'/',len(manifest),flush=True)

asyncio.run(voices())
