// The "Confirm your email" page. It checks the status every few seconds and continues the sign in
// once the link has been opened, and counts the Resend button down to when another link is allowed.
// Without it the page still works: the buttons post as ordinary forms.
(function () {
    "use strict";

    var POLL_MS = 4000;

    function countDown(button, seconds) {
        var label = button.dataset.label || button.textContent;
        button.dataset.label = label;
        if (seconds <= 0) {
            button.disabled = false;
            button.textContent = label;
            return;
        }
        button.disabled = true;
        button.textContent = label + " (" + seconds + "s)";
        window.setTimeout(function () {
            countDown(button, seconds - 1);
        }, 1000);
    }

    function poll(statusForm, continueForm) {
        fetch(statusForm.action, {
            method: "POST",
            body: new FormData(statusForm),
            credentials: "same-origin",
        })
            .then(function (response) {
                return response.ok ? response.json() : null;
            })
            .then(function (status) {
                if (status && (status.state === "Confirmed" || status.state === "SignInExpired")) {
                    continueForm.requestSubmit();
                    return;
                }
                window.setTimeout(function () {
                    poll(statusForm, continueForm);
                }, POLL_MS);
            })
            .catch(function () {
                window.setTimeout(function () {
                    poll(statusForm, continueForm);
                }, POLL_MS);
            });
    }

    document.addEventListener("DOMContentLoaded", function () {
        var page = document.querySelector("[data-email-verification]");
        if (!page) {
            return;
        }

        var resend = page.querySelector("[data-resend-wait]");
        if (resend) {
            countDown(resend, parseInt(resend.dataset.resendWait, 10) || 0);
        }

        var statusForm = page.querySelector("[data-email-verification-status]");
        var continueForm = page.querySelector("[data-email-verification-continue]");
        if (statusForm && continueForm) {
            window.setTimeout(function () {
                poll(statusForm, continueForm);
            }, POLL_MS);
        }
    });
})();
