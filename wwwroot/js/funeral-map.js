/* Funeral home map (Leaflet + OpenStreetMap). Markers use the coordinates stored in the database. */
(function () {
    "use strict";
    var el = document.getElementById("funeralMap");
    var dataEl = document.getElementById("funeral-data");
    if (!el || !dataEl || typeof L === "undefined") return;

    var homes = [];
    try { homes = JSON.parse(dataEl.textContent) || []; } catch (e) { homes = []; }

    var map = L.map(el, { scrollWheelZoom: false }).setView([12.8797, 121.774], 6);
    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors"
    }).addTo(map);

    // Enable wheel zoom only after the map is focused, so page scrolling is not hijacked.
    map.on("focus", function () { map.scrollWheelZoom.enable(); });
    map.on("blur", function () { map.scrollWheelZoom.disable(); });

    function popup(h) {
        var box = document.createElement("div");
        var title = document.createElement("strong"); title.textContent = h.name; box.appendChild(title);
        var addr = document.createElement("div"); addr.textContent = h.address; box.appendChild(addr);
        var tel = document.createElement("div"); tel.style.marginTop = "4px";
        var a = document.createElement("a"); a.href = "tel:" + h.contact; a.textContent = h.contact; tel.appendChild(a); box.appendChild(tel);
        if (h.url) {
            var more = document.createElement("div"); more.style.marginTop = "6px";
            var link = document.createElement("a"); link.href = h.url; link.textContent = "View details"; more.appendChild(link); box.appendChild(more);
        }
        return box;
    }

    var markers = {};
    var bounds = [];
    homes.forEach(function (h) {
        if (typeof h.lat !== "number" || typeof h.lng !== "number") return;
        var m = L.marker([h.lat, h.lng], { title: h.name }).addTo(map).bindPopup(popup(h));
        markers[h.id] = m;
        bounds.push([h.lat, h.lng]);
    });

    if (bounds.length === 1) map.setView(bounds[0], 15);
    else if (bounds.length > 1) map.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });

    document.querySelectorAll("[data-focus-home]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var m = markers[btn.getAttribute("data-focus-home")];
            if (!m) return;
            map.flyTo(m.getLatLng(), 16, { duration: 0.6 });
            m.openPopup();
            el.scrollIntoView({ behavior: "smooth", block: "nearest" });
        });
    });

    // Make sure tiles render correctly after layout changes / bfcache restores.
    setTimeout(function () { map.invalidateSize(); }, 150);
    window.addEventListener("pageshow", function () { map.invalidateSize(); });
    window.addEventListener("resize", function () { map.invalidateSize(); });
})();
