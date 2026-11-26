/* KABILIN shared behaviour: navigation, toasts, confirmation dialogs,
   duplicate-submit protection, password toggles, upload hints. */
(function () {
    "use strict";

    // ---- Mobile navigation ----
    var toggle = document.querySelector("[data-nav-toggle]");
    var scrim = document.querySelector(".scrim");
    function setNav(open) {
        document.body.classList.toggle("nav-open", open);
        if (toggle) toggle.setAttribute("aria-expanded", open ? "true" : "false");
    }
    if (toggle) toggle.addEventListener("click", function () { setNav(!document.body.classList.contains("nav-open")); });
    if (scrim) scrim.addEventListener("click", function () { setNav(false); });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") setNav(false); });

    // ---- Toasts ----
    document.querySelectorAll(".toast").forEach(function (t) {
        var close = function () { t.classList.add("hide"); setTimeout(function () { t.remove(); }, 280); };
        var btn = t.querySelector("button");
        if (btn) btn.addEventListener("click", close);
        setTimeout(close, t.classList.contains("toast-error") ? 8000 : 5000);
    });

    // ---- Confirmation dialog for destructive / important actions ----
    var dialog = document.getElementById("confirmDialog");
    if (dialog && typeof dialog.showModal === "function") {
        var pendingForm = null;
        var title = dialog.querySelector("[data-confirm-title]");
        var text = dialog.querySelector("[data-confirm-text]");
        var ok = dialog.querySelector("[data-confirm-ok]");
        document.addEventListener("click", function (e) {
            var trigger = e.target.closest("[data-confirm]");
            if (!trigger) return;
            var form = trigger.closest("form");
            if (!form) return;
            e.preventDefault();
            pendingForm = form;
            title.textContent = trigger.getAttribute("data-confirm-title") || "Please confirm";
            text.textContent = trigger.getAttribute("data-confirm");
            ok.textContent = trigger.getAttribute("data-confirm-button") || "Confirm";
            ok.className = "btn " + (trigger.getAttribute("data-confirm-danger") === "false" ? "btn-primary" : "btn-danger-solid");
            dialog.showModal();
        });
        dialog.querySelector("[data-confirm-cancel]").addEventListener("click", function () { dialog.close(); pendingForm = null; });
        ok.addEventListener("click", function () {
            dialog.close();
            if (pendingForm) { pendingForm.dataset.confirmed = "1"; pendingForm.requestSubmit(); }
        });
    }

    // ---- Prevent double submission + loading state ----
    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) return;
        setTimeout(function () {
            if (e.defaultPrevented) return;            // blocked by validation
            if (form.dataset.submitting === "1") return;
            form.dataset.submitting = "1";
            form.querySelectorAll("button[type=submit], input[type=submit]").forEach(function (b) {
                b.classList.add("is-loading");
                b.setAttribute("aria-disabled", "true");
            });
        }, 0);
        if (form.dataset.submitting === "1") e.preventDefault();
    }, true);
    // Re-enable when returning via the back/forward cache
    window.addEventListener("pageshow", function (ev) {
        if (!ev.persisted) return;
        document.querySelectorAll("form[data-submitting]").forEach(function (f) { delete f.dataset.submitting; });
        document.querySelectorAll(".is-loading").forEach(function (b) { b.classList.remove("is-loading"); });
    });

    // ---- Password visibility ----
    document.querySelectorAll("[data-password-toggle]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var input = btn.parentElement.querySelector("input");
            var show = input.type === "password";
            input.type = show ? "text" : "password";
            btn.setAttribute("aria-label", show ? "Hide password" : "Show password");
            btn.setAttribute("aria-pressed", show ? "true" : "false");
        });
    });

    // ---- Upload hints (client side; the server validates again) ----
    var MAX = 5 * 1024 * 1024, OK_EXT = [".pdf", ".jpg", ".jpeg", ".png"];
    document.querySelectorAll("input[type=file][data-upload]").forEach(function (input) {
        var msg = input.parentElement.querySelector("[data-upload-msg]");
        input.addEventListener("change", function () {
            var f = input.files && input.files[0];
            if (msg) { msg.textContent = ""; }
            if (!f) return;
            var ext = f.name.slice(f.name.lastIndexOf(".")).toLowerCase();
            var problem = OK_EXT.indexOf(ext) < 0 ? "Please choose a PDF, JPG or PNG file."
                        : f.size > MAX ? "This file is larger than 5 MB."
                        : f.size === 0 ? "This file is empty." : "";
            if (problem) { input.value = ""; if (msg) msg.textContent = problem; }
        });
    });
})();
