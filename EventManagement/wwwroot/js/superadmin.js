if (!Auth.require(true) || !Auth.user.isSuperAdmin) { location.href = '/index.html'; throw new Error('Super admin only'); }
renderNav(document.getElementById('nav'));

const OFFICE_LABEL = { Auxiliary: 'Auxiliary', ITMC: 'ITMC', College: 'College', TVET: 'TVET', Library: 'Library' };
Range.mount('i');
const instForm = document.getElementById('instForm');
const iMsg = document.getElementById('iMsg');
const usersBox = document.getElementById('users');
let allUsers = [];

// ---------- institutional event ----------
api('/api/facilities').then(facs => {
  const groups = {};
  facs.filter(f => !f.isOther).forEach(f => (groups[f.department] ??= []).push(f));
  document.getElementById('iFacs').innerHTML = Object.entries(groups).map(([d, list]) => `
    <div style="margin-bottom:8px"><b>${esc(OFFICE_LABEL[d] || d)}</b>
      <div class="fac-list">${list.map(f => `<label class="fac-item"><input type="checkbox" class="ifac" value="${f.id}" /> ${esc(f.name)}${f.capacity ? ` <span class="muted">(up to ${f.capacity})</span>` : ''}</label>`).join('')}</div>
    </div>`).join('');
}).catch(err => showMsg(iMsg, err.message, false));

instForm.addEventListener('submit', async e => {
  e.preventDefault();
  const g = id => document.getElementById(id).value.trim();
  const facs = [...document.querySelectorAll('.ifac:checked')].map(c => Number(c.value));
  const aud = [...document.querySelectorAll('.iaud:checked')].reduce((s, c) => s + Number(c.value), 0);
  if (!g('iTitle') || !g('iOrg') || !g('iPurpose')) { showMsg(iMsg, 'Please fill in the title, organization and purpose.', false); return; }
  if (!aud) { showMsg(iMsg, 'Select at least one audience.', false); return; }
  const range = Range.read('i');
  if (range.error) { showMsg(iMsg, range.error, false); return; }
  if (!facs.length) { showMsg(iMsg, 'Pick at least one facility.', false); return; }
  try {
    await withBusy(document.getElementById('iBtn'), () => api('/api/superadmin/events', {
      method: 'POST',
      body: {
        title: g('iTitle'), organization: g('iOrg'), purpose: g('iPurpose'), targetAudience: aud, audienceDetail: g('iDetail') || null,
        startTime: isoLocal(range.start), endTime: isoLocal(range.end), facilityIds: facs
      }
    }));
    showMsg(iMsg, 'School event posted. It is on the bulletin now.', true);
    instForm.reset();
  } catch (err) { showMsg(iMsg, err.message, false); }
});

// ---------- users ----------
function userRow(u) {
  const isAdminAcct = u.role !== 'User';
  const pos = isAdminAcct ? `<span class="muted">${esc(u.role)}${u.department ? ' · ' + esc(u.department) : ''}</span>`
    : u.position === 'Student' ? `<span class="muted">Student · ${esc(u.level)}</span>`
    : `<select data-pos="${u.id}">
         <option value="Faculty"${u.position === 'Faculty' ? ' selected' : ''}>Faculty / staff</option>
         <option value="Administrator"${u.position === 'Administrator' ? ' selected' : ''}>Administrator</option>
       </select>`;
  return `<div class="req"><strong>${esc(u.fullName)}</strong> <span class="muted">${esc(u.email)}</span><br>
    ${pos} <button class="btn ghost" data-reset="${u.id}" type="button">Reset password</button></div>`;
}

function renderUsers() {
  const term = document.getElementById('search').value.trim().toLowerCase();
  const rows = allUsers.filter(u => !term || u.email.toLowerCase().includes(term) || u.fullName.toLowerCase().includes(term));
  usersBox.innerHTML = rows.length ? rows.map(userRow).join('') : '<div class="empty-state">No users found.</div>';
}

api('/api/superadmin/users').then(list => { allUsers = list; renderUsers(); })
  .catch(err => { usersBox.innerHTML = `<div class="empty-state">${esc(err.message)}</div>`; });

document.getElementById('search').addEventListener('input', renderUsers);

usersBox.addEventListener('click', async e => {
  const id = e.target.dataset.reset;
  if (!id) return;
  const u = allUsers.find(x => x.id === id);
  if (!confirm(`Reset the password of ${u.email} to the default password?`)) return;
  try {
    const r = await withBusy(e.target, () => api(`/api/superadmin/users/${id}/reset-password`, { method: 'POST' }));
    toast(r.message);
  } catch (err) { toast(err.message, false); }
});

usersBox.addEventListener('change', async e => {
  const id = e.target.dataset.pos;
  if (!id) return;
  try {
    const r = await api(`/api/superadmin/users/${id}/position`, { method: 'PUT', body: { position: e.target.value } });
    allUsers.find(x => x.id === id).position = e.target.value;
    toast(r.message);
  } catch (err) { toast(err.message, false); renderUsers(); }
});
