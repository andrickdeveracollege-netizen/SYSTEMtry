if (!Auth.require(true)) throw new Error('Admins only');
renderNav(document.getElementById('nav'));
// Super admin picks an office; office admins are fixed to their own.
const dept = new URLSearchParams(location.search).get('dept') || 'Auxiliary';
const officeSel = document.getElementById('officeSel');
const scopeQ = Auth.user.isSuperAdmin ? 'dept=' + encodeURIComponent(officeSel.value = dept) : '';
document.getElementById('office').textContent = Auth.user.isSuperAdmin
  ? 'Approvals — all offices' : 'Approvals — ' + Auth.user.department + ' office';
if (Auth.user.isSuperAdmin) {
  document.getElementById('officePick').style.display = 'block';
  officeSel.onchange = () => { location.search = '?dept=' + officeSel.value; };
}
const q = (path, extra = '') => path + (path.includes('?') ? '&' : '?') + [extra, scopeQ].filter(Boolean).join('&');

const list = document.getElementById('list');
const rejectModal = document.getElementById('rejectModal');
const rejectMsg = document.getElementById('rejectMsg');
const reasonInput = document.getElementById('reason');
let tab = 'Pending';
let rejectingId = null;

function requestCard(r) {
  const equipment = r.equipment.length
    ? r.equipment.map(x => `${esc(x.name)} × ${x.quantity}${x.notes ? ' (' + esc(x.notes) + ')' : ''}`).join(', ')
    : '—';
  const letter = r.hasEndorsement
    ? `<button class="btn ghost" data-letter="${r.id}" type="button">View letter${r.endorsementFileName ? ' (' + esc(r.endorsementFileName) + ')' : ''}</button>`
    : `<span class="muted">${r.endorsementOptional ? 'No letter (optional for this office)' : 'No letter uploaded'}</span>`;
  const actions = r.status === 'Pending'
    ? `<button class="btn ok" data-approve="${r.id}" type="button">Approve</button>
       <button class="btn bad" data-reject="${r.id}" type="button">Reject</button>` : '';
  const cancel = (r.status === 'Pending' || r.status === 'Approved')
    ? `<button class="btn bad" data-cancel="${r.eventId}" type="button">Cancel event</button>` : '';
  return `<div class="req">
    <strong>${esc(r.title)}</strong> ${statusTag(r.status)}${r.role === 'Guest' ? ' <span class="tag guest">Outsider</span>' : ''}<br>
    <span class="muted">${fmt(r.startTime)} – ${fmtTime(r.endTime)} · ${esc(r.organization)} · ${esc(r.role)} · for ${esc(r.targetAudience)}${r.audienceDetail ? ' (' + esc(r.audienceDetail) + ')' : ''}</span>
    <p>${esc(r.purpose)}</p>
    <div><b>Requester:</b> ${esc(r.requesterName)} (${esc(r.requesterEmail)})</div>
    <div><b>Facilities:</b> ${r.facilities.length ? r.facilities.map(esc).join(', ') : '—'}</div>
    <div><b>Equipment:</b> ${equipment}</div>
    ${r.rejectionReason ? `<div><b>Reason:</b> ${esc(r.rejectionReason)}</div>` : ''}
    <label for="note-${r.id}">Note for the requester (any status)</label>
    <textarea id="note-${r.id}" maxlength="1000" rows="2" placeholder="e.g. Please bring your own extension cord.">${esc(r.note || '')}</textarea>
    <div class="row">${letter}${actions}${cancel}<button class="btn ghost" data-note="${r.id}" type="button">Save note</button></div>
  </div>`;
}

async function loadRequests() {
  const rows = await api(q('/api/admin/requests?status=' + tab));
  list.innerHTML = rows.length ? rows.map(requestCard).join('')
    : `<div class="empty-state">No ${tab.toLowerCase()} requests.</div>`;
}

// Report: seats taken vs capacity per event (ticket report)
async function loadReport() {
  const rows = await api(q('/api/admin/reports/events'));
  const total = rows.reduce((s, r) => s + r.registered, 0);
  list.innerHTML = rows.length
    ? `<div class="card"><h2>Registrations per event</h2>
       <p class="muted">${rows.length} events · ${total} registrations in total</p>
       <table style="width:100%;border-collapse:collapse">
         <tr style="text-align:left"><th>Event</th><th>Date</th><th>Your part</th><th>Event</th><th>Registered</th><th>Capacity</th><th>Left</th></tr>
         ${rows.map(r => `<tr style="border-top:1px solid var(--line)">
           <td>${esc(r.title)}</td><td>${fmt(r.startTime)}</td><td>${statusTag(r.departmentStatus)}</td>
           <td>${statusTag(r.eventStatus)}</td><td>${r.registered}</td><td>${r.capacity}</td><td>${r.remaining}</td></tr>`).join('')}
       </table></div>`
    : '<div class="empty-state">No events involve your office yet.</div>';
}

async function load() {
  list.innerHTML = '<div class="spinner">Loading…</div>';
  document.querySelectorAll('#tabs button').forEach(b => { b.className = 'btn' + (b.dataset.tab === tab ? '' : ' ghost'); });
  try {
    if (tab === 'Report') await loadReport();
    else await loadRequests();
  } catch (err) {
    list.innerHTML = `<div class="empty-state">${esc(err.message)}</div>`;
  }
}

document.getElementById('tabs').addEventListener('click', e => {
  if (!e.target.dataset.tab) return;
  tab = e.target.dataset.tab;
  load();
});

list.addEventListener('click', async e => {
  const { approve, reject, letter, cancel, note } = e.target.dataset;
  if (letter) { openEndorsement(letter); return; }
  if (note) {
    try {
      await withBusy(e.target, () => api(`/api/admin/requests/${note}/note`, {
        method: 'PUT', body: { note: document.getElementById('note-' + note).value }
      }));
      toast('Note saved. The requester can see it.');
    } catch (err) { toast(err.message, false); }
    return;
  }
  if (cancel) {
    if (!confirm('Cancel this whole event? All offices and registrations are affected.')) return;
    try {
      await withBusy(e.target, () => api(`/api/admin/events/${cancel}`, { method: 'DELETE' }));
      toast('Event cancelled.');
      load();
    } catch (err) { toast(err.message, false); }
    return;
  }
  if (reject) {
    rejectingId = reject;
    reasonInput.value = '';
    rejectMsg.className = 'msg';
    rejectModal.classList.add('open');
    return;
  }
  if (approve) {
    try {
      await withBusy(e.target, () => api(`/api/admin/requests/${approve}/approve`, { method: 'POST' }));
      toast('Request approved.');
      load();
    } catch (err) {
      toast(err.message, false);   // e.g. 409 "Another approved event already uses: Gym"
    }
  }
});

document.getElementById('closeReject').onclick = () => rejectModal.classList.remove('open');
document.getElementById('confirmReject').onclick = async e => {
  const reason = reasonInput.value.trim();
  if (!reason) { showMsg(rejectMsg, 'Please give a reason.', false); return; }
  try {
    await withBusy(e.target, () => api(`/api/admin/requests/${rejectingId}/reject`, { method: 'POST', body: { reason } }));
    rejectModal.classList.remove('open');
    toast('Request rejected.');
    load();
  } catch (err) { showMsg(rejectMsg, err.message, false); }
};

load();
