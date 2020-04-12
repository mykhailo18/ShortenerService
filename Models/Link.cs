using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShortnerService.Models
{
    public class Link
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Token { get; set; }
        public bool Notification { get; set; }
        public List<Statistic> Statistics { get; set; }
        public Link()
        {
            Statistics = new List<Statistic>();
        }
    }
}
