/**
 * EventSource bridge for Atlas AI session SSE (GET /ai/sessions/{id}/events).
 * Not codegen'd via NSwag — opened from Blazor via IJSRuntime.
 */
(function () {
  'use strict';

  var sources = Object.create(null);
  var EVENT_TYPES = [
    'session.started',
    'context.gathering',
    'history.loading',
    'model.requested',
    'model.delta',
    'session.completed',
    'session.failed',
  ];
  var TERMINAL_TYPES = {
    'session.completed': true,
    'session.failed': true,
  };

  function forward(dotnetRef, raw) {
    if (!dotnetRef || raw == null) return;
    try {
      dotnetRef.invokeMethodAsync('OnSessionEventJson', String(raw));
    } catch (_) {
      /* disposed */
    }
  }

  function forwardError(dotnetRef) {
    if (!dotnetRef) return;
    try {
      dotnetRef.invokeMethodAsync('OnSessionEventError');
    } catch (_) {
      /* disposed */
    }
  }

  function closeStream(streamId) {
    var entry = sources[streamId];
    if (!entry) return;
    try {
      entry.es.close();
    } catch (_) {
      /* ignore */
    }
    delete sources[streamId];
  }

  var resizeRef = null;
  var resizeMove = null;
  var resizeUp = null;

  window.atlasAiEvents = {
    ensureResizeListeners: function (dotnetRef) {
      resizeRef = dotnetRef;
      if (resizeMove) return;
      resizeMove = function (e) {
        if (!resizeRef) return;
        try {
          resizeRef.invokeMethodAsync('OnAiResizeMove', e.clientX, window.innerWidth);
        } catch (_) {}
      };
      resizeUp = function () {
        if (!resizeRef) return;
        try {
          resizeRef.invokeMethodAsync('OnAiResizeUp');
        } catch (_) {}
      };
      window.addEventListener('pointermove', resizeMove);
      window.addEventListener('pointerup', resizeUp);
    },

    clearResizeListeners: function () {
      if (resizeMove) window.removeEventListener('pointermove', resizeMove);
      if (resizeUp) window.removeEventListener('pointerup', resizeUp);
      resizeMove = null;
      resizeUp = null;
      resizeRef = null;
    },

    beginResizeCapture: function (el, pointerId) {
      if (!el || pointerId == null) return;
      try {
        el.setPointerCapture(pointerId);
      } catch (_) {
        /* capture unsupported or already released */
      }
    },

    open: function (streamId, url, dotnetRef) {
      closeStream(streamId);
      var es = new EventSource(url);
      sources[streamId] = { es: es, ref: dotnetRef };

      es.onmessage = function (event) {
        forward(dotnetRef, event.data);
      };

      EVENT_TYPES.forEach(function (type) {
        es.addEventListener(type, function (event) {
          forward(dotnetRef, event.data);
          // Match React: close on terminal before end-of-stream onerror can fire.
          if (TERMINAL_TYPES[type]) {
            closeStream(streamId);
          }
        });
      });

      es.onerror = function () {
        // Already closed after session.completed / session.failed — do not report Failed.
        if (!sources[streamId]) return;
        forwardError(dotnetRef);
      };
    },

    close: function (streamId) {
      closeStream(streamId);
    },

    scrollToBottom: function (el) {
      if (!el) return;
      el.scrollTop = el.scrollHeight;
    },

    copyText: async function (text) {
      if (!text) return false;
      try {
        await navigator.clipboard.writeText(text);
        return true;
      } catch (_) {
        return false;
      }
    },
  };

  window.atlasMarkdown = {
    highlight: function (root) {
      if (!root || typeof hljs === 'undefined') return;
      try {
        root.querySelectorAll('pre code').forEach(function (block) {
          hljs.highlightElement(block);
        });
      } catch (_) {
        /* ignore highlight failures */
      }
    },
  };
})();
