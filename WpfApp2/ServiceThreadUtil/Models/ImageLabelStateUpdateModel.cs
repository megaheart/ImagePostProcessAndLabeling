using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp2.Models;

namespace WpfApp2.ServiceThreadUtil.Models
{
    public class ImageLabelStateUpdateModel
    {
        public string WorkspacePath { get; set; }
        public string WsFolder { get; set; }
        public ImageLabelState State { get; set; }
    }
}
