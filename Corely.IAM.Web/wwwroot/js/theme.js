(function () {
    "use strict";

    var STORAGE_KEY = "corely-theme";
    var root = document.documentElement;

    function saved() {
        try {
            var value = window.localStorage.getItem(STORAGE_KEY);
            return value === "dark" || value === "light" ? value : null;
        } catch (e) {
            return null;
        }
    }

    function preferred() {
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function current() {
        return root.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
    }

    function label(theme) {
        return theme === "dark" ? "Use light theme" : "Use dark theme";
    }

    function labelToggles() {
        var next = label(current());
        document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
            button.setAttribute("aria-label", next);
            button.setAttribute("title", next);
        });
    }

    var applied = null;

    function apply(theme) {
        applied = theme;
        root.setAttribute("data-bs-theme", theme);
        labelToggles();
    }

    new MutationObserver(function () {
        if (applied && root.getAttribute("data-bs-theme") !== applied) {
            apply(applied);
        }
    }).observe(root, { attributes: true, attributeFilter: ["data-bs-theme"] });

    function toggle() {
        var next = current() === "dark" ? "light" : "dark";
        try {
            window.localStorage.setItem(STORAGE_KEY, next);
        } catch (e) {
        }
        apply(next);
        return next;
    }

    apply(saved() || preferred());

    if (window.matchMedia) {
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function () {
            if (!saved()) {
                apply(preferred());
            }
        });
    }

    document.addEventListener("DOMContentLoaded", labelToggles);

    document.addEventListener("click", function (event) {
        var button = event.target.closest("[data-theme-toggle]");
        if (button) {
            toggle();
        }
    });

    window.corelyTheme = { current: current, toggle: toggle };
})();
