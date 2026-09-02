const STATIC_CACHE = 'family-hub-static-v1';
const STATIC_PATH_PREFIXES = ['/css/', '/js/', '/lib/', '/icons/'];
const STATIC_EXACT_PATHS = ['/manifest.webmanifest', '/WebApp.styles.css', '/favicon.ico'];
const OFFLINE_URL = '/offline.html';

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(STATIC_CACHE).then((cache) => cache.addAll([OFFLINE_URL]))
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys.filter((key) => key !== STATIC_CACHE).map((key) => caches.delete(key)))
        )
    );
    self.clients.claim();
});

function isStaticAsset(url) {
    return STATIC_PATH_PREFIXES.some((prefix) => url.pathname.startsWith(prefix))
        || STATIC_EXACT_PATHS.includes(url.pathname);
}

self.addEventListener('fetch', (event) => {
    const request = event.request;
    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) {
        return;
    }

    if (isStaticAsset(url)) {
        event.respondWith(cacheFirst(request));
        return;
    }

    if (request.mode === 'navigate') {
        event.respondWith(networkWithOfflineFallback(request));
    }
    // Everything else (pages, API-ish routes) goes straight to the network --
    // this app is per-user and mostly dynamic, so it's never safe to cache.
});

async function cacheFirst(request) {
    const cache = await caches.open(STATIC_CACHE);
    const cached = await cache.match(request);
    if (cached) {
        fetchAndCache(request, cache);
        return cached;
    }
    const response = await fetch(request);
    if (response.ok) {
        cache.put(request, response.clone());
    }
    return response;
}

async function fetchAndCache(request, cache) {
    try {
        const response = await fetch(request);
        if (response.ok) {
            await cache.put(request, response.clone());
        }
    } catch {
        // Offline -- the caller already got the cached version.
    }
}

async function networkWithOfflineFallback(request) {
    try {
        return await fetch(request);
    } catch {
        const cache = await caches.open(STATIC_CACHE);
        return (await cache.match(OFFLINE_URL)) ?? Response.error();
    }
}
