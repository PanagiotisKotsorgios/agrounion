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

  let passwordFieldIndex = 0;
  const enhancePasswordField = input => {
    if (!(input instanceof HTMLInputElement) || input.dataset.passwordToggleReady === 'true') return;

    input.dataset.passwordToggleReady = 'true';
    if (!input.id) input.id = `password-field-${++passwordFieldIndex}`;

    const wrapper = document.createElement('span');
    wrapper.className = 'password-field-control';
    input.parentNode.insertBefore(wrapper, input);
    wrapper.appendChild(input);

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'password-visibility-toggle';
    button.setAttribute('aria-controls', input.id);
    button.setAttribute('aria-pressed', 'false');
    button.setAttribute('aria-label', 'Εμφάνιση κωδικού πρόσβασης');
    button.title = 'Εμφάνιση κωδικού πρόσβασης';
    button.innerHTML = '<i class="fa-regular fa-eye" aria-hidden="true"></i>';

    button.addEventListener('click', () => {
      const reveal = input.type === 'password';
      const label = reveal ? 'Απόκρυψη κωδικού πρόσβασης' : 'Εμφάνιση κωδικού πρόσβασης';
      input.type = reveal ? 'text' : 'password';
      button.setAttribute('aria-pressed', reveal ? 'true' : 'false');
      button.setAttribute('aria-label', label);
      button.title = label;
      button.querySelector('i').className = reveal ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye';
      input.focus({ preventScroll: true });
    });

    wrapper.appendChild(button);
  };

  document.querySelectorAll('input[type="password"]').forEach(enhancePasswordField);

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

  const dashboardModals = [...document.querySelectorAll('[data-dashboard-modal]')];
  let activeDashboardModal = null;
  let dashboardModalTrigger = null;
  const closeDashboardModal = modal => {
    if (!modal) return;
    modal.hidden = true;
    modal.setAttribute('aria-hidden', 'true');
    if (activeDashboardModal === modal) activeDashboardModal = null;
    document.body.classList.remove('dashboard-modal-open');
    dashboardModalTrigger?.focus();
    dashboardModalTrigger = null;
  };
  const openDashboardModal = (modal, trigger) => {
    if (!modal) return;
    if (activeDashboardModal) closeDashboardModal(activeDashboardModal);
    dashboardModalTrigger = trigger;
    activeDashboardModal = modal;
    modal.hidden = false;
    modal.setAttribute('aria-hidden', 'false');
    document.body.classList.add('dashboard-modal-open');
    window.requestAnimationFrame(() => modal.querySelector('.dashboard-modal-dialog')?.focus());
  };
  document.querySelectorAll('[data-dashboard-modal-open]').forEach(button => button.addEventListener('click', () => {
    openDashboardModal(document.getElementById(button.dataset.dashboardModalOpen), button);
  }));
  dashboardModals.forEach(modal => modal.querySelectorAll('[data-dashboard-modal-close]').forEach(button => {
    button.addEventListener('click', () => closeDashboardModal(modal));
  }));

  const openPdfPreview = (url, downloadUrl, trigger) => {
    if (!url) return;
    const modal = document.getElementById('pdf-preview-modal');
    if (!modal) return;
    const frame = modal.querySelector('[data-pdf-preview]');
    const download = modal.querySelector('[data-pdf-download]');
    if (frame) frame.src = url;
    if (download) download.href = downloadUrl || url;
    openDashboardModal(modal, trigger);
  };

  document.querySelectorAll('[data-file-preview]').forEach(button => button.addEventListener('click', event => {
    event.preventDefault();
    event.stopPropagation();
    openPdfPreview(button.dataset.filePreview, button.dataset.fileDownload, button);
  }));

  document.querySelectorAll('[data-record-browser]').forEach(panel => {
    const rows = [...panel.querySelectorAll('[data-record-row]')];
    const search = panel.querySelector('[data-record-search]');
    const filters = [...panel.querySelectorAll('[data-record-filter]')];
    const period = panel.querySelector('[data-record-period]');
    const count = panel.querySelector('[data-record-count]');
    const empty = panel.querySelector('[data-record-empty]');
    const pageLabel = panel.querySelector('[data-record-page]');
    const previous = panel.querySelector('[data-record-prev]');
    const next = panel.querySelector('[data-record-next]');
    const pageSize = Number(panel.dataset.pageSize) || 6;
    let currentPage = 1;

    const filteredRows = () => {
      const term = (search?.value || '').trim().toLocaleLowerCase('el-GR');
      const days = period?.value === 'all' || !period ? 0 : Number(period.value || 0);
      const cutoff = days ? new Date(Date.now() - days * 86400000) : null;
      return rows.filter(row => {
        const matchesSearch = !term || (row.dataset.search || '').includes(term);
        const matchesFilters = filters.every(filter => filter.value === 'all' || row.dataset[filter.dataset.recordFilter] === filter.value);
        const rowDate = row.dataset.date ? new Date(row.dataset.date + 'T00:00:00') : null;
        return matchesSearch && matchesFilters && (!cutoff || !rowDate || rowDate >= cutoff);
      });
    };
    const render = () => {
      const filtered = filteredRows();
      const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
      currentPage = Math.min(currentPage, pages);
      const start = (currentPage - 1) * pageSize;
      rows.forEach(row => { row.hidden = true; });
      filtered.slice(start, start + pageSize).forEach(row => { row.hidden = false; });
      if (count) count.textContent = filtered.length + (filtered.length === 1 ? ' εγγραφή' : ' εγγραφές');
      if (empty) empty.hidden = filtered.length > 0 || rows.length === 0;
      if (pageLabel) pageLabel.textContent = 'Σελίδα ' + currentPage + ' από ' + pages;
      if (previous) previous.disabled = currentPage <= 1;
      if (next) next.disabled = currentPage >= pages;
    };
    const showDetails = row => {
      const modal = document.getElementById(row.dataset.detailModal || '');
      if (!modal) return;
      modal.querySelectorAll('[data-record-field]').forEach(field => {
        field.textContent = row.dataset['detail' + field.dataset.recordField.charAt(0).toUpperCase() + field.dataset.recordField.slice(1)] || '—';
      });
      modal.querySelectorAll('[data-record-value]').forEach(field => {
        field.value = row.dataset['detail' + field.dataset.recordValue.charAt(0).toUpperCase() + field.dataset.recordValue.slice(1)] || '';
      });
      const preview = modal.querySelector('[data-record-preview]');
      const download = modal.querySelector('[data-record-download]');
      const fileUrl = row.dataset.detailFile || '';
      const downloadUrl = row.dataset.detailDownload || fileUrl;
      if (preview) {
        preview.hidden = !fileUrl;
        preview.dataset.previewUrl = fileUrl;
        preview.dataset.downloadUrl = downloadUrl;
      }
      if (download) {
        download.hidden = !downloadUrl;
        if (downloadUrl) download.href = downloadUrl;
      }
      openDashboardModal(modal, row);
    };

    [search, period, ...filters].forEach(control => control?.addEventListener(control === search ? 'input' : 'change', () => { currentPage = 1; render(); }));
    previous?.addEventListener('click', () => { currentPage -= 1; render(); });
    next?.addEventListener('click', () => { currentPage += 1; render(); });
    rows.forEach(row => {
      row.querySelectorAll('[data-record-open]').forEach(button => button.addEventListener('click', () => showDetails(row)));
      row.addEventListener('click', event => {
        if (event.target.closest('button,a,form,input,select,textarea,label')) return;
        showDetails(row);
      });
      row.addEventListener('keydown', event => {
        if ((event.key === 'Enter' || event.key === ' ') && !event.target.closest('button,a,input,select,textarea')) { event.preventDefault(); showDetails(row); }
      });
    });
    render();
  });

  document.querySelectorAll('[data-record-preview]').forEach(button => button.addEventListener('click', () => {
    openPdfPreview(button.dataset.previewUrl, button.dataset.downloadUrl, button);
  }));

  document.querySelectorAll('[data-delivery-table]').forEach(tablePanel => {
    const rows = [...tablePanel.querySelectorAll('[data-delivery-row]')];
    const search = tablePanel.querySelector('[data-delivery-search]');
    const count = tablePanel.querySelector('[data-delivery-count]');
    const empty = tablePanel.querySelector('[data-delivery-empty]');
    const pageLabel = tablePanel.querySelector('[data-delivery-page]');
    const previous = tablePanel.querySelector('[data-delivery-prev]');
    const next = tablePanel.querySelector('[data-delivery-next]');
    const detailsModal = document.getElementById('delivery-details-modal');
    const pageSize = Number(tablePanel.dataset.pageSize) || 5;
    let currentPage = 1;

    const visibleRows = () => {
      const term = (search?.value || '').trim().toLocaleLowerCase('el-GR');
      return term ? rows.filter(row => (row.dataset.search || '').includes(term)) : rows;
    };
    const renderRows = () => {
      const filtered = visibleRows();
      const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
      currentPage = Math.min(currentPage, pages);
      const start = (currentPage - 1) * pageSize;
      rows.forEach(row => { row.hidden = true; });
      filtered.slice(start, start + pageSize).forEach(row => { row.hidden = false; });
      if (count) count.textContent = filtered.length + (filtered.length === 1 ? ' παράδοση' : ' παραδόσεις');
      if (empty) empty.hidden = filtered.length > 0;
      if (pageLabel) pageLabel.textContent = 'Σελίδα ' + currentPage + ' από ' + pages;
      if (previous) previous.disabled = currentPage <= 1;
      if (next) next.disabled = currentPage >= pages;
    };
    const showDetails = row => {
      if (!detailsModal) return;
      detailsModal.querySelectorAll('[data-delivery-field]').forEach(field => {
        field.textContent = row.dataset[field.dataset.deliveryField] || '—';
      });
      openDashboardModal(detailsModal, row);
    };

    search?.addEventListener('input', () => { currentPage = 1; renderRows(); });
    previous?.addEventListener('click', () => { currentPage -= 1; renderRows(); });
    next?.addEventListener('click', () => { currentPage += 1; renderRows(); });
    rows.forEach(row => {
      row.addEventListener('click', () => showDetails(row));
      row.addEventListener('keydown', event => {
        if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); showDetails(row); }
      });
    });
    renderRows();
  });

  document.querySelectorAll('[data-producer-prices]').forEach(panel => {
    const rows = [...panel.querySelectorAll('[data-price-row]')];
    const search = panel.querySelector('[data-price-search]');
    const source = panel.querySelector('[data-price-source]');
    const count = panel.querySelector('[data-price-count]');
    const empty = panel.querySelector('[data-price-empty]');
    const render = () => {
      const term = (search?.value || '').trim().toLocaleLowerCase('el-GR');
      const selectedSource = source?.value || 'all';
      let visible = 0;
      rows.forEach(row => {
        const matchesTerm = !term || (row.dataset.search || '').includes(term);
        const matchesSource = selectedSource === 'all' || row.dataset.source === selectedSource;
        row.hidden = !(matchesTerm && matchesSource);
        if (!row.hidden) visible += 1;
      });
      if (count) count.textContent = visible + (visible === 1 ? ' εγγραφή' : ' εγγραφές');
      if (empty) empty.hidden = visible > 0 || rows.length === 0;
    };
    search?.addEventListener('input', render);
    source?.addEventListener('change', render);
    render();
  });

  document.querySelectorAll('.delivery-record-form').forEach(form => {
    const input = key => form.querySelector(`[data-settlement-input="${key}"]`);
    const output = key => form.querySelector(`[data-settlement-output="${key}"]`);
    const numberValue = key => Math.max(0, Number(input(key)?.value) || 0);
    const numberFormat = new Intl.NumberFormat('el-GR', { maximumFractionDigits: 3 });
    const moneyFormat = new Intl.NumberFormat('el-GR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const renderSettlementPreview = () => {
      const loaded = Math.max(0, numberValue('gross') - numberValue('tare'));
      const accepted = Math.max(0, loaded - numberValue('rejected'));
      const producerValue = accepted * numberValue('producer-price');
      const factoryValue = accepted * numberValue('factory-price');
      if (output('loaded')) output('loaded').textContent = numberFormat.format(loaded) + ' kg';
      if (output('accepted')) output('accepted').textContent = numberFormat.format(accepted) + ' kg';
      if (output('producer-value')) output('producer-value').textContent = moneyFormat.format(producerValue) + ' €';
      if (output('factory-value')) output('factory-value').textContent = moneyFormat.format(factoryValue) + ' €';
    };
    form.querySelectorAll('[data-settlement-input]').forEach(element => element.addEventListener('input', renderSettlementPreview));
    renderSettlementPreview();
  });

  document.querySelectorAll('[data-admin-route-filters]').forEach(filters => {
    const container = filters.closest('.workspace-editor') || document;
    const rows = [...container.querySelectorAll('[data-admin-route-row]')];
    const search = filters.querySelector('[data-admin-route-search]');
    const status = filters.querySelector('[data-admin-route-status]');
    const factory = filters.querySelector('[data-admin-route-factory]');
    const count = filters.querySelector('[data-admin-route-count]');
    const empty = container.querySelector('[data-admin-route-empty]');
    const render = () => {
      const term = (search?.value || '').trim().toLocaleLowerCase('el-GR');
      const selectedStatus = status?.value || 'all';
      const selectedFactory = factory?.value || 'all';
      let visible = 0;
      rows.forEach(row => {
        const matches = (!term || (row.dataset.search || '').includes(term))
          && (selectedStatus === 'all' || row.dataset.status === selectedStatus)
          && (selectedFactory === 'all' || row.dataset.factory === selectedFactory);
        row.hidden = !matches;
        if (matches) visible += 1;
      });
      if (count) count.textContent = visible + (visible === 1 ? ' δρομολόγιο' : ' δρομολόγια');
      if (empty) empty.hidden = visible > 0 || rows.length === 0;
    };
    search?.addEventListener('input', render);
    status?.addEventListener('change', render);
    factory?.addEventListener('change', render);
    render();
  });

  document.querySelectorAll('[data-finance-ledger]').forEach(panel => {
    const rows = [...panel.querySelectorAll('[data-finance-row]')];
    const search = panel.querySelector('[data-finance-search]');
    const category = panel.querySelector('[data-finance-category]');
    const type = panel.querySelector('[data-finance-type]');
    const period = panel.querySelector('[data-finance-period]');
    const count = panel.querySelector('[data-finance-count]');
    const empty = panel.querySelector('[data-finance-empty]');
    const pageLabel = panel.querySelector('[data-finance-page]');
    const previous = panel.querySelector('[data-finance-prev]');
    const next = panel.querySelector('[data-finance-next]');
    const pageSize = Number(panel.dataset.pageSize) || 8;
    let currentPage = 1;

    const filteredRows = () => {
      const term = (search?.value || '').trim().toLocaleLowerCase('el-GR');
      const selectedCategory = category?.value || 'all';
      const selectedType = type?.value || 'all';
      const days = period?.value === 'all' ? 0 : Number(period?.value || 0);
      const cutoff = days ? new Date(Date.now() - days * 86400000) : null;
      return rows.filter(row => {
        const rowDate = new Date((row.dataset.date || '') + 'T00:00:00');
        return (!term || (row.dataset.search || '').includes(term))
          && (selectedCategory === 'all' || row.dataset.category === selectedCategory)
          && (selectedType === 'all' || row.dataset.type === selectedType)
          && (!cutoff || rowDate >= cutoff);
      });
    };
    const render = () => {
      const filtered = filteredRows();
      const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
      currentPage = Math.min(currentPage, pages);
      const start = (currentPage - 1) * pageSize;
      rows.forEach(row => { row.hidden = true; });
      filtered.slice(start, start + pageSize).forEach(row => { row.hidden = false; });
      if (count) count.textContent = filtered.length + (filtered.length === 1 ? ' κίνηση' : ' κινήσεις');
      if (empty) empty.hidden = filtered.length > 0 || rows.length === 0;
      if (pageLabel) pageLabel.textContent = 'Σελίδα ' + currentPage + ' από ' + pages;
      if (previous) previous.disabled = currentPage <= 1;
      if (next) next.disabled = currentPage >= pages;
    };
    [search, category, type, period].forEach(control => control?.addEventListener(control === search ? 'input' : 'change', () => { currentPage = 1; render(); }));
    previous?.addEventListener('click', () => { currentPage -= 1; render(); });
    next?.addEventListener('click', () => { currentPage += 1; render(); });
    render();
  });

  if (window.Chart) {
    const chartFont = getComputedStyle(document.documentElement).getPropertyValue('--sans').trim() || 'IBM Plex Sans';
    const gridColor = 'rgba(45, 63, 52, .09)';
    const tickColor = '#68736c';
    const parseChartData = (element, key) => {
      try { return JSON.parse(element.dataset[key] || '[]'); } catch { return []; }
    };

    document.querySelectorAll('[data-producer-chart="deliveries"]').forEach(canvas => {
      const context = canvas.getContext('2d');
      const gradient = context.createLinearGradient(0, 0, 0, 260);
      gradient.addColorStop(0, 'rgba(31, 91, 57, .24)');
      gradient.addColorStop(1, 'rgba(31, 91, 57, 0)');
      new window.Chart(canvas, {
        data: {
          labels: parseChartData(canvas, 'labels'),
          datasets: [
            { type:'bar', label:'Παραδοτέος όγκος', data:parseChartData(canvas, 'weights'), yAxisID:'weight', backgroundColor:'rgba(181, 139, 69, .7)', borderRadius:5, maxBarThickness:28 },
            { type:'line', label:'Καθαρή αξία', data:parseChartData(canvas, 'revenue'), yAxisID:'value', borderColor:'#1f5b39', backgroundColor:gradient, borderWidth:2.3, pointRadius:3, pointHoverRadius:5, pointBackgroundColor:'#fff', pointBorderWidth:2, tension:.35, fill:true }
          ]
        },
        options: {
          responsive:true, maintainAspectRatio:false, interaction:{mode:'index',intersect:false},
          animation:{duration:1100,easing:'easeOutQuart'},
          plugins:{legend:{display:false},tooltip:{padding:12,bodyFont:{family:chartFont},titleFont:{family:chartFont,weight:'600'}}},
          scales:{
            x:{grid:{display:false},ticks:{color:tickColor,font:{family:chartFont,size:10}}},
            weight:{position:'left',beginAtZero:true,grid:{color:gridColor},ticks:{color:tickColor,font:{family:chartFont,size:9},callback:value=>value + ' kg'}},
            value:{position:'right',beginAtZero:true,grid:{display:false},ticks:{color:tickColor,font:{family:chartFont,size:9},callback:value=>value + ' €'}}
          }
        }
      });
    });

    document.querySelectorAll('[data-producer-chart="payments"]').forEach(canvas => {
      new window.Chart(canvas, {
        type:'doughnut',
        data:{labels:['Πληρωμένα','Υπόλοιπο'],datasets:[{data:[Number(canvas.dataset.paid)||0,Number(canvas.dataset.outstanding)||0],backgroundColor:['#1f5b39','#d8c49c'],borderWidth:0,hoverOffset:4}]},
        options:{responsive:true,maintainAspectRatio:false,cutout:'76%',animation:{animateRotate:true,duration:1200,easing:'easeOutQuart'},plugins:{legend:{display:false},tooltip:{padding:11,bodyFont:{family:chartFont},callbacks:{label:item=>item.label + ': ' + Number(item.raw).toLocaleString('el-GR') + ' €'}}}}
      });
    });

    document.querySelectorAll('[data-producer-chart="price-history"]').forEach(canvas => {
      const values = parseChartData(canvas, 'values');
      const context = canvas.getContext('2d');
      const gradient = context.createLinearGradient(0, 0, 0, 250);
      gradient.addColorStop(0, 'rgba(31, 91, 57, .23)');
      gradient.addColorStop(1, 'rgba(31, 91, 57, .01)');
      new window.Chart(canvas, {
        type:'line',
        data:{labels:parseChartData(canvas, 'labels'),datasets:[{label:'Τιμή αγοράς',data:values,borderColor:'#1f5b39',backgroundColor:gradient,borderWidth:2.4,pointRadius:4,pointHoverRadius:6,pointBackgroundColor:'#fff',pointBorderColor:'#1f5b39',pointBorderWidth:2,tension:.3,fill:true}]},
        options:{responsive:true,maintainAspectRatio:false,animation:{duration:1150,easing:'easeOutQuart'},plugins:{legend:{display:false},tooltip:{padding:12,bodyFont:{family:chartFont},callbacks:{label:item=>'Τιμή: ' + Number(item.raw).toLocaleString('el-GR',{minimumFractionDigits:3}) + ' €/kg'}}},scales:{x:{grid:{display:false},ticks:{color:tickColor,font:{family:chartFont,size:10}}},y:{beginAtZero:false,grid:{color:gridColor},ticks:{color:tickColor,font:{family:chartFont,size:9},callback:value=>Number(value).toLocaleString('el-GR') + ' €'}}}}
      });
    });

    document.querySelectorAll('[data-producer-chart="production-mix"]').forEach(canvas => {
      new window.Chart(canvas, {
        type:'doughnut',
        data:{labels:parseChartData(canvas, 'labels'),datasets:[{data:parseChartData(canvas, 'values'),backgroundColor:['#1f5b39','#b58b45','#6d8d77','#d8c49c','#354b3c','#a7b5a9'],borderColor:'#fff',borderWidth:3,hoverOffset:5}]},
        options:{responsive:true,maintainAspectRatio:false,cutout:'62%',animation:{animateRotate:true,duration:1200,easing:'easeOutQuart'},plugins:{legend:{position:'bottom',labels:{usePointStyle:true,pointStyle:'rectRounded',boxWidth:8,padding:16,color:tickColor,font:{family:chartFont,size:10}}},tooltip:{padding:12,bodyFont:{family:chartFont},callbacks:{label:item=>item.label + ': ' + Number(item.raw).toLocaleString('el-GR') + ' kg'}}}}
      });
    });

    document.querySelectorAll('[data-producer-chart="finance-flow"]').forEach(canvas => {
      new window.Chart(canvas, {
        type:'bar',
        data:{labels:parseChartData(canvas, 'labels'),datasets:[{label:'Πιστώσεις',data:parseChartData(canvas, 'income'),backgroundColor:'rgba(31,91,57,.82)',borderRadius:5,maxBarThickness:28},{label:'Χρεώσεις',data:parseChartData(canvas, 'expenses'),backgroundColor:'rgba(181,139,69,.7)',borderRadius:5,maxBarThickness:28}]},
        options:{responsive:true,maintainAspectRatio:false,interaction:{mode:'index',intersect:false},animation:{duration:1050,easing:'easeOutQuart'},plugins:{legend:{display:false},tooltip:{padding:12,bodyFont:{family:chartFont},callbacks:{label:item=>item.dataset.label + ': ' + Number(item.raw).toLocaleString('el-GR') + ' €'}}},scales:{x:{stacked:false,grid:{display:false},ticks:{color:tickColor,font:{family:chartFont,size:10}}},y:{beginAtZero:true,grid:{color:gridColor},ticks:{color:tickColor,font:{family:chartFont,size:9},callback:value=>Number(value).toLocaleString('el-GR') + ' €'}}}}
      });
    });

    document.querySelectorAll('[data-producer-chart="finance-status"]').forEach(canvas => {
      new window.Chart(canvas, {
        type:'doughnut',
        data:{labels:['Πληρωμένα','Υπόλοιπο'],datasets:[{data:[Number(canvas.dataset.paid)||0,Number(canvas.dataset.outstanding)||0],backgroundColor:['#1f5b39','#d8c49c'],borderWidth:0,hoverOffset:4}]},
        options:{responsive:true,maintainAspectRatio:false,cutout:'76%',animation:{animateRotate:true,duration:1200,easing:'easeOutQuart'},plugins:{legend:{display:false},tooltip:{padding:11,bodyFont:{family:chartFont},callbacks:{label:item=>item.label + ': ' + Number(item.raw).toLocaleString('el-GR') + ' €'}}}}
      });
    });

    document.querySelectorAll('[data-producer-chart="production-volume"]').forEach(canvas => {
      new window.Chart(canvas, {type:'bar',data:{labels:parseChartData(canvas,'labels'),datasets:[{label:'Δηλωμένο',data:parseChartData(canvas,'declared'),backgroundColor:'rgba(31,91,57,.82)',borderRadius:5,maxBarThickness:32},{label:'Παραδομένο',data:parseChartData(canvas,'delivered'),backgroundColor:'rgba(181,139,69,.72)',borderRadius:5,maxBarThickness:32}]},options:{responsive:true,maintainAspectRatio:false,animation:{duration:1100,easing:'easeOutQuart'},plugins:{legend:{position:'bottom',labels:{usePointStyle:true,boxWidth:8,color:tickColor,font:{family:chartFont,size:10}}},tooltip:{callbacks:{label:item=>item.dataset.label+': '+Number(item.raw).toLocaleString('el-GR')+' kg'}}},scales:{x:{grid:{display:false},ticks:{color:tickColor,font:{family:chartFont,size:10}}},y:{beginAtZero:true,grid:{color:gridColor},ticks:{color:tickColor,callback:value=>Number(value).toLocaleString('el-GR')+' kg'}}}}});
    });

    document.querySelectorAll('[data-producer-chart="logistics-weight"]').forEach(canvas => {
      new window.Chart(canvas, {type:'bar',data:{labels:parseChartData(canvas,'labels'),datasets:[{label:'Φορτώθηκε',data:parseChartData(canvas,'loaded'),backgroundColor:'rgba(53,75,60,.55)',borderRadius:4},{label:'Αποδεκτό',data:parseChartData(canvas,'accepted'),backgroundColor:'rgba(31,91,57,.85)',borderRadius:4},{label:'Απόρριψη',data:parseChartData(canvas,'rejected'),backgroundColor:'rgba(181,139,69,.8)',borderRadius:4}]},options:{responsive:true,maintainAspectRatio:false,animation:{duration:1100},plugins:{legend:{position:'bottom',labels:{usePointStyle:true,boxWidth:8,color:tickColor,font:{family:chartFont,size:10}}},tooltip:{callbacks:{label:item=>item.dataset.label+': '+Number(item.raw).toLocaleString('el-GR')+' kg'}}},scales:{x:{grid:{display:false},ticks:{color:tickColor}},y:{beginAtZero:true,grid:{color:gridColor},ticks:{color:tickColor}}}}});
    });

    ['production-status','logistics-payment','invoice-status','document-status'].forEach(chartName => {
      document.querySelectorAll(`[data-producer-chart="${chartName}"]`).forEach(canvas => {
        new window.Chart(canvas, {
          type:'doughnut',
          data:{
            labels:parseChartData(canvas,'labels'),
            datasets:[{data:parseChartData(canvas,'values'),backgroundColor:['#1f5b39','#b58b45','#6d8d77','#d8c49c','#354b3c','#a7b5a9'],borderColor:'#fff',borderWidth:3,hoverOffset:5}]
          },
          options:{
            responsive:true,maintainAspectRatio:false,cutout:'66%',
            animation:{animateRotate:true,duration:1200,easing:'easeOutQuart'},
            plugins:{
              legend:{position:'bottom',labels:{usePointStyle:true,pointStyle:'rectRounded',boxWidth:8,padding:13,color:tickColor,font:{family:chartFont,size:9}}},
              tooltip:{padding:11,bodyFont:{family:chartFont}}
            }
          }
        });
      });
    });
  }
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && activeDashboardModal) closeDashboardModal(activeDashboardModal);
  });
})();
