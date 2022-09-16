using System;
using System.Collections.Generic;
using SpotifyAPI.Services;
using SpotifyAPI.Web;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

//Heres where the calculations happen
namespace SpotifyAPI.Helpers
{
    //Some models
    public struct TrackNode
    {
        public float Acousticness { get; set; }
        public float Danceability { get; set; }
        public float Energy { get; set; }
        public float Instrumentalness { get; set; }
        public float Liveness { get; set; }
        public float Mode { get; set; }
        public float Speechiness { get; set; }
        public float Valence { get; set; }
        public float Tempo { get; set; }
        public int TimeSignature { get; set; }
        public string Id { get; set; }
        public string Uri { get; set; }
        public string Name { get; set; }
        public string[] Artists { get; set; }
        public Image CoverImage { get; set; }
    }


    public struct CellCoord
    {
        public int A { get; set; }
        public int D { get; set; }
        public int E { get; set; }
        public int I { get; set; }
        public int L { get; set; }
        public int S { get; set; }
        public int V { get; set; }
    }

    public struct NodeCell
    {
        public List<TrackNode> Nodes { get; set; }
        public CellCoord Position { get; set; }
    }

    public struct HeatMapTile
    {
        public NodeCell Cell { get; set; }
        public float Value { get; set; }
    }

    public struct PlaylistNodeSet
    {
        public List<TrackNode> Nodes { get; set; }
        public PlaylistInfo Info { get; set; }
    }

    public struct PlaylistTrackSet
    {
        public List<FullTrack> Tracks { get; set; }
        public PlaylistInfo Info { get; set; }
        public string SnapshotId { get; set; }
    }

    public struct PlaylistInfo
    {
        public string Name { get; set; }
        public PublicUser Owner { get; set; }
        public string Id { get; set; }
        public string Uri { get; set; }
        public bool IsPublic { get; set; }
        public bool IsCollaborative { get; set; }
        public string Description { get; set; }
        public Image CoverImage { get; set; }
        public int TotalSongs { get; set; }
    }

    public struct CreatePlaylistReqest
    {
        public string Name { get; set; }
        public List<string> Ids { get; set; }
    }

    public class AnalysisClient
    {
        public static readonly string ANALYSIS_MEMORY_PATH = "Data/AnalysisMemory/";

        //Create the cache dictionaries. Maps uri => item
        public static Dictionary<string, FullTrack> KnownTracks = new Dictionary<string, FullTrack>();
        public static Dictionary<string, TrackAudioFeatures> KnownFeatures = new Dictionary<string, TrackAudioFeatures>();
        public static Dictionary<string, PlaylistTrackSet> KnownPlaylists = new Dictionary<string, PlaylistTrackSet>();

        public SpotifyClient spotifyClient;

        //Load in the caches if they exist
        static AnalysisClient()
        {
            if (File.Exists(ANALYSIS_MEMORY_PATH + "KnownTracks.json")) KnownTracks = Util.LoadJsonFromFile<Dictionary<string, FullTrack>>(ANALYSIS_MEMORY_PATH + "KnownTracks.json");
            if (File.Exists(ANALYSIS_MEMORY_PATH + "KnownFeatures.json")) KnownFeatures = Util.LoadJsonFromFile<Dictionary<string, TrackAudioFeatures>>(ANALYSIS_MEMORY_PATH + "KnownFeatures.json");
            if (File.Exists(ANALYSIS_MEMORY_PATH + "KnownPlaylists.json")) KnownPlaylists = Util.LoadJsonFromFile<Dictionary<string, PlaylistTrackSet>>(ANALYSIS_MEMORY_PATH + "KnownPlaylists.json");
        }

        //userId is from the SpotifyService
        public AnalysisClient(string userId)
        {
            spotifyClient = SpotifyService.Instance.GetSpotifyClient(userId);
        }

        //Get the audio featues for a track
        public async Task<TrackAudioFeatures> GetTrackAudioFeature(string trackId)
        {
            //Utilise the cache
            if(KnownFeatures.ContainsKey(trackId))
            {
                return KnownFeatures[trackId];
            }

            var feat = await spotifyClient.Tracks.GetAudioFeatures(trackId);
            KnownFeatures.Add(trackId, feat);
            return feat;
        }

