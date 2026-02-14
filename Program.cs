using System;
using System.Collections.Generic;
using static CityBikeProject.Utils;

namespace CityBikeProject
{
    class Program
    {
        static void Main(string[] args)
        {
            string tripsPath = @"JC-202509-citibike-tripdata.csv";
            string weatherPath = @"open-meteo-40.74N74.04W11m.csv";

            var trips = Utils.LoadTrips(tripsPath);
            var hourlyWeather = Utils.LoadWeather(weatherPath);

            if (trips == null || trips.Count == 0) { Console.WriteLine("Trip data error!"); return; }
            if (hourlyWeather == null || hourlyWeather.Count == 0) { Console.WriteLine("Weather data error!"); return; }

            var dailyWeather = AggregateDailyWeather(hourlyWeather);

            Queries.BasicStatsTime(trips);
            Queries.FindPopularSpots(trips);
            Queries.CompareBikeTypes(trips);
            Queries.AnalyzeRainImpact(trips, dailyWeather);
            Queries.AnalyzeWeeklyTrends(trips, dailyWeather);
            Queries.AnalyzeTempBrackets(trips, hourlyWeather);
            Queries.AverageSpeedByWeather(trips, hourlyWeather);

            Console.WriteLine("Analysis finished.");
        }
    }
}