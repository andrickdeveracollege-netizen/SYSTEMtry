// Same form for school accounts (signed in) and guests (?guest=1, outsiders, weekends only).
const guest = new URLSearchParams(location.search).has('guest') && !Auth.token;
if (!guest && !Auth.require()) throw new Error('Not signed in');
if (!guest && Auth.user.isAdmin) { location.href = '/admin.html'; throw new Error('Admins cannot request events'); }
renderNav(document.getElementById('nav'));

const form = document.getElementById('form');
const msg = document.getElementById('msg');
const deptBox = document.getElementById('deptBox');
const capacityInput = document.getElementById('capacity');
const MAX_FILE = 5 * 1024 * 1024;
const FILE_TYPES = ['.jpg', '.jpeg', '.png', '.pdf', '.docx'];

const OFFICES = [
  { key: 'Auxiliary', label: 'Auxiliary', letterOptional: false },
  { key: 'ITMC', label: 'ITMC (Technical)', letterOptional: false },
  { key: 'College', label: 'College (Ms. Judith)', letterOptional: true },
  { key: 'TVET', label: 'TVET (Ms. Chavez)', letterOptional: false },
  { key: 'Library', label: 'Library', letterOptional: false }
];

// ---------- who is asking (role is never typed) ----------
const whoBox = document.getElementById('who');
if (guest) {
  whoBox.textContent = 'Visitor request. Please give your email. Outsiders can only book on weekends (Saturday and Sunday).';
  document.getElementById('guestFields').style.display = 'block';
  document.getElementById('weekendHint').style.display = 'block';
  document.getElementById('backLink').href = '/visitor.html';
} else {
  const u = Auth.user;
  const kind = u.position === 'Student' ? `Student (${u.level})` : u.position === 'Administrator' ? 'Administrator (institutional event)' : 'Faculty / staff';
  whoBox.textContent = `Requesting as ${kind}. Your role comes from your Angelicum email.`;
  document.getElementById('requesterName').value = u.fullName;
}

// ---------- build the office blocks ----------
(async () => {
  const [facilities, equipment] = await Promise.all([api('/api/facilities'), api('/api/equipment')]);
  deptBox.innerHTML = OFFICES.map(o => blockHtml(o,
    facilities.filter(f => f.department === o.key),
    equipment.filter(x => x.department === o.key))).join('');
  deptBox.querySelectorAll('.dept-block').forEach(wireBlock);
  filterFacilities();
})().catch(err => showMsg(msg, err.message, false));

function blockHtml(o, facilities, equipment) {
  return `
  <div class="dept-block" data-dept="${o.key}" data-optional="${o.letterOptional}">
    <header><label><input type="checkbox" class="dept-on" /> ${esc(o.label)}</label></header>
    <div class="body">
      ${facilities.length ? `<label>Facilities</label>
        <div class="fac-list">${facilities.map(f => `
          <label class="fac-item" data-cap="${f.capacity ?? ''}" data-other="${f.isOther}">
            <input type="checkbox" class="fac" value="${f.id}" data-other="${f.isOther}" />
            ${esc(f.name)}${f.capacity ? ` <span class="muted">(up to ${f.capacity})</span>` : ''}
          </label>`).join('')}</div>
        <div class="other-name" style="display:none">
          <label>Type the facility name (for "Others")</label>
          <input type="text" class="othertext" maxlength="100" placeholder="e.g. Chapel" />
        </div>
        <div class="hint no-fac" style="display:none">No facility in this office is big enough for that many people.</div>` : ''}

      ${equipment.length ? '<label>Equipment</label>' : ''}
      ${equipment.map(x => `
        <div class="eq-row" data-id="${x.id}" data-name="${esc(x.name)}" data-other="${x.isOther}">
          <label><input type="checkbox" class="eqchk" /> ${esc(x.name)}</label>
          <input type="number" class="eqqty" min="1" max="100" value="1" disabled title="Quantity" />
          ${x.isOther ? '<input type="text" class="eqnote" maxlength="500" placeholder="Describe what you need" disabled />' : ''}
        </div>`).join('')}

      <label>Endorsement letter for ${esc(o.label)}
        <span class="tag ${o.letterOptional ? 'approved' : 'pending'}">${o.letterOptional ? 'Optional' : 'Required'}</span></label>
      <input type="file" class="letter" accept=".jpg,.jpeg,.png,.pdf,.docx" />
      <div class="hint">Image (JPG/PNG), PDF or DOCX, max 5 MB.</div>
    </div>
  </div>`;
}

