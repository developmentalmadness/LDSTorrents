using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.Models
{
    public class Feed
    {
        public Int32 Id { get; set; }
        public string Host { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Url { get; set; }
        public IEnumerable<Url> Items { get; set; }
    }
}