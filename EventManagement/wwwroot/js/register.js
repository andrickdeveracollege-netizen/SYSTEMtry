// Works for school accounts (signed in) and for guests (?guest=1, email required).
const guest = new URLSearchParams(location.search).has('guest') && !Auth.token;
if (!guest && !Auth.require()) throw new Error('Not signed in');
if (!guest && Auth.user.isAdmin) { location.href = '/admin.html'; throw new Error('Admins cannot register'); }
renderNav(document.getElementById('nav'));

const form = document.getElementById('form');
const msg = document.getElementById('msg');
const eventSelect = document.getElementById('eventId');
const audience = document.getElementById('audience');
const emailInput = document.getElementById('email');
const nameInput = document.getElementById('fullName');
let events = [];

if (guest) {
  document.getElementById('who').textContent = 'Visitor registration. Please give your email. Only events open to outsiders will accept you.';
  document.getElementById('backLink').href = '/visitor.html';
} else {
  const u = Auth.user;
  const kind = u.position === 'Student' ? `Student (${u.level})` : u.position === 'Administrator' ? 'Administrator' : 'Faculty / staff';
  document.getElementById('who').textContent = `Registering as ${kind}. Your details come from your account.`;
  emailInput.value = u.email;
  nameInput.value = u.fullName;
  emailInput.readOnly = true;
  nameInput.readOnly = true;
}

function showAudience() {
  const ev = events.find(e => e.id === Number(eventSelect.value));
  audience.textContent = ev
    ? `Open to: ${ev.targetAudience}${ev.audienceDetail ? ' (' + ev.audienceDetail + ')' : ''} · ${Math.max(0, ev.capacity - ev.registered)} of ${ev.capacity} seats left`
    : '';
}
eventSelect.onchange = showAudience;

(async () => {
  const all = await api('/api/events/bulletin');
  events = all.filter(e => e.requiresRegistration);      // institutional events need no registration
  eventSelect.innerHTML = events.length
    ? events.map(e => `<option value="${e.id}">${esc(e.title)} — ${fmt(e.startTime)} @ ${esc(placesOf(e))}</option>`).join('')
    : '<option value="">No open events</option>';
  const pre = new URLSearchParams(location.search).get('eventId');
  if (pre) eventSelect.value = pre;
  showAudience();
})().catch(err => showMsg(msg, err.message, false));

function validate() {
  if (!eventSelect.value) return 'Please choose an event.';
  if (!emailInput.value.trim()) return 'Please enter your email.';
  if (!nameInput.value.trim()) return 'Please enter your full name.';
  if (!document.getElementById('confirmed').checked) return 'Please tick the confirmation box.';
  return null;
}

form.addEventListener('submit', async e => {
  e.preventDefault();
  const problem = validate();
  if (problem) { showMsg(msg, problem, false); return; }
  try {
    const r = await withBusy(document.getElementById('submitBtn'), () => api(guest ? '/api/guest/registrations' : '/api/registrations', {
      method: 'POST',
      body: {
        eventId: Number(eventSelect.value),
        email: emailInput.value.trim(),
        fullName: nameInput.value.trim(),
        confirmed: document.getElementById('confirmed').checked
      }
    }));
    showMsg(msg, r.message, true);
    document.getElementById('confirmed').checked = false;
  } catch (err) {
    showMsg(msg, err.message, false);
  }
});
