/* Appsdemo Travel CRM front-end glue */
(function () {
    document.addEventListener('htmx:responseError', function (evt) {
        console.error('HTMX error:', evt.detail);
    });

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        var lightIcon = document.querySelector('.theme-icon-light');
        var darkIcon = document.querySelector('.theme-icon-dark');
        if (!lightIcon || !darkIcon) return;
        if (theme === 'dark') { lightIcon.classList.add('d-none'); darkIcon.classList.remove('d-none'); }
        else { lightIcon.classList.remove('d-none'); darkIcon.classList.add('d-none'); }
    }

    document.addEventListener('DOMContentLoaded', function () {
        var saved = localStorage.getItem('tabler-theme') || 'light';
        applyTheme(saved);
        var btn = document.getElementById('theme-toggle');
        if (!btn) return;
        btn.addEventListener('click', function () {
            var current = document.documentElement.getAttribute('data-bs-theme') || 'light';
            var next = current === 'dark' ? 'light' : 'dark';
            localStorage.setItem('tabler-theme', next);
            applyTheme(next);
        });
    });
})();
