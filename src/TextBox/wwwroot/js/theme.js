/* Theme persistence for the dark mode toggle. The inline snippet in
   App.razor applies the stored theme before first paint; Blazor calls
   set() when the toggle in the navbar is clicked. */
window.textBoxTheme = {
  get: function () {
    try {
      return localStorage.getItem("textbox-theme") || "light";
    } catch (e) {
      return "light";
    }
  },
  set: function (theme) {
    document.documentElement.setAttribute("data-theme", theme);
    try {
      localStorage.setItem("textbox-theme", theme);
    } catch (e) {
      /* storage unavailable (e.g. private mode) - theme still applies */
    }
  },
};

/* Clipboard helper for the API-key copy button. Uses the async clipboard
   API when available, falling back to execCommand for plain-http hosts
   (e.g. LAN), where the async API is blocked. */
window.textBoxClipboard = {
  copy: function (text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text);
    }
    return new Promise(function (resolve, reject) {
      try {
        var ta = document.createElement("textarea");
        ta.value = text;
        ta.style.position = "fixed";
        ta.style.opacity = "0";
        document.body.appendChild(ta);
        ta.select();
        var ok = document.execCommand("copy");
        document.body.removeChild(ta);
        if (ok) {
          resolve();
        } else {
          reject(new Error("copy failed"));
        }
      } catch (e) {
        reject(e);
      }
    });
  },
};

