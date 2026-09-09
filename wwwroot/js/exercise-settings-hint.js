(function () {
    var select = document.getElementById('add-exercise-select');
    var hint = document.getElementById('add-exercise-settings-hint');
    if (!select || !hint) {
        return;
    }

    function apply() {
        var option = select.options[select.selectedIndex];
        var forward = option ? option.getAttribute('data-seat-forward') : null;
        var height = option ? option.getAttribute('data-seat-height') : null;

        if (!forward && !height) {
            hint.hidden = true;
            hint.textContent = '';
            return;
        }

        var parts = [];
        if (forward) {
            parts.push('↔ ' + forward);
        }
        if (height) {
            parts.push('↕ ' + height);
        }
        hint.textContent = 'Set the machine to: ' + parts.join('  ');
        hint.hidden = false;
    }

    select.addEventListener('change', apply);
    apply();
})();
