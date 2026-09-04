function setupListRepeater(listId, addBtnId, rowSelector, removeSelector, fieldSelector, fieldName, rowHtml) {
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
