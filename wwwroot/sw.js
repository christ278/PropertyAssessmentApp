const CACHE_NAME = 'fairtrack-pwa-v2';
const PRECACHE_URLS = [
    '/offline.html',
    '/images/pwa/icon-192.png',
    '/images/pwa/icon-512.png',
    '/css/typography.css',
    '/css/style.css',
    '/lib/tailwindcss/tailwind.js',
    '/lib/lucide/lucide.min.js'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(
                keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

function isStaticAsset(pathname) {
    return pathname.startsWith('/css/') ||
        pathname.startsWith('/js/') ||
        pathname.startsWith('/lib/') ||
        pathname.startsWith('/fonts/') ||
        pathname.startsWith('/images/');
}

self.addEventListener('fetch', (event) => {
    const request = event.request;
    if (request.method !== 'GET') return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;
    if (url.pathname.startsWith('/hubs/') || url.pathname.startsWith('/api/')) return;

    // Network-first for HTML page navigations so auth/CSRF & live data stay fresh
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(() => caches.match('/offline.html'))
        );
        return;
    }

    // Stale-while-revalidate for static assets
    if (isStaticAsset(url.pathname)) {
        event.respondWith(
            caches.match(request).then((cached) => {
                const networkFetch = fetch(request).then((response) => {
                    if (response && response.status === 200 && response.type === 'basic') {
                        const copy = response.clone();
                        caches.open(CACHE_NAME).then((cache) => cache.put(request, copy));
                    }
                    return response;
                }).catch(() => cached);

                return cached || networkFetch;
            })
        );
    }
});
