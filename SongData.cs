using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksm_download
{
    /// <summary>
    /// 和listview绑定的类
    /// </summary>
    public class song
    {
        public Guid id { get; set; }
        public string downState { get; set; }
        public string name { get; set; }
        public string[] levels { get; set; }
        public string artist { get; set; }
        public string charter { get; set; }
        public song(string _id, string _name, string[] _lv, string _art, string _charter)
        {
            id = new Guid(_id);
            name = _name;
            artist = _art;
            levels = _lv;
            charter = _charter;
        }
    }

    public class SongData : INotifyPropertyChanged
    {
        List<song> _songslist = new List<song>();
        public List<song> songslist
        {
            get
            {
                return _songslist;
            }
            set
            {
                OnPropertyChanged("已下载");
                _songslist = songslist;
            }

        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
