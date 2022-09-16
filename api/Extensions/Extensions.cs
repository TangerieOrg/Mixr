using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace SpotifyAPI.Extensions
{
    public static class HttpRequestExtension
    {
        //Add a function to the HttpRequest class
        //Gets a user id from the header (or "")
        public static string GetUserId(this HttpRequest request)
        {
            return request.Headers.FirstOrDefault(x => x.Key == "userid").Value.FirstOrDefault();
        }
    }
}
