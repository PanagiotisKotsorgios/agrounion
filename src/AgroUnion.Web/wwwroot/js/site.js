(() => {
  const nav = document.querySelector('.main-nav');
  const toggle = document.querySelector('.nav-toggle');
  const backdrop = document.querySelector('[data-nav-backdrop]');

  const setToggleIcon = open => {
    if (!toggle) return;
    const icon = toggle.querySelector('i');
    if (icon) icon.className = open ? 'fa-solid fa-xmark' : 'fa-solid fa-bars';
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    toggle.setAttribute('aria-label', open ? 'Κλείσιμο μενού' : 'Άνοιγμα μενού');
  };
  const closeNav = () => {
    nav?.classList.remove('open');
    backdrop?.classList.remove('open');
    document.body.classList.remove('nav-open');
    setToggleIcon(false);
  };
  toggle?.addEventListener('click', () => {
    const open = !nav?.classList.contains('open');
    nav?.classList.toggle('open', open);
    backdrop?.classList.toggle('open', open);
    document.body.classList.toggle('nav-open', open);
    setToggleIcon(open);
  });
  backdrop?.addEventListener('click', closeNav);
  nav?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => closeNav()));

  const dropdowns = [...document.querySelectorAll('.nav-item')];
  const closeAllDropdowns = () => dropdowns.forEach(item => {
    item.classList.remove('open');
    item.querySelector('.dropdown-toggle')?.setAttribute('aria-expanded', 'false');
  });
  dropdowns.forEach(item => {
    const button = item.querySelector('.dropdown-toggle');
    if (!button) return;
    button.addEventListener('click', event => {
      event.stopPropagation();
      const willOpen = !item.classList.contains('open');
      closeAllDropdowns();
      if (willOpen) {
        item.classList.add('open');
        button.setAttribute('aria-expanded', 'true');
      }
    });
  });
  document.addEventListener('click', event => {
    if (!event.target.closest('.nav-item')) closeAllDropdowns();
  });
  document.addEventListener('keydown', event => {
    if (event.key !== 'Escape') return;
    closeAllDropdowns();
    closeNav();
    closeSidebar();
  });
  window.addEventListener('resize', () => {
    if (window.innerWidth > 1024) { closeNav(); closeSidebar(); }
  });

  const sidebar = document.querySelector('.portal-sidebar');
  const sidebarBackdrop = document.querySelector('.portal-sidebar-backdrop');
  const sidebarToggle = document.querySelector('[data-sidebar-toggle]');
  const closeSidebar = () => {
    sidebar?.classList.remove('open');
    sidebarBackdrop?.classList.remove('open');
    document.body.classList.remove('sidebar-open');
  };
  sidebarToggle?.addEventListener('click', () => {
    const open = !sidebar?.classList.contains('open');
    sidebar?.classList.toggle('open', open);
    sidebarBackdrop?.classList.toggle('open', open);
    document.body.classList.toggle('sidebar-open', open);
  });
  sidebarBackdrop?.addEventListener('click', closeSidebar);
  sidebar?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => closeSidebar()));

  const observer = 'IntersectionObserver' in window
    ? new IntersectionObserver(entries => entries.forEach(entry => entry.isIntersecting && entry.target.classList.add('visible')), { threshold: .12 })
    : null;
  document.querySelectorAll('.reveal').forEach(el => observer ? observer.observe(el) : el.classList.add('visible'));

  document.querySelectorAll('.toast button').forEach(button => button.addEventListener('click', () => button.parentElement.remove()));
  document.querySelectorAll('.toast').forEach(toast => window.setTimeout(() => toast.remove(), 8000));

  document.querySelectorAll('[data-hero-slideshow]').forEach(hero => {
    const slides = [...hero.querySelectorAll('.hero-slide')];
    if (slides.length < 2 || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    let current = 0;
    let timer;
    const show = index => {
      current = (index + slides.length) % slides.length;
      slides.forEach((slide, slideIndex) => slide.classList.toggle('is-active', slideIndex === current));
    };
    const stop = () => window.clearInterval(timer);
    const start = () => {
      stop();
      if (!document.hidden) timer = window.setInterval(() => show(current + 1), 6500);
    };

    document.addEventListener('visibilitychange', () => document.hidden ? stop() : start());
    show(0);
    start();
  });

  document.querySelectorAll('.password-toggle').forEach(button => button.addEventListener('click', () => {
    const input = button.parentElement.querySelector('input');
    if (!input) return;
    const show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    button.setAttribute('aria-label', show ? 'Απόκρυψη κωδικού' : 'Εμφάνιση κωδικού');
    button.querySelector('i').className = show ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye';
  }));

  document.querySelectorAll('[data-carousel]').forEach(carousel => {
    const slides = [...carousel.querySelectorAll('[data-carousel-slide]')];
    const dots = [...carousel.querySelectorAll('[data-carousel-dot]')];
    if (slides.length < 2) return;
    let current = 0;
    let timer;
    const show = index => {
      current = (index + slides.length) % slides.length;
      slides.forEach((slide, slideIndex) => {
        const active = slideIndex === current;
        slide.classList.toggle('is-active', active);
        slide.style.display = active ? '' : 'none';
        slide.setAttribute('aria-hidden', active ? 'false' : 'true');
      });
      dots.forEach((dot, dotIndex) => {
        const active = dotIndex === current;
        dot.classList.toggle('is-active', active);
        active ? dot.setAttribute('aria-current', 'true') : dot.removeAttribute('aria-current');
      });
    };
    const stop = () => window.clearInterval(timer);
    const start = () => {
      stop();
      if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) timer = window.setInterval(() => show(current + 1), 7000);
    };
    carousel.querySelector('[data-carousel-prev]')?.addEventListener('click', () => { show(current - 1); start(); });
    carousel.querySelector('[data-carousel-next]')?.addEventListener('click', () => { show(current + 1); start(); });
    dots.forEach(dot => dot.addEventListener('click', () => { show(Number(dot.dataset.carouselDot)); start(); }));
    carousel.addEventListener('mouseenter', stop);
    carousel.addEventListener('mouseleave', start);
    carousel.addEventListener('focusin', stop);
    carousel.addEventListener('focusout', start);
    show(0);
    start();
  });

  document.querySelectorAll('[data-confirm]').forEach(button => button.addEventListener('click', event => {
    if (!window.confirm(button.dataset.confirm)) event.preventDefault();
  }));
})();
