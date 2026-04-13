
using System;
using System.IO;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Utilities.IO;

namespace TWGARProcessorService;

public static class FileDecryptor
{
    /// <summary>
    /// Decrypt an encrypted file to an output file using the supplied secret key (private key) and passphrase.
    /// </summary>
    /// <param name="inputEncryptedPath">Path to the .gpg/.pgp/.asc encrypted file</param>
    /// <param name="outputPlainPath">Path to write the decrypted payload (e.g., .pdf, .txt, etc.)</param>
    /// <param name="secretKeyPath">Path to your secret key (private key) file exported from GnuPG/Kleopatra (ASCII-armored .asc or binary .gpg)</param>
    /// <param name="passphrase">Passphrase protecting the private key (empty string if key is not protected)</param>
    public static void DecryptFile(
        string inputEncryptedPath,
        string outputPlainPath,
        string secretKeyPath,
        string passphrase)
    {
        using var encryptedIn = File.OpenRead(inputEncryptedPath);
        using var secretKeyIn = File.OpenRead(secretKeyPath);
        using var outStream = File.Create(outputPlainPath);

        DecryptStream(encryptedIn, secretKeyIn, passphrase, outStream);
    }

    /// <summary>
    /// Stream-based decryption (useful for Docker, cloud functions, or when keys are in memory).
    /// </summary>
    /// <param name="encryptedInput">Encrypted data stream (.gpg/.asc)</param>
    /// <param name="secretKeyInput">Secret key (private key) stream</param>
    /// <param name="passphrase">Key passphrase</param>
    /// <param name="plainOutput">Destination stream for the decrypted payload</param>
    public static void DecryptStream(
        Stream encryptedInput,
        Stream secretKeyInput,
        string passphrase,
        Stream plainOutput)
    {
        if (encryptedInput == null) throw new ArgumentNullException(nameof(encryptedInput));
        if (secretKeyInput == null) throw new ArgumentNullException(nameof(secretKeyInput));
        if (plainOutput == null) throw new ArgumentNullException(nameof(plainOutput));

        // 1) Decode armored or binary input safely
        using var decoderStream = PgpUtilities.GetDecoderStream(encryptedInput);
        var pgpObjFactory = new PgpObjectFactory(decoderStream);

        // The first object can be a marker packet; get the actual encrypted data list
        object firstObj = pgpObjFactory.NextPgpObject();
        PgpEncryptedDataList encList;

        if (firstObj is PgpEncryptedDataList list)
        {
            encList = list;
        }
        else
        {
            // Sometimes a marker packet is present; the next object should be the encrypted data list
            encList = pgpObjFactory.NextPgpObject() as PgpEncryptedDataList
                        ?? throw new PgpException("Encrypted data list not found in PGP stream.");
        }

        // 2) Load secret keys
        using var secretKeyDecoder = PgpUtilities.GetDecoderStream(secretKeyInput);
        var secretKeyBundle = new PgpSecretKeyRingBundle(secretKeyDecoder);

        // 3) Find the correct encrypted data object we can decrypt with our secret key
        PgpPrivateKey privateKey = null;
        PgpPublicKeyEncryptedData pbe = null;

        foreach (PgpPublicKeyEncryptedData pked in encList.GetEncryptedDataObjects())
        {
            var secretKey = secretKeyBundle.GetSecretKey(pked.KeyId);
            if (secretKey != null)
            {
                // Extract private key with passphrase
                privateKey = secretKey.ExtractPrivateKey((passphrase ?? string.Empty).ToCharArray());
                if (privateKey == null)
                    throw new PgpException("Failed to extract private key. Check passphrase or key format.");
                pbe = pked;
                break;
            }
        }

        if (privateKey == null || pbe == null)
            throw new PgpException("No matching secret key found for any recipient KeyId in the message.");

        // 4) Get the clear data stream using our private key
        using var clearData = pbe.GetDataStream(privateKey);

        // 5) Unwrap inner objects (may be compressed, literal, or signed)
        var plainFactory = new PgpObjectFactory(clearData);
        object message = plainFactory.NextPgpObject();

       
        //  Skip any number of One-Pass Signature packets
        while (message is PgpOnePassSignatureList)
        {
            // If verifying, init each OPS against its signer pubkey here
            message = plainFactory.NextPgpObject();
        }

        // If compressed, unwrap until we hit the literal payload
        while (message is PgpCompressedData compressed)
        {
            using var compData = compressed.GetDataStream();
            plainFactory = new PgpObjectFactory(compData);
            message = plainFactory.NextPgpObject();
            // After unwrap, message might again be OPS (rare) or literal, or another compressed layer
        }

        // Expect the actual payload
        if (message is not PgpLiteralData literal)
        {
            throw new PgpException($"Expected PgpLiteralData, found: {message?.GetType().FullName ?? "null"}");
        }

        // Stream out the payload; if verifying, update the OPS digest here
        using var literalIn = literal.GetInputStream();
        Streams.PipeAll(literalIn, plainOutput);


        // 6) Verify integrity (MDC) if present
        if (pbe.IsIntegrityProtected())
        {
            if (!pbe.Verify())
                throw new PgpException("PGP message failed integrity check (MDC verification failed).");
        }
    }
}
