const ON_AI_RESIZE_MOVE = 'OnAiResizeMove';
const ON_AI_RESIZE_UP = 'OnAiResizeUp';

let resizeRef = null;
let resizeMove = null;
let resizeUp = null;

export function ensureResizeListeners(dotNetRef) {
  resizeRef = dotNetRef;
  if (resizeMove) {
    return;
  }

  resizeMove = async (e) => {
    if (!resizeRef) {
      return;
    }

    try {
      await resizeRef.invokeMethodAsync(ON_AI_RESIZE_MOVE, e.clientX, window.innerWidth);
    } catch {
      /* disposed */
    }
  };

  resizeUp = async () => {
    if (!resizeRef) {
      return;
    }

    try {
      await resizeRef.invokeMethodAsync(ON_AI_RESIZE_UP);
    } catch {
      /* disposed */
    }
  };

  window.addEventListener('pointermove', resizeMove);
  window.addEventListener('pointerup', resizeUp);
}

export function clearResizeListeners() {
  if (resizeMove) {
    window.removeEventListener('pointermove', resizeMove);
  }

  if (resizeUp) {
    window.removeEventListener('pointerup', resizeUp);
  }

  resizeMove = null;
  resizeUp = null;
  resizeRef = null;
}

export function beginResizeCapture(el, pointerId) {
  if (!el || pointerId == null) {
    return;
  }

  try {
    el.setPointerCapture(pointerId);
  } catch {
    /* capture unsupported or already released */
  }
}
