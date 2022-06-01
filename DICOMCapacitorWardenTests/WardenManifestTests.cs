using DICOMCapacitorWarden;

namespace DICOMCapacitorWardenTests
{
  public class WardenManifestTests
  {
    private string examples = "../../../../examples/";

    [Fact]
    public void FailsOnMissingManifest()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}WARDEN-e1.zip"));
      Assert.False(warden.PrepareUpdate());
    }

    [Fact]
    public void FailsWithInvalidSignature()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}WARDEN-e0.zip"));
      Assert.True(warden.PrepareUpdate());
      Assert.False(warden.ProcessUpdate());
    }

    [Fact]
    public void RunsBasicManifest()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}WARDEN-0x5.zip"));
      warden.IgnoreHashLog = true;
      Assert.True(warden.PrepareUpdate());
      Assert.True(warden.ProcessUpdate());
    }

    [Fact]
    public void ContinuesOnErrorIgnore()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}WARDEN-0xDEADBEEF.zip"));
      warden.IgnoreHashLog = true;
      Assert.True(warden.PrepareUpdate());
      Assert.True(warden.ProcessUpdate());
      Assert.True(warden.UpdateErrors == 1);
    }
  }
}