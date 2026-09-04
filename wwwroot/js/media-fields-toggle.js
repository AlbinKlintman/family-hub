(function () {
    var select = document.getElementById('media-type-select');
    if (!select) {
        return;
    }

    var groups = document.querySelectorAll('.media-fields');

    function apply() {
        var type = select.value;
        groups.forEach(function (group) {
            var types = group.getAttribute('data-types').split(',');
            group.hidden = types.indexOf(type) === -1;
        });
    }

    select.addEventListener('change', apply);
    apply();
})();
