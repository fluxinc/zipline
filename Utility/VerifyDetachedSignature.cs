using log4net;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.IO;
using System.Text;

namespace DICOMCapacitorWarden.Utility
{
  internal static class VerifyDetachedSignature
  {
    private static readonly ILog Logger = LogManager.GetLogger("WardenLog");

    // Replace this with your public key
    private static readonly string WardenPublicKey =
        @"-----BEGIN PGP PUBLIC KEY BLOCK-----

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
Qvg8Zi0flChC6hlyLg==
=KFaR
-----END PGP PUBLIC KEY BLOCK-----";

    // If we want to use detached signatures. Modified for our use.
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
      using var keyIn = new MemoryStream(Encoding.UTF8.GetBytes(WardenPublicKey));

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