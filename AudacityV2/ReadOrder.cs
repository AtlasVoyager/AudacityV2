using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudacityV2
{
    internal class ReadOrder
    {
        public ulong UserId { get; set; }
        public BookMenuItem? SelectedBook { get; set; }
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);
        public int CurrentPage { get; set; } = 0;
        public int PageCount { get; set; }
        public string lastWord { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
    }
}
