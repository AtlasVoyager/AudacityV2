using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudacityV2
{
    sealed class ReadManager
    {
        public readonly ConcurrentDictionary<ulong, ReadOrder> readOrders = new(); //our processing queue
        public readonly ConcurrentDictionary<ulong, List<BookMenuItem>> activeMenus = new(); //temp queue for active menus

        private ReadManager() { }

        public static ReadManager Instance { get; } = new ReadManager();

        // ---- Menu Handling ----
        public void SaveMenu(ulong userId, List<string> resultHashes)
        {
            var menu = resultHashes.Select((hash, i) => new BookMenuItem
            {
                Index = i,
                Hash = hash
            }).ToList();

            activeMenus[userId] = menu; // overwrites if user searches again

        }

        public bool SelectBook(ulong userId, int chosenIndex)
        {
            if (activeMenus.TryRemove(userId, out var menu))
            {
                var chosen = menu.FirstOrDefault(x => x.Index == chosenIndex-1);
                if (chosen == null) return false;

                var order = new ReadOrder
                {
                    UserId = userId,
                    SelectedBook = chosen,
                    IsActive = true,
                    NextReadTime = DateTime.UtcNow
                };

                readOrders[userId] = order;
                return true;
            }

            return false; // no menu found
            
        }

        // ---- Reading Control ----
        public bool TryGetOrder(ulong userId, out ReadOrder order)
            => readOrders.TryGetValue(userId, out order!);

        public void StopReading(ulong userId)
        {
            if (readOrders.TryGetValue(userId, out var order))
            {
                order.IsActive = false;
            }
        }

        public void ClearUser(ulong userId)
        {
            activeMenus.TryRemove(userId, out _);
            readOrders.TryRemove(userId, out _);
        }

        public IEnumerable<ReadOrder> GetActiveOrders()
            => readOrders.Values.Where(o => o.IsActive);


    }
}
