namespace Shop.Application.Common.Interfaces;

public interface IDriverLocationService
{
    Task UpdateLocation(int driverId, double latitude, double longitude);
    (double Lat, double Lng, DateTime Timestamp)? GetLatestLocation(int driverId);
    double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2);
    List<int> GetDriversInRadius(double centerLat, double centerLng, double radiusKm);
}
