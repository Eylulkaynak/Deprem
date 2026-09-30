from pathlib import Path
import subprocess,json
root=Path(__file__).resolve().parents[2]
video=root/'ClientExports/YanYana/Recordings/physical-playthrough-20260914-085511.mp4'
ffmpeg=root/'.codex_tmp/kktc_visual_20260908/video_deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
pixels=subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-ss','204','-i',str(video),'-t','17','-vf','crop=76:78:30:40','-f','rawvideo','-pix_fmt','rgb24','pipe:1'],check=True,stdout=subprocess.PIPE).stdout
frame_bytes=76*78*3
assert len(pixels)%frame_bytes==0
counts=[]
for start in range(0,len(pixels),frame_bytes):
    frame=pixels[start:start+frame_bytes]
    counts.append(sum(r<160 and g<160 and b<160 for r,g,b in zip(frame[0::3],frame[1::3],frame[2::3])))
result={'video':str(video),'range_seconds':[204,221],'frames':len(counts),'portrait_roi':[30,40,76,78],'minimum_dark_pixels':min(counts),'maximum_dark_pixels':max(counts),'frames_below_100_dark_pixels':sum(n<100 for n in counts),'scope':'All decoded frames in this reunion segment only; image-content observation, not proof of every HUD frame in every route.'}
(root/'ClientExports/YanYana/Reports/hud-portrait-video-review.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