        //Audio features for multiple tracks
        public async Task<List<TrackAudioFeatures>> GetTrackAudioFeatures(List<string> trackIds)
        {
            Console.WriteLine("Getting Audio Features For Tracks");
            List<TrackAudioFeatures> nodes = new List<TrackAudioFeatures>();

            //If we already know them, dont redownload
            foreach(var track in trackIds)
            {
                if(KnownFeatures.ContainsKey(track))
                {
                    nodes.Add(KnownFeatures[track]);
                }
            }

            //Remove the cached trackids from the fetch list
            var trackEnum = trackIds.Where(x => !KnownFeatures.ContainsKey(x));

            trackIds = trackEnum.ToList();
            
            if (trackIds.Count > 0)
            {
                int trackOffset = 0;
                Console.WriteLine($"Fetching {trackIds.Count} Track Analysiseseses");

                while (trackIds.Count - trackOffset > 0)
                {
                    Console.WriteLine($"Fetching {Math.Min(trackIds.Count - trackOffset, 100)} Next Tracks ({trackIds.Count - trackOffset} Left)");
                    var features = await spotifyClient.Tracks.GetSeveralAudioFeatures(new TracksAudioFeaturesRequest(trackIds.Skip(trackOffset).Take(100).ToList()));
                    Console.WriteLine(features.AudioFeatures.Count);
                    nodes.AddRange(features.AudioFeatures);
                    foreach (var f in features.AudioFeatures)
                    {
                        if (KnownFeatures.ContainsKey(f.Id)) continue;
                        KnownFeatures.Add(f.Id, f);
                    }
                    trackOffset += 100;
                }

                Util.SaveJsonToFile(KnownFeatures, ANALYSIS_MEMORY_PATH + "KnownFeatures.json");
            }

            return nodes;
        }

        //Get the tracks from a playlist
        public async Task<PlaylistTrackSet> GetTracksFromPlaylist(string playlistId)
        {
            Console.WriteLine($"Getting Playlist {playlistId}");

            var playlist = await spotifyClient.Playlists.Get(playlistId);

            if (KnownPlaylists.ContainsKey(playlistId))
            {
                //Use the cache if the playlist is unchanged
                if(KnownPlaylists[playlistId].SnapshotId == playlist.SnapshotId)
                {
                    Console.WriteLine("Snapshot Unchanged");
                    return KnownPlaylists[playlistId];
                } else
                {
                    Console.WriteLine("Snapshot Changed");
                    KnownPlaylists.Remove(playlistId);
                }
            }
            

            PlaylistTrackSet trackSet = new PlaylistTrackSet();
            trackSet.Info = new PlaylistInfo()
            {
                Id = playlist.Id,
                IsCollaborative = playlist.Collaborative == true,
                IsPublic = playlist.Public == true,
                Description = playlist.Description,
                Name = playlist.Name,
                Owner = playlist.Owner,
                Uri = playlist.Uri,
                CoverImage = playlist.Images.OrderBy(x => x.Height * x.Width).First(),
                TotalSongs = (int)playlist.Tracks.Total,
            };

            trackSet.Tracks = new List<FullTrack>();

            trackSet.SnapshotId = playlist.SnapshotId;

            var pages = await spotifyClient.Playlists.GetItems(playlistId);
            Console.WriteLine("Fetching Pages");
            await foreach(var track in spotifyClient.Paginate(pages))
            {
                //Fetch all the tracks
                try
                {
                    if (track.Track.Type == ItemType.Episode || track.IsLocal || ((FullTrack)track.Track).Album.Images.Count == 0) continue;
                    trackSet.Tracks.Add((FullTrack)track.Track);
                } catch(Exception e)
                {

                }
            }

            try
            {
                //Save to cache
                KnownPlaylists.Add(playlistId, trackSet);

                //Save cache
                Util.SaveJsonToFile(KnownPlaylists, ANALYSIS_MEMORY_PATH + "KnownPlaylists.json");
            } catch(Exception e)
            {
                Console.WriteLine("Couldn't Add Playlist To Dict");
                //Console.WriteLine(e);
            }
            

            return trackSet;
        }

