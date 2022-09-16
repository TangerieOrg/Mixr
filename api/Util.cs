using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dotenv.net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace SpotifyAPI
{
    public class Util
    {
        //Only load the environment once
        private static bool envLoaded = false;

        //Load and read the environment variables from the .env file
        public static IDictionary<string, string> Environment
        {
            get
            {
                if(!envLoaded)
                {
                    DotEnv.Load();
                }
                return DotEnv.Read();
            }
        }

        //Just some random stuff
        private static Random rng = new Random();

        private const string AllowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        //Create a random string of length
        public static string GetRandomString(int length)
        {
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = AllowedChars[rng.Next(0, AllowedChars.Length)];
            }
            return new string(chars);
        }

        //Create a random string of a random length between min and max
        public static string GetRandomString(int min, int max)
        {
            return GetRandomString(rng.Next(min, max));
        }

        //Load a json file into a object of type T
        public static T LoadJsonFromFile<T>(string path)
        {
            if (!File.Exists(path)) return default(T);
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                T items = JsonConvert.DeserializeObject<T>(json);
                return items;
            }
        }

        //Save an object to a json file
        public static void SaveJsonToFile(object data, string path)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            } catch
            {

            }
           
        }
    }
}
