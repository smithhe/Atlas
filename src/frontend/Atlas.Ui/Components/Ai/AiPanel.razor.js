export function scrollToBottom(el) {
  if (!el) {
    return;
  }

  el.scrollTop = el.scrollHeight;
}

export async function copyText(text) {
  if (!text) {
    return false;
  }

  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}
