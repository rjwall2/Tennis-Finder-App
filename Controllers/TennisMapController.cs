using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json; 
using Microsoft.EntityFrameworkCore;
using Tennis_Finder_App.Data;
using Tennis_Finder_App.Models;

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
        private readonly ApplicationDbContext _context; // Injected DB context
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30); //30 min cache duration

        // Defines the bucket size (e.g., 1 km x 1 km)
        private const double BucketSize = 0.1; // Approximately 10 km in latitude/longitude

        public TennisMapController(IConfiguration configuration, ILogger<TennisMapController> logger, IDistributedCache cache, ApplicationDbContext context)
        {
            _googleMapsApiKey = configuration["GoogleMaps:ApiKey"]; //env variable
            _logger = logger;
            _cache = cache;
            _context = context; // database instance

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

            // Check database first
            var existingCourts = await _context.TennisCourts
                .Where(court => Math.Abs(court.Latitude - lat) < 0.05 && Math.Abs(court.Longitude - lng) < 0.05)
                .ToListAsync();

            if (existingCourts.Count > 0)
            {
                _logger.LogInformation("Returning data from database for nearby courts.");
                var dbResponse = new
                {
                    results = existingCourts.Select(court => new
                    {
                        name = court.Name,
                        vicinity = court.Address,
                        geometry = new
                        {
                            location = new
                            {
                                lat = court.Latitude,
                                lng = court.Longitude
                            }
                        }
                    }).ToList()
                };
                var jsonResponse = JsonSerializer.Serialize(dbResponse);
                return Ok(jsonResponse); // Return data from the database
            }

            // Check cache second
            if (cachedResponse != null)
            {
                _logger.LogInformation("Returning cached response for bucket: {Bucket}", cacheKey);
                return Ok(cachedResponse);
            }

            // Fetch data from Google Maps API
            var client = new HttpClient();
            var radius = 5000; // Adjust radius as needed
            var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={lat},{lng}&radius={radius}&keyword=tennis_court&key={_googleMapsApiKey}";
            _logger.LogInformation("New request made");
            var response = await client.GetStringAsync(url); // Gets JSON data and turns it into string type

            // Parse the API response and save relevant data to the database
            var googleResponse = JsonSerializer.Deserialize<GoogleMapsResponse>(response); // Use JsonSerializer
            foreach (var place in googleResponse.Results)
            {
                var tennisCourt = new TennisCourt
                {
                    Name = place.Name,
                    Address = place.Vicinity,
                    Latitude = place.Geometry.Location.Lat,
                    Longitude = place.Geometry.Location.Lng,
                    LastUpdated = DateTime.UtcNow
                };

                _context.TennisCourts.Add(tennisCourt);
            }

            await _context.SaveChangesAsync(); // Save all the new courts to the database

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