function wireBlock(block) {
  const on = block.querySelector('.dept-on');
  on.onchange = () => block.classList.toggle('on', on.checked);
  block.querySelectorAll('.eq-row').forEach(row => {
    const chk = row.querySelector('.eqchk');
    chk.onchange = () => row.querySelectorAll('.eqqty, .eqnote').forEach(el => { el.disabled = !chk.checked; });
  });
  block.querySelectorAll('.fac').forEach(f => { f.onchange = () => syncOtherName(block); });
}

function syncOtherName(block) {
  const otherTicked = [...block.querySelectorAll('.fac')].some(f => f.checked && f.dataset.other === 'true');
  const box = block.querySelector('.other-name');
  if (box) box.style.display = otherTicked ? 'block' : 'none';
}

// Big event => only big facilities are shown. Small rooms disappear (and get unticked).
function filterFacilities() {
  const people = Number(capacityInput.value) || 0;
  document.querySelectorAll('.dept-block').forEach(block => {
    let visible = 0;
    block.querySelectorAll('.fac-item').forEach(item => {
      const cap = item.dataset.cap === '' ? null : Number(item.dataset.cap);
      const fits = cap === null || cap >= people;
      item.style.display = fits ? '' : 'none';
      if (!fits) item.querySelector('.fac').checked = false;
      if (fits) visible++;
    });
    const note = block.querySelector('.no-fac');
    if (note) note.style.display = visible === 0 ? 'block' : 'none';
    syncOtherName(block);
  });
}
capacityInput.addEventListener('input', filterFacilities);

// ---------- validation (the server checks everything again) ----------
function isWeekendRange(start, end) {
  const d = new Date(start.getFullYear(), start.getMonth(), start.getDate());
  const last = new Date(end.getFullYear(), end.getMonth(), end.getDate());
  for (; d <= last; d.setDate(d.getDate() + 1)) if (d.getDay() !== 0 && d.getDay() !== 6) return false;
  return true;
}

function audienceValue() {
  return [...document.querySelectorAll('.aud:checked')].reduce((sum, c) => sum + Number(c.value), 0);
}

function validate(selected) {
  const g = id => document.getElementById(id).value.trim();
  if (!g('title')) return 'Please enter the event title.';
  if (!g('requesterName')) return 'Please enter the requester name.';
  if (guest && !/^\S+@\S+\.\S+$/.test(g('guestEmail'))) return 'Please enter a valid email address.';
  if (!g('organization')) return 'Please enter your department / organization / club.';
  if (!audienceValue()) return 'Please select at least one target audience.';
  const cap = Number(g('capacity'));
  if (!(cap >= 1 && cap <= 10000)) return 'Number of attendees must be between 1 and 10,000.';
  if (!g('purpose')) return 'Please enter the purpose.';
  if (!g('start') || !g('end')) return 'Please set the start and end time.';
  const start = new Date(g('start')), end = new Date(g('end'));
  if (end <= start) return 'End time must be after the start time.';
  if (start < new Date()) return 'The event must start in the future.';
  if (guest && !isWeekendRange(start, end)) return 'Outsiders can only book on Saturdays and Sundays.';
  if (selected.length === 0) return 'Tick at least one office you need.';

  for (const block of selected) {
    const label = block.querySelector('.dept-on').parentElement.textContent.trim();
    const facs = [...block.querySelectorAll('.fac:checked')];
    const eqs = [...block.querySelectorAll('.eq-row')].filter(r => r.querySelector('.eqchk').checked);
    if (facs.length === 0 && eqs.length === 0) return `Pick at least one facility or equipment for ${label}, or untick it.`;
    if (facs.some(f => f.dataset.other === 'true') && !block.querySelector('.othertext').value.trim())
      return `Please type the facility name for "Others" under ${label}.`;
    for (const row of eqs) {
      if (row.dataset.other === 'true' && !row.querySelector('.eqnote').value.trim())
        return `Please describe the 'Others' equipment for ${label}.`;
    }
    const file = block.querySelector('.letter').files[0];
    if (!file && block.dataset.optional !== 'true') return `Please upload the endorsement letter for ${label}.`;
    if (file) {
      const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
      if (!FILE_TYPES.includes(ext)) return `The letter for ${label} must be JPG, PNG, PDF or DOCX.`;
      if (file.size > MAX_FILE) return `The letter for ${label} must be 5 MB or smaller.`;
    }
  }
  return null;
}

