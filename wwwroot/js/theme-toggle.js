(function () {
    var STORAGE_KEY = 'theme';
    var DARK_COLOR = '#000000';
    var LIGHT_COLOR = '#f2f2f7';

    var toggleButton = document.getElementById('theme-toggle-btn');
    if (!toggleButton) {
        return;
    }

    var metaThemeColor = document.querySelector('meta[name="theme-color"]');

    function currentTheme() {
        return document.documentElement.getAttribute('data-bs-theme') === 'light' ? 'light' : 'dark';
    }

    function updateButton(theme) {
        toggleButton.innerHTML = theme === 'light' ? '&#9728;&#65039; Light mode' : '&#127769; Dark mode';
        toggleButton.setAttribute('aria-label', theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode');
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        if (metaThemeColor) {
            metaThemeColor.setAttribute('content', theme === 'light' ? LIGHT_COLOR : DARK_COLOR);
        }
        updateButton(theme);
    }

    updateButton(currentTheme());

    toggleButton.addEventListener('click', function () {
        var next = currentTheme() === 'light' ? 'dark' : 'light';
        try {
            localStorage.setItem(STORAGE_KEY, next);
        } catch (e) {}
        applyTheme(next);
    });
})();
