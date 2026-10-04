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
