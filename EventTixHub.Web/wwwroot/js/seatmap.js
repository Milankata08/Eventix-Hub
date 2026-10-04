// Seat map: renders the grid from /api/showtimes/{id}/seats and polls every 5 s.
// data-mode="select" lets the user pick free seats; "monitor" is read-only (admin).
(function () {
    const root = document.getElementById('seatmap');
    if (!root) return;

    const showtimeId = root.dataset.showtimeId;
    const monitor = root.dataset.mode === 'monitor';
    const grid = root.querySelector('[data-role="grid"]');
    const notice = root.querySelector('[data-role="notice"]');
    const inputs = root.querySelector('[data-role="inputs"]');
    const submit = root.querySelector('[data-role="submit"]');
    const selectedCount = root.querySelector('[data-role="selected-count"]');
    const total = root.querySelector('[data-role="total"]');
    const selected = new Set();
    let seats = [];
    let prices = { Standard: Number(root.dataset.standardPrice), Vip: Number(root.dataset.vipPrice) };

    const statusText = { Free: 'свободно', Held: 'резервирано', Sold: 'продадено' };

    function seatClass(seat) {
        if (selected.has(seat.id)) return 'seat-selected';
        if (seat.status === 'Held') return 'seat-held';
        if (seat.status === 'Sold') return 'seat-sold';
        return seat.category === 'Vip' ? 'seat-vip' : 'seat-free';
    }

    function render() {
        grid.replaceChildren();
        const rows = new Map();
        for (const seat of seats) {
            if (!rows.has(seat.row)) rows.set(seat.row, []);
            rows.get(seat.row).push(seat);
        }
        for (const [row, rowSeats] of rows) {
            const line = document.createElement('div');
            line.className = 'seat-row';
            const label = document.createElement('span');
            label.className = 'seat-row-label';
            label.textContent = row;
            line.appendChild(label);
            for (const seat of rowSeats) {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'seat ' + seatClass(seat);
                button.textContent = seat.number;
                const state = selected.has(seat.id) ? 'избрано' : statusText[seat.status];
                const price = prices[seat.category].toFixed(2);
                button.setAttribute('aria-label',
                    `Ред ${seat.row}, място ${seat.number}, ${seat.category === 'Vip' ? 'VIP' : 'стандартно'}, ${price} €, ${state}`);
                button.setAttribute('aria-pressed', selected.has(seat.id) ? 'true' : 'false');
                if (monitor || seat.status !== 'Free') button.disabled = true;
                button.addEventListener('click', () => toggle(seat.id));
                line.appendChild(button);
            }
            grid.appendChild(line);
        }
    }

    function updateSummary() {
        if (monitor) return;
        const sum = seats.filter(s => selected.has(s.id)).reduce((acc, s) => acc + prices[s.category], 0);
        selectedCount.textContent = selected.size;
        total.textContent = sum.toFixed(2) + ' €';
        submit.disabled = selected.size === 0;
        inputs.replaceChildren();
        for (const id of selected) {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'seatIds';
            input.value = id;
            inputs.appendChild(input);
        }
    }

    function toggle(id) {
        if (selected.has(id)) selected.delete(id); else selected.add(id);
        notice.classList.add('d-none');
        render();
        updateSummary();
    }

    function showCounts(map) {
        root.querySelector('[data-count="free"]').textContent = map.freeCount;
        root.querySelector('[data-count="held"]').textContent = map.heldCount;
        root.querySelector('[data-count="sold"]').textContent = map.soldCount;
    }

    async function load() {
        let map;
        try {
            const response = await fetch(`/api/showtimes/${showtimeId}/seats`, { headers: { Accept: 'application/json' } });
            if (!response.ok) return;
            map = await response.json();
        } catch {
            return; // network hiccup or login redirect: keep the last map, try again in 5 s
        }
        seats = map.seats;
        prices = { Standard: map.standardPrice, Vip: map.vipPrice };

        const lost = seats.filter(s => selected.has(s.id) && s.status !== 'Free');
        for (const s of lost) selected.delete(s.id);
        if (lost.length > 0) {
            notice.textContent = 'Следните места вече не са свободни и бяха премахнати от избора: ' +
                lost.map(s => `ред ${s.row}, място ${s.number}`).join('; ') + '.';
            notice.classList.remove('d-none');
        }
        showCounts(map);
        render();
        updateSummary();
    }

    load();
    setInterval(load, 5000);
})();
