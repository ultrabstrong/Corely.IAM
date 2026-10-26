const modal = document.getElementById('components-reconnect-modal');

if (modal) {
    new MutationObserver(() => {
        if (modal.classList.contains('components-reconnect-rejected')) {
            location.reload();
        }
    }).observe(modal, { attributes: true, attributeFilter: ['class'] });
}
