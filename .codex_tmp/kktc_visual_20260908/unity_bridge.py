import asyncio, json, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent / 'video_deps'))
from websockets.asyncio.client import connect
from websockets.exceptions import ConnectionClosed

async def main():
    method = sys.argv[1]
    args = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
    async with connect('ws://[::1]:8090/McpUnity', compression=None, max_size=16000000, close_timeout=2) as ws:
        await ws.send(json.dumps({'id':'kktc-review', 'method':method, 'params':args}))
        while True:
            try:
                result = await asyncio.wait_for(ws.recv(), 50)
            except ConnectionClosed as e:
                print(json.dumps({'connection_closed': str(e), 'note':'Inspect Unity logs/results before retrying a mutation.'}))
                return
            print(result)
            if json.loads(result).get('id') == 'kktc-review': break

asyncio.run(main())
