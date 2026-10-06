// Hold countdown: every [data-hold-seconds] starts from a server-rendered value (no clock skew).
// Text updates are plain DOM writes; one shared status region speaks only at 5 min, 1 min and expiry.
(function () {
    const els = [...document.querySelectorAll('[data-hold-seconds]')];
    if (els.length === 0) return;
    const announce = document.getElementById('hold-announce');
    const onCartPage = /^\/Booking\/(Cart|Checkout)\/?$/i.test(location.pathname);
    const speak = { 300: 'Остават 5 минути.', 60: 'Остава 1 минута.', 0: 'Резервацията изтече.' };
    let left = Math.min(...els.map(e => Number(e.dataset.holdSeconds)));
    const tick = () => {
        if (speak[left] && announce) announce.textContent = speak[left];
        if (left <= 0) {
            if (onCartPage) { location.reload(); return; }
            for (const el of els) {
                el.closest('[data-hold-text]').textContent = 'Резервацията изтече';
            }
            clearInterval(timer);
            return;
        }
        const text = `${Math.floor(left / 60)}:${String(left % 60).padStart(2, '0')}`;
        for (const el of els) el.textContent = text;
        if (left <= 120) for (const b of document.querySelectorAll('[data-hold-badge]')) b.classList.add('is-urgent');
        left--;
    };
    const timer = setInterval(tick, 1000);
    tick();
})();

// [data-autosubmit] fields apply their form on change (Афиш city). Without JS the form's submit button still works.
// Arrow keys and type-ahead on a closed select fire change on every key, so a keyboard pick waits for Enter.
(function () {
    let key = null;
    document.addEventListener('keydown', e => {
        if (!e.target.matches('[data-autosubmit]')) return;
        key = e.key;
        if (e.key === 'Enter' && e.target.dataset.pending) { e.preventDefault(); e.target.form.requestSubmit(); }
    });
    document.addEventListener('keyup', () => { key = null; });
    document.addEventListener('change', e => {
        if (!e.target.matches('[data-autosubmit]')) return;
        if (key && key !== 'Enter') e.target.dataset.pending = '1';
        else e.target.form.requestSubmit();
    });
})();

// Dark mode toggle (Bootstrap 5.3 color modes). The <head> script in _Layout applies the saved theme before first paint.
(function () {
    const html = document.documentElement;
    const button = document.getElementById('theme-toggle');
    const syncPressed = () => button.setAttribute('aria-pressed', html.dataset.bsTheme === 'dark');
    syncPressed();
    button.addEventListener('click', () => {
        html.dataset.bsTheme = html.dataset.bsTheme === 'dark' ? 'light' : 'dark';
        syncPressed();
        localStorage.setItem('theme', html.dataset.bsTheme);
    });
    // Printing drops backgrounds, so dark mode's light text would vanish on a printed ticket.
    let screenTheme;
    addEventListener('beforeprint', () => { screenTheme = html.dataset.bsTheme; html.dataset.bsTheme = 'light'; });
    addEventListener('afterprint', () => { html.dataset.bsTheme = screenTheme; });
})();
