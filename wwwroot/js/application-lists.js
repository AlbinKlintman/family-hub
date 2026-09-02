(function () {
    function setupRepeater(listId, addBtnId, rowSelector, removeSelector, fieldSelector, fieldName, rowHtml) {
        var list = document.getElementById(listId);
        var addBtn = document.getElementById(addBtnId);
        if (!list || !addBtn) {
            return;
        }

        function reindex() {
            Array.from(list.children).forEach(function (row, i) {
                row.querySelector(fieldSelector).name = fieldName + '[' + i + ']';
            });
        }

        addBtn.addEventListener('click', function () {
            list.insertAdjacentHTML('beforeend', rowHtml());
            reindex();
        });

        list.addEventListener('click', function (e) {
            if (e.target.matches(removeSelector)) {
                e.target.closest(rowSelector).remove();
                reindex();
            }
        });
    }

    setupRepeater('descriptions-list', 'add-description', '.description-row', '.remove-description', 'textarea', 'Input.Descriptions', function () {
        return '<div class="d-flex gap-2 mb-2 description-row">' +
            '<textarea name="Input.Descriptions[0]" class="form-control" rows="3"></textarea>' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-description align-self-start">&times;</button>' +
            '</div>';
    });

    setupRepeater('links-list', 'add-link', '.link-row', '.remove-link', 'input', 'Input.Links', function () {
        return '<div class="d-flex gap-2 mb-2 link-row">' +
            '<input type="url" name="Input.Links[0]" class="form-control" placeholder="https://..." />' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-link">&times;</button>' +
            '</div>';
    });
})();
