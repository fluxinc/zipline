using Org.BouncyCastle.Bcpg.OpenPgp;
using System.IO;
using System.Text;

namespace DICOMCapacitorWarden.util
{
  internal static class VerifyDetachedSignature
  {
    private static readonly string FluxPublicKey =
        @"-----BEGIN PGP PUBLIC KEY BLOCK-----

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
ZQ==
=Z3/r
-----END PGP PUBLIC KEY BLOCK-----";

    // If we want to use detached signatures. Modified for our use.
    // https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/openpgp/examples/DetachedSignatureProcessor.cs

    public static bool VerifySignature(
        string fileName,
        string inputFileName)
    {
      using var input = File.OpenRead(inputFileName);
      using var keyIn = new MemoryStream(Encoding.UTF8.GetBytes(FluxPublicKey));

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