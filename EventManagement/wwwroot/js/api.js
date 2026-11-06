const Auth = {
  get token() { return localStorage.getItem('em_token'); },
  get user() { try { return JSON.parse(localStorage.getItem('em_user') || 'null'); } catch { return null; } },
  save(r) { localStorage.setItem('em_token', r.token); localStorage.setItem('em_user', JSON.stringify(r)); },
  clear() { localStorage.removeItem('em_token'); localStorage.removeItem('em_user'); },
  require(adminOnly = false) {
    if (!this.token) { location.href = '/index.html?signin=1'; return false; }
    if (adminOnly && !(this.user && this.user.isAdmin)) { location.href = '/dashboard.html'; return false; }
    return true;
  },
  logout() { this.clear(); location.href = '/index.html'; }
};

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
  try { data = text ? JSON.parse(text) : null; } catch { /* not json */ }
  if (!res.ok) {
    let msg = (data && (data.message || data.title)) || 'Request failed (' + res.status + ')';
    if (data && data.errors) msg = Object.values(data.errors).flat().join(' ');
    throw new Error(msg);
  }
  return data;
}

async function openEndorsement(id) {
  const res = await fetch(`/api/admin/requests/${id}/endorsement`, { headers: { Authorization: 'Bearer ' + Auth.token } });
  if (!res.ok) { let m = 'Could not open file.'; try { m = (await res.json()).message || m; } catch {} alert(m); return; }
  const name = decodeURIComponent(res.headers.get('X-Download-Name') || 'endorsement');
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  if (blob.type === 'application/pdf' || blob.type.startsWith('image/')) window.open(url, '_blank');
  else { const a = document.createElement('a'); a.href = url; a.download = name; a.click(); }
}

const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const fmt = d => new Date(d).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' });
const fmtTime = d => new Date(d).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
const pad = n => String(n).padStart(2, '0');
const isoLocal = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`;

function showMsg(el, text, ok) { el.textContent = text; el.className = 'msg ' + (ok ? 'ok' : 'err'); }

function renderNav(el) {
  const brand = document.querySelector('header.top .brand');
  if (brand && !brand.querySelector('.brand-logo')) {
    brand.insertAdjacentHTML('afterbegin', '<img class="brand-logo" src="/images/angelicum-logo.png" alt="UST Angelicum College logo" />');
  }
  const u = Auth.user;
  if (!u) { el.innerHTML = '<a href="/index.html">Home</a>'; return; }
  el.innerHTML = `<a href="/index.html">Bulletin</a>` +
    `<div class="nav-dropdown"><a class="nav-dropdown-toggle" href="/dashboard.html">Dashboard ▾</a><div class="nav-dropdown-menu"><a href="/register.html">Register for an Event</a><a href="/request.html">Request an Event</a></div></div>` +
    (u.isAdmin ? '<a href="/admin.html">Approvals</a>' : '') +
    `<a href="#" id="logoutLink">Sign out</a>`;
  document.getElementById('logoutLink').onclick = async e => {
    e.preventDefault();
    if (await confirmLogout()) Auth.logout();
  };
  for (const a of el.querySelectorAll('a')) {
    if (a.href && new URL(a.href).pathname === location.pathname) a.classList.add('active');
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
}