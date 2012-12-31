using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using log4net;
using Massive;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Elmah;

namespace Web.Code
{
    public class Crawler
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Crawler));

        static dynamic Torrents = new DynamicModel("LDSTorrents", tableName: "Torrents", primaryKeyField: "TorrentID");
        static int Running = 0;
        static string downloadDir;

        public static void ScrapeChannels(object args)
        {
            if (Interlocked.CompareExchange(ref Running, 1, 0) != 0)
                return;

            try
            {
                if (String.IsNullOrEmpty(downloadDir))
                {
                    string dir = ConfigurationManager.AppSettings["torrentsdir"];
                    if (!String.IsNullOrEmpty(dir))
                    {
                        var context = args as HttpContextBase;
                        if (Path.IsPathRooted(dir) == false && context != null)
                            dir = context.Server.MapPath(dir);

                        if (Directory.Exists(dir))
                            downloadDir = dir;
                        else
                            downloadDir = Environment.CurrentDirectory;
                    }
                }

                var table = new DynamicModel("LDSTorrents", tableName: "Channels", primaryKeyField: "ChannelID");

                var channels = table.All();
                foreach (var channel in channels)
                {
                    try
                    {
                        logger.InfoFormat("Scraping '{0}'...", channel.Title);
                        var videos = ScrapeChannel(channel);
                        Torrents.Save(videos.ToArray());
                        table.Update(new { LastUpdated = DateTime.Now }, channel.ChannelID);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(String.Format("Failed to scrape channel '{0}' with the following exception: ", channel.Title), ex);
                        ErrorLog.GetDefault(null).Log(new Error(ex));
                    }
#if DEBUG
                    break;
#endif
                }
            }
            catch (Exception ex)
            {
                logger.Error("ScrapeChannels failed with the following exception: ", ex);
                ErrorLog.GetDefault(null).Log(new Error(ex));
            }

            Interlocked.Exchange(ref Running, 0);
        }

        private static IEnumerable<dynamic> ScrapeChannel(dynamic channel)
        {
            var list = new List<dynamic>();

            var request = WebRequest.Create(channel.Url);

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
                        if (exists != null && exists.FileByteLength != 0)
                            continue;

                        logger.InfoFormat("{0} ==> {1}", title, resource);
                        
                        string result = String.Empty;
                        string rh = String.Empty;
                        string torrentUrl = String.Empty;

                        while (true)
                        {
                            var torrentRequest = (HttpWebRequest) WebRequest.Create("http://burnbit.com/regfile");
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
                                    logger.InfoFormat("Oops! What happened? '{0}'", result);
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
                                logger.InfoFormat("Torrent: {0}", torrentUrl);
                                break;
                            }
                            else if (result == "error")
                            {
                                //TODO: if error includes "not resumable" I'll need to create the torrent manually: Download the file, create the torrent, upload the torrent (via ftp) to a hosted url, then add it to the feed
                                var error = response.GetValue("html").Value<String>();
                                logger.WarnFormat("burnbit.com/regfile returned and error while trying to burn '{1}' - error: {0}", error, resource);
                                break;
                            }

                            rh = response.GetValue("rh").Value<String>();
                            logger.InfoFormat("Waiting (rh: {0})...", rh);
                            Thread.Sleep(5000);
                        }

                        if (torrentUrl.Length == 0)
                        {
                            logger.WarnFormat("No torrent available for '{0}'", resource);
                            continue;
                        }

                        var fileSize = 0L;
                        var fileName = String.Format("{0}.torrent", torrentUrl.Substring(torrentUrl.LastIndexOf("/") + 1));
                        string localPath = String.Format("torrents{4}{0}{4}{1}{4}{2}{4}{3}", channel.Host, channel.Category, channel.Title, fileName, Path.DirectorySeparatorChar);
                        localPath = localPath.Replace(' ', '_').Replace(",", String.Empty).Replace("'", String.Empty);
                        string filePath = Path.Combine(downloadDir, localPath);

                        if (!Directory.Exists(filePath))
                        {
                            var directory = Path.GetDirectoryName(filePath);
                            Directory.CreateDirectory(directory);
                        }

                        //download torrent
                        logger.InfoFormat("Downloading torrent file '{0}'", torrentUrl);
                        var getTorrent = WebRequest.Create(torrentUrl);
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
                        try
                        {
                            var pubDateRegex = Regex.Match(fileName, @"(?<year>\d{4})_(?<month>\d{2})_(?<day>\d{3})");
                            if (pubDateRegex.Success)
                            {
                                int day = 1;
                                Int32.TryParse(pubDateRegex.Groups["day"].Value, out day);
                                pubDate = new DateTime(Int32.Parse(pubDateRegex.Groups["year"].Value),
                                                        Int32.Parse(pubDateRegex.Groups["month"].Value),
                                                        day);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.WarnFormat("Unable to parse date from the following url: {0}", fileName);
                        }

                        logger.InfoFormat("Adding '{0}' ({1} bytes)...", title, fileSize);
                        list.Add(new
                        {
                            TorrentID = exists != null ? exists.TorrentID : 0,
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
#if DEBUG
                if (list.Count != 0)
                    break;
#endif
            }

            return list;
        }
    }
}