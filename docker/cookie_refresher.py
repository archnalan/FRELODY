"""
YouTube cookie refresher — keeps a logged-in cookie jar alive for the ChordMini
yt-dlp sidecar so the datacenter-IP bot-wall stays bypassed without a human
re-exporting cookies every ~2 weeks.

Why this exists
---------------
On a datacenter IP a PO token is necessary but NOT sufficient (measured ~1/4
success); logged-in cookies are the reliable fix, but YouTube rotates/expires
them every couple of weeks. Re-doing the manual export that often is the real
operational pain. This sidecar does the export ONCE (human) and then keeps the
session warm by periodically revisiting YouTube with a persistent
Playwright/Chromium profile and re-exporting fresh cookies.

It deliberately does NOT automate the *login*. A fresh headless Google login
from a datacenter IP is exactly what Google's bot-detection flags (CAPTCHA / 2FA
/ "verify it's you"). So the human logs in once on a trusted machine, exports the
cookies (Firefox / Cookie-Editor — the only methods that work in 2026), and
hands them over. From then on this keeps them alive.

Critically: this container shares the host's public egress IP with ChordMini, so
the cookies are minted from the same IP yt-dlp calls from (YouTube binds sessions
to IP/fingerprint — mismatched-IP cookies get re-walled, see yt-dlp #15392).

Cookie slots (rotation)
-----------------------
Fresh cookies can be pasted from the SuperAdmin UI; each paste lands as a new
*slot* file in SLOTS_DIR (written by the FRELODY API into the shared frelody_media
volume). We keep up to MAX_SLOTS so a bad/expired paste never clobbers a working
session — the refresher tries slots newest→oldest and activates the first one
that actually authenticates, falling back to the legacy seed file and finally the
already-warmed profile. The API prunes slots to MAX_SLOTS (deletes the oldest).

Files
-----
  SLOTS_DIR/*.seed.txt   INPUT  — rotation slots (newest wins). API-written.
  SEED_FILE              INPUT  — legacy one-time human export (lowest priority).
  OUT_FILE               OUTPUT — the live jar ChordMini reads (:ro on its side).
                                  Written atomically (0644), ONLY when authenticated.
  STATUS_FILE            heartbeat for the Docker healthcheck (in /cookies).
  STATUS_PUBLISH         same status in frelody_media so the API/UI can read it.

Env tunables
------------
  COOKIES_DIR                 default /cookies
  PROFILE_DIR                 default /profile      (persistent Chromium profile)
  SLOTS_DIR                   default /media/cookie-seeds
  STATUS_PUBLISH              default /media/cookie-status.json
  MAX_SLOTS                   default 3
  REFRESH_INTERVAL_HOURS      default 24
  SLOT_POLL_SECONDS           default 60   (how often to notice a fresh paste)
  REFRESH_ALLOW_UNAUTHENTICATED  default false — set true ONLY to test the
                              browser→file→sidecar plumbing without a real login.
                              Never leave on in production.
"""

import glob
import json
import os
import sys
import time
import tempfile
from datetime import datetime, timezone

from playwright.sync_api import sync_playwright

COOKIES_DIR = os.environ.get("COOKIES_DIR", "/cookies").rstrip("/")
PROFILE_DIR = os.environ.get("PROFILE_DIR", "/profile").rstrip("/")
SLOTS_DIR = os.environ.get("SLOTS_DIR", "/media/cookie-seeds").rstrip("/")
STATUS_PUBLISH = os.environ.get("STATUS_PUBLISH", "/media/cookie-status.json")
MAX_SLOTS = int(os.environ.get("MAX_SLOTS", "3"))
INTERVAL_HOURS = float(os.environ.get("REFRESH_INTERVAL_HOURS", "24"))
POLL_SECONDS = float(os.environ.get("SLOT_POLL_SECONDS", "60"))
ALLOW_ANON = os.environ.get("REFRESH_ALLOW_UNAUTHENTICATED", "false").lower() == "true"

SEED_FILE = f"{COOKIES_DIR}/youtube.seed.txt"
OUT_FILE = f"{COOKIES_DIR}/youtube.txt"
STATUS_FILE = f"{COOKIES_DIR}/.refresher-status.json"

# A jar with any of these cookies on .youtube.com is a real logged-in session.
AUTH_COOKIE_NAMES = {"LOGIN_INFO", "SID", "__Secure-3PSID", "__Secure-1PSID"}


def log(msg: str) -> None:
    print(f"[cookie-refresher] {datetime.now(timezone.utc).isoformat()} {msg}",
          flush=True)


# ── Netscape cookies.txt I/O ────────────────────────────────────────────────

