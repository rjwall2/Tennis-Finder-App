using System.Text.Json.Serialization;

namespace Tennis_Finder_App.Models
{
    public class GoogleMapsResponse
    {
        [JsonPropertyName("results")]
        public List<Place> Results { get; set; }
    }

    public class Place
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("vicinity")]
        public string Vicinity { get; set; } // Address
        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; }
    }

    public class Geometry
    {
        [JsonPropertyName("location")]
        public Location Location { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}
