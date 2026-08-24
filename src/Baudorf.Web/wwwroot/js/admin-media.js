// Baudorf Admin — Medien-Picker + WYSIWYG (Quill) Verdrahtung. Vanilla JS, kein Framework.
(function () {
  "use strict";

  // ---------- Medien-Picker ----------
  const el = document.getElementById("bd-media-picker");
  let cb = null;
  let multi = false;      // Mehrfachauswahl-Modus
  let selected = [];      // gewählte URLs im Mehrfachmodus

  if (el) {
    const listUrl = el.dataset.listUrl;
    const uploadUrl = el.dataset.uploadUrl;
    const tokenEl = el.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenEl ? tokenEl.value : "";
    const grid = el.querySelector('[data-mp-panel="library"]');
    const status = el.querySelector("[data-mp-status]");
    const fileInput = el.querySelector("#bd-mp-file");
    const multiFoot = el.querySelector("[data-mp-multifoot]");
    const countEl = el.querySelector("[data-mp-count]");
    const confirmBtn = el.querySelector("[data-mp-confirm]");

    const updateCount = () => { if (countEl) countEl.textContent = selected.length + " ausgewählt"; };
    const close = () => { el.hidden = true; cb = null; multi = false; selected = []; if (multiFoot) multiFoot.hidden = true; };
    const choose = (url, asset) => { const c = cb; close(); if (c) c(url, asset); };

    async function loadLibrary() {
      grid.innerHTML = '<p class="bd-muted bd-mp__loading">Lädt…</p>';
      try {
        const res = await fetch(listUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        const items = await res.json();
        if (!items.length) { grid.innerHTML = '<p class="bd-muted">Noch keine Medien. Wechsle zu „Hochladen".</p>'; return; }
        grid.innerHTML = "";
        const isVideo = (u) => /\.(mp4|webm|ogg|ogv|mov)(\?|$)/i.test(u || "");
        items.forEach((it) => {
          const b = document.createElement("button");
          b.type = "button";
          b.className = "bd-mp__item";
          if (isVideo(it.url)) {
            b.classList.add("bd-mp__item--video");
            b.innerHTML = '<video src="' + it.url + '" muted preload="metadata"></video><span class="bd-mp__play">▶</span>';
          } else {
            b.style.backgroundImage = "url('" + it.url + "')";
          }
          b.title = it.fileName || it.url;
          if (multi && selected.indexOf(it.url) >= 0) b.classList.add("is-selected");
          b.addEventListener("click", () => {
            if (multi) {
              const i = selected.indexOf(it.url);
              if (i >= 0) { selected.splice(i, 1); b.classList.remove("is-selected"); }
              else { selected.push(it.url); b.classList.add("is-selected"); }
              updateCount();
            } else {
              choose(it.url, it);
            }
          });
          grid.appendChild(b);
        });
      } catch {
        grid.innerHTML = '<p class="bd-muted">Fehler beim Laden.</p>';
      }
    }

    function open(callback, opts) {
      cb = callback;
      multi = !!(opts && opts.multi);
      selected = [];
      if (multiFoot) multiFoot.hidden = !multi;
      updateCount();
      el.hidden = false;
      // Standard-Tab: Mediathek
      el.querySelector('[data-mp-tab="library"]').click();
    }

    // Mehrfachauswahl bestätigen → cb(Array der URLs).
    if (confirmBtn) {
      confirmBtn.addEventListener("click", () => {
        const c = cb; const urls = selected.slice();
        close();
        if (c) c(urls);
      });
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

    // Vorschau setzen und dabei — je nach Medientyp — <img> ⇆ <video> tauschen,
    // damit auch Video-URLs korrekt angezeigt werden (statt kaputtem Bild-Icon).
    const isVideoUrl = (u) => /\.(mp4|webm|ogg|ogv|mov)(\?|$)/i.test(u || "");
    function setPreview(selector, url) {
      let el = document.querySelector(selector);
      if (!el) return;
      const wantVideo = isVideoUrl(url);
      const isVideoEl = el.tagName === "VIDEO";
      if (wantVideo !== isVideoEl) {
        const neu = document.createElement(wantVideo ? "video" : "img");
        neu.id = el.id;
        neu.className = el.className;
        neu.setAttribute("style", el.getAttribute("style") || "");
        if (wantVideo) { neu.muted = true; neu.setAttribute("playsinline", ""); neu.setAttribute("preload", "metadata"); }
        else { neu.alt = ""; }
        el.parentNode.replaceChild(neu, el);
        el = neu;
      }
      el.src = url;
      el.style.display = url ? "block" : "none";
    }

    // Generische Feld-Picker: Button [data-media-pick] setzt ein Ziel-Input + optionale Vorschau.
    document.querySelectorAll("[data-media-pick]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const target = btn.dataset.mediaTarget ? document.querySelector(btn.dataset.mediaTarget) : null;
        open((url) => {
          if (target) target.value = url;
          if (btn.dataset.mediaPreview) setPreview(btn.dataset.mediaPreview, url);
        });
      });
    });

    // Galerie-Picker: Button [data-media-add="#formId"] wählt EIN ODER MEHRERE Medien aus der
    // Mediathek und hängt sie per fetch (robust, kein stilles form.submit) als Medien an.
    document.querySelectorAll("[data-media-add]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const form = document.querySelector(btn.dataset.mediaAdd);
        if (!form) return;
        const action = form.getAttribute("action");
        const formToken = form.querySelector('input[name="__RequestVerificationToken"]');
        open((urls) => {
          const list = Array.isArray(urls) ? urls : (urls ? [urls] : []);
          if (!list.length) return;
          const fd = new FormData();
          if (formToken) fd.append("__RequestVerificationToken", formToken.value);
          list.forEach((u) => fd.append("url", u));
          fetch(action, { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" }, body: fd })
            .then(function () { location.reload(); })
            .catch(function () { location.reload(); });
        }, { multi: true });
      });
    });
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
