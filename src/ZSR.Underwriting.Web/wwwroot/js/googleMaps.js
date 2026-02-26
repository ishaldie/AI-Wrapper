// Google Maps JS interop for Deal Map Dashboard
window.googleMapsInterop = {
    _map: null,
    _markers: [],
    _infoWindow: null,
    _loaded: false,

    loadGoogleMaps: function (apiKey) {
        return new Promise((resolve, reject) => {
            if (window.google && window.google.maps) {
                this._loaded = true;
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=marker`;
            script.async = true;
            script.defer = true;
            script.onload = () => {
                this._loaded = true;
                resolve();
            };
            script.onerror = () => reject('Failed to load Google Maps API');
            document.head.appendChild(script);
        });
    },

    initMap: function (elementId, pins, dotNetRef) {
        if (!this._loaded || !window.google) return;

        const mapElement = document.getElementById(elementId);
        if (!mapElement) return;

        this._map = new google.maps.Map(mapElement, {
            zoom: 4,
            center: { lat: 39.8283, lng: -98.5795 }, // Center of US
            mapTypeControl: true,
            streetViewControl: false
        });

        this._infoWindow = new google.maps.InfoWindow();
        this._clearMarkers();

        if (!pins || pins.length === 0) return;

        const bounds = new google.maps.LatLngBounds();

        pins.forEach(pin => {
            const position = { lat: pin.latitude, lng: pin.longitude };
            const marker = new google.maps.Marker({
                position: position,
                map: this._map,
                title: pin.propertyName
            });

            marker.addListener('click', () => {
                const capRate = pin.capRate != null ? (pin.capRate * 100).toFixed(2) + '%' : 'N/A';
                const irr = pin.irr != null ? (pin.irr * 100).toFixed(2) + '%' : 'N/A';
                const price = pin.purchasePrice
                    ? '$' + pin.purchasePrice.toLocaleString('en-US', { maximumFractionDigits: 0 })
                    : 'N/A';

                const content = `
                    <div style="font-family: 'Plus Jakarta Sans', sans-serif; min-width: 200px;">
                        <h3 style="margin: 0 0 8px; font-size: 16px; color: #1a1a2e;">${pin.propertyName}</h3>
                        <p style="margin: 0 0 4px; font-size: 13px; color: #666;">${pin.address}</p>
                        <div style="margin-top: 8px; font-size: 13px;">
                            <span style="display: inline-block; padding: 2px 8px; border-radius: 4px; background: ${this._statusColor(pin.status)}; color: white; font-size: 11px;">${pin.status}</span>
                        </div>
                        <table style="margin-top: 8px; font-size: 13px; border-collapse: collapse;">
                            <tr><td style="padding: 2px 12px 2px 0; color: #888;">Units</td><td>${pin.unitCount}</td></tr>
                            <tr><td style="padding: 2px 12px 2px 0; color: #888;">Price</td><td>${price}</td></tr>
                            <tr><td style="padding: 2px 12px 2px 0; color: #888;">Cap Rate</td><td>${capRate}</td></tr>
                            <tr><td style="padding: 2px 12px 2px 0; color: #888;">IRR</td><td>${irr}</td></tr>
                        </table>
                    </div>`;

                this._infoWindow.setContent(content);
                this._infoWindow.open(this._map, marker);

                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnMarkerClicked', pin.id);
                }
            });

            this._markers.push(marker);
            bounds.extend(position);
        });

        if (pins.length === 1) {
            this._map.setCenter(bounds.getCenter());
            this._map.setZoom(14);
        } else {
            this._map.fitBounds(bounds, { padding: 50 });
        }
    },

    _statusColor: function (status) {
        switch (status) {
            case 'Draft': return '#888';
            case 'InProgress': return '#f59e0b';
            case 'Complete': return '#10b981';
            case 'Archived': return '#6b7280';
            default: return '#3b82f6';
        }
    },

    _clearMarkers: function () {
        this._markers.forEach(m => m.setMap(null));
        this._markers = [];
    },

    dispose: function () {
        this._clearMarkers();
        if (this._infoWindow) this._infoWindow.close();
        this._map = null;
    }
};
