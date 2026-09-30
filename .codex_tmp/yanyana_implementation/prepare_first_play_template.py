from pathlib import Path
import hashlib, json, copy, importlib.util

root = Path(__file__).resolve().parents[2]
folder = root / 'ClientExports/YanYana/Playtests'
folder.mkdir(exist_ok=True)
target = folder / 'observations.template.json'
template = {
    'schema': 1,
    'scene_sha256': hashlib.sha256((root/'Assets/YanYana/Scenes/YanYana_Adventure.unity').read_bytes()).hexdigest(),
    'build_archive_sha256': None,
    'note': 'Empty observation form. No participant has been observed. Fill the actual candidate package hash in your copy.',
    'sessions': [{
        'participant_id': f'P{i:02d}', 'age': None, 'first_play': None,
        'observer_confirmed': False, 'completed': None, 'ending': None,
        'device': {'model': None, 'input': None, 'physical_android': None},
        'timing': {'wall_seconds': None, 'excluded_seconds': None, 'excluded_intervals': []},
        'independent': {'walk': None, 'pickup': None, 'place': None},
        'explained_links': [], 'external_help': [], 'observations': [],
        'favorite_moment': None, 'frustrating_moment': None, 'would_replay': None,
    } for i in range(1,9)]
}
package_path=root/'ClientExports/YanYana/Reports/windows-package.json'
if package_path.exists():
    package=json.loads(package_path.read_text(encoding='utf-8'))
    if package.get('sceneSha256')==template['scene_sha256']:
        template['build_archive_sha256']=package['sha256']
existing=json.loads(target.read_text(encoding='utf-8')) if target.exists() else None
empty=existing is None or all(not s.get('observer_confirmed') and s.get('age') is None and s.get('completed') is None for s in existing.get('sessions',[]))
if empty:
    target.write_text(json.dumps(template,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
else:
    print('Existing participant observations preserved; create a separate template for the new candidate.')
print('Empty observation template:',target)

# In-memory synthetic calculator checks; never written as participant evidence.
spec=importlib.util.spec_from_file_location('firstplay',root/'Tools/YanYana/assess_first_play.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
data=copy.deepcopy(template);data['build_archive_sha256']='0'*64
for s in data['sessions']:
    s.update(age=10,first_play=True,observer_confirmed=True,completed=True,ending=2)
    s['timing'].update(wall_seconds=1860,excluded_seconds=60)
    s['independent']=dict(walk=True,pickup=True,place=True)
    s['explained_links']=[dict(cause=str(i),effect='synthetic',accurate=True) for i in range(3)]
assert module.assess(data)['median_minutes']==30
assert module.assess(data)['status']=='OBSERVED_NUMERICAL_GATES_PASSED'
data['sessions'][0]['independent']['place']=False
assert module.assess(data)['gates']['at_least_80_percent_independent']
data['sessions'][1]['independent']['place']=False
assert not module.assess(data)['gates']['at_least_80_percent_independent']
data['sessions'][1]['completed']=False
assert module.assess(data)['completed']==7
assert not module.assess(data)['gates']['eight_completed_first_plays']
assert not module.assess(data)['gates']['no_unresolved_incomplete_sessions']
data=copy.deepcopy(template);data['build_archive_sha256']='0'*64
assert module.assess(data)['median_minutes'] is None
assert module.assess(data)['status']=='NOT_ACCEPTED'
print('Synthetic calculator checks passed; no child evidence produced.')
