using System;
using System.Collections.Generic;
using System.Linq;

namespace GoTruckYourself.Server.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
        {
            return list.OrderBy(l => Guid.NewGuid());
        }
    }
}
