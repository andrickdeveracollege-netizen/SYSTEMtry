if (!Auth.require()) throw new Error('Not signed in');
if (Auth.user.isAdmin) { location.href = '/admin.html'; throw new Error('Admins use the approvals page'); }
renderNav(document.getElementById('nav'));
const myRegs = document.getElementById('myRegs');

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


loadMyRegs();
