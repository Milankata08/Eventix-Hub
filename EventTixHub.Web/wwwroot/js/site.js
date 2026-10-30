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

// [data-autosubmit] fields apply their form on change (Афиш and admin list filters, cart concessions). Without JS the
// form's button (in <noscript>) applies them. Arrow keys and type-ahead on a closed select fire change on every key,
// so a keyboard pick waits for Enter or for leaving the field.
// data-autosubmit="blur" (search, date) applies on Enter or when leaving the field. Enter is handled here because
// a form with two text-like fields and no submit button has no implicit submission.
(function () {
    let key = null;
    document.addEventListener('keydown', e => {
        const el = e.target;
        if (!el.matches('[data-autosubmit]')) return;
        key = e.key;
        if (e.key === 'Enter' && (el.dataset.pending || el.dataset.autosubmit === 'blur')) { e.preventDefault(); el.form.requestSubmit(); }
    });
    document.addEventListener('keyup', () => { key = null; });
    document.addEventListener('change', e => {
        const el = e.target;
        if (!el.matches('[data-autosubmit]')) return;
        if (el.dataset.autosubmit === 'blur' || (key && key !== 'Enter')) el.dataset.pending = '1';
        else el.form.requestSubmit();
    });
    document.addEventListener('focusout', e => {
        if (e.target.matches('[data-autosubmit]') && e.target.dataset.pending) e.target.form.requestSubmit();
    });
})();

// Pending submit: a button-submitted form locks its button until the next page, so a double click sends one request.
// Cancelled confirms and failed client validation stop the event before it reaches document; auto-submits have no
// submitter. aria-disabled, not disabled: a disabled submitter drops out of the form data. bfcache restore unlocks.
(function () {
    document.addEventListener('submit', e => {
        const button = e.submitter;
        if (!button || e.defaultPrevented) return;
        if (e.target.dataset.submitting) { e.preventDefault(); return; }
        e.target.dataset.submitting = '1';
        button.dataset.locked = '1';
        button.classList.add('disabled');
        button.setAttribute('aria-disabled', 'true');
        if (button.dataset.pendingText) {
            button.dataset.idleText = button.textContent;
            button.textContent = button.dataset.pendingText;
        }
    });
    addEventListener('pageshow', e => {
        if (!e.persisted) return;
        for (const form of document.querySelectorAll('form[data-submitting]')) delete form.dataset.submitting;
        for (const button of document.querySelectorAll('[data-locked]')) {
            delete button.dataset.locked;
            button.classList.remove('disabled');
            button.removeAttribute('aria-disabled');
            if (button.dataset.idleText) button.textContent = button.dataset.idleText;
        }
    });
})();

// Empty date filters: data-empty swaps the browser's locale mask for the Bulgarian placeholder (site.css, .date-field).
for (const input of document.querySelectorAll('.date-field input')) {
    const sync = () => input.toggleAttribute('data-empty', !input.value && !input.validity.badInput); // a half-typed date stays visible
    input.addEventListener('input', sync);
    sync();
}

// Field errors. A page that comes back with server-side errors (FieldErrorTagHelper sets aria-invalid) focuses the
// first one, as a failed client submit does; a form-level error (e.g. wrong password) only shows in the summary, so
// then the summary takes focus and is read out. jquery.validate sets aria-invalid only on per-field checks, not on submit,
// so highlight/unhighlight keep it in step with the danger border. The ready handler registers before
// jquery.validate.unobtrusive's, so these defaults are in place when it builds each form's validator.
(function () {
    const firstError = document.querySelector('main form [aria-invalid="true"]:not([type=hidden])')
        ?? document.querySelector('main form .validation-summary-errors');
    if (firstError?.matches('.validation-summary-errors')) firstError.tabIndex = -1;
    firstError?.focus();
    $(() => {
        if (!$.validator) return;
        const { highlight, unhighlight } = $.validator.defaults;
        $.validator.setDefaults({
            highlight(el, ...rest) { highlight.call(this, el, ...rest); el.setAttribute('aria-invalid', 'true'); },
            unhighlight(el, ...rest) { unhighlight.call(this, el, ...rest); el.setAttribute('aria-invalid', 'false'); },
        });
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
