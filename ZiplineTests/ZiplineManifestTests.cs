using Zipline;

namespace ZiplineTests
{
  public class ZiplineManifestTests
  {
    private string examples = "../../../../examples/zipline-";

    [Fact]
    public void FailsOnMissingManifest()
    {
      var zipline = new ZiplinePackage(new FileInfo($"{examples}e1.zip"));
      Assert.True(zipline.PrepareUpdate());
      Assert.False(zipline.ProcessUpdate());
    }

    [Fact]
    public void FailsWithInvalidSignature()
    {
      var zipline = new ZiplinePackage(new FileInfo($"{examples}e0.zip"));
      Assert.True(zipline.PrepareUpdate());
      Assert.False(zipline.ProcessUpdate());
    }

    [Fact]
    public void RunsBasicManifest()
    {
      var zipline = new ZiplinePackage(new FileInfo($"{examples}0x5.zip"));
      zipline.IgnoreHashLog = true;
      Assert.True(zipline.PrepareUpdate());
      Assert.True(zipline.ProcessUpdate());
    }

    [Fact]
    public void ContinuesOnErrorIgnore()
    {
      var zipline = new ZiplinePackage(new FileInfo($"{examples}0xdeadbeef.zip"));
      zipline.IgnoreHashLog = true;
      Assert.True(zipline.PrepareUpdate());
      Assert.True(zipline.ProcessUpdate());
      Assert.True(zipline.UpdateErrors == 1);
    }
  }
}