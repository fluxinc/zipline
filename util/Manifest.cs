using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DICOMCapacitorWarden.util
{
  class Manifest
  {
    public string Type { get; set; }
    public string Command { get; set; }
    public string WorkingPath { get; set; }
    public string Arguments { get; set; }
    public string OnError { get; set; } = "abort";
  }
}
