using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownReader
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TabItemBase> TabItems { get; set; }

        // Other properties and commands for MainPage
    }
}
