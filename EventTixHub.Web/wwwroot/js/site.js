// Cart hold countdown: data-countdown holds the seconds left (computed by the server, so no clock skew).
// When it reaches zero the page reloads and shows the "expired" message.
(function () {
    const el = document.querySelector('[data-countdown]');
    if (!el) return;
    let left = Number(el.dataset.countdown);
    const tick = () => {
        if (left <= 0) {
            location.reload();
            return;
        }
        const m = Math.floor(left / 60);
        const s = String(left % 60).padStart(2, '0');
        el.textContent = `${m}:${s} мин.`;
        left--;
    };
    tick();
    setInterval(tick, 1000);
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