        /* Get the tracks
         */
        public async Task<List<FullTrack>> GetTracks(List<string> trackIds)
        {
            List<FullTrack> tracks = new List<FullTrack>();

            foreach(var id in trackIds)
            {
                if(KnownFeatures.ContainsKey(id))
                {
                    trackIds.Remove(id);
                    tracks.Add(KnownTracks[id]);
                }
            }

            int trackOffset = 0;

            while(trackIds.Count - trackOffset > 0)
            {
                Console.WriteLine($"Fetching {Math.Min(trackIds.Count - trackOffset, 50)} Next Tracks ({trackIds.Count - trackOffset} Left)");
                var ts = await spotifyClient.Tracks.GetSeveral(new TracksRequest(trackIds.Skip(trackOffset).Take(50).ToList()));
                tracks.AddRange(ts.Tracks);

                foreach(var track in ts.Tracks)
                {
                    KnownTracks.Add(track.Id, track);
                }

                trackOffset += 100;
            }

            if(trackIds.Count > 0)
            {
                Util.SaveJsonToFile(KnownTracks, ANALYSIS_MEMORY_PATH + "KnownTracks.json");
            }
            
            return tracks;
        }

        //Get a single track
        public async Task<FullTrack> GetTrack(string trackId)
        {
            if(KnownTracks.ContainsKey(trackId))
            {
                return KnownTracks[trackId];
            }
            FullTrack track = await spotifyClient.Tracks.Get(trackId);
            if (track == null) return null;
            KnownTracks.Add(trackId, track);
            return track;
        }

        //Get a track node
        public async Task<TrackNode> GetTrackNode(string trackId)
        {
            FullTrack track = await GetTrack(trackId);
            TrackAudioFeatures feat = await GetTrackAudioFeature(trackId);
            return makeNode(track, feat);
        }

        //Get multiple track nodes
        public async Task<List<TrackNode>> GetNodes(List<string> trackIds)
        {
            List<TrackNode> nodes = new List<TrackNode>();

            List<FullTrack> tracks = await GetTracks(trackIds);
            List<TrackAudioFeatures> features = await GetTrackAudioFeatures(trackIds);

            foreach(var track in tracks)
            {
                TrackAudioFeatures feat = features.Where(x => x.Id == track.Id).First();
                nodes.Add(makeNode(track, feat));
            }

            //We dont care for duplicates
            nodes = RemoveDuplicateNodes(nodes);

            return nodes;
        }

        //Get all the nodes in a playlist
        public async Task<PlaylistNodeSet> GetNodesFromPlaylist(string playlistId)
        {
            Console.WriteLine("Getting Nodes For Playlist");
            PlaylistNodeSet nodeSet = new PlaylistNodeSet();

            var tracks = await GetTracksFromPlaylist(playlistId);

            nodeSet.Info = tracks.Info;

            List<TrackAudioFeatures> features = await GetTrackAudioFeatures(tracks.Tracks.Select(x => x.Id).ToList());

            nodeSet.Nodes = tracks.Tracks.Select(x =>
                makeNode(x, features.Where(f => f.Id == x.Id).FirstOrDefault()))
                .ToList();

            nodeSet.Nodes = RemoveDuplicateNodes(nodeSet.Nodes);

            return nodeSet;
        }

        //Get all the nodesets in a user
        public async Task<List<PlaylistNodeSet>> GetNodesFromUser(string userId)
        {
            Console.WriteLine($"Getting Playlists From User {userId}");

            var playlists = await spotifyClient.Playlists.GetUsers(userId);

            Console.WriteLine($"Found {playlists.Total} Playlists");

            List<PlaylistNodeSet> sets = new List<PlaylistNodeSet>();

            await foreach(var playlist in spotifyClient.Paginate(playlists))
            {
                sets.Add(await GetNodesFromPlaylist(playlist.Id));
            }

            return sets;
        }

