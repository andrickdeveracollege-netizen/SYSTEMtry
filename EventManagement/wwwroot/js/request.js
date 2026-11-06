if (!Auth.require()) throw new Error('Not signed in');
renderNav(document.getElementById('nav'));

const form = document.getElementById('form');
const msg = document.getElementById('msg');
const deptBox = document.getElementById('deptBox');
const MAX_FILE = 5 * 1024 * 1024;
const FILE_TYPES = ['.jpg', '.jpeg', '.png', '.pdf', '.docx'];

const OFFICES = [
  { key: 'Auxiliary', label: 'Auxiliary', letterOptional: false },
  { key: 'ITMC', label: 'ITMC (Technical)', letterOptional: false },
  { key: 'College', label: 'College (Ms. Judith)', letterOptional: true },
  { key: 'TVET', label: 'TVET (Ms. Chavez)', letterOptional: false },
  { key: 'Library', label: 'Library', letterOptional: false }
];

document.getElementById('requesterName').value = Auth.user.fullName;

(async () => {
  const [facilities, equipment] = await Promise.all([api('/api/facilities'), api('/api/equipment')]);
  deptBox.innerHTML = OFFICES.map(o => blockHtml(o,
    facilities.filter(f => f.department === o.key),
    equipment.filter(x => x.department === o.key))).join('');
  deptBox.querySelectorAll('.dept-block').forEach(wireBlock);
})().catch(err => showMsg(msg, err.message, false));

function blockHtml(o, facilities, equipment) {
  return `
  <div class="dept-block" data-dept="${o.key}" data-optional="${o.letterOptional}">
    <header><label><input type="checkbox" class="dept-on" /> ${esc(o.label)}</label></header>
    <div class="body">
      ${facilities.length ? `<label>Facilities</label>
        <div class="fac-list">${facilities.map(f =>
          `<label><input type="checkbox" class="fac" value="${f.id}" /> ${esc(f.name)}</label>`).join('')}</div>` : ''}

      <label>Equipment</label>
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
}

// Everything the server also checks, so the user gets friendly messages first
function validate(selected) {
  const g = id => document.getElementById(id).value.trim();
  if (!g('title')) return 'Please enter the event title.';
  if (!g('requesterName')) return 'Please enter the requester name.';
  if (!g('organization')) return 'Please enter your department / organization / club.';
  if (!g('role')) return 'Please select your role.';
  if (!audienceValue()) return 'Please select at least one target audience.';
  const cap = Number(g('capacity'));
  if (!(cap >= 1 && cap <= 10000)) return 'Capacity must be between 1 and 10,000.';
  if (!g('purpose')) return 'Please enter the purpose.';
  if (!g('start') || !g('end')) return 'Please set the start and end time.';
  if (new Date(g('end')) <= new Date(g('start'))) return 'End time must be after the start time.';
  if (new Date(g('start')) < new Date()) return 'The event must start in the future.';
  if (selected.length === 0) return 'Tick at least one office you need.';

  for (const block of selected) {
    const label = block.querySelector('.dept-on').parentElement.textContent.trim();
    const facs = block.querySelectorAll('.fac:checked').length;
    const eqs = [...block.querySelectorAll('.eq-row')].filter(r => r.querySelector('.eqchk').checked);
    if (facs === 0 && eqs.length === 0) return `Pick at least one facility or equipment for ${label}, or untick it.`;
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

function audienceValue() {
  return [...document.querySelectorAll('.aud:checked')].reduce((sum, c) => sum + Number(c.value), 0);
}

function buildFormData(selected) {
  const g = id => document.getElementById(id).value.trim();
  const fd = new FormData();
  fd.append('Title', g('title'));
  fd.append('RequesterName', g('requesterName'));
  fd.append('Organization', g('organization'));
  fd.append('Role', g('role'));
  fd.append('TargetAudience', audienceValue());
  fd.append('Capacity', g('capacity'));
  fd.append('Purpose', g('purpose'));
  fd.append('StartTime', isoLocal(new Date(g('start'))));
  fd.append('EndTime', isoLocal(new Date(g('end'))));

  selected.forEach((block, i) => {
    const p = `Departments[${i}]`;
    fd.append(`${p}.Department`, block.dataset.dept);
    [...block.querySelectorAll('.fac:checked')].forEach((c, j) => fd.append(`${p}.FacilityIds[${j}]`, c.value));
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
    await withBusy(document.getElementById('submitBtn'),
      () => api('/api/events/request', { method: 'POST', form: buildFormData(selected) }));
    showMsg(msg, 'Request submitted! Each office you ticked will review its own part.', true);
    form.reset();
    document.querySelectorAll('.dept-on, .eqchk').forEach(c => c.dispatchEvent(new Event('change')));
  } catch (err) {
    showMsg(msg, err.message, false);
  }
});
