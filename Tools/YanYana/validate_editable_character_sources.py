"""Checks exported source geometry and skin bindings without changing Unity assets."""
import json,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
sources=root/'ArtDirection/YanYana/Characters/ApprovedStyle';report=[]
expected=dict(Ada=1.46,Efe=1.25,Derya=1.70,Emre=1.82,Yusuf=1.73,Idil=1.75,Bora=1.80,Selma=1.70,Mert=1.82,Gul=1.78,Deniz=1.82,Asli=1.70,Ozan=1.82)
for path in sorted(sources.glob('*.mesh.json')):
 data=json.loads(path.read_text(encoding='utf-8-sig'));vertices=[v for s in data['surfaces'] for v in s['vertices']]
 height=max(v['y'] for v in vertices)-min(v['y'] for v in vertices);errors=[]
 if abs(min(v['y'] for v in vertices))>.005:errors.append('rest surface is not grounded')
 if not expected[data['name']]*.88<height<expected[data['name']]*1.12:errors.append('rest height outside declared character range')
 for surface in data['surfaces']:
  if len(surface['weights'])!=len(surface['vertices']):errors.append('weight/vertex length mismatch')
  for weight in surface['weights']:
   if abs(sum(weight['values'])-1)>.02:errors.append('unnormalized weight');break
   if any(w>0 and (b<0 or b>=len(surface['bones'])) for b,w in zip(weight['bones'],weight['values'])):errors.append('invalid bone index');break
  if any(i<0 or i>=len(surface['vertices']) for group in surface['groups'] for i in group['indices']):errors.append('invalid triangle')
  for shape in surface.get('shapes',[]):
   if len(shape['deltas'])!=len(surface['vertices']):errors.append('shape/vertex length mismatch');continue
   moved=[i for i,d in enumerate(shape['deltas']) if sum(d[k]**2 for k in ('x','y','z'))>1e-10]
   if not moved:errors.append('empty shape '+shape['name'])
   side='Right' if shape['name'].startswith('Right_') else 'Left'
   for i in moved:
    weight=surface['weights'][i];dominant=weight['bones'][weight['values'].index(max(weight['values']))]
    if surface['bones'][dominant]['name'] not in (side+'Hand',side[0]+'Hand'):errors.append('shape moves outside its hand '+shape['name']);break
  for group in surface['groups']:
   for field in ('texture','regionTexture'):
    if group.get(field) and not (sources/group[field]).is_file():errors.append('missing '+field)
  for vertex,weight in zip(surface['vertices'],surface['weights']):
   dominant=weight['bones'][weight['values'].index(max(weight['values']))];bone=surface['bones'][dominant]['point']
   radius=sum((vertex[k]-bone[k])**2 for k in ('x','y','z'))**.5
   if radius>.95:errors.append('surface and skeleton rest spaces do not agree');break
 shapes=[s['name'] for surface in data['surfaces'] for s in surface.get('shapes',[])]
 if data['name'] in ('Ada','Efe','Idil') and set(shapes)!={side+'_'+pose for side in ('Left','Right') for pose in ('Loop','Cylinder','Soft','Pinch')}:errors.append('missing interaction hand shapes')
 if not (sources/(data['name']+'.blend')).exists():errors.append('missing Blender source')
 report.append(dict(character=data['name'],heightMetres=round(height,4),intendedHeight=expected[data['name']],surfaces=len(data['surfaces']),vertices=len(vertices),valid=not errors,errors=errors))
out=root/'ClientExports/YanYana/Reports/editable-character-sources.json';out.write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
if not all(x['valid'] for x in report):raise SystemExit(1)
