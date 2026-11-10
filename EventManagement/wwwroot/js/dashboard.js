if (!Auth.require()) throw new Error('Not signed in');
if (Auth.user.isAdmin) { location.href = '/admin.html'; throw new Error('Admins use the approvals page'); }
renderNav(document.getElementById('nav'));
document.getElementById('hello').textContent = 'Welcome, ' + Auth.user.fullName;
document.getElementById('whoami').textContent = describeUser(Auth.user);

function describeUser(u) {
  if (u.position === 'Student') return 'Student · ' + u.level;
  if (u.position === 'Administrator') return 'Administrator';
  return 'Faculty / staff';
}

const calendar = initCalendar({
  grid: document.getElementById('cal'),
  title: document.getElementById('monthTitle'),
  filter: document.getElementById('facFilter'),
  dayList: document.getElementById('dayList'),
  prev: document.getElementById('prev'),
  next: document.getElementById('next'),
  today: document.getElementById('today')
});

const myEvents = document.getElementById('myEvents');
const myRegs = document.getElementById('myRegs');
const editModal = document.getElementById('editModal');
const editForm = document.getElementById('editForm');
const editMsg = document.getElementById('editMsg');
const moveModal = document.getElementById('moveModal');
const moveForm = document.getElementById('moveForm');
const moveMsg = document.getElementById('moveMsg');
let mine = [];
let editingId = null;
let movingId = null;

// ---------- My event requests (organizer): see registrations, edit, move, cancel ----------
function eventRow(d) {
  const e = d.event;
  const open = e.status === 'Pending' || e.status === 'Approved';
  const offices = d.departments.map(x =>
    `<div class="muted">${esc(x.office)}: ${statusTag(x.status)}${x.rejectionReason ? ' — ' + esc(x.rejectionReason) : ''}</div>`).join('');
  const seats = e.requiresRegistration
    ? `<b>${e.registered}</b> of ${e.capacity} registered`
    : 'Institutional event · no registration';
  return `<div class="req">
    <strong>${esc(e.title)}</strong> ${statusTag(e.status)}<br>
    <span class="muted">${fmt(e.startTime)} · ${seats}</span>
    <div class="muted">${esc(placesOf(e))}</div>
    ${offices}
    <div class="row">
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
  const { edit, move, cancel } = ev.target.dataset;
  if (edit) openEdit(Number(edit));
  if (move) openMove(Number(move));
  if (cancel && confirm('Cancel this event? Its booked slots will be released.')) {
    try {
      await withBusy(ev.target, () => api('/api/events/' + cancel, { method: 'DELETE' }));
      toast('Event cancelled.');
      await loadMyEvents();
      calendar.refresh();
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
  document.getElementById('moveStart').value = isoLocal(new Date(d.event.startTime)).slice(0, 16);
  document.getElementById('moveEnd').value = isoLocal(new Date(d.event.endTime)).slice(0, 16);
  moveMsg.className = 'msg';
  moveModal.classList.add('open');
}
document.getElementById('moveCancel').onclick = () => moveModal.classList.remove('open');

moveForm.addEventListener('submit', async e => {
  e.preventDefault();
  const start = document.getElementById('moveStart').value;
  const end = document.getElementById('moveEnd').value;
  if (!start || !end) { showMsg(moveMsg, 'Please set the new start and end time.', false); return; }
  if (new Date(end) <= new Date(start)) { showMsg(moveMsg, 'End time must be after the start time.', false); return; }
  if (new Date(start) < new Date()) { showMsg(moveMsg, 'The new date must be in the future.', false); return; }
  try {
    await withBusy(document.getElementById('moveSave'), () => api(`/api/events/${movingId}/reschedule`, {
      method: 'PUT',
      body: { startTime: isoLocal(new Date(start)), endTime: isoLocal(new Date(end)) }
    }));
    moveModal.classList.remove('open');
    toast('Event moved. The offices will review the new date.');
    loadMyEvents();
    calendar.refresh();
  } catch (err) { showMsg(moveMsg, err.message, false); }   // e.g. 409 "Already booked for this time: Gym"
});

// ---------- My registrations (attendee) ----------
async function loadMyRegs() {
  try {
    const list = await api('/api/registrations/mine');
    myRegs.innerHTML = list.length
      ? list.map(r => `<div class="mini-row">
          <div><strong>${esc(r.eventTitle)}</strong><br><span class="muted">${fmt(r.startTime)}</span></div>
          ${r.cancelled ? statusTag('Cancelled')
            : `<button class="btn ghost" data-reg="${r.id}" type="button">Cancel</button>`}
        </div>`).join('')
      : '<div class="empty-state">You have not registered for any event.</div>';
  } catch (err) {
    myRegs.innerHTML = `<div class="empty-state">${esc(err.message)}</div>`;
  }
}

myRegs.addEventListener('click', async ev => {
  const id = ev.target.dataset.reg;
  if (!id || !confirm('Cancel your registration?')) return;
  try {
    await withBusy(ev.target, () => api(`/api/registrations/${id}/cancel`, { method: 'POST' }));
    toast('Registration cancelled.');
    loadMyRegs();
  } catch (err) { toast(err.message, false); }
});

// ---------- Change password ----------
document.getElementById('pwForm').addEventListener('submit', async e => {
  e.preventDefault();
  const msg = document.getElementById('pwMsg');
  const currentPassword = document.getElementById('curPw').value;
  const newPassword = document.getElementById('newPw').value;
  if (!currentPassword) { showMsg(msg, 'Enter your current password.', false); return; }
  if (newPassword.length < 8) { showMsg(msg, 'New password must be at least 8 characters.', false); return; }
  try {
    const r = await withBusy(document.getElementById('pwSave'),
      () => api('/api/auth/change-password', { method: 'POST', body: { currentPassword, newPassword } }));
    showMsg(msg, r.message, true);
    e.target.reset();
  } catch (err) { showMsg(msg, err.message, false); }
});

loadMyEvents();
loadMyRegs();
