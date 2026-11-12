renderNav(document.getElementById('nav'));

const manageForm = document.getElementById('manageForm');
const mMsg = document.getElementById('mMsg');
const manageBox = document.getElementById('manageBox');
let current = null;   // { id, email, code }

// ---------- upcoming events (open to outsiders flagged) ----------
api('/api/events/bulletin').then(list => {
  document.getElementById('events').innerHTML = list.length
    ? list.map(e => `<div class="req"><strong>${esc(e.title)}</strong>
        ${e.targetAudience.includes('Outsider') ? '<span class="tag approved">Open to outsiders</span>' : ''}
        ${!e.requiresRegistration ? '<span class="tag institutional">School event</span>' : ''}<br>
        <span class="muted">${fmt(e.startTime)} · ${esc(placesOf(e))}</span></div>`).join('')
    : '<div class="empty-state">No upcoming events.</div>';
}).catch(err => { document.getElementById('events').innerHTML = `<div class="empty-state">${esc(err.message)}</div>`; });

// ---------- manage a guest request ----------
function query() { return `email=${encodeURIComponent(current.email)}&code=${encodeURIComponent(current.code)}`; }

function renderManage(d) {
  const e = d.event;
  const open = e.status === 'Pending' || e.status === 'Approved';
  manageBox.innerHTML = `<div class="req">
    <strong>${esc(e.title)}</strong> ${statusTag(e.status)}<br>
    <span class="muted">${fmt(e.startTime)} – ${fmtTime(e.endTime)} · ${esc(placesOf(e))} · ${e.registered}/${e.capacity} registered</span>
    ${d.departments.map(x => `<div class="muted">${esc(x.office)}: ${statusTag(x.status)}${x.rejectionReason ? ' — ' + esc(x.rejectionReason) : ''}</div>
      ${x.note ? '<div class="hint-box" style="margin:4px 0"><b>Note from ' + esc(x.office) + ':</b> ' + esc(x.note) + '</div>' : ''}`).join('')}
    ${open ? `
    <hr style="border:0;border-top:1px solid var(--line);margin:12px 0" />
    <label for="gStart">Move to (weekend only)</label><input type="datetime-local" id="gStart" />
    <label for="gEnd">Until</label><input type="datetime-local" id="gEnd" />
    <div class="msg" id="gMsg"></div>
    <div class="row">
      <button class="btn ghost" id="gMove" type="button">Move event</button>
      <button class="btn bad" id="gCancel" type="button">Cancel event</button>
    </div>` : ''}
  </div>`;

  if (!open) return;
  const gMsg = document.getElementById('gMsg');
  document.getElementById('gMove').onclick = async ev => {
    const start = document.getElementById('gStart').value, end = document.getElementById('gEnd').value;
    if (!start || !end) { showMsg(gMsg, 'Please set the new start and end time.', false); return; }
    if (new Date(end) <= new Date(start)) { showMsg(gMsg, 'End time must be after the start time.', false); return; }
    try {
      const r = await withBusy(ev.target, () => api(`/api/guest/events/${current.id}/reschedule?${query()}`, {
        method: 'PUT', body: { startTime: isoLocal(new Date(start)), endTime: isoLocal(new Date(end)) }
      }));
      toast('Event moved. The offices will review the new date.');
      renderManage(r);
    } catch (err) { showMsg(gMsg, err.message, false); }
  };
  document.getElementById('gCancel').onclick = async ev => {
    if (!confirm('Cancel this event?')) return;
    try {
      await withBusy(ev.target, () => api(`/api/guest/events/${current.id}?${query()}`, { method: 'DELETE' }));
      toast('Event cancelled.');
      manageBox.innerHTML = '';
    } catch (err) { showMsg(gMsg, err.message, false); }
  };
}

manageForm.addEventListener('submit', async e => {
  e.preventDefault();
  const id = Number(document.getElementById('mId').value);
  const email = document.getElementById('mEmail').value.trim();
  const code = document.getElementById('mCode').value.trim();
  if (!id || !email || !code) { showMsg(mMsg, 'Please fill in the event number, email and manage code.', false); return; }
  current = { id, email, code };
  try {
    const d = await withBusy(document.getElementById('mFind'), () => api(`/api/guest/events/${id}?${query()}`));
    mMsg.className = 'msg';
    renderManage(d);
  } catch (err) { manageBox.innerHTML = ''; showMsg(mMsg, err.message, false); }
});
