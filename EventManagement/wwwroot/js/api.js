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
const isoLocal = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`;
const statusTag = s => `<span class="tag ${String(s).toLowerCase()}">${esc(s)}</span>`;
const placesOf = e => e.facilityNames.length ? e.facilityNames.join(', ') : 'Equipment only';

function showMsg(el, text, ok) { el.textContent = text; el.className = 'msg ' + (ok ? 'ok' : 'err'); }

function toast(text, ok = true) {
  const el = document.createElement('div');
  el.className = 'toast' + (ok ? '' : ' err');
  el.textContent = text;
  document.body.appendChild(el);
  setTimeout(() => el.remove(), 4000);
}

// Disables a button while a request is running so a form cannot be submitted twice
async function withBusy(btn, work) {
  const label = btn.textContent;
  btn.disabled = true;
  btn.textContent = 'Please wait…';
  try { return await work(); }
  finally { btn.disabled = false; btn.textContent = label; }
}

function renderNav(el) {
  const u = Auth.user;
  if (!u) { el.innerHTML = '<a href="/index.html">Home</a>'; return; }
  el.innerHTML = `<a href="/index.html">Bulletin</a><a href="/dashboard.html">Dashboard</a>` +
    (u.isAdmin ? '<a href="/admin.html">Approvals</a>' : '') +
    `<span>${esc(u.fullName)}</span><a href="#" id="logoutLink">Sign out</a>`;
  document.getElementById('logoutLink').onclick = e => { e.preventDefault(); Auth.logout(); };
}
