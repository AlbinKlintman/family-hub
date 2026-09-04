(function () {
    setupListRepeater('links-list', 'add-link', '.link-row', '.remove-link', 'input', 'Input.Links', function () {
        return '<div class="d-flex gap-2 mb-2 link-row">' +
            '<input type="url" name="Input.Links[0]" class="form-control" placeholder="https://..." />' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-link">&times;</button>' +
            '</div>';
    });
})();
