if ('serviceWorker' in navigator) {
    window.addEventListener('load', function () {
        navigator.serviceWorker.register('/service-worker.js').catch(function () {
            // Registration failing (e.g. unsupported browser, private mode) is fine --
            // the app works the same without it, just without the offline/install extras.
        });
    });
}
