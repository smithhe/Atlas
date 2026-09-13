export function alert(message) {
  window.alert(message);
}

export function confirm(message) {
  return window.confirm(message);
}

export function prompt(message, defaultValue) {
  return window.prompt(message, defaultValue);
}
