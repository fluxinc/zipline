using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warden.util
{
  class VerifyDetachedSignature
  {

    private static readonly string FluxPublicKey = "FluxPublicKey.asc";

    // If we want to use detached signatures. Modified for our use.
    // https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/openpgp/examples/DetachedSignatureProcessor.cs
    
    public static bool VerifySignature(
        string fileName,
        string inputFileName)
    {
      using (Stream input = File.OpenRead(inputFileName),
          keyIn = File.OpenRead(FluxPublicKey))
      {
        return VerifySignature(fileName, input, keyIn);
      }
    }

    public static bool VerifySignature(
        string fileName,
        Stream inputStream,
        Stream keyIn)
    {
      inputStream = PgpUtilities.GetDecoderStream(inputStream);

      PgpObjectFactory pgpFact = new PgpObjectFactory(inputStream);
      PgpSignatureList p3 = null;
      PgpObject o = pgpFact.NextPgpObject();
      if (o is PgpCompressedData)
      {
        PgpCompressedData c1 = (PgpCompressedData)o;
        pgpFact = new PgpObjectFactory(c1.GetDataStream());

        p3 = (PgpSignatureList)pgpFact.NextPgpObject();
      }
      else
      {
        p3 = (PgpSignatureList)o;
      }

      PgpPublicKeyRingBundle pgpPubRingCollection = new PgpPublicKeyRingBundle(
          PgpUtilities.GetDecoderStream(keyIn));
      Stream dIn = File.OpenRead(fileName);
      PgpSignature sig = p3[0];
      PgpPublicKey key = pgpPubRingCollection.GetPublicKey(sig.KeyId);
      sig.InitVerify(key);

      int ch;
      while ((ch = dIn.ReadByte()) >= 0)
      {
        sig.Update((byte)ch);
      }

      dIn.Close();

      return sig.Verify();

    }
  }
}
