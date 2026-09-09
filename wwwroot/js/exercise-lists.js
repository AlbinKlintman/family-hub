(function () {
    setupListRepeater('machine-weights-list', 'add-machine-weight', '.machine-weight-row', '.remove-machine-weight', 'input', 'Input.MachineWeights', function () {
        return '<div class="d-flex gap-2 mb-2 machine-weight-row">' +
            '<input type="number" step="0.5" name="Input.MachineWeights[0]" class="form-control" />' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-machine-weight">&times;</button>' +
            '</div>';
    });

    setupListRepeater('machine-addons-list', 'add-machine-addon', '.machine-addon-row', '.remove-machine-addon', 'input', 'Input.MachineAddOns', function () {
        return '<div class="d-flex gap-2 mb-2 machine-addon-row">' +
            '<input type="number" step="0.5" name="Input.MachineAddOns[0]" class="form-control" />' +
            '<button type="button" class="btn btn-outline-danger btn-sm remove-machine-addon">&times;</button>' +
            '</div>';
    });
})();
