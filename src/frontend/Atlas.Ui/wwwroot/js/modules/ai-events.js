/**
 * EventSource bridge for Atlas AI session SSE (GET /ai/sessions/{id}/events).
 */
const ON_SESSION_EVENT_JSON = 'OnSessionEventJson';
const ON_SESSION_EVENT_ERROR = 'OnSessionEventError';

const sources = Object.create(null);
const EVENT_TYPES = [
  'session.started',
  'context.gathering',
  'history.loading',
  'model.requested',
  'model.delta',
  'session.completed',
  'session.failed',
];
const TERMINAL_TYPES = {
  'session.completed': true,
  'session.failed': true,
};

function forward(dotnetRef, raw) {
  if (!dotnetRef || raw == null) {
    return;
  }

  try {
    dotnetRef.invokeMethodAsync(ON_SESSION_EVENT_JSON, String(raw));
  } catch {
    /* disposed */
  }
}

function forwardError(dotnetRef) {
  if (!dotnetRef) {
    return;
  }

  try {
    dotnetRef.invokeMethodAsync(ON_SESSION_EVENT_ERROR);
  } catch {
    /* disposed */
  }
}

function closeStream(streamId) {
  const entry = sources[streamId];
  if (!entry) {
    return;
  }

  try {
    entry.es.close();
  } catch {
    /* ignore */
  }

  delete sources[streamId];
}

export function open(streamId, url, dotnetRef) {
  closeStream(streamId);
  const es = new EventSource(url);
  sources[streamId] = { es, ref: dotnetRef };

  es.onmessage = function (event) {
    forward(dotnetRef, event.data);
  };

  EVENT_TYPES.forEach(function (type) {
    es.addEventListener(type, function (event) {
      forward(dotnetRef, event.data);
      if (TERMINAL_TYPES[type]) {
        closeStream(streamId);
      }
    });
  });

  es.onerror = function () {
    if (!sources[streamId]) {
      return;
    }

    forwardError(dotnetRef);
  };
}

export function close(streamId) {
  closeStream(streamId);
}

export function dispose() {
  Object.keys(sources).forEach(closeStream);
}
