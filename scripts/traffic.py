#!/usr/bin/env python3
"""Send random traffic through the gateway so the dashboards have something to show."""

import argparse
import json
import os
import random
import secrets
import sys
import time
import urllib.error
import urllib.request
from collections import Counter
from pathlib import Path

STATE_FILE = Path(__file__).with_name(".traffic-state.json")
API_SLUG = "traffic-sim"
CONSUMER_PASSWORD = "Sim-traffic-1!"
TOP_UP = 500

TIERS = [
    {"name": "Basic", "requestsPerMinute": 30, "monthlyQuota": 5_000, "creditCostPerRequest": 1},
    {"name": "Plus", "requestsPerMinute": 120, "monthlyQuota": 100_000, "creditCostPerRequest": 0.5},
]

# (weight, method, path template)
REQUESTS = [
    (30, "GET", "/get"),
    (12, "GET", "/anything/users/{id}"),
    (10, "GET", "/anything/orders/{id}"),
    (6, "GET", "/uuid"),
    (10, "POST", "/post"),
    (5, "PUT", "/put"),
    (4, "DELETE", "/delete"),
    (5, "GET", "/status/404"),
    (4, "GET", "/status/500"),
    (2, "GET", "/status/503"),
    (8, "GET", "/delay/0.03"),
    (6, "GET", "/delay/0.08"),
    (4, "GET", "/delay/0.2"),
    (2, "GET", "/delay/0.4"),
    (1, "GET", "/delay/1.1"),
]


def http(method, url, token=None, api_key=None, body=None):
    headers = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    if api_key:
        headers["X-API-Key"] = api_key
    data = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body).encode()

    request = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status, response.read()
    except urllib.error.HTTPError as error:
        return error.code, error.read()


def parse(raw):
    try:
        return json.loads(raw)
    except ValueError:
        return None


class Dashboard:
    """The management API, used only to set things up."""

    def __init__(self, base_url):
        self.base_url = base_url

    def call(self, method, path, token=None, body=None):
        status, raw = http(method, self.base_url + path, token=token, body=body)
        if status >= 400:
            detail = (parse(raw) or {}).get("detail", raw.decode(errors="replace")[:200])
            sys.exit(f"{method} {path} failed with {status}: {detail}")
        return parse(raw)

    def login(self, email, password):
        return self.call("POST", "/api/auth/login", body={"email": email, "password": password})["accessToken"]


def provision(dashboard, args):
    owner = dashboard.login(args.owner_email, args.owner_password)

    apis = dashboard.call("GET", "/api/apis", owner)
    api = next((a for a in apis if a["slug"] == API_SLUG), None)
    if api is None:
        api = dashboard.call("POST", "/api/apis", owner, {
            "name": "Traffic Simulator",
            "slug": API_SLUG,
            "description": "Target of scripts/traffic.py",
            "targetBaseUrl": args.upstream_url,
        })

    tiers = dashboard.call("GET", f"/api/apis/{api['id']}/tiers", owner)
    for wanted in TIERS:
        if not any(t["name"] == wanted["name"] for t in tiers):
            tiers.append(dashboard.call("POST", f"/api/apis/{api['id']}/tiers", owner, wanted))
    tiers = [t for t in tiers if t["name"] in {w["name"] for w in TIERS}]

    consumers = []
    for _ in range(args.consumers):
        email = f"sim-{secrets.token_hex(3)}@example.test"
        dashboard.call("POST", "/api/auth/register", body={
            "name": f"Sim {email[4:10]}",
            "email": email,
            "password": CONSUMER_PASSWORD,
            "role": "Consumer",
        })
        tier = random.choice(tiers)
        issued = dashboard.call("POST", f"/api/apis/{api['id']}/keys", owner, {
            "name": "traffic",
            "tierId": tier["id"],
            "consumerEmail": email,
        })
        consumers.append({"email": email, "key": issued["secret"], "tier": tier["name"]})

    return {"baseUrl": args.base_url, "consumers": consumers}


def load_state(args):
    if args.fresh or not STATE_FILE.exists():
        return None
    state = parse(STATE_FILE.read_text())
    if not state or state.get("baseUrl") != args.base_url or len(state["consumers"]) != args.consumers:
        return None

    # keys from an earlier run are useless if the database was reset since
    status, _ = http("GET", f"{args.base_url}/gw/{API_SLUG}/status/200", api_key=state["consumers"][0]["key"])
    return None if status in (401, 403) else state


