window.atlasUi = window.atlasUi || {};

window.atlasUi.scrollToCard = function (headerEl, targetEl) {
  requestAnimationFrame(function () {
    if (!targetEl) return;
    var headerHeight = headerEl && headerEl.offsetHeight ? headerEl.offsetHeight : 0;
    var rect = targetEl.getBoundingClientRect();
    var y = rect.top + window.scrollY - headerHeight - 12;
    window.scrollTo({ top: Math.max(0, y), behavior: 'smooth' });
  });
};
