window.qrCodeInterop = {
    generate: function (elementId, text, size) {
        var container = document.getElementById(elementId);
        if (!container) return;
        container.innerHTML = '';
        new QRCode(container, {
            text: text,
            width: size || 200,
            height: size || 200,
            correctLevel: QRCode.CorrectLevel.M
        });
    }
};
