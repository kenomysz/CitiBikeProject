using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CityBikeProject
{
    public static class Utils
    {
        public static List<BikeTrip> LoadTrips(string path)
        {
            if (!File.Exists(path)) return new List<BikeTrip>();

            return File.ReadAllLines(path).Skip(1)
                .Select(ParseCsvLine)
                .Where(cols => cols.Count >= 13)
                .Select(cols => {
                    try
                    {
                        return new BikeTrip
                        {
                            RideId = cols[0],
                            RideableType = cols[1],
                            StartedAt = DateTime.Parse(cols[2]),
                            EndedAt = DateTime.Parse(cols[3]),
                            StartStationName = cols[4],
                            EndStationName = cols[6],
                            StartLat = ParseDouble(cols[8]),
                            StartLng = ParseDouble(cols[9]),
                            EndLat = ParseDouble(cols[10]),
                            EndLng = ParseDouble(cols[11]),
                            MemberCasual = cols[12]
                        };
                    }
                    catch { return null; }
                })
                .Where(trip => trip != null)
                .ToList();
        }

        public static List<Weather> LoadWeather(string path)
        {
            if (!File.Exists(path)) return new List<Weather>();

            var lines = File.ReadAllLines(path);
            int skip = 4;

            return lines.Skip(skip)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(','))
                .Select(cols => {
                    try
                    {
                        return new Weather
                        {
                            DateTime = DateTime.Parse(cols[0]),
                            Temperature = ParseDouble(cols[1]),
                            Precipitation = ParseDouble(cols[3]),
                            WindSpeed = cols.Length > 6 ? ParseDouble(cols[6]) : 0
                        };
                    }
                    catch { return null; }
                })
                .Where(w => w != null)
                .ToList();
        }
        private static double ParseDouble(string val)
        {
            if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double res))
                return res;
            return 0.0;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var res = new List<string>();
            bool inQuotes = false;
            string current = "";
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    res.Add(current);
                    current = "";
                }
                else current += c;
            }
            res.Add(current);
            return res;
        }
        private static object GetProp(object obj, string propName)
        {
            return obj.GetType().GetProperty(propName).GetValue(obj, null);
        }

        public static List<AvgWeather> AggregateDailyWeather(List<Weather> hourlyWeather)
        {
            return hourlyWeather
                .GroupBy(w => w.DateTime.Date)
                .Select(g => new AvgWeather
                {
                    Date = g.Key,
                    AvgTemp = Math.Round(g.Average(w => w.Temperature), 1),
                    TotalRain = Math.Round(g.Sum(w => w.Precipitation), 2)
                })
                .ToList();
        }

        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // radius km 
            var dLat = (lat2 - lat1) * (Math.PI / 180);
            var dLon = (lon2 - lon1) * (Math.PI / 180);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        public static string FormatTripsOutput(IEnumerable<BikeTrip> selectedTrips)
        {
            if (!selectedTrips.Any()) return "Brak danych.";

            var lines = selectedTrips.Select(t =>
            {
                double distKm = CalculateDistance(t.StartLat, t.StartLng, t.EndLat, t.EndLng);
                string start = t.StartStationName != "" ? t.StartStationName : "[NO DATA]";
                string end = t.EndStationName != "" ? t.EndStationName : "[NO DATA]";
                return $"RIDE ID: {t.RideId} : {start} -> {end}";
            });

            return string.Join(Environment.NewLine, lines);
        }
        public static bool IsValidGps(BikeTrip t) // gps data validity
        {
            return Math.Abs(t.StartLat) > 0.001 && Math.Abs(t.StartLng) > 0.001 &&
                   Math.Abs(t.EndLat) > 0.001 && Math.Abs(t.EndLng) > 0.001;
        }
    }
}