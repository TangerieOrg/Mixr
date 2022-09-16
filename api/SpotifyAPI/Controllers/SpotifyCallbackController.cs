using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotenv.net;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Services;
using SpotifyAPI.Web;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

//This manages everything to do with spotify authentication
namespace SpotifyAPI.Controllers
{
    //Get a code from the request and register it
    //Returns a token for later use
    [Route("spotify")]
    public class SpotifyCallbackController : Controller
    {
        // GET: /<controller>/
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code)
        {
            Console.WriteLine(code);

            string id = await SpotifyService.Instance.AuthorizeAccessToken(code);

            return Ok(id);
        }

        //Generate the callback url
        [HttpGet("url")]
        public string GetUrl()
        {
            var env = DotEnv.Read();
            LoginRequest loginRequest = new LoginRequest(
                new Uri(env["SPOTIFY_CALLBACK_URL"]),
                env["SPOTIFY_CLIENT_ID"],
                LoginRequest.ResponseType.Code);

            loginRequest.Scope = new string[]
            {
                Scopes.AppRemoteControl,
                Scopes.PlaylistModifyPrivate,
                Scopes.PlaylistModifyPublic,
                Scopes.PlaylistReadCollaborative,
                Scopes.PlaylistReadPrivate,
                Scopes.Streaming,
                Scopes.UgcImageUpload,
                Scopes.UserFollowModify,
                Scopes.UserFollowRead,
                Scopes.UserLibraryModify,
                Scopes.UserLibraryRead,
                Scopes.UserModifyPlaybackState,
                Scopes.UserReadCurrentlyPlaying,
                Scopes.UserReadEmail,
                Scopes.UserReadPlaybackPosition,
                Scopes.UserReadPlaybackState,
                Scopes.UserReadPrivate,
                Scopes.UserReadRecentlyPlayed,
                Scopes.UserTopRead
            };

            return loginRequest.ToUri().ToString();
        }
    }
}