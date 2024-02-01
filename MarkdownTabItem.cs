using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownReader
{
    public class MarkdownTabItem : TabItemBase
    {
        public override string Title => "MarkdownTab";
        public override View Content { get; } // Implement specific content for Markdown

        // Specific Markdown methods
    }
}
