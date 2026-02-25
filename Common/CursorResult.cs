using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CursorResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int? NextCursor { get; set; }
        public bool HasMore { get; set; }
    }
}
