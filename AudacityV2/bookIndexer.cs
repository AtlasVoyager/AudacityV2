using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudacityV2
{
    public class Metadata
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileName { get; set; }
        public int PageCount { get; set; }
        //future update. Going to use AI to get the tags for the books
        //public List<string> Tags { get; set; }

    }

}