def parse_netscape(path: str) -> list:
    """Parse a Netscape cookies.txt into Playwright add_cookies() dicts."""
    cookies = []
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        for line in fh:
            raw = line.rstrip("\n")
            if not raw or (raw.startswith("#") and not raw.startswith("#HttpOnly_")):
                continue
            http_only = raw.startswith("#HttpOnly_")
            if http_only:
                raw = raw[len("#HttpOnly_"):]
            parts = raw.split("\t")
            if len(parts) != 7:
                continue
            domain, _sub, path_, secure, expiry, name, value = parts
            try:
                exp = int(expiry)
            except ValueError:
                exp = 0
            cookie = {
                "name": name,
                "value": value,
                "domain": domain,
                "path": path_ or "/",
                "httpOnly": http_only,
                "secure": secure.upper() == "TRUE",
            }
            if exp > 0:
                cookie["expires"] = exp
            cookies.append(cookie)
    return cookies


def to_netscape(cookies: list) -> str:
    """Serialize Playwright context cookies to Netscape format for yt-dlp."""
    lines = [
        "# Netscape HTTP Cookie File",
        "# Auto-generated by FRELODY cookie-refresher. Do not edit.",
        "",
    ]
    for c in cookies:
        domain = c.get("domain", "")
        if not domain:
            continue
        include_sub = "TRUE" if domain.startswith(".") else "FALSE"
        secure = "TRUE" if c.get("secure") else "FALSE"
        expires = c.get("expires", 0)
        expires = int(expires) if expires and expires > 0 else 0
        prefix = "#HttpOnly_" if c.get("httpOnly") else ""
        lines.append("\t".join([
            f"{prefix}{domain}", include_sub, c.get("path", "/") or "/",
            secure, str(expires), c.get("name", ""), c.get("value", ""),
        ]))
    return "\n".join(lines) + "\n"


def atomic_write(path: str, content: str, mode: int = 0o644) -> None:
    """Write then rename so readers never see a half-written file."""
    d = os.path.dirname(path) or "."
    os.makedirs(d, exist_ok=True)
    fd, tmp = tempfile.mkstemp(prefix=".tmp_yt_", dir=d)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as fh:
            fh.write(content)
        # mkstemp creates 0600; the cookie jar is read cross-container by the
        # chordmini sidecar's non-root user (uid 1001 'app'), so make it
        # world-readable or that copyfile fails with Permission denied.
        os.chmod(tmp, mode)
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.remove(tmp)


# ── seed slots ──────────────────────────────────────────────────────────────

def auth_info(cookies: list) -> tuple:
    """(has_auth, min_auth_expiry_epoch) for a parsed cookie list."""
    auth = [c for c in cookies if c["name"] in AUTH_COOKIE_NAMES]
    exps = [int(c["expires"]) for c in auth if c.get("expires", 0) and c["expires"] > 0]
    return (len(auth) > 0, min(exps) if exps else 0)


def list_seed_candidates() -> list:
    """All seed inputs, highest priority first: newest slots → legacy seed.

    Returns [(label, path, mtime, has_auth, min_expiry), ...].
    """
    out = []
    try:
        slots = glob.glob(f"{SLOTS_DIR}/*.seed.txt")
    except Exception:
        slots = []
    slots.sort(key=lambda p: os.path.getmtime(p), reverse=True)
    for p in slots:
        try:
            has_auth, exp = auth_info(parse_netscape(p))
            out.append((os.path.basename(p), p, os.path.getmtime(p), has_auth, exp))
        except Exception as exc:
            log(f"unreadable slot {p}: {exc}")
    if os.path.isfile(SEED_FILE):
        try:
            has_auth, exp = auth_info(parse_netscape(SEED_FILE))
            out.append(("legacy", SEED_FILE, os.path.getmtime(SEED_FILE), has_auth, exp))
        except Exception as exc:
            log(f"unreadable legacy seed: {exc}")
    return out


def newest_slot_mtime() -> float:
    """Most recent mtime across all seed inputs (for change detection)."""
    cands = list_seed_candidates()
    return max((c[2] for c in cands), default=0.0)


# ── status ──────────────────────────────────────────────────────────────────

def days_until(epoch: int):
    if not epoch:
        return None
    return round((epoch - time.time()) / 86400, 1)


def write_status(payload: dict) -> None:
    payload["lastRun"] = datetime.now(timezone.utc).isoformat()
    payload["intervalHours"] = INTERVAL_HOURS
    body = json.dumps(payload, indent=2)
    for path in (STATUS_FILE, STATUS_PUBLISH):
        try:
            atomic_write(path, body)
        except Exception as exc:  # status is best-effort
            log(f"could not write status {path}: {exc}")


