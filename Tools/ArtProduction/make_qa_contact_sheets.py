"""Arrange existing QA renders for inspection; production art is never modified."""
from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2]
output=root/'ClientExports/KKTC/CameraSheets'
output.mkdir(parents=True,exist_ok=True)
for chapter in range(1,5):
    files=sorted((root/'Temp/StoryCameraQA').glob(f'Story_{chapter:02}_*Tall9x19_5.png'))
    for page in range(0,len(files),6):
        sheet=Image.new('RGB',(1080,860),'#20262b')
        draw=ImageDraw.Draw(sheet)
        for index,path in enumerate(files[page:page+6]):
            picture=Image.open(path).convert('RGB');picture.thumbnail((350,390))
            x=(index%3)*360;y=(index//3)*430
            sheet.paste(picture,(x+(360-picture.width)//2,y))
            name=path.stem.replace(f'Story_{chapter:02}_RebuildPreview_','').replace('_Tall9x19_5','')
            draw.text((x+6,y+394),name[:48],fill='white')
        sheet.save(output/f'Story{chapter:02}_Page{page//6+1}.jpg',quality=90)
        print(output/f'Story{chapter:02}_Page{page//6+1}.jpg')
