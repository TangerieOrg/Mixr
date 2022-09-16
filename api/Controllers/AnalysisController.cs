using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Extensions;
using SpotifyAPI.Helpers;
using SpotifyAPI.Middleware;
using SpotifyAPI.Web;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

//The endpoints for analysing/getting data from spotify
namespace SpotifyAPI.Controllers
{

    [UserIDRequired] //If an invalid userID is provided the call wont reach the controller
    public class AnalysisController : Controller
    {

        //Gets a playlist's info
        [HttpGet("playlist/info")]
        public async Task<PlaylistInfo> GetPlaylistInfo(string playlistId)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());
            return await client.GetPlaylistInfo(playlistId);
        }

        //Gets severeal playlists info
        [HttpGet("playlists/info")]
        public async Task<List<PlaylistInfo>> GetSeveralPlaylistInfo(string playlistIds)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());

            List<PlaylistInfo> infos = new List<PlaylistInfo>();

            //Get the uris
            foreach(string id in playlistIds.Split(","))
            {
                if (infos.Exists(x => x.Id == id)) continue;
                try
                {
                    //add each info
                    infos.Add(await client.GetPlaylistInfo(id));
                } catch
                {
                    //Do Nothing
                    //Failures occur nearly never but not all can be accounted for as some spotify tracks appear "corrupted"
                }
            }

            return infos;
        }

        //Get the current users profile
        [HttpGet("me/profile")]
        public async Task<PrivateUser> GetMyProfile()
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());

            return await client.GetMyProfile();
        }

        //Mix playlists with config
        [HttpGet("playlists/mix")]
        public async Task<List<TrackNode>> Mix(string playlistIds, int divisions = 8, int num = 50)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());

            List<HeatMapTile> multOutput = null;

            foreach (string id in playlistIds.Split(","))
            {
                PlaylistNodeSet set = await client.GetNodesFromPlaylist(id);
                var cells = client.GetNodeCells(set.Nodes, divisions);

                if(multOutput == null)
                {
                    multOutput = client.GetTilesFromCells(cells);
                } else
                {
                    multOutput = client.MultiplyHeatMap(multOutput, client.GetTilesFromCells(cells));
                }
            }

            var rnd = new Random();

            multOutput = multOutput.OrderBy(x => rnd.Next()).ToList();

            multOutput.Sort((a, b) =>
            {
                return b.Value.CompareTo(a.Value);
            });

            List<TrackNode> nodes = new List<TrackNode>();

            foreach(var tile in multOutput)
            {
                Console.WriteLine(tile.Value);
                if (nodes.Count >= num) break;
                nodes.AddRange(tile.Cell.Nodes.OrderBy(x => rnd.Next()));
            }


            return nodes;
        }

        //Create a new playlist
        [HttpPost("playlist/new")]
        public async Task<PlaylistInfo> CreatePlaylist([FromBody] CreatePlaylistReqest request)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());

            var play = await client.CreatePlaylist(request);

            return play;
        }

        //Get a user's info via id
        [HttpGet("user/info")]
        public async Task<PublicUser> GetUser(string userId)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());
            return await client.spotifyClient.UserProfile.Get(userId);
        }

        //Get the node for a track
        [HttpGet("track/node")]
        public async Task<TrackNode> GetTrackNode(string trackId)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());
            return await client.GetTrackNode(trackId);
        }

        //Get the nodeset for a playlist
        [HttpGet("playlist/nodeset")]
        public async Task<PlaylistNodeSet> GetPlaylistNodeSet(string playlistId)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());
            PlaylistNodeSet p = await client.GetNodesFromPlaylist(playlistId);
            return p;
        }

        //Get the list of nodesets for a user
        [HttpGet("user/nodesets")]
        public async Task<List<PlaylistNodeSet>> GetUserNodeSets(string userId)
        {
            AnalysisClient client = new AnalysisClient(HttpContext.Request.GetUserId());
            return await client.GetNodesFromUser(userId);
        }
    }

    //Extra endpoints that dont require authentication
    public class OtherController : Controller
    {
        //Get a random album cover
        [HttpGet("cover/random")]
        public Image GetRandomCoverImage()
        {
            /* Search in playlists as that is more likely to have tracks
             */
            var random = new Random();

            if (AnalysisClient.KnownTracks.Count > 100)
            {
                return AnalysisClient.KnownTracks.ElementAt(random.Next(AnalysisClient.KnownTracks.Count)).Value.Album.Images.FirstOrDefault();
            }

            if (AnalysisClient.KnownPlaylists.Count > 0)
            {
                var playlist = AnalysisClient.KnownPlaylists.ElementAt(random.Next(AnalysisClient.KnownPlaylists.Count)).Value;

                return playlist.Tracks.ElementAt(random.Next(playlist.Tracks.Count)).Album.Images.FirstOrDefault();
            }

            return new Image()
            {
                Height = 0,
                Width = 0,
                Url = "https://i.scdn.co/image/ab67616d0000b273d6e6aa04227f463f9c6d7c38"
            };
        }

        //Return the server status
        //Its always "online" so that any response means the server is functioning
        [HttpGet("status")]
        public IActionResult IsAlive()
        {
            return Ok("online");
        }
    }
}
