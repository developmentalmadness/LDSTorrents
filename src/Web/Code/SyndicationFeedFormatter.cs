using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Web.Models;

namespace Web.Code
{
    public class SyndicationFeedFormatter : MediaTypeFormatter
    {
        private readonly string atom = "application/atom+xml";
        private readonly string rss = "application/rss+xml";

        public SyndicationFeedFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(atom));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(rss));
        }

        Func<Type, bool> SupportedType = (type) =>
        {
            if (type == typeof(Feed))
                return true;
            else
                return false;
        };

        public override bool CanReadType(Type type)
        {
            return SupportedType(type);
        }

        public override bool CanWriteType(Type type)
        {
            return SupportedType(type);
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            return Task.Factory.StartNew(() =>
            {
                if (type == typeof(Feed))
                    BuildSyndicationFeed((Feed)value, writeStream, content.Headers.ContentType.MediaType);
            });
        }

        private void BuildSyndicationFeed(Feed model, Stream stream, string contenttype)
        {
            List<SyndicationItem> items = new List<SyndicationItem>();
            var feed = new SyndicationFeed()
            {
                Title = new TextSyndicationContent(model.Title),
            };
            feed.Links.Add(new SyndicationLink(new Uri(model.Url)));
            feed.Authors.Add(new SyndicationPerson { Name = model.Host, Uri = String.Format("http://{0}", model.Host) });
            feed.Categories.Add(new SyndicationCategory { Name = model.Category });

            var enumerator = model.Items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                items.Add(BuildSyndicationItem(enumerator.Current));
            }

            feed.Items = items;

            using (XmlWriter writer = XmlWriter.Create(stream))
            {
                if (string.Equals(contenttype, atom))
                {
                    Atom10FeedFormatter atomformatter = new Atom10FeedFormatter(feed);
                    atomformatter.WriteTo(writer);
                }
                else
                {
                    Rss20FeedFormatter rssformatter = new Rss20FeedFormatter(feed);
                    rssformatter.WriteTo(writer);
                }
            }
        }

        private SyndicationItem BuildSyndicationItem(Url u)
        {
            var item = new SyndicationItem()
            {
                Title = new TextSyndicationContent(u.Title),
                BaseUri = new Uri(u.Address),
                Id = u.Address,
                LastUpdatedTime = u.CreatedAt,
                Content = new TextSyndicationContent(u.Description)
            };
            item.Authors.Add(new SyndicationPerson() { Name = u.CreatedBy });
            item.Links.Add(SyndicationLink.CreateMediaEnclosureLink(item.BaseUri, "application/x-bittorrent", u.FileSize));
            return item;
        }
    }
}