        //Remove duplicate nodes
        public List<TrackNode> RemoveDuplicateNodes(List<TrackNode> nodes)
        {
            List<TrackNode> doneNodes = new List<TrackNode>();

            foreach(TrackNode n in nodes)
            {
                if (doneNodes.Where(x => x.Name == n.Name && x.Artists.All(n.Artists.Contains)).Count() == 0) doneNodes.Add(n);
                //Instead of Uri we compare these
                //This is because sometimes artists release the same song multiple times
            }

            return doneNodes;
        }

        //Create node cells from a node list
        public List<NodeCell> GetNodeCells(List<TrackNode> nodes, int divisions)
        {
            var watch = new Stopwatch();
            List<NodeCell> cells = new List<NodeCell>();

            foreach(var node in nodes)
            {
                CellCoord coord = getNodeCoord(node, divisions);
                bool added = false;
                foreach(NodeCell cell in cells)
                {
                    if(cell.Position.Equals(coord))
                    {
                        added = true;
                        cell.Nodes.Add(node);
                    }
                }

                if(!added)
                {
                    cells.Add(new NodeCell()
                    {
                        Nodes = new List<TrackNode>() { node },
                        Position = coord
                    });
                }
            }

            return cells;
        }

        //Create heatmap tiles from the node cells
        public List<HeatMapTile> GetTilesFromCells(List<NodeCell> cells)
        {
            var tiles = new List<HeatMapTile>();

            float maxVal = cells.Max(c => c.Nodes.Count);
            Console.WriteLine($"Maximum Value Found {maxVal}");
            foreach (var cell in cells)
            {
                tiles.Add(new HeatMapTile
                {
                    Cell = cell,
                    Value = cell.Nodes.Count / maxVal
                });
            }

            return tiles;
        }

        //"Multiply" two heatmaps
        //This is how heatmaps are combined
        public List<HeatMapTile> MultiplyHeatMap(List<HeatMapTile> A, List<HeatMapTile> B)
        {
            List<HeatMapTile> tiles = new List<HeatMapTile>();
            List<CellCoord> doneCoords = new List<CellCoord>();
            foreach(HeatMapTile bTile in B)
            {
                bool found = false;
                foreach(HeatMapTile aTile in A)
                {
                    if (found) continue; //If we've already found it, just ignore

                    //If the tiles are of the same position
                    if (areCellCoordEqual(aTile.Cell.Position, bTile.Cell.Position))
                    {
                        found = true;
                        NodeCell cell = new NodeCell()
                        {
                            Nodes = new List<TrackNode>(),
                            Position = bTile.Cell.Position
                        };

                        //Add the nodes, whislt ignoreing duplicates
                        foreach(var n in aTile.Cell.Nodes)
                        {
                            //Same duplicate checking code as in the removeDuplications function
                            if (cell.Nodes.Where(x => x.Name == n.Name && x.Artists.All(n.Artists.Contains)).Count() == 0) cell.Nodes.Add(n);
                        }

                        foreach (var n in bTile.Cell.Nodes)
                        {
                            if (cell.Nodes.Where(x => x.Name == n.Name && x.Artists.All(n.Artists.Contains)).Count() == 0) cell.Nodes.Add(n);
                        }

                        //cell.Nodes = RemoveDuplicateNodes(cell.Nodes);
                        tiles.Add(new HeatMapTile()
                        {
                            Cell = cell,
                            Value = aTile.Value * bTile.Value
                        });
                    }
                }

                //If thet arent found, add this tile but divide value by 2 to reduce its significance
                if (!found)
                {
                    tiles.Add(new HeatMapTile() {
                        Cell = bTile.Cell,
                        Value = bTile.Value / 2
                    });
                }
                doneCoords.Add(bTile.Cell.Position);
            }

            //If thet arent found, add this tile but divide value by 2 to reduce its significance
            foreach (HeatMapTile aTile in A)
            {
                if(!doneCoords.Contains(aTile.Cell.Position))
                {
                    tiles.Add(new HeatMapTile() {
                        Cell = aTile.Cell,
                        Value = aTile.Value / 2
                    });
                }
            }

            return tiles;
        }


