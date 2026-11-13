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
