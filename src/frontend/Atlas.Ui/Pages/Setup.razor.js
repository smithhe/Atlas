export function scrollToCard(headerEl, targetEl) {
  requestAnimationFrame(function () {
    if (!targetEl) {
      return;
    }

    const headerHeight = headerEl && headerEl.offsetHeight ? headerEl.offsetHeight : 0;
    const rect = targetEl.getBoundingClientRect();
    const y = rect.top + window.scrollY - headerHeight - 12;
    window.scrollTo({ top: Math.max(0, y), behavior: 'smooth' });
  });
}
