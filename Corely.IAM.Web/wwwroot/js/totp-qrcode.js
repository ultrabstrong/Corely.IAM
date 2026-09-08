// Imported by TotpSection rather than loaded by the host. A component that needs a script the host
// must remember to add fails for every consumer that does not know to add it.
let libraryLoad;

function loadLibrary(libraryUrl) {
    if (window.QRCode) {
        // Already present - a host that loads the script tags itself still works unchanged.
        return Promise.resolve();
    }

    libraryLoad ??= new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = libraryUrl;
        script.onload = resolve;
        script.onerror = () => reject(new Error("Could not load " + libraryUrl));
        document.head.appendChild(script);
    });

    return libraryLoad;
}

export async function generate(elementId, text, size, libraryUrl) {
    await loadLibrary(libraryUrl);

    const container = document.getElementById(elementId);
    if (!container) {
        return;
    }

    container.replaceChildren();
    new QRCode(container, {
        text: text,
        width: size || 200,
        height: size || 200,
        correctLevel: QRCode.CorrectLevel.M,
    });
}
