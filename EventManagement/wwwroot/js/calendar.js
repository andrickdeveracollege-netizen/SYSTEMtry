// Shared facility calendar (used on the landing page and the dashboard).
// Reads the public /api/events/calendar endpoint.
function initCalendar({ grid, title, filter, dayList, prev, next, today }) {
  let view = new Date();
  view.setDate(1);
  let events = [];

  async function loadFacilities() {
    const list = await api('/api/facilities');
    list.forEach(f => filter.insertAdjacentHTML('beforeend',
      `<option value="${f.id}">${esc(f.name)} (${esc(f.approvingOffice)})</option>`));
  }

  function eventsOn(d) {
    const s = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const e = new Date(s);
    e.setDate(e.getDate() + 1);
    return events.filter(ev => new Date(ev.startTime) < e && new Date(ev.endTime) > s);
  }

  async function render() {
    const y = view.getFullYear(), m = view.getMonth();
    title.textContent = view.toLocaleString([], { month: 'long', year: 'numeric' });
    const start = new Date(y, m, 1 - new Date(y, m, 1).getDay());
    const end = new Date(start);
    end.setDate(end.getDate() + 42);
    const fac = filter.value;
    grid.innerHTML = '<div class="spinner" style="grid-column:1/-1">Loading…</div>';
    try {
      events = await api(`/api/events/calendar?from=${isoLocal(start)}&to=${isoLocal(end)}${fac ? '&facilityId=' + fac : ''}`);
    } catch (err) {
      grid.innerHTML = `<div class="empty-state" style="grid-column:1/-1">${esc(err.message)}</div>`;
      return;
    }

    let html = ['S', 'M', 'T', 'W', 'T', 'F', 'S'].map(d => `<div class="dow">${d}</div>`).join('');
    const todayStr = new Date().toDateString();
    for (let i = 0; i < 42; i++) {
      const d = new Date(start);
      d.setDate(start.getDate() + i);
      const list = eventsOn(d);
      const cls = ['day', d.getMonth() !== m ? 'other' : '', d.toDateString() === todayStr ? 'today' : ''].join(' ');
      html += `<div class="${cls}" data-date="${isoLocal(d)}">
        <div class="n">${d.getDate()}</div>
        ${list.slice(0, 2).map(e => `<div class="ev ${e.status === 'Pending' ? 'pending' : ''}" title="${esc(e.title)}">${esc(e.title)}</div>`).join('')}
        ${list.length > 2 ? `<div class="more">+${list.length - 2}</div>` : ''}
      </div>`;
    }
    grid.innerHTML = html;
    grid.querySelectorAll('.day').forEach(el => { el.onclick = () => showDay(new Date(el.dataset.date)); });
    dayList.innerHTML = '';
  }

  function showDay(d) {
    const list = eventsOn(d);
    dayList.innerHTML = `<h3>${d.toLocaleDateString([], { dateStyle: 'full' })}</h3>` +
      (list.length
        ? list.map(e => `<div class="req"><strong>${esc(e.title)}</strong> ${statusTag(e.status)}<br>
            <span class="muted">${esc(placesOf(e))} · ${fmtTime(e.startTime)} – ${fmtTime(e.endTime)} · ${esc(e.organization)}</span></div>`).join('')
        : '<div class="empty-state">No bookings — all facilities are free.</div>');
  }

  prev.onclick = () => { view.setMonth(view.getMonth() - 1); render(); };
  next.onclick = () => { view.setMonth(view.getMonth() + 1); render(); };
  today.onclick = () => { view = new Date(); view.setDate(1); render(); };
  filter.onchange = render;

  loadFacilities().then(render).catch(err => toast(err.message, false));
  return { refresh: render };
}
