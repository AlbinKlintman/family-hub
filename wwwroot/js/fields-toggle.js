(function () {
    document.querySelectorAll('[data-fields-toggle]').forEach(function (select) {
        var groups = document.querySelectorAll('.' + select.dataset.fieldsToggle);

        function apply() {
            var value = select.value;
            groups.forEach(function (group) {
                var types = group.getAttribute('data-types').split(',');
                group.hidden = types.indexOf(value) === -1;
            });
        }

        select.addEventListener('change', apply);
        apply();
    });
})();
