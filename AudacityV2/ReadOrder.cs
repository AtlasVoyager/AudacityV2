using AudacityV2.comms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudacityV2
{
    public class ReadOrder
    {
        public ReadOrder() { }

        /// <summary>
        /// current stage of the read order
        /// </summary>
        public enum currentStage
        {
            unparsed,
            parsed,
            beingRead,
            paused,
            completed
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public ulong UserId { get; set; }
        public BookMenuItem? SelectedBook { get; set; }
        public TimeSpan Interval { get; set; }
        public int CurrentChapter { get; set; } = 0;
        public int ChapterCount { get; set; }
        public string lastWord { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public DateTime NextReadTime { get; internal set; }
        public currentStage Stage { get; set; } = currentStage.unparsed;

        public override bool Equals(object? obj)
        {
            if (obj is ReadOrder that)
            {
                return Id.Equals(that.Id);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
