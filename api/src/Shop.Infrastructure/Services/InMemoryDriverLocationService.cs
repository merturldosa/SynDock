using System.Collections.Concurrent;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class InMemoryDriverLocationService : IDriverLocationService
{
    private readonly ConcurrentDictionary<int, (double Lat, double Lng, DateTime Timestamp)> _locations = new();

    public Task UpdateLocation(int driverId, double latitude, double longitude)
    {
        _locations[driverId] = (latitude, longitude, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public (double Lat, double Lng, DateTime Timestamp)? GetLatestLocation(int driverId)
    {
        return _locations.TryGetValue(driverId, out var location) ? location : null;
    }

    public double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    public List<int> GetDriversInRadius(double centerLat, double centerLng, double radiusKm)
    {
        var result = new List<int>();

        foreach (var (driverId, location) in _locations)
        {
            // Skip stale locations (older than 5 minutes)
            if ((DateTime.UtcNow - location.Timestamp).TotalMinutes > 5)
                continue;

            var distance = CalculateDistanceKm(centerLat, centerLng, location.Lat, location.Lng);
            if (distance <= radiusKm)
                result.Add(driverId);
        }

        return result;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
