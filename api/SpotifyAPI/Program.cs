using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using dotenv.net;
using SpotifyAPI.Helpers;

namespace SpotifyAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DotEnv.Load();

            CreateHostBuilder(args).Build().Run();
            Console.WriteLine("Host Builder Ended");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

/* Directory Structure
 *  Controllers => Api Endpoints
 *  Data => Files to save to n such
 *  Extensions => Extensions to C# methods
 *  Helpers => Utitlies n such
 *  Middleware => Web route middleware
 *  Services => Single-Instance Services
 * 
 * Special Files
 *  .env => environmental variables
 */