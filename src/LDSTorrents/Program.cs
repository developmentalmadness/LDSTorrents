using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MonoTorrent.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using Massive;
using System.Collections.Generic;
using System.Dynamic;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace LDSTorrents
{
    class Program
    {
        static dynamic Torrents = new DynamicModel("LDSTorrents", tableName: "Torrents", primaryKeyField: "TorrentID");

        static void Main(string[] args)
        {
            //ScrapeChannel();
            //ScrapeCategories();
            ScrapeChannels();
        }

        private static void ScrapeChannels()
        {
            var table = new DynamicModel("LDSTorrents", tableName: "Channels", primaryKeyField: "ChannelID");

            var channels = table.All();
            foreach (var channel in channels)
            {
                Console.WriteLine("Scraping '{0}'...", channel.Title);
                var videos = ScrapeChannel(channel);
                Torrents.Save(videos.ToArray());
                break;
            }
        }

        private static void ScrapeCategories()
        {
            string url = "http://www.mormonchannel.org/";
            var request = WebRequest.CreateHttp(url);

            HtmlDocument html = new HtmlDocument();
            using (var input = request.GetResponse())
                html.Load(input.GetResponseStream(), true);

            var doc = html.DocumentNode;

            var categories = doc.QuerySelectorAll(".ribbon");
            foreach (var item in categories)
            {
                var category = item.QuerySelector(".ribbon-title").InnerText;
                category = category.Substring(0, category.IndexOf("&#160;")).TrimEnd();

                var channels = item.QuerySelectorAll(".teaser_title");
                foreach (var channel in channels)
                {
                    var name = channel.InnerText;
                    var location = channel.GetAttributeValue("href", String.Empty);
                    Console.WriteLine("('{0}','mormonchannel.org','{1}','{2}'),", name, location, category);
                }
            }
        }

        private static IEnumerable<dynamic> ScrapeChannel(dynamic channel)
        {
            var list = new List<dynamic>();

            var request = WebRequest.CreateHttp(channel.Url);

            HtmlDocument html = new HtmlDocument();
            using (var input = request.GetResponse())
                html.Load(input.GetResponseStream(), true);

            var doc = html.DocumentNode;

            var videos = doc.QuerySelectorAll(".video");
            foreach (var video in videos)
            {
                var urls = video.QuerySelectorAll(".download_links  a");
                foreach (var item in urls)
                {
                    if (String.Compare(item.InnerText, "mp4", true) == 0)
                    {
                        var title = HttpUtility.HtmlDecode(video.QuerySelector("a.teaser_title").InnerText);
                        var resource = item.GetAttributeValue("href", String.Empty);
                        var exists = Torrents.First(ResourceUri: resource);
                        if (exists != null)
                            continue;

                        Console.WriteLine("{0} ==> {1}", title, resource);

                        string result = String.Empty;
                        string rh = String.Empty;
                        string torrentUrl = String.Empty;

                        while (true)
                        {
                            var torrentRequest = WebRequest.CreateHttp("http://burnbit.com/regfile");
                            torrentRequest.Method = "POST";
                            torrentRequest.ContentType = "application/x-www-form-urlencoded";
                            torrentRequest.Accept = "application/json; charset=utf-8";

                            switch (result)
                            {
                                case "":
                                    using (StreamWriter writer = new StreamWriter(torrentRequest.GetRequestStream()))
                                        writer.Write(String.Format("file={0}", HttpUtility.UrlEncode(resource)));
                                    break;
                                case "wait":
                                    using (StreamWriter writer = new StreamWriter(torrentRequest.GetRequestStream()))
                                        writer.Write(String.Format("rh={0}", rh));
                                    break;
                                default:
                                    Console.WriteLine("Oops! What happened? '{0}'", result);
                                    break;
                            }

                            string json = String.Empty;
                            using (StreamReader reader = new StreamReader(torrentRequest.GetResponse().GetResponseStream()))
                                json = reader.ReadToEnd();

                            var response = JObject.Parse(json);
                            result = response.GetValue("status").Value<String>();

                            if ("|success|exists|".IndexOf(result) != -1)
                            {
                                torrentUrl = String.Format("http://burnbit.com{0}", response.GetValue("redirect").Value<String>().Replace("/torrent/", "/download/"));
                                Console.WriteLine("Torrent: {0}", torrentUrl);
                                break;
                            }
                            else if (result == "error")
                            {
                                //TODO: if error includes "not resumable" I'll need to create the torrent manually: Download the file, create the torrent, upload the torrent (via ftp) to a hosted url, then add it to the feed
                                var error = response.GetValue("html").Value<String>();
                                Console.WriteLine("Error: {0}", error);
                                break;
                            }

                            rh = response.GetValue("rh").Value<String>();
                            Console.WriteLine("Waiting (rh: {0})...", rh);
                            Thread.Sleep(5000);
                        }

                        if (torrentUrl.Length == 0)
                            continue;

                        var fileSize = 0L;
                        var fileName = String.Format("{0}.torrent", torrentUrl.Substring(torrentUrl.LastIndexOf("/") + 1));
                        string localPath = String.Format("torrents{4}{0}{4}{1}{4}{2}{4}{3}", channel.Host, channel.Category, channel.Title, fileName, Path.DirectorySeparatorChar);
                        localPath = localPath.Replace(' ', '_').Replace(",", String.Empty).Replace("'", String.Empty);
                        string filePath = Path.Combine(Environment.CurrentDirectory, localPath);

                        if (!Directory.Exists(filePath))
                        {
                            var directory = Path.GetDirectoryName(filePath);
                            Directory.CreateDirectory(directory);
                        }

                        //download torrent
                        Console.WriteLine("Downloading torrent file '{0}'", torrentUrl);
                        var getTorrent = WebRequest.CreateHttp(torrentUrl);
                        getTorrent.Method = "GET";
                        getTorrent.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");

                        using (var downloadTorrent = new GZipStream(getTorrent.GetResponse().GetResponseStream(), CompressionMode.Decompress))
                        {
                            using (var torrentFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            {
                                byte[] torrentBuffer = new byte[4096];
                                int bytesRead = -1;
                                while (0 < (bytesRead = downloadTorrent.Read(torrentBuffer, 0, torrentBuffer.Length)))
                                    torrentFile.Write(torrentBuffer, 0, bytesRead);
                                fileSize = torrentFile.Length;
                            }
                        }

                        var pubDate = DateTime.Today;
                        var pubDateRegex = Regex.Match(fileName, @"(?<year>\d{4})_(?<month>\d{2})_(?<day>\d{3})");
                        if (pubDateRegex.Success)
                        {
                            pubDate = new DateTime(Int32.Parse(pubDateRegex.Groups["year"].Value),
                                                    Int32.Parse(pubDateRegex.Groups["month"].Value),
                                                    Int32.Parse(pubDateRegex.Groups["day"].Value));
                        }

                        list.Add(new { 
                            ChannelID = channel.ChannelID,
                            Title = title,
                            DatePublished = pubDate,
                            TorrentUri = torrentUrl,
                            ResourceUri = resource,
                            LocalPath = String.Format("{0}{1}", Path.DirectorySeparatorChar, localPath),
                            FileByteLength = fileSize
                        });
                    }
                }

                if(list.Count != 0)
                    break;
            }

            return list;
        }
    }
}
