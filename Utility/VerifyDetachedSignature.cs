using log4net;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.IO;
using System.Text;
using Zipline.Properties;

namespace Zipline.Utility
{
  internal static class VerifyDetachedSignature
  {
    private static readonly ILog Logger = LogManager.GetLogger("ZiplineLog");

    private static readonly string ZiplinePublicKey = Settings.Default.publicKey;

    // Modified for our use.
    // https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/openpgp/examples/DetachedSignatureProcessor.cs

    public static bool VerifyFile(FileInfo file)
    {
      if (!File.Exists($"{file.FullName}.sig")) return false;

      FileInfo fileInfo = new FileInfo(file.FullName);
      FileInfo signature = new FileInfo($"{file.FullName}.sig");

      // In this method we can just sign .zips using pgp.
      // If the .zip is modified the signature should fail
      // giving us checksums for free too. 

      Logger.Info($"Verifying {file.FullName} against signature {signature.FullName}");

      if (!VerifySignature(fileInfo.FullName, signature.FullName)) return false;

      return true;
    }

    public static bool VerifySignature(
        string fileName,
        string inputFileName)
    {
      using var input = File.OpenRead(inputFileName);
      using var keyIn = File.OpenRead(ZiplinePublicKey);

      return VerifySignature(fileName, input, keyIn);
    }

    public static bool VerifySignature(
        string fileName,
        Stream inputStream,
        Stream keyIn)
    {
      inputStream = PgpUtilities.GetDecoderStream(inputStream);

      var pgpFact = new PgpObjectFactory(inputStream);
      PgpSignatureList p3 = null;
      var o = pgpFact.NextPgpObject();
      if (o is PgpCompressedData)
      {
        var c1 = (PgpCompressedData)o;
        pgpFact = new PgpObjectFactory(c1.GetDataStream());

        p3 = (PgpSignatureList)pgpFact.NextPgpObject();
      }
      else
      {
        p3 = (PgpSignatureList)o;
      }

      var pgpPubRingCollection = new PgpPublicKeyRingBundle(
          PgpUtilities.GetDecoderStream(keyIn));
      Stream dIn = File.OpenRead(fileName);
      var sig = p3[0];
      var key = pgpPubRingCollection.GetPublicKey(sig.KeyId);
      sig.InitVerify(key);

      int ch;
      while ((ch = dIn.ReadByte()) >= 0) sig.Update((byte)ch);

      dIn.Close();

      return sig.Verify();
    }
  }
}