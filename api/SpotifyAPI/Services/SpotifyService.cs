using System;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SpotifyAPI.Services
{
    /*
     * This service manages the authentication flow of spotify accounts
     */
    public class SpotifyService
    {
        private static string CLIENT_FILE_PATH = "Data/Clients.json";

        //Only allow one
        private static SpotifyService _instance;
        public static SpotifyService Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new SpotifyService();
                }
                return _instance;
            }
        }

        //The store of clients, token => account
        private Dictionary<string, AuthorizationCodeTokenResponse> clients;

        //Load in the clients
        public SpotifyService()
        {
            clients = new Dictionary<string, AuthorizationCodeTokenResponse>();
            if(File.Exists(CLIENT_FILE_PATH))
            {
                clients = Util.LoadJsonFromFile<Dictionary<string, AuthorizationCodeTokenResponse>>(CLIENT_FILE_PATH);
            }
        }

        
        /* Authorises a client and produces a token of length 50 (there are over 52^50 combinations)
         * It pretty much just means that you can request it later (this can be stored in the users local storage)
         */
        public async Task<string> AuthorizeAccessToken(string accessToken)
        {
            var response = await new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(
                Util.Environment["SPOTIFY_CLIENT_ID"],
                Util.Environment["SPOTIFY_CLIENT_SECRET"],
                accessToken,
                new Uri(Util.Environment["SPOTIFY_CALLBACK_URL"]))); //Actually register with the Spotify API

            //If you somehow manage to create 2 matching keys, redo it
            string token = Util.GetRandomString(50);
            while(clients.ContainsKey(token))
            {
                token = Util.GetRandomString(50);
            }

            //Save the client list and return the token
            clients[token] = response;
            Util.SaveJsonToFile(clients, CLIENT_FILE_PATH);
            return token;
        }


        // Get a client from a token
        public SpotifyClient GetSpotifyClient(string id)
        {
            if (clients.ContainsKey(id))
            {
                //If the client needs re-registering (every 10 minutes or so spotify revokes your access) do it and return the new client
                SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(new AuthorizationCodeAuthenticator(
                    Util.Environment["SPOTIFY_CLIENT_ID"],
                    Util.Environment["SPOTIFY_CLIENT_SECRET"],
                    clients[id]));

                return new SpotifyClient(config);
            }
            return null;
        }
    }
}
