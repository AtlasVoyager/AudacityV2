namespace AudacityV2
{
    public class ParsedEBookTemplate
    {
        public string Title { get; set; } = string.Empty;
        public Dictionary<EbookNavStuff, List<string>> snippets { get; set; } = new();
    }

    public class EbookNavStuff
    {
        public string chapterKey { get; set; } = string.Empty;
        public string chapterTitle { get; set; } = string.Empty;
    }
}