def slot_summaries() -> list:
    """Human/UI-facing summary of every seed input (no cookie values)."""
    out = []
    for label, _path, mtime, has_auth, exp in list_seed_candidates():
        out.append({
            "name": label,
            "updatedAt": datetime.fromtimestamp(mtime, timezone.utc).isoformat(),
            "hasAuthCookies": has_auth,
            "expiresInDays": days_until(exp),
        })
    return out


# ── one refresh cycle ───────────────────────────────────────────────────────

def _export_if_authenticated(ctx, active_label: str) -> bool:
    """After a YouTube visit, export the jar iff it carries a logged-in session."""
    cookies = ctx.cookies()
    yt = [c for c in cookies
          if "youtube.com" in c.get("domain", "") or "google.com" in c.get("domain", "")]
    has_auth, exp = auth_info(yt)
    if has_auth or ALLOW_ANON:
        atomic_write(OUT_FILE, to_netscape(yt))
        note = "authenticated" if has_auth else "ANON TEST WRITE"
        log(f"wrote {len(yt)} cookies → {OUT_FILE} via '{active_label}' ({note})")
        write_status({
            "authenticated": has_auth, "cookieCount": len(yt), "wroteOutput": True,
            "activeSlot": active_label, "expiresInDays": days_until(exp),
            "minExpiryEpoch": exp, "slots": slot_summaries(), "note": note,
        })
        return True
    return False


def refresh_once(force_reseed: bool) -> None:
    with sync_playwright() as pw:
        ctx = pw.chromium.launch_persistent_context(
            PROFILE_DIR,
            headless=True,
            args=[
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-blink-features=AutomationControlled",
            ],
        )
        try:
            page = ctx.new_page()

            # Fast path: the warmed profile is still logged in and no fresh paste
            # is asking us to switch — just revisit to renew expiries and export.
            have_login = any(c["name"] in AUTH_COOKIE_NAMES for c in ctx.cookies())
            if have_login and not force_reseed:
                page.goto("https://www.youtube.com", wait_until="domcontentloaded",
                          timeout=60000)
                page.wait_for_timeout(4000)
                if _export_if_authenticated(ctx, "profile"):
                    return
                log("warmed profile no longer authenticates — falling back to seeds")

            # Reseed path: try each seed input newest→oldest until one logs in.
            candidates = list_seed_candidates()
            if not candidates:
                log(f"no seeds in {SLOTS_DIR} or {SEED_FILE} — paste a cookie export "
                    f"in the SuperAdmin UI to bootstrap (see CLAUDE.md)")
                write_status({
                    "authenticated": False, "cookieCount": 0, "wroteOutput": False,
                    "activeSlot": None, "expiresInDays": None, "minExpiryEpoch": 0,
                    "slots": [], "note": "no seed available",
                })
                return

            for label, path, _mtime, has_auth, _exp in candidates:
                if not has_auth:
                    log(f"slot '{label}' has no auth cookies — skipping")
                    continue
                ctx.clear_cookies()
                ctx.add_cookies(parse_netscape(path))
                log(f"trying seed '{label}' ({len(parse_netscape(path))} cookies)")
                page.goto("https://www.youtube.com", wait_until="domcontentloaded",
                          timeout=60000)
                page.wait_for_timeout(4000)
                if _export_if_authenticated(ctx, label):
                    return

            log("no seed authenticated — leaving youtube.txt untouched")
            write_status({
                "authenticated": False, "cookieCount": 0, "wroteOutput": False,
                "activeSlot": None, "expiresInDays": None, "minExpiryEpoch": 0,
                "slots": slot_summaries(), "note": "no seed authenticated",
            })
        finally:
            ctx.close()


def main() -> None:
    log(f"starting | interval={INTERVAL_HOURS}h | poll={POLL_SECONDS}s | "
        f"slots={SLOTS_DIR} | legacy_seed={SEED_FILE} | out={OUT_FILE} | "
        f"allow_anon={ALLOW_ANON}")
    last_full = 0.0
    last_seen_mtime = -1.0
    while True:
        try:
            now = time.time()
            cur_mtime = newest_slot_mtime()
            new_paste = cur_mtime > last_seen_mtime and last_seen_mtime >= 0
            due = (now - last_full) >= INTERVAL_HOURS * 3600
            if due or new_paste or last_seen_mtime < 0:
                if new_paste:
                    log("detected a newer seed slot — reseeding now")
                refresh_once(force_reseed=new_paste)
                last_full = time.time()
            last_seen_mtime = cur_mtime
        except Exception as exc:
            log(f"refresh failed: {exc}")
            write_status({
                "authenticated": False, "cookieCount": 0, "wroteOutput": False,
                "activeSlot": None, "note": f"error: {exc}", "slots": [],
            })
        time.sleep(max(POLL_SECONDS, 5))


if __name__ == "__main__":
    sys.exit(main())
