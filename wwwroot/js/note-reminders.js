(function () {
    var list = document.getElementById('reminders-list');
    var addBtn = document.getElementById('add-reminder');
    if (!list || !addBtn) {
        return;
    }

    function rowHtml(index) {
        return '<div class="d-flex gap-2 mb-2 reminder-row align-items-center">' +
            '<input name="Input.Reminders[' + index + '].OffsetValue" type="number" min="1" value="30" class="form-control" style="max-width:6rem" />' +
            '<select name="Input.Reminders[' + index + '].OffsetUnit" class="form-select">' +
            '<option value="Minutes">Minutes before</option>' +
            '<option value="Hours">Hours before</option>' +
            '<option value="Days">Days before</option>' +
            '</select>' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-reminder">&times;</button>' +
            '</div>';
    }

    function reindex() {
        Array.from(list.children).forEach(function (row, i) {
            row.querySelector('input').name = 'Input.Reminders[' + i + '].OffsetValue';
            row.querySelector('select').name = 'Input.Reminders[' + i + '].OffsetUnit';
        });
    }

    addBtn.addEventListener('click', function () {
        list.insertAdjacentHTML('beforeend', rowHtml(list.children.length));
    });

    list.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-reminder')) {
            e.target.closest('.reminder-row').remove();
            reindex();
        }
    });
})();
