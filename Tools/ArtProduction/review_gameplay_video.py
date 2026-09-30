"""Create timestamped inspection sheets from an unmodified Unity Recorder video."""
from pathlib import Path
import argparse,sys,subprocess,re,json
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(root/'.codex_tmp/kktc_visual_20260908/video_deps'))
import imageio_ffmpeg
parser=argparse.ArgumentParser()
parser.add_argument('video',type=Path)
parser.add_argument('--step',type=float,default=10)
parser.add_argument('--times',default='')
args=parser.parse_args()
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
folder=root/'ClientExports/KKTC/GameplayReview'/args.video.stem
folder.mkdir(parents=True,exist_ok=True)
probe=subprocess.run([ffmpeg,'-hide_banner','-i',str(args.video)],capture_output=True,text=True)
(folder/'VideoMetadata.txt').write_text(probe.stderr,encoding='utf-8')
if args.times:
    for stamp in [float(s) for s in args.times.split(',')]:
        subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-ss',str(stamp),'-i',str(args.video),'-frames:v','1',str(folder/f'detail_{stamp:07.2f}.png')],check=True)
else:
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-i',str(args.video),'-vf',f'fps=1/{args.step},scale=270:-2',str(folder/'frame_%04d.jpg')],check=True)
    files=sorted(folder.glob('frame_*.jpg'))
    for page in range(0,len(files),12):
        sheet=Image.new('RGB',(1080,1515),'#20262b');draw=ImageDraw.Draw(sheet)
        for index,path in enumerate(files[page:page+12]):
            picture=Image.open(path).convert('RGB');picture.thumbnail((270,480))
            x=(index%4)*270;y=(index//4)*505
            sheet.paste(picture,(x,y));stamp=(page+index)*args.step+args.step*.5
            draw.text((x+8,y+484),f'{int(stamp//60):02}:{stamp%60:04.1f}',fill='white')
        sheet.save(folder/f'sheet_{page//12+1:02}.jpg',quality=92)
    audio=subprocess.run([ffmpeg,'-hide_banner','-i',str(args.video),'-vn','-af','volumedetect','-f','null','-'],capture_output=True,text=True)
    (folder/'AudioLevels.txt').write_text(audio.stderr,encoding='utf-8')
print(folder)
