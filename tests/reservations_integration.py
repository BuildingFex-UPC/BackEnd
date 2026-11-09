"""Run against a disposable, seeded BuildingFex API: python tests/reservations_integration.py URL."""
import concurrent.futures
import json
import sys
import urllib.error
import urllib.request
import uuid

base = sys.argv[1].rstrip('/')

def request(path, data, token=None):
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(base + path, data=json.dumps(data).encode(), headers=headers)
    try:
        with urllib.request.urlopen(req) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        return error.code, json.loads(error.read())

status, login = request('/api/v1/authentication/sign-in', {'email': 'admin@buildingfex.test', 'password': 'admin123'})
assert status == 200, (status, login)
token = login['token']
owner = login['user']['id']
spaces = [f'test-space-{uuid.uuid4().hex}' for _ in range(2)]
for space in spaces:
    status, result = request('/socialSpaces', {'id': space, 'name': 'Test', 'ownerAdminId': owner}, token)
    assert status == 201, (status, result)

def reserve(space, date, start, end):
    return request('/reservations', {
        'id': f'test-{uuid.uuid4().hex}', 'ownerAdminId': owner,
        'spaceId': space, 'residentId': owner, 'date': date,
        'startTime': start, 'endTime': end,
    }, token)

with concurrent.futures.ThreadPoolExecutor(max_workers=2) as executor:
    results = list(executor.map(lambda _: reserve(spaces[0], '2035-01-01', '08:00', '10:00'), range(2)))
assert sorted(status for status, _ in results) == [201, 400], results
assert next(body['code'] for status, body in results if status == 400) == 'RESERVATION_OVERLAP'
for start, end in [('08:00', '10:00'), ('09:00', '11:00'), ('08:30', '09:30')]:
    status, body = reserve(spaces[0], '2035-01-01', start, end)
    assert status == 400 and body['code'] == 'RESERVATION_OVERLAP', (status, body)
for space, date, start, end in [(spaces[0], '2035-01-01', '10:00', '12:00'),
                                (spaces[0], '2035-01-02', '08:00', '10:00'),
                                (spaces[1], '2035-01-01', '08:00', '10:00')]:
    status, body = reserve(space, date, start, end)
    assert status == 201, (status, body)
status, body = reserve(spaces[0], '2035-1-01', '08:00', '10:00')
assert status == 400 and body['code'] == 'RESERVATION_DATE_INVALID', (status, body)
print('Reservation integration checks passed.')
