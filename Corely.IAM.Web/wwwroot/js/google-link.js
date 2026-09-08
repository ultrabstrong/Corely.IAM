// Loads Google Identity Services on demand so the profile page can link an account without the
// host adding a script tag it has no reason to know about.
const GIS_URL = "https://accounts.google.com/gsi/client";

let libraryLoad;

function loadLibrary() {
    if (window.google?.accounts?.id) {
        return Promise.resolve();
    }

    libraryLoad ??= new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = GIS_URL;
        script.async = true;
        script.onload = resolve;
        script.onerror = () => reject(new Error("Could not load Google Identity Services"));
        document.head.appendChild(script);
    });

    return libraryLoad;
}

// Renders Google's own button. Their terms require it rather than a look-alike, and it also keeps
// the consent dialog working across browsers that block the popup variant.
export async function renderLinkButton(elementId, clientId, dotNetRef) {
    await loadLibrary();

    const container = document.getElementById(elementId);
    if (!container) {
        return;
    }

    window.google.accounts.id.initialize({
        client_id: clientId,
        callback: (response) => dotNetRef.invokeMethodAsync("OnGoogleCredential", response.credential),
    });

    container.replaceChildren();
    window.google.accounts.id.renderButton(container, {
        type: "standard",
        size: "medium",
        theme: "outline",
        text: "signin_with",
        shape: "rectangular",
    });
}
