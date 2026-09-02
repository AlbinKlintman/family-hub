(function () {
    function isInsideForm(target) {
        return target.closest('form') !== null;
    }

    function navigate(card) {
        var href = card.dataset.href;
        if (href) {
            window.location.href = href;
        }
    }

    document.querySelectorAll('.note-card-clickable').forEach(function (card) {
        card.addEventListener('click', function (e) {
            if (isInsideForm(e.target)) {
                return;
            }
            navigate(card);
        });

        card.addEventListener('keydown', function (e) {
            if (isInsideForm(e.target)) {
                return;
            }
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                navigate(card);
            }
        });
    });
})();
