using System;

namespace CityBikeProject
{
    public class BikeTrip
    {
        public string RideId { get; set; }
        public string RideableType { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public string StartStationName { get; set; }
        public string EndStationName { get; set; }
        public double StartLat { get; set; }
        public double StartLng { get; set; }
        public double EndLat { get; set; }
        public double EndLng { get; set; }
        public string MemberCasual { get; set; }

        public double DurationMinutes => (EndedAt - StartedAt).TotalMinutes;
    }

    public class Weather
    {
        public DateTime DateTime { get; set; }
        public double Temperature { get; set; }
        public double Precipitation { get; set; }
        public double WindSpeed { get; set; }

        public string Condition => Precipitation > 0.1 ? "Rainy" : "Sunny";
    }

    public class AvgWeather // average weather during a single day
    {
        public DateTime Date { get; set; }
        public double AvgTemp { get; set; }
        public double TotalRain { get; set; }
        public string DominantCondition => TotalRain > 1.0 ? "Rainy" : "Sunny";
    }
}