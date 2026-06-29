// Baudorf Admin — Medien-Picker + WYSIWYG (Quill) Verdrahtung. Vanilla JS, kein Framework.
(function () {
  "use strict";

  // ---------- Medien-Picker ----------
  const el = document.getElementById("bd-media-picker");
  let cb = null;

  if (el) {
    const listUrl = el.dataset.listUrl;
    const uploadUrl = el.dataset.uploadUrl;
    const tokenEl = el.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenEl ? tokenEl.value : "";
    const grid = el.querySelector('[data-mp-panel="library"]');
    const status = el.querySelector("[data-mp-status]");
    const fileInput = el.querySelector("#bd-mp-file");

    const close = () => { el.hidden = true; cb = null; };
    const choose = (url, asset) => { const c = cb; close(); if (c) c(url, asset); };

    async function loadLibrary() {
      grid.innerHTML = '<p class="bd-muted bd-mp__loading">Lädt…</p>';
      try {
        const res = await fetch(listUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        const items = await res.json();
        if (!items.length) { grid.innerHTML = '<p class="bd-muted">Noch keine Medien. Wechsle zu „Hochladen".</p>'; return; }
        grid.innerHTML = "";
        items.forEach((it) => {
          const b = document.createElement("button");
          b.type = "button";
          b.className = "bd-mp__item";
          b.style.backgroundImage = "url('" + it.url + "')";
          b.title = it.fileName || it.url;
          b.addEventListener("click", () => choose(it.url, it));
          grid.appendChild(b);
        });
      } catch {
        grid.innerHTML = '<p class="bd-muted">Fehler beim Laden.</p>';
      }
    }

    function open(callback) {
      cb = callback;
      el.hidden = false;
      // Standard-Tab: Mediathek
      el.querySelector('[data-mp-tab="library"]').click();
    }

    el.querySelectorAll("[data-mp-close]").forEach((x) => x.addEventListener("click", close));
    document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !el.hidden) close(); });

    el.querySelectorAll("[data-mp-tab]").forEach((t) => {
      t.addEventListener("click", () => {
        el.querySelectorAll("[data-mp-tab]").forEach((x) => x.classList.toggle("is-active", x === t));
        el.querySelectorAll("[data-mp-panel]").forEach((p) => { p.hidden = p.dataset.mpPanel !== t.dataset.mpTab; });
        if (t.dataset.mpTab === "library") loadLibrary();
      });
    });

    if (fileInput) {
      fileInput.addEventListener("change", async () => {
        if (!fileInput.files.length) return;
        status.textContent = "Lädt hoch…";
        const fd = new FormData();
        fd.append("__RequestVerificationToken", token);
        for (const f of fileInput.files) fd.append("dateien", f);
        try {
          const res = await fetch(uploadUrl, { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" }, body: fd });
          const data = await res.json();
          const errs = (data.errors || []);
          status.textContent = (data.ok ? data.ok.length : 0) + " hochgeladen" + (errs.length ? " · " + errs.length + " Fehler" : "");
          if (data.ok && data.ok.length === 1) { choose(data.ok[0].url, data.ok[0]); }
          else { el.querySelector('[data-mp-tab="library"]').click(); }
        } catch {
          status.textContent = "Fehler beim Hochladen.";
        }
        fileInput.value = "";
      });
    }

    window.BaudorfMedia = { open, close };
  }

  // ---------- WYSIWYG (Quill) ----------
  // Wandelt jede <textarea class="js-rte"> in einen Quill-Editor und synct den HTML-Inhalt
  // beim Absenden zurück in die Textarea (damit das Model-Binding HTML erhält).
  function initEditors() {
    if (typeof Quill === "undefined") return;
    document.querySelectorAll("textarea.js-rte").forEach((ta) => {
      if (ta.dataset.rteReady) return;
      ta.dataset.rteReady = "1";

      const holder = document.createElement("div");
      holder.className = "bd-rte";
      ta.parentNode.insertBefore(holder, ta);
      ta.style.display = "none";

      const quill = new Quill(holder, {
        theme: "snow",
        modules: {
          toolbar: {
            container: [
              [{ header: [2, 3, false] }],
              ["bold", "italic", "underline"],
              [{ list: "ordered" }, { list: "bullet" }],
              ["link", "image"],
              ["clean"],
            ],
            handlers: {
              image: function () {
                if (window.BaudorfMedia) {
                  window.BaudorfMedia.open((url) => {
                    const range = quill.getSelection(true);
                    quill.insertEmbed(range.index, "image", url, "user");
                    quill.setSelection(range.index + 1);
                  });
                } else {
                  const url = prompt("Bild-URL:");
                  if (url) {
                    const range = quill.getSelection(true);
                    quill.insertEmbed(range.index, "image", url, "user");
                  }
                }
              },
            },
          },
        },
      });

      quill.root.innerHTML = ta.value;
      const form = ta.closest("form");
      if (form) {
        form.addEventListener("submit", () => {
          const html = quill.root.innerHTML;
          ta.value = (html === "<p><br></p>") ? "" : html;
        });
      }
    });
  }

  if (document.readyState !== "loading") initEditors();
  else document.addEventListener("DOMContentLoaded", initEditors);
})();
