"""Bake existing speech WAV amplitude into native-animation authoring data."""
import array, json, math, pathlib, re, wave
ROOT=pathlib.Path(__file__).resolve().parents[2]
DEST=ROOT/'ArtDirection/YanYana/Characters/NewResidents/speech-curves.json'
ALIASES={'Ece':'Ece','Aylin':'Aylin','Zeynep':'Zeynep','Eren':'Eren','Mina':'Mina','Deniz':'Deniz','Gül':'Gul'}
rows=[]
for line in json.loads((ROOT/'ArtDirection/YanYana/Audio/Physical/manifest.json').read_text(encoding='utf-8-sig')):
    if not line.get('ready'):continue
    speakers=list(re.finditer(r'([A-ZÇĞİÖŞÜ][a-zçğıöşü]+):\s*',line['line']))
    if not any(s.group(1) in ALIASES for s in speakers):continue
    with wave.open(str(ROOT/'Assets/YanYana/Audio/Physical'/f"{line['key']}.wav"),'rb') as wav:
        assert wav.getsampwidth()==2
        rate=wav.getframerate();ch=wav.getnchannels();samples=array.array('h',wav.readframes(wav.getnframes()));duration=len(samples)/(rate*ch)
    hop=max(1,rate//30);mono=[sum(samples[i:i+ch])/ch/32768 for i in range(0,len(samples),ch)]
    rms=[math.sqrt(sum(v*v for v in mono[i:i+hop])/max(1,len(mono[i:i+hop]))) for i in range(0,len(mono),hop)]
    peak=max(rms) or 1;active=[i for i,v in enumerate(rms) if v>peak*.07];start=active[0]/30 if active else 0;end=active[-1]/30 if active else duration
    # The existing WAVs combine turns. Segment time is estimated from text,
    # while mouth opening follows the measured waveform including real pauses.
    segments=[]
    for i,speaker in enumerate(speakers):
        text=line['line'][speaker.end():speakers[i+1].start() if i+1<len(speakers) else len(line['line'])]
        segments.append((speaker.group(1),text,max(1,len(text))))
    total=sum(s[2] for s in segments);cursor=0
    for who,text,weight in segments:
        lo=start+(end-start)*cursor/total;hi=start+(end-start)*(cursor+weight)/total;cursor+=weight
        if who not in ALIASES:continue
        name=ALIASES[who];frames=[];letters=[c for c in text.lower() if c in 'aeıioöuü'] or ['a']
        for i,energy in enumerate(rms):
            time=i/30;env=min(1,max(0,(energy/peak-.045)/.7))**.68 if lo<=time<=hi else 0
            vowel=letters[min(len(letters)-1,int(max(0,time-lo)/max(.01,hi-lo)*len(letters)))];shape='O' if vowel in 'oöuü' else 'E' if vowel in 'eıi' else 'A'
            frames.append({'time':round(time,5),'a':round(env*(1 if shape=='A' else .18),4),'e':round(env*(.84 if shape=='E' else 0),4),'o':round(env*(.88 if shape=='O' else 0),4)})
        frames.extend([{'time':round(duration,5),'a':0,'e':0,'o':0},{'time':round(duration+.06,5),'a':0,'e':0,'o':0}])
        rows.append({'actor':name,'audio':line['key'],'line':line['line'],'frames':frames,'timing':'waveform amplitude; estimated text turn boundaries'})
DEST.write_text(json.dumps({'lines':rows},ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print('Baked',len(rows),'speaker animation tracks for',sorted({r['actor'] for r in rows}))
