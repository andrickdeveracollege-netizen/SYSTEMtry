if (!Auth.require()) throw new Error('Not signed in');
renderNav(document.getElementById('nav'));

const form = document.getElementById('form');
const msg = document.getElementById('msg');
const eventSelect = document.getElementById('eventId');
const audience = document.getElementById('audience');
document.getElementById('email').value = Auth.user.email;
document.getElementById('fullName').value = Auth.user.fullName;
let events = [];

function showAudience() {
  const ev = events.find(e => e.id === Number(eventSelect.value));
  audience.textContent = ev
    ? `Open to: ${ev.targetAudience} · ${Math.max(0, ev.capacity - ev.registered)} of ${ev.capacity} seats left`
    : '';
}
eventSelect.onchange = showAudience;

(async () => {
  events = await api('/api/events/bulletin');
  eventSelect.innerHTML = events.length
    ? events.map(e => `<option value="${e.id}">${esc(e.title)} — ${fmt(e.startTime)} @ ${esc(placesOf(e))}</option>`).join('')
    : '<option value="">No open events</option>';
  const pre = new URLSearchParams(location.search).get('eventId');
  if (pre) eventSelect.value = pre;
  showAudience();
})().catch(err => showMsg(msg, err.message, false));

function validate() {
  if (!eventSelect.value) return 'Please choose an event.';
  if (!document.getElementById('email').value.trim()) return 'Please enter your email.';
  if (!document.getElementById('fullName').value.trim()) return 'Please enter your full name.';
  if (!document.getElementById('yearLevel').value) return 'Please select your year level.';
  if (!document.getElementById('confirmed').checked) return 'Please tick the confirmation box.';
  return null;
}

form.addEventListener('submit', async e => {
  e.preventDefault();
  const problem = validate();
  if (problem) { showMsg(msg, problem, false); return; }
  try {
    const r = await withBusy(document.getElementById('submitBtn'), () => api('/api/registrations', {
      method: 'POST',
      body: {
        eventId: Number(eventSelect.value),
        email: document.getElementById('email').value.trim(),
        fullName: document.getElementById('fullName').value.trim(),
        yearLevel: document.getElementById('yearLevel').value,
        confirmed: document.getElementById('confirmed').checked
      }
    }));
    showMsg(msg, r.message, true);
    document.getElementById('confirmed').checked = false;
  } catch (err) {
    showMsg(msg, err.message, false);
  }
});
