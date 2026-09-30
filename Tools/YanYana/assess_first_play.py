"""Assess supplied observations; this tool never produces participant evidence."""
import argparse
import json
import statistics
from pathlib import Path


def assess(data):
    if data.get('schema') != 1:
        raise ValueError('Unsupported observation schema')
    if not data.get('scene_sha256') or not data.get('build_archive_sha256'):
        raise ValueError('Record the actual scene and build SHA256 values')
    sessions = data.get('sessions', [])
    ids = [s['participant_id'] for s in sessions]
    if len(ids) != len(set(ids)):
        raise ValueError('Participant IDs must be unique')
    eligible, pending, durations, independent, understood = [], [], [], [], []
    for s in sessions:
        pid = s['participant_id']
        age = s.get('age')
        if s.get('observer_confirmed') is not True or age is None:
            pending.append(pid)
            continue
        if not isinstance(age, int) or not 8 <= age <= 12 or s.get('first_play') is not True:
            continue
        eligible.append(pid)
        independent.append(all(s.get('independent', {}).get(k) is True for k in ('walk', 'pickup', 'place')))
        links = s.get('explained_links', [])
        understood.append(len({x['cause'].strip() for x in links if x.get('accurate') is True and x.get('cause', '').strip() and x.get('effect', '').strip()}) >= 3)
        if s.get('completed') is not True:
            continue
        if s.get('ending') not in (1, 2, 3, 4):
            raise ValueError(f'{pid}: completed session needs the observed ending')
        timing = s.get('timing', {})
        values = [timing.get(k) for k in ('wall_seconds', 'excluded_seconds')]
        if any(type(x) not in (int, float) or x < 0 for x in values):
            raise ValueError(f'{pid}: complete nonnegative timings are required')
        elapsed = values[0] - values[1]
        if elapsed <= 0:
            raise ValueError(f'{pid}: exclusions must be less than wall time')
        durations.append(elapsed / 60)
    median = statistics.median(durations) if durations else None
    ratio = sum(independent) / len(eligible) if eligible else None
    gates = {
        'eight_completed_first_plays': len(durations) >= 8,
        'no_unresolved_incomplete_sessions': bool(eligible) and len(durations) == len(eligible) and not pending,
        'median_28_to_35_minutes': median is not None and 28 <= median <= 35,
        'at_least_80_percent_independent': ratio is not None and ratio >= .8,
        'three_links_per_observed_player': bool(understood) and all(understood),
    }
    return {
        'status': 'OBSERVED_NUMERICAL_GATES_PASSED' if all(gates.values()) else 'NOT_ACCEPTED',
        'scope': 'Supplied first-play observations only. Fun, visual quality and physical Android performance require separate review.',
        'scene_sha256': data['scene_sha256'],
        'build_archive_sha256': data['build_archive_sha256'],
        'eligible_started': len(eligible),
        'completed': len(durations),
        'unconfirmed': pending,
        'median_minutes': median,
        'independent_fraction': ratio,
        'gates': gates,
    }


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('observations', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    try:
        result = assess(json.loads(args.observations.read_text(encoding='utf-8-sig')))
    except (KeyError, ValueError, TypeError) as error:
        parser.error(str(error))
    rendered = json.dumps(result, ensure_ascii=False, indent=2)
    if args.output:
        args.output.write_text(rendered + '\n', encoding='utf-8')
    print(rendered)
    raise SystemExit(0 if result['status'] == 'OBSERVED_NUMERICAL_GATES_PASSED' else 2)
