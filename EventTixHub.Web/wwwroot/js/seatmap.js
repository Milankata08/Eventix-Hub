// Seat map: builds the hall once from /api/showtimes/{id}/seats, then updates seats in place on every
// 5 s poll and click, so keyboard focus is never lost. data-mode="select" lets the user pick free
// seats; "monitor" is read-only (admin, or sales closed). One roving tab stop; arrows move between seats.
(function () {
    const root = document.getElementById('seatmap');
    if (!root) return;

    const showtimeId = root.dataset.showtimeId;
    const monitor = root.dataset.mode === 'monitor';
    const role = name => root.querySelector(`[data-role="${name}"]`);
    const grid = role('grid');
    const notice = role('notice');
    const statusNotice = role('status');
    const announce = role('announce');
    const list = role('list');
    const empty = role('empty');
    const submit = role('submit');
    const total = role('total');
    const barText = role('bar-text');
    const toggle = role('toggle');
    const panel = role('panel');

    const selected = new Set();
    const seatEls = new Map(); // seat id -> element
    const zoneLabels = [];     // [element, category] refreshed when prices change
    let rows = [];             // [[seat, ...], ...] in display order
    let position = new Map();  // seat id -> { r, i }
    let seats = [];
    let signature = '';
    let tabId = null;
    let failures = 0;
    let firstLoad = true;
    let prices = { Standard: Number(root.dataset.standardPrice), Vip: Number(root.dataset.vipPrice) };

    const moneyFormat = new Intl.NumberFormat('bg-BG', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const money = n => moneyFormat.format(n) + ' €';
    const statusText = { Free: 'свободно', Held: 'резервирано', Sold: 'продадено' };
    const where = s => `ред ${s.row}, място ${s.number}`;
    const seatById = id => seats.find(s => s.id === id);

    function stateOf(seat) {
        if (selected.has(seat.id)) return 'Selected';
        return seat.status;
    }

    function build() {
        grid.replaceChildren();
        seatEls.clear();
        zoneLabels.length = 0;
        rows = [];
        position = new Map();
        const byRow = new Map();
        for (const seat of seats) {
            if (!byRow.has(seat.row)) byRow.set(seat.row, []);
            byRow.get(seat.row).push(seat);
        }
        let zone = null;
        for (const rowSeats of byRow.values()) {
            const vip = rowSeats[0].category === 'Vip';
            if (vip && !zone) {
                zone = document.createElement('div');
                zone.className = 'zone-vip';
                const label = document.createElement('div');
                label.className = 'zone-label label-caps';
                zoneLabels.push([label, 'Vip']);
                zone.appendChild(label);
                grid.appendChild(zone);
            } else if (!vip) {
                zone = null;
            }

            const line = document.createElement('div');
            line.className = 'seat-row';
            line.setAttribute('role', 'group');
            line.setAttribute('aria-label', `Ред ${rowSeats[0].row}`);
            line.appendChild(rowLabel(rowSeats[0].row));
            const ids = [];
            for (const seat of rowSeats) {
                const el = document.createElement(monitor ? 'span' : 'button');
                if (monitor) el.setAttribute('role', 'img'); else el.type = 'button';
                el.dataset.seatId = seat.id;
                el.textContent = seat.number;
                if (!monitor) el.tabIndex = -1;
                seatEls.set(seat.id, el);
                position.set(seat.id, { r: rows.length, i: ids.length });
                ids.push(seat);
                line.appendChild(el);
            }
            line.appendChild(rowLabel(rowSeats[0].row));
            rows.push(ids);
            (vip ? zone : grid).appendChild(line);
        }
        if (!monitor) {
            const keep = seatEls.has(tabId) ? tabId : (seats.find(s => s.status === 'Free') ?? seats[0])?.id;
            setTab(keep);
        }
    }

    function rowLabel(row) {
        const span = document.createElement('span');
        span.className = 'seat-row-label';
        span.setAttribute('aria-hidden', 'true');
        span.textContent = row;
        return span;
    }

    // Sets state on existing nodes only; never replaces one.
    function update() {
        for (const [el, category] of zoneLabels) el.textContent = `VIP · ${money(prices[category])}`;
        for (const seat of seats) {
            const el = seatEls.get(seat.id);
            const state = stateOf(seat);
            el.className = 'seat seat-' + state.toLowerCase();
            const free = seat.status === 'Free';
            el.setAttribute('aria-label',
                `Ред ${seat.row}, място ${seat.number}, ${seat.category === 'Vip' ? 'VIP' : 'стандартно'}, ` +
                `${money(prices[seat.category])}, ${state === 'Selected' ? 'избрано' : statusText[seat.status]}`);
            if (monitor) continue;
            el.setAttribute('aria-pressed', state === 'Selected' ? 'true' : 'false');
            if (free) el.removeAttribute('aria-disabled'); else el.setAttribute('aria-disabled', 'true');
        }
    }

    function setTab(id) {
        if (id === tabId && seatEls.get(id)?.tabIndex === 0) return;
        const old = seatEls.get(tabId);
        if (old) old.tabIndex = -1;
        const el = seatEls.get(id);
        if (el) el.tabIndex = 0;
        tabId = id;
    }

    function moveFocus(id, key) {
        const { r, i } = position.get(id);
        const row = rows[r];
        let target = null;
        if (key === 'ArrowLeft') target = row[Math.max(0, i - 1)];
        else if (key === 'ArrowRight') target = row[Math.min(row.length - 1, i + 1)];
        else if (key === 'Home') target = row[0];
        else if (key === 'End') target = row[row.length - 1];
        else {
            const next = rows[r + (key === 'ArrowUp' ? -1 : 1)];
            if (!next) return true;
            const number = seatById(id).number;
            target = next.reduce((best, s) => Math.abs(s.number - number) < Math.abs(best.number - number) ? s : best);
        }
        seatEls.get(target.id).focus();
        return true;
    }

    function renderSummary() {
        if (monitor) return;
        const chosen = seats.filter(s => selected.has(s.id));
        const sum = chosen.reduce((acc, s) => acc + prices[s.category], 0);
        total.textContent = money(sum);
        barText.textContent = `${chosen.length} ${chosen.length === 1 ? 'място' : 'места'} · ${money(sum)}`;
        submit.disabled = chosen.length === 0;
        empty.hidden = chosen.length > 0;
        list.replaceChildren(...chosen.map(s => {
            const li = document.createElement('li');
            const text = document.createElement('span');
            text.textContent = `Ред ${s.row} · място ${s.number}${s.category === 'Vip' ? ' · VIP' : ''} · ${money(prices[s.category])}`;
            const remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'btn btn-sm btn-outline-secondary';
            remove.dataset.remove = s.id;
            remove.textContent = 'Премахни';
            remove.setAttribute('aria-label', `Премахни ред ${s.row}, място ${s.number}`);
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'seatIds';
            input.value = s.id;
            li.append(text, remove, input);
            return li;
        }));
    }

    function currentTotal() {
        return seats.filter(s => selected.has(s.id)).reduce((acc, s) => acc + prices[s.category], 0);
    }

    // Clear first so a repeated message is announced again.
    function say(text) {
        announce.textContent = '';
        setTimeout(() => { announce.textContent = text; }, 50);
    }

    function choose(id) {
        const seat = seatById(id);
        if (!selected.has(id) && seat.status !== 'Free') {
            say('Мястото е заето.');
            return;
        }
        const adding = !selected.has(id);
        if (adding) selected.add(id); else selected.delete(id);
        notice.classList.add('d-none');
        update();
        renderSummary();
        say(`${adding ? 'Добавено' : 'Премахнато'}: ${where(seat)}. Общо ${money(currentTotal())}.`);
    }

    function showCounts(map) {
        root.querySelector('[data-count="free"]').textContent = map.freeCount;
        root.querySelector('[data-count="held"]').textContent = map.heldCount;
        root.querySelector('[data-count="sold"]').textContent = map.soldCount;
    }

    function showAlert(text) {
        notice.textContent = text;
        notice.classList.remove('d-none');
    }

    function preselect() {
        if (monitor) return;
        const wanted = (new URLSearchParams(location.search).get('seats') || '')
            .split(',').map(Number).filter(id => Number.isInteger(id) && id > 0 && seatById(id));
        if (wanted.length === 0) return;
        const missed = [];
        for (const id of wanted) {
            const seat = seatById(id);
            if (seat.status === 'Free') selected.add(id); else missed.push(where(seat));
        }
        if (missed.length > 0)
            showAlert('Тези места не са свободни и не бяха избрани: ' + missed.join('; ') + '.');
    }

    function fail() {
        failures++;
        if (firstLoad) role('loading').hidden = true;
        if (firstLoad || failures >= 3) statusNotice.classList.remove('d-none');
    }

    async function load() {
        let map;
        try {
            const response = await fetch(`/api/showtimes/${showtimeId}/seats`, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error(response.status);
            map = await response.json();
        } catch {
            fail(); // network hiccup or login redirect: keep the last map, try again in 5 s
            return;
        }
        failures = 0;
        statusNotice.classList.add('d-none');
        seats = map.seats;
        prices = { Standard: map.standardPrice, Vip: map.vipPrice };

        const ids = seats.map(s => s.id).join(',');
        const rebuild = ids !== signature;
        signature = ids;
        if (rebuild) build();
        if (firstLoad) {
            firstLoad = false;
            preselect();
        }

        const lost = seats.filter(s => selected.has(s.id) && s.status !== 'Free');
        for (const s of lost) selected.delete(s.id);
        if (lost.length > 0)
            showAlert('Следните места вече не са свободни и бяха премахнати от избора: ' + lost.map(where).join('; ') + '.');
        showCounts(map);
        update();
        renderSummary();
    }

    grid.addEventListener('focusin', e => {
        const el = e.target.closest('[data-seat-id]');
        if (el && !monitor) setTab(Number(el.dataset.seatId));
    });

    if (!monitor) {
        grid.addEventListener('click', e => {
            const el = e.target.closest('[data-seat-id]');
            if (el) choose(Number(el.dataset.seatId));
        });

        grid.addEventListener('keydown', e => {
            const el = e.target.closest('[data-seat-id]');
            if (!el || e.altKey || e.ctrlKey || e.metaKey) return;
            if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(e.key)) {
                e.preventDefault();
                moveFocus(Number(el.dataset.seatId), e.key);
            }
        });

        list.addEventListener('click', e => {
            const button = e.target.closest('[data-remove]');
            if (!button) return;
            const id = Number(button.dataset.remove);
            const index = [...list.children].indexOf(button.closest('li'));
            choose(id);
            const buttons = list.querySelectorAll('button');
            (buttons[Math.min(index, buttons.length - 1)] ?? seatEls.get(id)).focus();
        });

        toggle.addEventListener('click', () => {
            const open = panel.classList.toggle('is-open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
    }

    load();
    setInterval(load, 5000);
})();
