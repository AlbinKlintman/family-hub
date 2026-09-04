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

    // Temporary: reports whether the Badging API call actually worked on this
    // device, since it can't be inspected from a desktop browser's devtools.
    // Safe to remove once iOS home-screen badging is confirmed working.
    function reportDebug(info) {
        try {
            var payload = JSON.stringify(Object.assign({
                standalone: window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true
            }, info));
            if (navigator.sendBeacon) {
                navigator.sendBeacon('/api/badge-debug', payload);
            } else {
                fetch('/api/badge-debug', { method: 'POST', body: payload, credentials: 'same-origin' }).catch(function () {});
            }
        } catch (e) {}
    }

    function applyCounts(data) {
        targets.notes.forEach(function (el) { applyBadge(el, data.notes); });
        targets.applications.forEach(function (el) { applyBadge(el, data.applications); });

        // Badging API: puts the total on the home-screen/app icon itself.
        // Supported on Chromium desktop/Android and installed iOS 16.4+ web
        // apps; silently unavailable elsewhere (Firefox, LibreWolf, unsupported
        // Safari), which is fine -- the badges above already cover those.
        if ('setAppBadge' in navigator) {
            var call = data.total > 0 ? navigator.setAppBadge(data.total) : ('clearAppBadge' in navigator ? navigator.clearAppBadge() : Promise.resolve());
            call.then(function () {
                reportDebug({ hasSetAppBadge: true, total: data.total, result: 'ok' });
            }).catch(function (err) {
                reportDebug({ hasSetAppBadge: true, total: data.total, result: 'error', message: String(err) });
            });
        } else {
            reportDebug({ hasSetAppBadge: false, total: data.total });
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
