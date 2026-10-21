// Fill in the footer's "built from commit" line for dev builds. The commit sha
// lives in build-info.json next to the HTML rather than in the pages
// themselves, so republishing the dev docs rewrites one file instead of every
// page. The placeholder stays empty if the fetch fails or the file is absent.
document.addEventListener("DOMContentLoaded", function () {
  var el = document.querySelector(".build-info[data-build-info-url]");
  if (!el) {
    return;
  }
  fetch(el.dataset.buildInfoUrl)
    .then(function (response) {
      return response.ok ? response.json() : null;
    })
    .then(function (info) {
      if (!info || !info.commit) {
        return;
      }
      var link = document.createElement("a");
      link.href = "https://github.com/krpc/krpc/commit/" + info.commit;
      // Link to the full sha so it is unambiguous, but display a short sha.
      link.textContent = info.commit.substring(0, 7);
      // external-links.js has already run by the time the fetch resolves, so
      // this link opts into the new tab itself.
      link.target = "_blank";
      link.rel = "noopener";
      el.appendChild(document.createTextNode("Built from commit "));
      el.appendChild(link);
    })
    .catch(function () {});
});
