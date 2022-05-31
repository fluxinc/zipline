namespace DICOMCapacitorWarden.util
{
  internal class Manifest
  {
    public string Type { get; set; }
    public string Command { get; set; }
    public string WorkingPath { get; set; }
    public string Arguments { get; set; }
    public string OnError { get; set; } = "abort";
  }
}