        //Gets a playlists info and formats it
        public async Task<PlaylistInfo> GetPlaylistInfo(string playlistId)
        {
            var playlist = await spotifyClient.Playlists.Get(playlistId);

            return new PlaylistInfo()
            {
                Id = playlist.Id,
                IsCollaborative = playlist.Collaborative == true,
                IsPublic = playlist.Public == true,
                Description = playlist.Description,
                Name = playlist.Name,
                Owner = playlist.Owner,
                Uri = playlist.Uri,
                CoverImage = playlist.Images.OrderBy(x => x.Height * x.Width).FirstOrDefault(),
                TotalSongs = (int)playlist.Tracks.Total
            };

        }

        //Create a playlist and add tracks to it
        public async Task<PlaylistInfo> CreatePlaylist(CreatePlaylistReqest request)
        {
            var playlist = await spotifyClient.Playlists.Create((await spotifyClient.UserProfile.Current()).Id, new PlaylistCreateRequest(request.Name));

            int trackOffset = 0;

            while(request.Ids.Count - trackOffset > 0)
            {
                SnapshotResponse res = await spotifyClient.Playlists.AddItems(playlist.Id, new PlaylistAddItemsRequest(request.Ids.Skip(trackOffset).Take(100).Select(x => "spotify:track:" + x).ToList()));
                trackOffset += 100;
            }

            return new PlaylistInfo()
            {
                Id = playlist.Id,
                IsCollaborative = playlist.Collaborative == true,
                IsPublic = playlist.Public == true,
                Description = playlist.Description,
                Name = playlist.Name,
                Owner = playlist.Owner,
                Uri = playlist.Uri,
                CoverImage = playlist.Images.OrderBy(x => x.Height * x.Width).FirstOrDefault(),
                TotalSongs = (int)playlist.Tracks.Total,
            };
        }

        //Get the current users profile
        public async Task<PrivateUser> GetMyProfile()
        {
            return await spotifyClient.UserProfile.Current();
        }

        //Create a node from track and features
        private static TrackNode makeNode(FullTrack track, TrackAudioFeatures feat)
        {
            return new TrackNode()
            {
                Acousticness = feat.Acousticness,
                Danceability = feat.Danceability,
                Energy = feat.Energy,
                Instrumentalness = feat.Instrumentalness,
                Liveness = feat.Liveness,
                Mode = feat.Mode,
                Speechiness = feat.Speechiness,
                Valence = feat.Valence,
                Uri = feat.Uri,
                Id = feat.Id,
                Name = track.Name,
                Artists = track.Artists.Select(x => x.Name).ToArray(),
                Tempo = feat.Tempo,
                TimeSignature = feat.TimeSignature,
                CoverImage = track.Album.Images.OrderBy(x => x.Height * x.Width).FirstOrDefault()
            };
        }

        //Get the coord of a node
        private static CellCoord getNodeCoord(TrackNode n, int divisions)
        {
            return new CellCoord()
            {
                A = getDivision(n.Acousticness, 1, divisions),
                D = getDivision(n.Danceability, 1, divisions),
                E = getDivision(n.Energy, 1, divisions),
                I = getDivision(n.Instrumentalness, 1, divisions),
                L = getDivision(n.Liveness, 1, divisions),
                S = getDivision(n.Speechiness, 1, divisions),
                V = getDivision(n.Valence, 1, divisions)
            };
        }

        // Create "steps" from a continuous value
        private static int getDivision(float val, float max, int divisions)
        {
            return (int)Math.Min(Math.Floor(val / max * divisions), divisions - 1);
        }

        //Compares 2 cell coords
        private static bool areCellCoordEqual(CellCoord A, CellCoord B)
        {
            return A.A == B.A &&
                A.D == B.D &&
                A.E == B.E &&
                A.I == B.I &&
                A.L == B.L &&
                A.S == B.S &&
                A.V == B.V;
        }
    }
}
