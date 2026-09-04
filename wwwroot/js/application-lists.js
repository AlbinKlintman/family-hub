(function () {
    setupListRepeater('descriptions-list', 'add-description', '.description-row', '.remove-description', 'textarea', 'Input.Descriptions', function () {
        return '<div class="d-flex gap-2 mb-2 description-row">' +
            '<textarea name="Input.Descriptions[0]" class="form-control" rows="3"></textarea>' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-description align-self-start">&times;</button>' +
            '</div>';
    });

    setupListRepeater('links-list', 'add-link', '.link-row', '.remove-link', 'input', 'Input.Links', function () {
        return '<div class="d-flex gap-2 mb-2 link-row">' +
            '<input type="url" name="Input.Links[0]" class="form-control" placeholder="https://..." />' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-link">&times;</button>' +
            '</div>';
    });
})();
