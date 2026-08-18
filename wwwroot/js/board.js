(function () {
    const tokenMeta = document.querySelector('meta[name="request-verification-token"]');
    const csrfToken = tokenMeta ? tokenMeta.content : '';

    function updateColumnCount(columnEl) {
        const count = columnEl.querySelectorAll('.kanban-card').length;
        const badge = columnEl.querySelector('.kanban-count');
        if (badge) {
            badge.textContent = count;
        }
    }

    function markAppliedToday(cardEl) {
        const dateEl = cardEl.querySelector('.applied-date');
        if (dateEl) {
            dateEl.innerHTML = '<span>Applied today</span>';
        }
    }

    async function onCardMoved(evt) {
        const cardEl = evt.item;
        const targetColumn = evt.to.closest('.kanban-column');
        const sourceColumn = evt.from.closest('.kanban-column');

        const payload = {
            cardId: Number(cardEl.dataset.id),
            status: targetColumn.dataset.status,
            orderedCardIds: Array.from(evt.to.children).map(el => Number(el.dataset.id))
        };

        try {
            const response = await fetch('/Board?handler=Reorder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': csrfToken
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                location.reload();
                return;
            }

            const result = await response.json();
            if (result.appliedDate) {
                markAppliedToday(cardEl);
            }

            updateColumnCount(targetColumn);
            if (sourceColumn !== targetColumn) {
                updateColumnCount(sourceColumn);
            }
        } catch {
            location.reload();
        }
    }

    document.querySelectorAll('.kanban-cards').forEach(function (el) {
        new Sortable(el, {
            group: 'kanban',
            animation: 150,
            ghostClass: 'sortable-ghost',
            onEnd: onCardMoved
        });
    });
})();
