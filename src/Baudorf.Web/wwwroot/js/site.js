// Baudorf — leichte Interaktionen (kein Framework). Respektiert prefers-reduced-motion.
(function () {
  "use strict";

  const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // Scroll-Reveal via IntersectionObserver
  const revealEls = document.querySelectorAll("[data-reveal]");
  if (reduced || !("IntersectionObserver" in window)) {
    revealEls.forEach((el) => el.classList.add("is-visible"));
  } else {
    const io = new IntersectionObserver(
      (entries, obs) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-visible");
            obs.unobserve(entry.target);
          }
        });
      },
      { rootMargin: "0px 0px -10% 0px", threshold: 0.1 }
    );
    revealEls.forEach((el) => io.observe(el));
  }

  // ---------- Kennzahlen-Count-up (.hx-zahl b) ----------
  const counters = document.querySelectorAll(".hx-zahl b");
  if (counters.length) {
    const animateCount = (b) => {
      const node = b.firstChild; // Textknoten mit der Zahl (Suffix steckt im <span class="hx-unit">)
      if (!node || node.nodeType !== 3) return;
      const target = parseInt((node.nodeValue || "").replace(/\D/g, ""), 10);
      if (isNaN(target)) return;
      const fmt = (n) => n.toLocaleString("de-DE");
      if (reduced) { node.nodeValue = fmt(target); return; }
      const dur = 1400;
      const start = performance.now();
      const tick = (now) => {
        const p = Math.min(1, (now - start) / dur);
        const eased = 1 - Math.pow(1 - p, 3); // easeOutCubic
        node.nodeValue = fmt(Math.round(target * eased));
        if (p < 1) requestAnimationFrame(tick);
        else node.nodeValue = fmt(target);
      };
      requestAnimationFrame(tick);
    };

    if (reduced || !("IntersectionObserver" in window)) {
      counters.forEach(animateCount);
    } else {
      const cio = new IntersectionObserver(
        (entries, obs) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              animateCount(entry.target);
              obs.unobserve(entry.target);
            }
          });
        },
        { threshold: 0.4 }
      );
      counters.forEach((b) => cio.observe(b));
    }
  }

  // ---------- Cookie-Consent ----------
  const cc = document.getElementById("bd-cookie");
  if (cc) {
    const COOKIE = "bd_consent";
    const prefs = cc.querySelector(".bd-cc__prefs");
    const btnCustomize = cc.querySelector('[data-cc-action="customize"]');
    const btnSave = cc.querySelector('[data-cc-action="save"]');

    const readCookie = (name) => {
      const m = document.cookie.match("(?:^|; )" + name + "=([^;]*)");
      return m ? decodeURIComponent(m[1]) : null;
    };
    const setCookie = (name, value, days) => {
      const d = new Date();
      d.setTime(d.getTime() + days * 864e5);
      document.cookie = name + "=" + encodeURIComponent(value) + "; expires=" + d.toUTCString() + "; path=/; SameSite=Lax";
    };

    const hide = () => { cc.hidden = true; };
    const show = () => { cc.hidden = false; };
    const openCustomize = () => {
      prefs.hidden = false;
      btnCustomize.hidden = true;
      btnSave.hidden = false;
    };

    const save = (categories) => {
      setCookie(COOKIE, categories.join(","), 180);
      hide();
      // Vorbereitet für künftiges Script-Gating:
      window.dispatchEvent(new CustomEvent("bd-consent", { detail: categories }));
    };

    cc.querySelectorAll("[data-cc-action]").forEach((b) => {
      b.addEventListener("click", () => {
        const action = b.dataset.ccAction;
        if (action === "accept") save(["necessary", "statistics", "marketing"]);
        else if (action === "reject") save(["necessary"]);
        else if (action === "customize") openCustomize();
        else if (action === "save") {
          const chosen = ["necessary"];
          cc.querySelectorAll("[data-cc]").forEach((t) => { if (t.checked) chosen.push(t.dataset.cc); });
          save(chosen);
        }
      });
    });

    // Beim erneuten Öffnen den gespeicherten Stand vorbelegen.
    window.BaudorfCookie = {
      open: () => {
        const current = (readCookie(COOKIE) || "").split(",");
        cc.querySelectorAll("[data-cc]").forEach((t) => { t.checked = current.includes(t.dataset.cc); });
        openCustomize();
        show();
      },
    };

    if (!readCookie(COOKIE)) show();
  }
})();
