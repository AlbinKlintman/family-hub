(function () {
    document.querySelectorAll('[data-char-counter]').forEach(function (field) {
        var counter = document.getElementById(field.getAttribute('data-char-counter'));
        if (!counter) {
            return;
        }

        function update() {
            counter.textContent = field.value.length + ' / ' + field.maxLength;
        }

        field.addEventListener('input', update);
        update();
    });
})();
