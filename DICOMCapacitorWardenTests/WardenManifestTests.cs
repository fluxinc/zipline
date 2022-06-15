using DICOMCapacitorWarden;

namespace DICOMCapacitorWardenTests
{
  public class WardenManifestTests
  {
    private string examples = "../../../../examples/warden-";

    [Fact]
    public void FailsOnMissingManifest()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}e1.zip"));
      Assert.True(warden.PrepareUpdate());
      Assert.False(warden.ProcessUpdate());
    }

    [Fact]
    public void FailsWithInvalidSignature()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}e0.zip"));
      Assert.True(warden.PrepareUpdate());
      Assert.False(warden.ProcessUpdate());
    }

    [Fact]
    public void RunsBasicManifest()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}0x5.zip"));
      warden.IgnoreHashLog = true;
      Assert.True(warden.PrepareUpdate());
      Assert.True(warden.ProcessUpdate());
    }

    [Fact]
    public void ContinuesOnErrorIgnore()
    {
      var warden = new WardenPackage(new FileInfo($"{examples}0xdeadbeef.zip"));
      warden.IgnoreHashLog = true;
      Assert.True(warden.PrepareUpdate());
      Assert.True(warden.ProcessUpdate());
      Assert.True(warden.UpdateErrors == 1);
    }
  }
}