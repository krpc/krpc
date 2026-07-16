// Open external links in a new tab. mailto: and other non-http(s) schemes are
// excluded by the protocol check (their parsed host never matches either).
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll("a[href]").forEach(function (link) {
    if (/^https?:$/.test(link.protocol) && link.host !== window.location.host) {
      link.setAttribute("target", "_blank");
      link.setAttribute("rel", "noopener");
    }
  });
});
