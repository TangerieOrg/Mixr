using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace SpotifyAPI.Middleware
{
    public class SpotifyUserMiddleware
    {
        private readonly RequestDelegate _next;

        public SpotifyUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        //This middleware checks if the user is authenticated and if the current route requires authentication
        public Task Invoke(HttpContext httpContext)
        {
            //Add some cache control headers to the response
            httpContext.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");

            //Sometimes would error out
            if (httpContext == null || httpContext.GetEndpoint() == null || httpContext.GetEndpoint().Metadata == null)
            {
                return _next(httpContext);
            }

            //Get the attributes on the route
            UserIDRequiredAttribute attr = httpContext.GetEndpoint().Metadata.GetMetadata<UserIDRequiredAttribute>();

            //If the route doesn't require authentication, continue
            if (attr == null) return _next(httpContext);

            StringValues userHeaders = new();

            httpContext.Request.Headers.TryGetValue("userId", out userHeaders);

            //No user id is provided
            if(userHeaders.Count == 0)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return httpContext.Response.WriteAsync("No UserID Provided");
            }

            //Get the user id
            string userKey = userHeaders.First();

            //Check if the id is registered
            if(Services.SpotifyService.Instance.GetSpotifyClient(userHeaders) == null)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return httpContext.Response.WriteAsync("Invalid UserID Provided");
            }

            //If it passed all checks, continue
            return _next(httpContext);
        }
    }


    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class SpotifyUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseMiddlewareClassTemplate(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SpotifyUserMiddleware>();
        }
    }

    //The empty attribute
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class UserIDRequiredAttribute : Attribute
    {

        public UserIDRequiredAttribute()
        {
        }
    }
}
