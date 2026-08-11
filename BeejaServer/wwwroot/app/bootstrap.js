const params = new URLSearchParams(window.location.search);
const legacyKeys = ["tour", "demo", "role", "publictour", "player", "host", "organizer", "public"];
const legacyMode = legacyKeys.some((key) => params.has(key));

if (legacyMode) {
  import("./app.js");
} else {
  import("./product-app.js");
}
