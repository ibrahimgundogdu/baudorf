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
    const COOKIE_V = "bd_consent_v";
    const VERSION = cc.dataset.ccVersion || "";
    const POST_URL = cc.dataset.ccUrl || "";
    const token = cc.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
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

    const hide = () => { cc.hidden = true; document.body.style.overflow = ""; };
    const show = () => { cc.hidden = false; document.body.style.overflow = "hidden"; };
    const openCustomize = () => {
      prefs.hidden = false;
      btnCustomize.hidden = true;
      btnSave.hidden = false;
    };

    const save = (categories, how) => {
      setCookie(COOKIE, categories.join(","), 180);
      setCookie(COOKIE_V, VERSION, 180);
      hide();
      // Serverseitiger Nachweis (DSGVO Rechenschaftspflicht) — fire-and-forget.
      if (POST_URL) {
        try {
          fetch(POST_URL, {
            method: "POST",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token },
            body: JSON.stringify({ categories, version: VERSION, action: how }),
            keepalive: true,
          }).catch(() => {});
        } catch (e) { /* Nachweis optional; UI nie blockieren */ }
      }
      // Vorbereitet für künftiges Script-Gating:
      window.dispatchEvent(new CustomEvent("bd-consent", { detail: categories }));
    };

    cc.querySelectorAll("[data-cc-action]").forEach((b) => {
      b.addEventListener("click", () => {
        const action = b.dataset.ccAction;
        if (action === "accept") save(["necessary", "statistics", "marketing"], "accept");
        else if (action === "reject") save(["necessary"], "reject");
        else if (action === "customize") openCustomize();
        else if (action === "save") {
          const chosen = ["necessary"];
          cc.querySelectorAll("[data-cc]").forEach((t) => { if (t.checked) chosen.push(t.dataset.cc); });
          save(chosen, "custom");
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

    // Erneut einholen, wenn noch keine Einwilligung ODER der Textstand sich geändert hat.
    if (!readCookie(COOKIE) || readCookie(COOKIE_V) !== VERSION) show();
  }

  // ---------- Hero-Carousel (vanilla, CSP-sicher — kein eval/Alpine nötig) ----------
  const hero = document.getElementById("heroCarousel");
  if (hero) {
    const dataEl = document.getElementById("heroSlidesData");
    let slides = [];
    try { slides = JSON.parse(dataEl.textContent); } catch (e) { slides = []; }
    const n = slides.length;
    if (n > 0) {
      const bgs = hero.querySelectorAll(".hx-hero__bg");
      const dots = hero.querySelectorAll("[data-hero-dot]");
      const overlineWrap = hero.querySelector("[data-hero-overline]");
      const overlineSpan = hero.querySelector("[data-hero-overline] span");
      const titleEl = hero.querySelector("[data-hero-title]");
      const leadEl = hero.querySelector("[data-hero-lead]");
      const imageDuration = parseInt(hero.dataset.duration, 10) || 6000;
      let i = 0;
      let timer = null;

      const setText = (s) => {
        if (overlineSpan) overlineSpan.textContent = s.o || "";
        if (overlineWrap) overlineWrap.style.display = s.o ? "" : "none";
        if (titleEl) titleEl.innerHTML = s.t || "";
        if (leadEl) leadEl.textContent = s.x || "";
      };

      const activate = () => {
        clearTimeout(timer);
        bgs.forEach((b) => b.classList.toggle("is-on", Number(b.dataset.slide) === i));
        dots.forEach((d) => d.classList.toggle("is-on", Number(d.dataset.heroDot) === i));
        setText(slides[i]);
        hero.querySelectorAll("video.hx-hero__bg").forEach((v) => {
          if (Number(v.dataset.slide) !== i) { v.pause(); }
        });
        const s = slides[i];
        if (s && s.video) {
          const v = hero.querySelector('video.hx-hero__bg[data-slide="' + i + '"]');
          if (v) {
            v.muted = true;
            try { v.currentTime = 0; } catch (e) { }
            const p = v.play();
            if (p && p.catch) { p.catch(() => { if (n > 1) timer = setTimeout(next, imageDuration); }); }
          } else if (n > 1) {
            timer = setTimeout(next, imageDuration);
          }
        } else if (n > 1) {
          timer = setTimeout(next, imageDuration);
        }
      };

      function go(idx) { i = ((idx % n) + n) % n; activate(); }
      function next() { go(i + 1); }
      function prev() { go(i - 1); }

      const nextBtn = hero.querySelector("[data-hero-next]");
      const prevBtn = hero.querySelector("[data-hero-prev]");
      if (nextBtn) nextBtn.addEventListener("click", next);
      if (prevBtn) prevBtn.addEventListener("click", prev);
      dots.forEach((d) => d.addEventListener("click", () => go(Number(d.dataset.heroDot))));
      hero.querySelectorAll("video.hx-hero__bg").forEach((v) =>
        v.addEventListener("ended", () => { if (Number(v.dataset.slide) === i) next(); })
      );

      activate();
    }
  }

  // ---------- Nav: Scroll-Verkleinerung + Mobile-Menü (vanilla) ----------
  const nav = document.getElementById("siteNav");
  if (nav) {
    const onScroll = () => nav.classList.toggle("is-scrolled", window.scrollY > 30);
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();
  }
  const mobileMenu = document.getElementById("mobileMenu");
  if (mobileMenu) {
    const openBtn = document.querySelector("[data-menu-open]");
    const openMenu = () => { mobileMenu.classList.add("is-open"); document.body.style.overflow = "hidden"; };
    const closeMenu = () => { mobileMenu.classList.remove("is-open"); document.body.style.overflow = ""; };
    if (openBtn) openBtn.addEventListener("click", openMenu);
    mobileMenu.querySelectorAll("a, [data-menu-close]").forEach((el) => el.addEventListener("click", closeMenu));
    document.addEventListener("keydown", (e) => { if (e.key === "Escape") closeMenu(); });
  }

  // ---------- Objekt-Galerie + Lightbox (vanilla) ----------
  const gallery = document.querySelector("[data-gallery]");
  if (gallery) {
    const main = gallery.querySelector("[data-gallery-main]");
    const thumbs = Array.from(gallery.querySelectorAll("[data-gallery-thumb]"));
    const dots = Array.from(gallery.querySelectorAll("[data-gallery-dot]"));
    const srcs = thumbs.length
      ? thumbs.map((t) => t.dataset.full || t.querySelector("img")?.src)
      : (main ? [main.getAttribute("src")] : []);
    let idx = 0;

    const sync = () => {
      if (main && srcs[idx]) main.setAttribute("src", srcs[idx]);
      thumbs.forEach((t, k) => t.classList.toggle("is-active", k === idx));
      dots.forEach((d, k) => d.classList.toggle("is-on", k === idx));
    };
    const goTo = (i) => { idx = (i + srcs.length) % srcs.length; sync(); };

    thumbs.forEach((t, k) => t.addEventListener("click", (e) => { e.stopPropagation(); goTo(k); }));
    dots.forEach((d, k) => d.addEventListener("click", (e) => { e.stopPropagation(); goTo(k); }));

    // Lightbox (Vollansicht mit Navigation)
    const lightbox = document.getElementById("hxLightbox");
    if (lightbox && srcs.length) {
      const lbImg = lightbox.querySelector("img");
      const lbCount = lightbox.querySelector("[data-lb-count]");
      const paint = () => {
        if (lbImg && srcs[idx]) lbImg.src = srcs[idx];
        if (lbCount) lbCount.textContent = (idx + 1) + " / " + srcs.length;
      };
      const openLb = () => { paint(); lightbox.classList.add("is-open"); lightbox.setAttribute("aria-hidden", "false"); document.body.style.overflow = "hidden"; };
      const closeLb = () => { lightbox.classList.remove("is-open"); lightbox.setAttribute("aria-hidden", "true"); document.body.style.overflow = ""; };
      const nav = (delta) => { idx = (idx + delta + srcs.length) % srcs.length; sync(); paint(); };

      if (main) main.addEventListener("click", openLb);
      gallery.querySelector("[data-gallery-open]")?.addEventListener("click", (e) => { e.stopPropagation(); openLb(); });
      lightbox.querySelector("[data-lb-prev]")?.addEventListener("click", (e) => { e.stopPropagation(); nav(-1); });
      lightbox.querySelector("[data-lb-next]")?.addEventListener("click", (e) => { e.stopPropagation(); nav(1); });
      lightbox.querySelector("[data-lb-close]")?.addEventListener("click", closeLb);
      lightbox.addEventListener("click", (e) => { if (e.target === lightbox) closeLb(); });
      document.addEventListener("keydown", (e) => {
        if (!lightbox.classList.contains("is-open")) return;
        if (e.key === "Escape") closeLb();
        else if (e.key === "ArrowLeft") nav(-1);
        else if (e.key === "ArrowRight") nav(1);
      });
    }
  }

  // ---------- Tabs (Admin-Aktivität u. Ä., vanilla) ----------
  document.querySelectorAll("[data-tabs]").forEach((group) => {
    const btns = group.querySelectorAll("[data-tab]");
    const panels = group.querySelectorAll("[data-tab-panel]");
    const select = (name) => {
      btns.forEach((b) => {
        const on = b.dataset.tab === name;
        b.classList.toggle("bd-btn--ink", on);
        b.classList.toggle("bd-btn--plain", !on);
      });
      panels.forEach((p) => { p.hidden = p.dataset.tabPanel !== name; });
    };
    btns.forEach((b) => b.addEventListener("click", () => select(b.dataset.tab)));
  });

  // ---------- Testimonial-Rotator (mehrere Stimmen, automatisch wechselnd) ----------
  document.querySelectorAll("[data-testi-rotator]").forEach((root) => {
    const slides = root.querySelectorAll("[data-testi-slide]");
    if (slides.length < 2) return;
    const dots = root.querySelectorAll("[data-testi-dot]");
    let idx = 0, timer = null;
    const show = (n) => {
      idx = (n + slides.length) % slides.length;
      slides.forEach((s, i) => s.classList.toggle("is-active", i === idx));
      dots.forEach((d, i) => d.classList.toggle("is-on", i === idx));
    };
    const stop = () => { if (timer) { clearInterval(timer); timer = null; } };
    const start = () => { if (!reduced) { stop(); timer = setInterval(() => show(idx + 1), 6000); } };
    dots.forEach((d, i) => d.addEventListener("click", () => { show(i); start(); }));
    root.addEventListener("mouseenter", stop);
    root.addEventListener("mouseleave", start);
    start();
  });
})();
