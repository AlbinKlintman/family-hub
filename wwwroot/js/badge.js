(function () {
    var POLL_MS = 2 * 60 * 1000;
    var navBadge = document.getElementById('nav-badge');

    // Signed-out pages don't render #nav-badge at all -- nothing to do.
    if (!navBadge) {
        return;
    }

    function applyCount(count) {
        if (count > 0) {
            navBadge.textContent = count > 99 ? '99+' : String(count);
            navBadge.hidden = false;
        } else {
            navBadge.hidden = true;
        }

        // Badging API: puts the number on the home-screen/app icon itself.
        // Supported on Chromium desktop/Android and installed iOS 16.4+ web
        // apps; silently unavailable elsewhere (Firefox, LibreWolf, unsupported
        // Safari), which is fine -- the navbar badge above already covers those.
        if ('setAppBadge' in navigator) {
            if (count > 0) {
                navigator.setAppBadge(count).catch(function () {});
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
                    applyCount(data.count);
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
