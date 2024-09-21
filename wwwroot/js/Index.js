const POPUP_WIDTH_PERCENTAGE = 0.2; // 20% of the viewport
const POPUP_MAX_WIDTH = 300; // Max width in pixels

let map;
let markers = [];

function initMap() {
    map = new google.maps.Map(document.getElementById('map'), {
        center: { lat: 49.2827, lng: -123.1207 },  //Vancouver, BC
        zoom: 12
    });

    // Perform the initial fetch of places
    updateMarkers();

    // Add an event listener to update markers on bounds change
    map.addListener('bounds_changed', updateMarkers);
}

function updateMarkers() {
    let bounds = map.getBounds();
    if (!bounds) return;

    let center = bounds.getCenter();  // Get the center of the bounds
    let northEast = bounds.getNorthEast();  // Get the northeast corner
    let southWest = bounds.getSouthWest();  // Get the southwest corner

    // Retrieve latitude and longitude values
    let centerLat = center.lat();  // Latitude of the center
    let centerLng = center.lng();  // Longitude of the center
    let northEastLat = northEast.lat();  // Latitude of the northeast corner
    let northEastLng = northEast.lng();  // Longitude of the northeast corner
    let southWestLat = southWest.lat();  // Latitude of the southwest corner
    let southWestLng = southWest.lng();  // Longitude of the southwest corner

    // Use these values to construct your request URL
    let requestUrl = `/api/tennismap/tenniscourts?lat=${centerLat}&lng=${centerLng}&northEastLat=${northEastLat}&northEastLng=${northEastLng}&southWestLat=${southWestLat}&southWestLng=${southWestLng}`;


    fetch(requestUrl)
        // Response object returned from fetch is converted to JSON
        .then(response => response.json())
        // Handling of JSON data returned by the promise
        .then(data => {
            //clearMarkers();

            // data.results returns an array of places that the API provided, thus we can use foreach on it
            data.results.forEach(place => {
                const marker = new google.maps.Marker({
                    position: place.geometry.location,
                    map: map,
                    title: place.name
                });


                marker.addListener('click', () => {
                    openPopup();
                    //alert('You clicked on ' + place.name);
                });

                markers.push(marker);
            });
        })
        .catch(error => console.error('Error fetching tennis courts:', error));
}

function clearMarkers() {
    markers.forEach(marker => marker.setMap(null));
    markers = [];
}

function openPopup() {
    const popup = document.getElementById("popup");
    const mainContent = document.getElementById("main-content");

    // Show the popup
    popup.classList.remove("close");

    // Check for overlap
    let windowSize = window.innerWidth;
    let popupWidth = windowSize * POPUP_WIDTH_PERCENTAGE;
    const mainRect = mainContent.getBoundingClientRect();
    const overlaps = !(popupWidth < mainRect.left || POPUP_MAX_WIDTH < mainRect.left);

    // Shift main content if there is an overlap
    if (overlaps) {
        mainContent.classList.add("shift-right");
    }
}

function closePopup() {
    const popup = document.getElementById("popup");
    const mainContent = document.getElementById("main-content");

    // Hide the popup
    popup.classList.add("close");

    // Remove the shift from main content
    mainContent.classList.remove("shift-right");
}