function buildFormData(selected) {
  const g = id => document.getElementById(id).value.trim();
  const fd = new FormData();
  fd.append('Title', g('title'));
  fd.append('RequesterName', g('requesterName'));
  fd.append('Organization', g('organization'));
  fd.append('TargetAudience', audienceValue());
  fd.append('Capacity', g('capacity'));
  fd.append('Purpose', g('purpose'));
  fd.append('StartTime', isoLocal(new Date(g('start'))));
  fd.append('EndTime', isoLocal(new Date(g('end'))));
  if (guest) {
    fd.append('GuestEmail', g('guestEmail'));
    if (g('phone')) fd.append('Phone', g('phone'));
  }

  selected.forEach((block, i) => {
    const p = `Departments[${i}]`;
    fd.append(`${p}.Department`, block.dataset.dept);
    [...block.querySelectorAll('.fac:checked')].forEach((c, j) => fd.append(`${p}.FacilityIds[${j}]`, c.value));
    const other = block.querySelector('.othertext');
    if (other && other.value.trim()) fd.append(`${p}.OtherFacilityName`, other.value.trim());
    [...block.querySelectorAll('.eq-row')].filter(r => r.querySelector('.eqchk').checked).forEach((row, j) => {
      fd.append(`${p}.Equipment[${j}].EquipmentId`, row.dataset.id);
      fd.append(`${p}.Equipment[${j}].Quantity`, row.querySelector('.eqqty').value);
      const note = row.querySelector('.eqnote');
      if (note && note.value.trim()) fd.append(`${p}.Equipment[${j}].Notes`, note.value.trim());
    });
    const file = block.querySelector('.letter').files[0];
    if (file) fd.append(`${p}.Endorsement`, file);
  });
  return fd;
}

form.addEventListener('submit', async e => {
  e.preventDefault();
  const selected = [...document.querySelectorAll('.dept-block')].filter(b => b.querySelector('.dept-on').checked);
  const problem = validate(selected);
  if (problem) { showMsg(msg, problem, false); return; }

  try {
    const created = await withBusy(document.getElementById('submitBtn'),
      () => api(guest ? '/api/guest/events/request' : '/api/events/request', { method: 'POST', form: buildFormData(selected) }));
    let text = 'Request submitted! Each office you ticked will review its own part.';
    if (guest) text += ` SAVE THIS: Event number ${created.event.id} · manage code ${created.manageCode}. You need your email and this code to move or cancel the request.`;
    showMsg(msg, text, true);
    form.reset();
    if (!guest) document.getElementById('requesterName').value = Auth.user.fullName;
    document.querySelectorAll('.dept-on, .eqchk').forEach(c => c.dispatchEvent(new Event('change')));
    filterFacilities();
  } catch (err) {
    showMsg(msg, err.message, false);
  }
});
