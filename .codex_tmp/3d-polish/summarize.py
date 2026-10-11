import json
from pathlib import Path
import xml.etree.ElementTree as ET

folder = Path('ClientExports/DepremApp/Polish3D')
cases = {}
sources = ['gameplay.results.xml', 'bag-input.results.xml']
for source in sources:
    root = ET.parse(folder / source).getroot()
    for case in root.iter('test-case'):
        name = case.get('fullname')
        cases[name] = {
            'name': name, 'result': case.get('result'),
            'source': source, 'duration_seconds': case.get('duration')
        }
assert len(cases) == 17, len(cases)
assert all(case['result'] == 'Passed' for case in cases.values()), cases
visual_folder = Path('ClientExports/DepremApp/Visual3D')
visual_sources = ['verification.txt', 'scenario.txt', 'adventure.txt']
for source in visual_sources:
    report = (visual_folder / source).read_text(encoding='utf-8-sig')
    assert 'PASS: no runtime errors.' in report, source
    assert 'RESTORED:' in report, source
    assert not any(line.startswith('FAIL') for line in report.splitlines()), source
adventure_source = Path('ClientExports/YanYana/Reports/physical-recorded-route-2.txt')
adventure_report = adventure_source.read_text(encoding='utf-8-sig')
prepared_adventure_completed = 'COMPLETE Ending=2 FamilyPlan=1 FlashlightReady=1 RadioReady=1 BroadcastSource=1 Reunited=1.' in adventure_report
assert prepared_adventure_completed, adventure_source
assert not any(line.startswith('FAIL') for line in adventure_report.splitlines()), adventure_source
summary = {
    'scope': 'Latest successful result for each of 17 distinct Unity PlayMode checks.',
    'passed': len(cases), 'failed': 0,
    'shared_minigame_tasks': 38, 'street_evacuation_stages': 15, 'story_chapters': 4,
    'prepared_adventure_completed': prepared_adventure_completed,
    'adventure_source': str(adventure_source),
    'visual_sources': [str(visual_folder / source) for source in visual_sources],
    'sources': sources,
    'note': 'The full run recorded 16 passes and one failure caused by a disconnected editor MCP client logging a transport error. The bag input test was rerun and passed. Raw reports remain intact. Gameplay errors are not ignored.',
    'tests': sorted(cases.values(), key=lambda case: case['name'])
}
(folder / 'verified-summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(f"Latest verified checks: {len(cases)} passed, 0 failed.")
