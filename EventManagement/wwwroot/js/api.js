const Auth = {
  get token() { return localStorage.getItem('em_token'); },
  get user() {
    try { return JSON.parse(localStorage.getItem('em_user') || 'null'); }
    catch (err) { return null; }
  },
  save(r) { localStorage.setItem('em_token', r.token); localStorage.setItem('em_user', JSON.stringify(r)); },
  clear() { localStorage.removeItem('em_token'); localStorage.removeItem('em_user'); },
  require(adminOnly = false) {
    if (!this.token) { location.href = '/index.html?signin=1'; return false; }
    if (adminOnly && !(this.user && this.user.isAdmin)) { location.href = '/dashboard.html'; return false; }
    return true;
  },
  logout() { this.clear(); location.href = '/index.html'; }
};

// Single place that talks to the API: attaches the token, handles 401 / 403 and error bodies.
// Public (guest) calls work without a token. A 401 only happens on protected endpoints.
async function api(path, { method = 'GET', body, form } = {}) {
  const headers = {};
  if (Auth.token) headers['Authorization'] = 'Bearer ' + Auth.token;
  let payload;
  if (form) payload = form;
  else if (body !== undefined) { headers['Content-Type'] = 'application/json'; payload = JSON.stringify(body); }

  const res = await fetch(path, { method, headers, body: payload });
  if (res.status === 401) {
    Auth.clear();
    location.href = '/index.html?signin=1';
    throw new Error('Please sign in again.');
  }
  const text = await res.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); }
    catch (err) { data = null; }
  }
  if (!res.ok) {
    let msg = (data && (data.message || data.title))
      || (res.status === 403 ? 'You are not allowed to do that.' : 'Request failed (' + res.status + ')');
    if (data && data.errors) msg = Object.values(data.errors).flat().join(' ');
    throw new Error(msg);
  }
  return data;
}

async function openEndorsement(id) {
  try {
    const res = await fetch(`/api/admin/requests/${id}/endorsement`, { headers: { Authorization: 'Bearer ' + Auth.token } });
    if (!res.ok) {
      const data = await res.json().catch(() => null);
      throw new Error((data && data.message) || 'Could not open the file.');
    }
    const name = decodeURIComponent(res.headers.get('X-Download-Name') || 'endorsement');
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    if (blob.type === 'application/pdf' || blob.type.startsWith('image/')) window.open(url, '_blank');
    else { const a = document.createElement('a'); a.href = url; a.download = name; a.click(); }
  } catch (err) {
    toast(err.message, false);
  }
}

const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const fmt = d => new Date(d).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' });
const fmtTime = d => new Date(d).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
const pad = n => String(n).padStart(2, '0');
// Date and time are separate boxes and the end date is always shown.
// "Same date as the start" (ticked by default) locks the end date to the start date, so only the end TIME matters.
const Range = {
  html(p, startLabel = 'Start', endLabel = 'End') {
    const today = isoLocal(new Date()).slice(0, 10);
    return `
      <div class="range-row">
        <div><label for="${p}StartDate">${startLabel} date</label><input type="date" id="${p}StartDate" min="${today}" /></div>
        <div><label for="${p}StartTime">${startLabel} time</label><input type="time" id="${p}StartTime" /></div>
      </div>
      <label class="inline-check"><input type="checkbox" id="${p}Same" checked /> ${endLabel} date is the same as the start date</label>
      <div class="range-row">
        <div><label for="${p}EndDate">${endLabel} date</label><input type="date" id="${p}EndDate" min="${today}" disabled /></div>
        <div><label for="${p}EndTime">${endLabel} time</label><input type="time" id="${p}EndTime" /></div>
      </div>`;
  },
  mount(p, startLabel, endLabel) {
    document.querySelector(`[data-range="${p}"]`).innerHTML = this.html(p, startLabel, endLabel);
    this.wire(p);
  },
  wire(p) {
    const same = document.getElementById(p + 'Same');
    const startDate = document.getElementById(p + 'StartDate');
    const endDate = document.getElementById(p + 'EndDate');
    const sync = () => {
      endDate.disabled = same.checked;
      if (same.checked) endDate.value = startDate.value;     // locked to the start date
    };
    same.onchange = sync;
    startDate.oninput = sync;
    startDate.onchange = sync;
    sync();
  },
  set(p, start, end) {
    const s = new Date(start), e = new Date(end);
    document.getElementById(p + 'StartDate').value = isoLocal(s).slice(0, 10);
    document.getElementById(p + 'StartTime').value = isoLocal(s).slice(11, 16);
    document.getElementById(p + 'EndDate').value = isoLocal(e).slice(0, 10);
    document.getElementById(p + 'EndTime').value = isoLocal(e).slice(11, 16);
    document.getElementById(p + 'Same').checked = s.toDateString() === e.toDateString();
    this.wire(p);
  },
  // returns { start: Date, end: Date } or { error }
  read(p) {
    const g = id => document.getElementById(p + id).value;
    if (!g('StartDate')) return { error: 'Please set the start date.' };
    if (!g('StartTime')) return { error: 'Please set the start time.' };
    if (!g('EndTime')) return { error: 'Please set the end time.' };
    const endDate = document.getElementById(p + 'Same').checked ? g('StartDate') : g('EndDate');
    if (!endDate) return { error: 'Please set the end date.' };
    const start = new Date(g('StartDate') + 'T' + g('StartTime'));
    const end = new Date(endDate + 'T' + g('EndTime'));
    if (end <= start) return { error: 'The end must be after the start.' };
    return { start, end };
  }
};

