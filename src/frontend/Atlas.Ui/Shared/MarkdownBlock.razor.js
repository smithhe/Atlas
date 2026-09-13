export function highlight(root) {
  if (!root || typeof hljs === 'undefined') {
    return;
  }

  try {
    root.querySelectorAll('pre code').forEach(function (block) {
      hljs.highlightElement(block);
    });
  } catch {
    /* ignore highlight failures */
  }
}
