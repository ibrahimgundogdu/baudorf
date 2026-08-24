// Mediathek-Seite: Upload mit Fortschritt, Bilder/Video-Tabs, Lightbox, Datei ersetzen, URL-Kopieren.
(function () {
  "use strict";

  // ---------- Upload mit Fortschrittsanzeige ----------
  var form = document.getElementById("mediaUpload");
  var input = document.getElementById("mediaFileInput");
  var progress = document.getElementById("mediaProgress");
  var bar = document.getElementById("mediaProgressBar");
  var txt = document.getElementById("mediaProgressTxt");

  if (form && input) {
    input.addEventListener("change", function () {
      if (!input.files.length) return;
      var fd = new FormData();
      var token = form.querySelector('input[name="__RequestVerificationToken"]');
      if (token) fd.append("__RequestVerificationToken", token.value);
      for (var i = 0; i < input.files.length; i++) fd.append("dateien", input.files[i]);

      var xhr = new XMLHttpRequest();
      xhr.open("POST", form.getAttribute("action"), true);
      xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");

      if (progress) progress.hidden = false;
      if (bar) bar.style.width = "0%";
      if (txt) txt.textContent = "Wird hochgeladen… 0%";

      xhr.upload.onprogress = function (e) {
        if (e.lengthComputable && bar) {
          var p = Math.round((e.loaded / e.total) * 100);
          bar.style.width = p + "%";
          if (txt) txt.textContent = "Wird hochgeladen… " + p + "%";
        }
      };
      xhr.onload = function () {
        if (xhr.status >= 200 && xhr.status < 300) {
          if (bar) bar.style.width = "100%";
          if (txt) txt.textContent = "Fertig — Ansicht wird aktualisiert…";
          setTimeout(function () { location.reload(); }, 500);
        } else if (txt) {
          txt.textContent = "Fehler beim Hochladen (" + xhr.status + "). Datei ggf. zu groß?";
        }
      };
      xhr.onerror = function () { if (txt) txt.textContent = "Fehler beim Hochladen."; };
      xhr.send(fd);
    });
  }

  // ---------- Tabs: Bilder / Videos ----------
  var grid = document.getElementById("mediaGrid");
  document.querySelectorAll("[data-mtab]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      document.querySelectorAll("[data-mtab]").forEach(function (b) { b.classList.toggle("is-active", b === btn); });
      if (grid) grid.setAttribute("data-mtab-view", btn.dataset.mtab);
    });
  });

  // ---------- Datei ersetzen (gleicher Name/URL) ----------
  // "Ersetzen" öffnet den versteckten Datei-Dialog; nach der Auswahl wird das Formular
  // sofort abgeschickt → Server überschreibt die Datei unter derselben URL.
  document.querySelectorAll("[data-replace-trigger]").forEach(function (btn) {
    var frm = btn.closest("form");
    var file = frm && frm.querySelector("[data-replace-input]");
    if (!file) return;
    btn.addEventListener("click", function () { file.click(); });
    file.addEventListener("change", function () {
      if (file.files && file.files.length) {
        btn.textContent = "Wird ersetzt…";
        btn.disabled = true;
        frm.submit();
      }
    });
  });

  // ---------- Lightbox (Bild + Video, mit Navigation je Tab) ----------
  var lb = document.getElementById("mediaLb");
  var stage = document.getElementById("mediaLbStage");
  var lbList = [], lbIdx = 0;

  function currentItems() {
    if (!grid) return [];
    var view = grid.getAttribute("data-mtab-view") || "image";
    return Array.prototype.slice.call(grid.querySelectorAll('.bd-media-card[data-mtype="' + view + '"] [data-lb]'));
  }
  function renderLb() {
    var el = lbList[lbIdx];
    if (!el || !stage) return;
    var src = el.getAttribute("data-lb-src");
    stage.innerHTML = el.getAttribute("data-lb-type") === "video"
      ? '<video src="' + src + '" controls autoplay playsinline></video>'
      : '<img src="' + src + '" alt="" />';
  }
  function openLb(el) {
    lbList = currentItems();
    lbIdx = Math.max(0, lbList.indexOf(el));
    renderLb();
    lb.classList.add("is-open");
    lb.setAttribute("aria-hidden", "false");
    document.body.style.overflow = "hidden";
  }
  function closeLb() {
    lb.classList.remove("is-open");
    lb.setAttribute("aria-hidden", "true");
    document.body.style.overflow = "";
    if (stage) stage.innerHTML = "";
  }
  function navLb(d) { if (lbList.length) { lbIdx = (lbIdx + d + lbList.length) % lbList.length; renderLb(); } }

  if (grid) {
    grid.addEventListener("click", function (e) {
      var el = e.target.closest("[data-lb]");
      if (el && lb) { e.preventDefault(); openLb(el); }
    });
  }
  if (lb) {
    lb.querySelector("[data-lb-close]").addEventListener("click", closeLb);
    lb.querySelector("[data-lb-prev]").addEventListener("click", function (e) { e.stopPropagation(); navLb(-1); });
    lb.querySelector("[data-lb-next]").addEventListener("click", function (e) { e.stopPropagation(); navLb(1); });
    lb.addEventListener("click", function (e) { if (e.target === lb) closeLb(); });
    document.addEventListener("keydown", function (e) {
      if (!lb.classList.contains("is-open")) return;
      if (e.key === "Escape") closeLb();
      else if (e.key === "ArrowLeft") navLb(-1);
      else if (e.key === "ArrowRight") navLb(1);
    });
  }

  // ---------- URL kopieren ----------
  document.querySelectorAll(".bd-media-card__url").forEach(function (inp) {
    inp.addEventListener("click", function () {
      inp.select();
      try { navigator.clipboard.writeText(inp.value); } catch (e) { try { document.execCommand("copy"); } catch (e2) {} }
      inp.classList.add("is-copied");
      setTimeout(function () { inp.classList.remove("is-copied"); }, 900);
    });
  });
})();
