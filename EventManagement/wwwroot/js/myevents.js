if (!Auth.require()) throw new Error('Not signed in');
if (Auth.user.isAdmin) { location.href = '/admin.html'; throw new Error('Admins use the approvals page'); }
renderNav(document.getElementById('nav'));
const myEvents = document.getElementById('myEvents');
const editModal = document.getElementById('editModal');
const editForm = document.getElementById('editForm');
const editMsg = document.getElementById('editMsg');
const moveModal = document.getElementById('moveModal');
const moveForm = document.getElementById('moveForm');
const moveMsg = document.getElementById('moveMsg');
Range.mount('mv', 'New start', 'End');
let mine = [];
let editingId = null;
let movingId = null;

// ---------- My event requests (organizer): see registrations, edit, move, cancel ----------
function eventRow(d) {
  const e = d.event;
  const open = e.status === 'Pending' || e.status === 'Approved';
  const offices = d.departments.map(x =>
    `<div class="muted">${esc(x.office)}: ${statusTag(x.status)}${x.rejectionReason ? ' — ' + esc(x.rejectionReason) : ''}</div>
     ${x.note ? '<div class="hint-box" style="margin:4px 0"><b>Note from ' + esc(x.office) + ':</b> ' + esc(x.note) + '</div>' : ''}`).join('');
  const seats = e.requiresRegistration
    ? `<b>${e.registered}</b> of ${e.capacity} registered`
    : 'Institutional event · no registration';
  return `<div class="req">
    <strong>${esc(e.title)}</strong> ${statusTag(e.status)}<br>
    <span class="muted">${fmt(e.startTime)} · ${seats}</span>
    <div class="muted">${esc(placesOf(e))}</div>
    ${offices}
    <div class="row">
      ${e.requiresRegistration && e.status !== 'Rejected' ? `<button class="btn" data-regs="${e.id}" type="button">Registered (${e.registered})</button>` : ''}
      ${e.status === 'Pending' ? `<button class="btn ghost" data-edit="${e.id}" type="button">Edit</button>` : ''}
      ${open ? `<button class="btn ghost" data-move="${e.id}" type="button">Move</button>` : ''}
      ${open ? `<button class="btn bad" data-cancel="${e.id}" type="button">Cancel event</button>` : ''}
    </div></div>`;
}

async function loadMyEvents() {
  try {
    mine = await api('/api/events/mine');
    myEvents.innerHTML = mine.length ? mine.map(eventRow).join('')
      : '<div class="empty-state">You have not requested any event yet.</div>';
  } catch (err) {
    myEvents.innerHTML = `<div class="empty-state">${esc(err.message)}</div>`;
  }
}

myEvents.addEventListener('click', async ev => {
  const { edit, move, cancel, regs } = ev.target.dataset;
  if (regs) { openRegistrants(`/api/events/${regs}/registrations`, `/api/events/${regs}/registrations/export`); return; }
  if (edit) openEdit(Number(edit));
  if (move) openMove(Number(move));
  if (cancel && confirm('Cancel this event? Its booked slots will be released.')) {
    try {
      await withBusy(ev.target, () => api('/api/events/' + cancel, { method: 'DELETE' }));
      toast('Event cancelled.');
      await loadMyEvents();
    } catch (err) { toast(err.message, false); }
  }
});

function openEdit(id) {
  const d = mine.find(x => x.event.id === id);
  editingId = id;
  document.getElementById('editTitle').value = d.event.title;
  document.getElementById('editPurpose').value = d.event.purpose;
  document.getElementById('editCapacity').value = d.event.capacity;
  editMsg.className = 'msg';
  editModal.classList.add('open');
}
document.getElementById('editCancel').onclick = () => editModal.classList.remove('open');

editForm.addEventListener('submit', async e => {
  e.preventDefault();
  const title = document.getElementById('editTitle').value.trim();
  const purpose = document.getElementById('editPurpose').value.trim();
  const capacity = Number(document.getElementById('editCapacity').value);
  if (!title || !purpose) { showMsg(editMsg, 'Title and purpose are required.', false); return; }
  if (!(capacity >= 1 && capacity <= 10000)) { showMsg(editMsg, 'Number of attendees must be between 1 and 10,000.', false); return; }
  try {
    await withBusy(document.getElementById('editSave'),
      () => api('/api/events/' + editingId, { method: 'PUT', body: { title, purpose, capacity } }));
    editModal.classList.remove('open');
    toast('Request updated.');
    loadMyEvents();
  } catch (err) { showMsg(editMsg, err.message, false); }
});

// ---------- Move (reschedule) ----------
function openMove(id) {
  const d = mine.find(x => x.event.id === id);
  movingId = id;
  Range.set('mv', d.event.startTime, d.event.endTime);
  moveMsg.className = 'msg';
  moveModal.classList.add('open');
}
document.getElementById('moveCancel').onclick = () => moveModal.classList.remove('open');

moveForm.addEventListener('submit', async e => {
  e.preventDefault();
  const range = Range.read('mv');
  if (range.error) { showMsg(moveMsg, range.error, false); return; }
  const start = range.start, end = range.end;
  if (start < new Date()) { showMsg(moveMsg, 'The new date must be in the future.', false); return; }
  try {
    await withBusy(document.getElementById('moveSave'), () => api(`/api/events/${movingId}/reschedule`, {
      method: 'PUT',
      body: { startTime: isoLocal(start), endTime: isoLocal(end) }
    }));
    moveModal.classList.remove('open');
    toast('Event moved. The offices will review the new date.');
    loadMyEvents();
  } catch (err) { showMsg(moveMsg, err.message, false); }   // e.g. 409 "Already booked for this time: Gym"
});


loadMyEvents();
