/* Admin map picker: click or drag to set the funeral home's latitude/longitude. */
(function () {
    "use strict";
    var el = document.getElementById("pickerMap");
    var latIn = document.getElementById("Latitude");
    var lngIn = document.getElementById("Longitude");
    if (!el || !latIn || !lngIn || typeof L === "undefined") return;

    var lat = parseFloat(latIn.value), lng = parseFloat(lngIn.value);
    var has = !isNaN(lat) && !isNaN(lng);
    var map = L.map(el).setView(has ? [lat, lng] : [12.8797, 121.774], has ? 16 : 6);
    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors"
    }).addTo(map);

    var marker = null;
    function place(latlng, fly) {
        if (!marker) {
            marker = L.marker(latlng, { draggable: true }).addTo(map);
            marker.on("dragend", function () { write(marker.getLatLng()); });
        } else marker.setLatLng(latlng);
        if (fly) map.setView(latlng, Math.max(map.getZoom(), 16));
    }
    function write(ll) {
        latIn.value = ll.lat.toFixed(6);
        lngIn.value = ll.lng.toFixed(6);
    }
    if (has) place([lat, lng], false);

    map.on("click", function (e) { place(e.latlng, false); write(e.latlng); });
    function fromInputs() {
        var a = parseFloat(latIn.value), b = parseFloat(lngIn.value);
        if (!isNaN(a) && !isNaN(b) && Math.abs(a) <= 90 && Math.abs(b) <= 180) place([a, b], true);
    }
    latIn.addEventListener("change", fromInputs);
    lngIn.addEventListener("change", fromInputs);
    document.getElementById("clearCoords").addEventListener("click", function () {
        latIn.value = ""; lngIn.value = "";
        if (marker) { map.removeLayer(marker); marker = null; }
    });
    setTimeout(function () { map.invalidateSize(); }, 150);
})();
