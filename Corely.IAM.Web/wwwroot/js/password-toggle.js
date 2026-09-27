(function () {
    "use strict";

    function icon(name) {
        var i = document.createElement("i");
        i.className = "bi " + name;
        i.setAttribute("aria-hidden", "true");
        return i;
    }

    function enhance(input) {
        if (input.dataset.passwordToggle === "true") {
            return;
        }
        input.dataset.passwordToggle = "true";

        var group = document.createElement("div");
        group.className = "input-group";
        input.parentNode.insertBefore(group, input);
        group.appendChild(input);

        var button = document.createElement("button");
        button.type = "button";
        button.className = "btn btn-outline-secondary";
        button.tabIndex = -1;
        button.setAttribute("aria-label", "Show password");
        button.appendChild(icon("bi-eye"));
        button.addEventListener("click", function () {
            var shown = input.type === "text";
            input.type = shown ? "password" : "text";
            button.setAttribute("aria-label", shown ? "Show password" : "Hide password");
            button.replaceChildren(icon(shown ? "bi-eye" : "bi-eye-slash"));
        });
        group.appendChild(button);
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll('input[type="password"]').forEach(enhance);
    });
})();
