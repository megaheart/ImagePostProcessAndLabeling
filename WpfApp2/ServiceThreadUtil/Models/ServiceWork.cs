using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.ServiceThreadUtil.Models
{
    public class ServiceWork
    {
        public ServiceWorkType Type { get; set; }
        public object Data { get; set; }
        public Action Callback { get; set; }
        public Action<int> ProgressCallback { get; set; }
    }
}
