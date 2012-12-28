using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Massive;
using Web.Models;

namespace Web.Controllers
{
    public class RssController : ApiController
    {
        public static dynamic Feeds = new DynamicModel("LDSTorrents", tableName: "Channels", primaryKeyField: "ChannelID");
        public static dynamic Items = new DynamicModel("LDSTorrents", tableName: "Torrents", primaryKeyField: "TorrentID");

        // GET api/rss
        public Feed Get()
        {
            var feed = new Feed
            {
                Id = 0,
                Title = "LDS Torrents",
                Url = "http://lds.org",
                Category = "All feeds",
                Host = "lds.org"
            };
            
            var items = new List<Url>();
            var channels = Feeds.All();

            foreach (var item in channels)
            {
                items.Add(new Url
                {
                    Title = item.Title,
                    Address = new Uri(Request.RequestUri, String.Format("/api/rss/{0}", item.ChannelID)).AbsoluteUri,
                    CreatedBy = item.Host,
                    CreatedAt = item.DateCreated,
                    UpdatedAt = item.LastUpdated,
                    Description = String.Format("Generated torrents of the '{0}' channel hosted by {1}", item.Title, item.Host),
                    
                });
            }

            feed.Items = items;
            return feed;
        }

        // GET api/rss/5
        public Feed Get(int id)
        {
            var feed = Feeds.First(ChannelID: id);

            var result = new Feed
            {
                Id = feed.ChannelID,
                Title = feed.Title,
                Url = feed.Url,
                Category = feed.Category,
                Host = feed.Host
            };

            var torrents = Items.Find(ChannelID: id, OrderBy: "DatePublished DESC");
            var items = new List<Url>();
            foreach (var item in torrents)
            {
                items.Add(new Url
                {
                    UrlId = item.TorrentID,
                    Title = item.Title,
                    Address = item.TorrentUri,
                    CreatedBy = feed.Host,
                    CreatedAt = item.DatePublished,
                    UpdatedAt = item.DatePublished,
                    FileSize = item.FileByteLength
                });
            }

            result.Items = items;

            return result;
        }

        // POST api/rss
        public void Post([FromBody]string value)
        {
        }

        // PUT api/rss/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/rss/5
        public void Delete(int id)
        {
        }
    }
}