def random_request():
    weights = [weight for weight, _, _ in REQUESTS]
    _, method, path = random.choices(REQUESTS, weights)[0]
    path = path.format(id=random.randint(1, 500))
    body = {"item": random.randint(1, 100), "note": secrets.token_hex(4)} if method in ("POST", "PUT") else None
    return method, path, body


class Traffic:
    def __init__(self, dashboard, state, args):
        self.dashboard = dashboard
        self.consumers = state["consumers"]
        self.gateway = f"{args.base_url}/gw/{API_SLUG}"
        self.quiet = args.quiet
        self.outcomes = Counter()
        self.sent = 0

    def send(self, consumer, method, path, body, key=...):
        key = consumer["key"] if key is ... else key
        status, raw = http(method, self.gateway + path, api_key=key, body=body)
        code = (parse(raw) or {}).get("code") if status >= 400 else None

        self.sent += 1
        self.outcomes[f"{status} {code}" if code else str(status)] += 1
        if not self.quiet:
            who = consumer["email"] if key else "(no key)"
            print(f"{time.strftime('%H:%M:%S')}  {who:<28} {method:<6} {path:<26} {status} {code or ''}")

        if code == "insufficient_credits":
            self.top_up(consumer)

    def top_up(self, consumer):
        token = self.dashboard.login(consumer["email"], CONSUMER_PASSWORD)
        self.dashboard.call("POST", "/api/me/credits/top-up", token, {"amount": TOP_UP})
        if not self.quiet:
            print(f"{time.strftime('%H:%M:%S')}  {consumer['email']:<28} topped up {TOP_UP} credits")

    def step(self):
        """One unit of traffic: usually a single request, now and then a burst or a bad key."""
        consumer = random.choice(self.consumers)
        roll = random.random()

        if roll < 0.02:
            self.send(consumer, "GET", "/get", None, key=None)
        elif roll < 0.05:
            self.send(consumer, "GET", "/get", None, key="gw_" + secrets.token_urlsafe(32))
        elif roll < 0.09:
            for _ in range(random.randint(15, 45)):
                self.send(consumer, *random_request())
        else:
            self.send(consumer, *random_request())

    def summary(self):
        print(f"\n{self.sent} requests")
        for outcome, count in sorted(self.outcomes.items()):
            print(f"  {outcome:<28} {count}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("-n", "--count", type=int, default=100, help="how many requests to send (default 100)")
    parser.add_argument("-f", "--forever", action="store_true", help="keep sending until interrupted (Ctrl-C)")
    parser.add_argument("-d", "--delay", type=float, default=1.0, help="maximum pause between requests in seconds (default 1)")
    parser.add_argument("-c", "--consumers", type=int, default=4, help="how many simulated consumers (default 4)")
    parser.add_argument("-q", "--quiet", action="store_true", help="print only the summary")
    parser.add_argument("--fresh", action="store_true", help="create new consumers and keys instead of reusing the last run's")
    parser.add_argument("--base-url", default=os.environ.get("BASE_URL", "http://localhost:8080").rstrip("/"))
    parser.add_argument("--upstream-url", default=os.environ.get("UPSTREAM_URL", "http://upstream:8080"),
                        help="where the simulated API forwards to, as seen from the backend")
    parser.add_argument("--owner-email", default=os.environ.get("OWNER_EMAIL", "owner@demo.local"))
    parser.add_argument("--owner-password", default=os.environ.get("PASSWORD", "Password123!"))
    args = parser.parse_args()

    dashboard = Dashboard(args.base_url)
    state = load_state(args)
    if state is None:
        state = provision(dashboard, args)
        STATE_FILE.write_text(json.dumps(state, indent=2))
        print(f"created {len(state['consumers'])} consumers with keys for /gw/{API_SLUG}")

    traffic = Traffic(dashboard, state, args)
    try:
        while args.forever or traffic.sent < args.count:
            traffic.step()
            time.sleep(random.uniform(0, args.delay))
    except KeyboardInterrupt:
        pass
    finally:
        traffic.summary()


if __name__ == "__main__":
    main()
