using System;
using System.Collections.Generic;

namespace AudacityV2
{
    public class parsedEBookTemplate
    {
        public string Title { get; set; } = string.Empty;
        public Dictionary<ebookNavStuff, List<string>> snippets { get; set; } = new();
    }

    public class ebookNavStuff
    {
        public string chapterKey { get; set; } = string.Empty;
        public string chapterTitle { get; set; } = string.Empty;
    }
}