const isoLocal = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`;
const statusTag = s => `<span class="tag ${String(s).toLowerCase()}">${esc(s)}</span>`;
const placesOf = e => e.facilityNames.length ? e.facilityNames.join(', ') : 'Equipment only';

function showMsg(el, text, ok) { el.textContent = text; el.className = 'msg ' + (ok ? 'ok' : 'err'); }

function toast(text, ok = true) {
  const el = document.createElement('div');
  el.className = 'toast' + (ok ? '' : ' err');
  el.textContent = text;
  document.body.appendChild(el);
  setTimeout(() => el.remove(), 4500);
}

// Disables a button while a request is running so a form cannot be submitted twice
async function withBusy(btn, work) {
  const label = btn.textContent;
  btn.disabled = true;
  btn.textContent = 'Please wait…';
  try { return await work(); }
  finally { btn.disabled = false; btn.textContent = label; }
}

function confirmLogout() {
  return new Promise(resolve => {
    const modal = document.createElement('div');
    modal.className = 'logout-modal';
    modal.innerHTML = `
      <div class="logout-dialog" role="dialog" aria-modal="true" aria-labelledby="logoutTitle">
        <div class="logout-icon">!</div>
        <h2 id="logoutTitle">Sign out?</h2>
        <p>Are you sure you want to sign out?</p>
        <div class="logout-actions">
          <button type="button" class="logout-cancel">Cancel</button>
          <button type="button" class="logout-confirm">Sign out</button>
        </div>
      </div>`;
    document.body.appendChild(modal);

    const close = result => { modal.remove(); resolve(result); };
    modal.querySelector('.logout-cancel').onclick = () => close(false);
    modal.querySelector('.logout-confirm').onclick = () => close(true);
    modal.onclick = e => { if (e.target === modal) close(false); };
  });
}

function renderNav(el) {
  const brand = document.querySelector('header.top .brand');
  if (brand && !brand.querySelector('.brand-logo')) {
    brand.insertAdjacentHTML('afterbegin', '<img class="brand-logo" src="/images/angelicum-logo.png" alt="UST Angelicum College logo" />');
  }

  const u = Auth.user;
  if (!u) {
    el.innerHTML = '<a href="/visitor.html">Visitors</a>';
    markActive(el);
    return;
  }

  // Admins only manage events, so they get no Register / Request links
  const dashboard = u.isAdmin ? '' :
    `<div class="nav-dropdown"><a class="nav-dropdown-toggle" href="/dashboard.html">Dashboard ▾</a><div class="nav-dropdown-menu"><a href="/register.html">Register for an Event</a><a href="/request.html">Request an Event</a></div></div>`;
  const myEvents = u.isAdmin ? '' : '<a href="/myevents.html">My Events</a>';
  el.innerHTML = `<a href="/index.html">Bulletin</a>` + myEvents + dashboard +
    (u.isAdmin ? '<a href="/admin.html">Approvals</a>' : '') +
    (u.isSuperAdmin ? '<a href="/superadmin.html">Super admin</a>' : '') +
    `<a href="#" id="logoutLink">Sign out</a>`;
  document.getElementById('logoutLink').onclick = async e => {
    e.preventDefault();
    if (await confirmLogout()) Auth.logout();
  };
  markActive(el);
}

function markActive(el) {
  // My Events and My Registrations are two pages of the same section
  const path = location.pathname === '/myregistrations.html' ? '/myevents.html' : location.pathname;
  for (const a of el.querySelectorAll('a')) {
    if (a.href && new URL(a.href).pathname === path) a.classList.add('active');
  }
}
