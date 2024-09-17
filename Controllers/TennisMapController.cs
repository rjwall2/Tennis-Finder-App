using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace YourNamespace.Controllers
{
    // Defines api url route
    [Route("api/[controller]")]

    // Denotes this is an api controller, gives additional functionality such as data binding
    [ApiController]

    public class TennisMapController : ControllerBase
    {
        private readonly string _googleMapsApiKey;
        private readonly ILogger<TennisMapController> _logger;
        private readonly IDistributedCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30); //30 min cache duration

        // Defines the bucket size (e.g., 1 km x 1 km)
        private const double BucketSize = 0.1; // Approximately 10 km in latitude/longitude

        public TennisMapController(IConfiguration configuration, ILogger<TennisMapController> logger, IDistributedCache cache)
        {
            _googleMapsApiKey = configuration["GoogleMaps:ApiKey"]; //env variable
            _logger = logger;
            _cache = cache;

            //_logger.LogInformation("TennisMapController created.");
        }

        // Specifies that the function below should trigger when a GET request is sent to api/tennismaps/tenniscourts
        [HttpGet("tenniscourts")]

        // Databinding makes url parameters automatically map to function parameters
        public async Task<IActionResult> GetNearbyTennisCourts(double lat, double lng)
        {
            // Calculate bucket key
            var (bucketLat, bucketLng) = GetBucketCoordinates(lat, lng);
            var cacheKey = GenerateCacheKey(bucketLat, bucketLng);
            var cachedResponse = await _cache.GetStringAsync(cacheKey); // Gets cached data from cachekey string

            if (cachedResponse != null)
            {
                _logger.LogInformation("Returning cached response for bucket: {Bucket}", cacheKey);
                return Ok(cachedResponse);
            }

            // Fetch data from Google Maps API
            var client = new HttpClient();
            var radius = 5000; // Adjust radius as needed
            var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={lat},{lng}&radius={radius}&keyword=tennis_court&key={_googleMapsApiKey}";
            _logger.LogInformation("Request made");
            var response = await client.GetStringAsync(url); // Gets JSON data and turns it into string type

            // Cache the response
            // DistributedCacheEntryOptions is an object that specifies cache settings, such as expiration
            await _cache.SetStringAsync(cacheKey, response, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });

            return Ok(response);
        }

        private (double, double) GetBucketCoordinates(double lat, double lng)
        {
            // Round coordinates to nearest bucket size
            double bucketLat = Math.Floor(lat / BucketSize) * BucketSize;
            double bucketLng = Math.Floor(lng / BucketSize) * BucketSize;
            return (bucketLat, bucketLng);
        }

        // Creates a cache key string that includes the rounded or bucketed coordinates
        private string GenerateCacheKey(double bucketLat, double bucketLng)
        {
            return $"TennisCourts-{bucketLat}-{bucketLng}";
        }

    }
}
