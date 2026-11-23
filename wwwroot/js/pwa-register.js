(function () {
    let deferredInstallPrompt = null;

    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js', { scope: '/' }).catch(function (err) {
                console.warn('Service worker registration failed:', err);
            });
        });
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredInstallPrompt = e;
        const btn = document.getElementById('btnInstallPwa');
        if (btn) btn.classList.remove('hidden');
    });

    window.addEventListener('appinstalled', function () {
        deferredInstallPrompt = null;
        const btn = document.getElementById('btnInstallPwa');
        if (btn) btn.classList.add('hidden');
    });

    window.installFairTrackPwa = async function () {
        if (!deferredInstallPrompt) return;
        deferredInstallPrompt.prompt();
        await deferredInstallPrompt.userChoice;
        deferredInstallPrompt = null;
        const btn = document.getElementById('btnInstallPwa');
        if (btn) btn.classList.add('hidden');
    };
})();
