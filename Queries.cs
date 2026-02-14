using System;
using System.Collections.Generic;
using System.Linq;
using static CityBikeProject.Utils;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace CityBikeProject
{
    public static class Queries
    {
        public static void BasicStatsTime(IEnumerable<BikeTrip> trips)
        {
            var validTimeTrips = trips.Where(t => t.DurationMinutes > 0).ToList();
            if (!validTimeTrips.Any())
            {
                Console.WriteLine("1. Ride Time Statistics");
                DisplayQResult(new { Message = "No valid trips found." });
                return;
            }
            var maxTime = validTimeTrips.Max(t => t.DurationMinutes);
            var stats = new
            {
                Summary = new
                {
                    AnalyzedTrips = validTimeTrips.Count,
                    TotalTimeMinutes = Math.Round(validTimeTrips.Sum(t => t.DurationMinutes), 2),
                    AvgTimeMinutes = Math.Round(validTimeTrips.Average(t => t.DurationMinutes), 2),
                    MaxTimeMinutes = Math.Round(maxTime, 2)
                },
                LongestTrip = validTimeTrips
                    .Where(t => t.DurationMinutes == maxTime)
                    .Select(t => new
                    {
                        t.RideId,
                        t.StartStationName,
                        t.EndStationName,
                        StartedAt = t.StartedAt,
                        Duration = Math.Round(t.DurationMinutes, 2)
                    })
                    .FirstOrDefault()
            };

            Console.WriteLine("1. Ride Time Statistics");
            DisplayQResult(stats);
        }

        public static void FindPopularSpots(IEnumerable<BikeTrip> trips)
        {
            var topStations = trips
                .Select(t => t.StartStationName)
                .Concat(trips.Select(t => t.EndStationName))
                .Where(s => !string.IsNullOrEmpty(s))
                .GroupBy(stationName => stationName)
                .Select(g => new {
                    StationName = g.Key,
                    TotalVisits = g.Count()
                })
                .OrderByDescending(x => x.TotalVisits)
                .Take(5);
            var topRoutes = trips
                .Where(t => !string.IsNullOrEmpty(t.StartStationName) && !string.IsNullOrEmpty(t.EndStationName))
                .GroupBy(t => new { t.StartStationName, t.EndStationName })
                .Select(g => new
                {
                    From = g.Key.StartStationName,
                    To = g.Key.EndStationName,
                    TripCount = g.Count()
                })
                .OrderByDescending(x => x.TripCount)
                .Take(5);

            var popularSpots = new
            {
                TopStations = topStations.ToList(),
                TopRoutes = topRoutes.ToList()
            };

            Console.WriteLine("2. Popularity of stations and routes ");
            DisplayQResult(popularSpots);
        }

        public static void CompareBikeTypes(IEnumerable<BikeTrip> trips)
        {
            var stats = trips
                .GroupBy(t => t.RideableType)
                .Select(g =>
                {
                    var topStart = g.Where(t => !string.IsNullOrEmpty(t.StartStationName))
                                    .GroupBy(s => s.StartStationName)
                                    .OrderByDescending(grp => grp.Count())
                                    .Select(grp => new { grp.Key, Count = grp.Count() })
                                    .FirstOrDefault();
                    var topEnd = g.Where(t => !string.IsNullOrEmpty(t.EndStationName))
                                  .GroupBy(s => s.EndStationName)
                                  .OrderByDescending(grp => grp.Count())
                                  .Select(grp => new { grp.Key, Count = grp.Count() })
                                  .FirstOrDefault();

                    return new
                    {
                        BikeType = g.Key,
                        Durations = new
                        {
                            AverageMinutes = Math.Round(g.Average(t => t.DurationMinutes), 2),
                            MaxMinutes = Math.Round(g.Max(t => t.DurationMinutes), 2)
                        },
                        UserCounts = new
                        {
                            Casual = g.Count(t => t.MemberCasual == "casual"),
                            Member = g.Count(t => t.MemberCasual == "member"),
                            Total = g.Count()
                        },
                        TopStations = new
                        {
                            MostFrequentStart = topStart != null ? $"{topStart.Key} ({topStart.Count})" : "N/A",
                            MostFrequentEnd = topEnd != null ? $"{topEnd.Key} ({topEnd.Count})" : "N/A"
                        }
                    };
                })
                .OrderByDescending(r => r.UserCounts.Total)
                .ToList();

            Console.WriteLine("3. Bike Type Comparison");
            DisplayQResult(stats);
        }

        public static void AnalyzeRainImpact(IEnumerable<BikeTrip> trips, IEnumerable<AvgWeather> dailyWeather)
        {
            var stats = trips
                .Join(dailyWeather,
                    trip => trip.StartedAt.Date,
                    weather => weather.Date.Date,
                    (trip, weather) => new { trip.DurationMinutes, trip.MemberCasual, trip.RideableType, trip.StartedAt.Date, weather.DominantCondition })
                .GroupBy(x => x.DominantCondition)
                .Select(g => {
                    int total = g.Count();
                    int days = g.Select(x => x.Date).Distinct().Count();
                    return new
                    {
                        Condition = g.Key,
                        General = new
                        {
                            DaysInAnalysis = days,
                            TotalTrips = total,
                            AvgTripsPerDay = days > 0 ? Math.Round((double)total / days, 1) : 0,
                            AvgDurationMin = Math.Round(g.Average(x => x.DurationMinutes), 2)
                        },
                        UserDistribution = new
                        {
                            Member = new { 
                                Count = g.Count(x => x.MemberCasual == "member"), 
                                Share = total > 0 ? $"{(double)g.Count(x => x.MemberCasual == "member") / total:P1}" : "0%" 
                            },
                            Casual = new { 
                                Count = g.Count(x => x.MemberCasual == "casual"), 
                                Share = total > 0 ? $"{(double)g.Count(x => x.MemberCasual == "casual") / total:P1}" : "0%" 
                            }
                        },
                        BikeDistribution = new
                        {
                            Electric = new { 
                                Count = g.Count(x => x.RideableType == "electric_bike"), 
                                Share = total > 0 ? $"{(double)g.Count(x => x.RideableType == "electric_bike") / total:P1}" : "0%" 
                            },
                            Classic = new { 
                                Count = g.Count(x => x.RideableType == "classic_bike"), 
                                Share = total > 0 ? $"{(double)g.Count(x => x.RideableType == "classic_bike") / total:P1}" : "0%" 
                            }
                        }
                    };
                })
                .ToList();

            Console.WriteLine("4. Rain impact");
            DisplayQResult(stats);
        }

        public static void AnalyzeWeeklyTrends(IEnumerable<BikeTrip> trips, IEnumerable<AvgWeather> dailyWeather)
        {
            var stats = trips
                .Join(dailyWeather,
                    t => t.StartedAt.Date,
                    w => w.Date.Date,
                    (trip, weather) => new {
                        Date = trip.StartedAt.Date,
                        Day = trip.StartedAt.DayOfWeek,
                        Condition = weather.DominantCondition
                    })
                .GroupBy(x => x.Day)
                .Select(g => {
                    int total = g.Count();
                    int sunnyCount = g.Count(x => x.Condition == "Sunny");
                    int rainyCount = g.Count(x => x.Condition == "Rainy");

                    return new
                    {
                        Day = g.Key.ToString(),
                        TotalTrips = total,
                        SunnyWeather = new
                        {
                            Trips = sunnyCount,
                            Share = total > 0 ? $"{(double)sunnyCount / total:P0}" : "0%",
                            DistinctDays = g.Where(x => x.Condition == "Sunny").Select(x => x.Date).Distinct().Count()
                        },
                        RainyWeather = new
                        {
                            Trips = rainyCount,
                            Share = total > 0 ? $"{(double)rainyCount / total:P0}" : "0%",
                            DistinctDays = g.Where(x => x.Condition == "Rainy").Select(x => x.Date).Distinct().Count()
                        },
                        SortKey = g.Key == DayOfWeek.Sunday ? 7 : (int)g.Key
                    };
                })
                .OrderBy(x => x.SortKey)
                .Select(x => new { x.Day, x.TotalTrips, x.SunnyWeather, x.RainyWeather })
                .ToList();

            Console.WriteLine("5. Weekly analysis (with weather)");
            DisplayQResult(stats);
        }


        public static void AnalyzeTempBrackets(IEnumerable<BikeTrip> trips, IEnumerable<Weather> hourlyWeather)
        {
            var stats = trips
                .Join(hourlyWeather,
                    t => new { t.StartedAt.Date, t.StartedAt.Hour },
                    w => new { w.DateTime.Date, w.DateTime.Hour },
                    (t, w) => w.Temperature)
                .GroupBy(temp => Math.Floor(temp / 5) * 5)
                .Select(g => new {
                    TemperatureRange = $"{g.Key} - {g.Key + 5}°C",
                    TripCount = g.Count(),
                    AverageTempInBracket = Math.Round(g.Average(), 1)
                })
                .OrderByDescending(x => x.TripCount)
                .ToList();

            Console.WriteLine("6. Trip popularity by temp. brackets");
            DisplayQResult(stats);
        }

        public static void AverageSpeedByWeather(IEnumerable<BikeTrip> trips, IEnumerable<Weather> hourlyWeather)
        {
            var stats = trips
                    .Join(hourlyWeather,
                        trip => new { trip.StartedAt.Date, trip.StartedAt.Hour },
                        weather => new { Date = weather.DateTime.Date, weather.DateTime.Hour },
                        (trip, weather) => new
                        {
                            trip.DurationMinutes,
                            DistanceKm = CalculateDistance(trip.StartLat, trip.StartLng, trip.EndLat, trip.EndLng),
                            weather.WindSpeed
                        })
                    .Where(x => x.DurationMinutes > 0 && x.DistanceKm > 0)
                    .Select(x => new
                    {
                        WindBracket = (int)(x.WindSpeed / 5) * 5,
                        SpeedKmh = x.DistanceKm / (x.DurationMinutes / 60.0)
                    })
                    .Where(x => x.SpeedKmh < 60)
                    .GroupBy(x => x.WindBracket)
                    .Select(g => new
                    {
                        WindRange = $"{g.Key}-{g.Key + 5} km/h",
                        AverageSpeed = Math.Round(g.Average(x => x.SpeedKmh), 2),
                        TripCount = g.Count(),
                        SortKey = g.Key
                    })
                    .OrderBy(r => r.SortKey)
                    .Select(r => new
                    {
                        r.WindRange,
                        r.AverageSpeed,
                        r.TripCount
                    })
                    .ToList();

            Console.WriteLine("7. Avg Trip speed vs Wind Speed (km/h)");
            DisplayQResult(stats);
        }

        private static JsonSerializerOptions serializerOptions
            = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        private static void DisplayQResult<T>(T query)
        {
            var json = JsonSerializer.Serialize(query, serializerOptions);

            Console.WriteLine(json);
        }
    }

    }

