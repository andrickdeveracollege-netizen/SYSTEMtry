renderNav(document.getElementById('nav'));

// Signed-in users get the calendar on their dashboard, so the home page shows it to visitors only
const signedInUser = !!(Auth.token && Auth.user);
const calSection = document.querySelector('.col-cal');
if (signedInUser) {
  calSection.style.display = 'none';
} else {
  initCalendar({
    grid: document.getElementById('cal'),
    title: document.getElementById('monthTitle'),
    filter: document.getElementById('facFilter'),
    dayList: document.getElementById('dayList'),
    prev: document.getElementById('prev'),
    next: document.getElementById('next'),
    today: document.getElementById('today')
  });
}

// ---------- Sign-in box: signed in vs signed out ----------
const authForm = document.getElementById('authForm');
const authMsg = document.getElementById('authMsg');
const emailInput = document.getElementById('email');
const passwordInput = document.getElementById('password');
const fullNameInput = document.getElementById('fullName');
let mode = 'in';

if (signedInUser) {
  document.getElementById('signedOut').style.display = 'none';
  document.getElementById('signedIn').style.display = 'block';
  document.getElementById('who').textContent = Auth.user.fullName + ' · ' + Auth.user.email;
  if (Auth.user.isAdmin) {
    // admins only manage events: they do not register or request
    document.getElementById('adminLink').style.display = 'block';
    document.getElementById('registerLink').style.display = 'none';
    document.getElementById('requestLink').style.display = 'none';
  }
  if (Auth.user.isSuperAdmin) document.getElementById('superLink').style.display = 'block';
  document.getElementById('logoutBtn').onclick = () => Auth.logout();
}

if (new URLSearchParams(location.search).has('signin')) {
  showMsg(authMsg, 'Please sign in to continue.', false);
  emailInput.focus();
}

function setMode(m) {
  mode = m;
  document.getElementById('nameRow').style.display = m === 'up' ? 'block' : 'none';
  document.getElementById('authSubmit').textContent = m === 'up' ? 'Create account' : 'Sign in';
  document.getElementById('tabIn').className = 'btn' + (m === 'in' ? '' : ' ghost');
  document.getElementById('tabUp').className = 'btn' + (m === 'up' ? '' : ' ghost');
  authMsg.className = 'msg';
}
document.getElementById('tabIn').onclick = () => setMode('in');
document.getElementById('tabUp').onclick = () => setMode('up');

// Shows what the email means, so nobody has to pick a role
const LEVELS = { college: 'College student', shs: 'Senior High student', jhs: 'Junior High student', elem: 'Elementary student' };
emailInput.addEventListener('input', () => {
  if (mode !== 'up') return;
  const parts = emailInput.value.split('@')[0].split('.').filter(Boolean);
  const hint = document.getElementById('roleHint');
  if (parts.length >= 3 && LEVELS[parts[parts.length - 1].toLowerCase()]) hint.textContent = 'Detected: ' + LEVELS[parts[parts.length - 1].toLowerCase()];
  else if (parts.length === 2) hint.textContent = 'Detected: Faculty / staff';
  else hint.innerHTML = 'Students: firstname.surname.college / .shs / .jhs / .elem<br>Faculty &amp; staff: firstname.surname';
});

function validateAuth() {
  if (!emailInput.value.trim()) return 'Please enter your email.';
  if (mode === 'up' && !fullNameInput.value.trim()) return 'Please enter your full name.';
  if (passwordInput.value.length < 8) return 'Password must be at least 8 characters.';
  return null;
}

authForm.addEventListener('submit', async e => {
  e.preventDefault();
  const problem = validateAuth();
  if (problem) { showMsg(authMsg, problem, false); return; }

  const body = { email: emailInput.value.trim(), password: passwordInput.value };
  if (mode === 'up') body.fullName = fullNameInput.value.trim();
  try {
    const r = await withBusy(document.getElementById('authSubmit'),
      () => api(mode === 'up' ? '/api/auth/register' : '/api/auth/login', { method: 'POST', body }));
    Auth.save(r);
    location.href = r.isAdmin ? '/admin.html' : '/dashboard.html';
  } catch (err) {
    showMsg(authMsg, err.message, false);
  }
});

// ---------- Cork board (4 notes per page) ----------
const PER_PAGE = 4;
const colors = ['yellow', 'pink', 'blue', 'green', 'orange', 'purple'];
const board = document.getElementById('board');
let allEvents = [];
let page = 0;

function noteHtml(e, i) {
  const d = new Date(e.startTime);
  const poster = i % 4 === 3 ? ' poster' : '';
  const left = Math.max(0, e.capacity - e.registered);
  return `
    <article class="note ${colors[e.id % colors.length]} r${i % 4}${poster}">
      <span class="pin"></span>
      <div class="when">
        <div class="day"><b>${d.getDate()}</b><small>${d.toLocaleString([], { month: 'short' })}</small></div>
        <div>${fmtTime(e.startTime)} – ${fmtTime(e.endTime)}</div>
      </div>
      <h3>${esc(e.title)}</h3>
      <div class="org">${esc(e.organization)}</div>
      ${e.audienceDetail ? `<div class="org">For: ${esc(e.audienceDetail)}</div>` : ''}
      <div class="where">📍 ${esc(placesOf(e))}</div>
      <div class="seats">${!e.requiresRegistration ? 'Open to all' : left > 0 ? left + ' seats left' : 'Full'}</div>
      <div class="go">${e.requiresRegistration
        ? `<a class="btn" href="/register.html?eventId=${e.id}">Register</a>`
        : '<span class="seats">School event · no registration</span>'}</div>
    </article>`;
}

function renderBoard() {
  const pages = Math.max(1, Math.ceil(allEvents.length / PER_PAGE));
  page = Math.min(page, pages - 1);
  const slice = allEvents.slice(page * PER_PAGE, page * PER_PAGE + PER_PAGE);
  board.innerHTML = slice.length
    ? slice.map((e, i) => noteHtml(e, i)).join('')
    : '<div class="empty">No upcoming events yet.<br>Be the first to post one!</div>';
  document.getElementById('boardPage').textContent = `Page ${page + 1} of ${pages}`;
  document.getElementById('boardPrev').disabled = page === 0;
  document.getElementById('boardNext').disabled = page >= pages - 1;
}

document.getElementById('boardPrev').onclick = () => { page--; renderBoard(); };
document.getElementById('boardNext').onclick = () => { page++; renderBoard(); };

api('/api/events/bulletin')
  .then(events => { allEvents = events; renderBoard(); })
  .catch(err => { board.innerHTML = '<div class="empty">' + esc(err.message) + '</div>'; });
