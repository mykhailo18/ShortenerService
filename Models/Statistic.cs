using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShortnerService.Models
{
    public class Statistic
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string IP { get; set; }
        public string OS { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Browser { get; set; }

        public int LinkId { get; set; }
        public Link Link { get; set; }
    }
}
