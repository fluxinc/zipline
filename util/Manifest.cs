using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DICOMCapacitorWarden.util
{
  class Manifest
  {
    public string Operation { get; set; }
    public string Type { get; set; }
    public string Args { get; set; }
    public string Services { get; set; }
    public string Executable { get; set; }
    public string ReturnFile { get; set; }
  }
}
