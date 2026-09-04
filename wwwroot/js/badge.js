(function () {
    var POLL_MS = 2 * 60 * 1000;

    var targets = {
        notes: [document.getElementById('nav-badge-notes'), document.getElementById('tile-badge-notes')].filter(Boolean),
        applications: [document.getElementById('nav-badge-applications'), document.getElementById('tile-badge-applications')].filter(Boolean)
    };

    // Signed-out pages don't render any of these -- nothing to do.
    if (targets.notes.length === 0 && targets.applications.length === 0) {
        return;
    }

    function applyBadge(el, count) {
        if (count > 0) {
            el.textContent = count > 99 ? '99+' : String(count);
            el.hidden = false;
        } else {
            el.hidden = true;
        }
    }

    function applyCounts(data) {
        targets.notes.forEach(function (el) { applyBadge(el, data.notes); });
        targets.applications.forEach(function (el) { applyBadge(el, data.applications); });

        // Badging API: puts the total on the home-screen/app icon itself.
        // Supported on Chromium desktop/Android and installed iOS 16.4+ web
        // apps; silently unavailable elsewhere (Firefox, LibreWolf, unsupported
        // Safari), which is fine -- the badges above already cover those.
        if ('setAppBadge' in navigator) {
            if (data.total > 0) {
                navigator.setAppBadge(data.total).catch(function () {});
            } else if ('clearAppBadge' in navigator) {
                navigator.clearAppBadge().catch(function () {});
            }
        }
    }

    function refresh() {
        fetch('/api/badge-count', { credentials: 'same-origin' })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (data) {
                if (data) {
                    applyCounts(data);
                }
            })
            .catch(function () {});
    }

    refresh();
    setInterval(refresh, POLL_MS);
    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) {
            refresh();
        }
    });
})();
