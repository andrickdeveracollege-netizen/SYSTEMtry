if (!Auth.require()) throw new Error('Not signed in');
renderNav(document.getElementById('nav'));
document.getElementById('hello').textContent = 'Welcome, ' + Auth.user.fullName;

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
let mine = [];
let editingId = null;

// ---------- My event requests (organizer) ----------
function eventRow(d) {
  const e = d.event;
  const canEdit = e.status === 'Pending';
  const canCancel = e.status === 'Pending' || e.status === 'Approved';
  const offices = d.departments.map(x =>
    `<div class="muted">${esc(x.office)}: ${statusTag(x.status)}${x.rejectionReason ? ' — ' + esc(x.rejectionReason) : ''}</div>`).join('');
  return `<div class="req">
    <strong>${esc(e.title)}</strong> ${statusTag(e.status)}<br>
    <span class="muted">${fmt(e.startTime)} · ${e.registered}/${e.capacity} registered</span>
    ${offices}
    <div class="row">
      ${canEdit ? `<button class="btn ghost" data-edit="${e.id}" type="button">Edit</button>` : ''}
      ${canCancel ? `<button class="btn bad" data-cancel="${e.id}" type="button">Cancel event</button>` : ''}
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
  const editId = ev.target.dataset.edit;
  const cancelId = ev.target.dataset.cancel;
  if (editId) openEdit(Number(editId));
  if (cancelId && confirm('Cancel this event? Its booked slots will be released.')) {
    try {
      await withBusy(ev.target, () => api('/api/events/' + cancelId, { method: 'DELETE' }));
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
  if (!(capacity >= 1 && capacity <= 10000)) { showMsg(editMsg, 'Capacity must be between 1 and 10,000.', false); return; }
  try {
    await withBusy(document.getElementById('editSave'),
      () => api('/api/events/' + editingId, { method: 'PUT', body: { title, purpose, capacity } }));
    editModal.classList.remove('open');
    toast('Request updated.');
    loadMyEvents();
    calendar.refresh();
  } catch (err) { showMsg(editMsg, err.message, false); }
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

loadMyEvents();
loadMyRegs();
