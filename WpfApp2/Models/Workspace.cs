using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Models
{
    public class Workspace
    {
        public DateTime LastVisitedTime { get; set; }
        public string Path { get; set; }
        public string WsFolder { get => System.IO.Path.Combine(Path, ".workspace"); }
        public string OutputFolder { get => System.IO.Path.Combine(Path, ".workspace", "out"); }
    }
}
