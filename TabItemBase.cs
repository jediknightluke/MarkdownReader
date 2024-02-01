using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownReader
{
    public abstract class TabItemBase
    {
        public abstract string Title { get; }
        public abstract View Content { get; }
    }
}
