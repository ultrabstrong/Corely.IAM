// Progressive enhancement for full-page form posts. Without it the form still posts, which is why
// this is a script rather than markup that assumes it ran.
(function () {
    "use strict";

    var SPINNER_DELAY_MS = 150;

    function spinner() {
        var span = document.createElement("span");
        span.className = "spinner-border spinner-border-sm";
        span.setAttribute("role", "status");
        span.setAttribute("aria-hidden", "true");
        return span;
    }

    function showBusy(button) {
        // Width is captured before the label is replaced so the control does not resize and shift
        // the card around it.
        button.style.minWidth = button.offsetWidth + "px";
        button.replaceChildren(spinner());
    }

    function onSubmit(form) {
        var buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
        if (buttons.length === 0) {
            return;
        }

        form.setAttribute("aria-busy", "true");

        buttons.forEach(function (button) {
            // Deferred so the button's own name and value are still part of the submitted payload;
            // a disabled control is omitted from form data.
            window.setTimeout(function () {
                button.disabled = true;
                button.setAttribute("aria-busy", "true");
            }, 0);

            // Anything that resolves faster than the delay should show nothing at all - a spinner
            // that appears and vanishes reads as a glitch.
            if (button.hasAttribute("data-busy-spinner")) {
                window.setTimeout(function () {
                    showBusy(button);
                }, SPINNER_DELAY_MS);
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("form").forEach(function (form) {
            var method = (form.getAttribute("method") || "get").toLowerCase();
            if (method !== "post") {
                return;
            }

            form.addEventListener("submit", function () {
                // A form that failed constraint validation never raises submit, so there is no
                // stuck button to recover from here.
                if (form.dataset.busySubmitted === "true") {
                    return;
                }
                form.dataset.busySubmitted = "true";
                onSubmit(form);
            });
        });
    });
})();
