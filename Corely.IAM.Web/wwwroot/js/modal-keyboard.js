// Keyboard handler for Blazor modals
window.modalKeyboard = {
    init: function (dotNetRef) {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                dotNetRef.invokeMethodAsync('OnEscapePressed');
            }
        });
    }
};
