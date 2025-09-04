using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudacityV2
{

    public class parsedBook
    {
        public string Title { get; set; } = string.Empty;
        public List<TocNode> TableOfContents { get; set; } = new();
        public Dictionary<int, string> Snippets { get; set; } = new();
    }

    public class TocNode
    {
        public string Title { get; set; } = string.Empty;

        // Section indices from ReadingOrder (could be one or multiple)
        public List<int> SectionIndices { get; set; } = new();

        // Nested children (subsections, parts, etc.)
        public List<TocNode> Children { get; set; } = new();
    }
}
