import argparse
import concurrent.futures
import json
import pathlib
import urllib.request

OUT = pathlib.Path(__file__).resolve().parent

def request(url, body=None, timeout=90):
    data = None if body is None else json.dumps(body).encode('utf-8')
    req = urllib.request.Request(url, data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=timeout) as response:
        return json.loads(response.read().decode('utf-8-sig'))

def discover(port):
    try:
        result = request(f'http://127.0.0.1:{port}/health', timeout=1)
        return {'port': port, 'health': result}
    except Exception:
        return None

parser = argparse.ArgumentParser()
parser.add_argument('endpoint', nargs='?', default='/health')
parser.add_argument('--body')
parser.add_argument('--save')
parser.add_argument('--port', type=int)
parser.add_argument('--discover', action='store_true')
args = parser.parse_args()
if args.discover:
    with concurrent.futures.ThreadPoolExecutor(max_workers=11) as pool:
        results = [result for result in pool.map(discover, range(8090, 8101)) if result]
    print(json.dumps(results, ensure_ascii=False, indent=2))
    if results:
        (OUT/'connection.json').write_text(json.dumps(results[0]), encoding='utf-8')
else:
    port = args.port or json.loads((OUT/'connection.json').read_text(encoding='utf-8'))['port']
    body = json.loads(pathlib.Path(args.body).read_text(encoding='utf-8-sig')) if args.body else None
    result = request(f'http://127.0.0.1:{port}{args.endpoint}', body)
    if args.save:
        pathlib.Path(args.save).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps({'saved': args.save}))
    else:
        print(json.dumps(result, ensure_ascii=False, indent=2))
