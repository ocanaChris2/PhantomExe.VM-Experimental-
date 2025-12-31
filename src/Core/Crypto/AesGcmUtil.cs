using System;
using System.Security.Cryptography;

#if NET6_0_OR_GREATER
using System.Buffers.Binary;
#else
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace PhantomExe.Core.Crypto
{
    public static class AesGcmUtil
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;

        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(key));

            var nonce = new byte[NonceSize];
#if NET6_0_OR_GREATER
            RandomNumberGenerator.Fill(nonce);
#else
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(nonce);
#endif

#if NET6_0_OR_GREATER
            var tag = new byte[TagSize];
            var ciphertext = new byte[plaintext.Length];
            using (var aes = new AesGcm(key))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }
#else
            var engine = new AesEngine();
            var cipher = new GcmBlockCipher(engine);
            var parameters = new AeadParameters(
                new KeyParameter(key), TagSize * 8, nonce, null);
            cipher.Init(true, parameters);

            var output = new byte[cipher.GetOutputSize(plaintext.Length)];
            var len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
            len += cipher.DoFinal(output, len);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];
            Array.Copy(output, 0, ciphertext, 0, ciphertext.Length);
            Array.Copy(output, ciphertext.Length, tag, 0, TagSize);
#endif

            var result = new byte[NonceSize + TagSize + plaintext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);
            return result;
        }

        public static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            if (encrypted.Length < NonceSize + TagSize)
                throw new ArgumentException("Invalid encrypted data.");

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ctLen = encrypted.Length - NonceSize - TagSize;
            var ciphertext = new byte[ctLen];

            Buffer.BlockCopy(encrypted, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encrypted, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(encrypted, NonceSize + TagSize, ciphertext, 0, ctLen);

#if NET6_0_OR_GREATER
            var plaintext = new byte[ctLen];
            using (var aes = new AesGcm(key))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            return plaintext;
#else
            var engine = new AesEngine();
            var cipher = new GcmBlockCipher(engine);
            var parameters = new AeadParameters(
                new KeyParameter(key), TagSize * 8, nonce, null);
            cipher.Init(false, parameters);

            var input = new byte[ciphertext.Length + TagSize];
            Buffer.BlockCopy(ciphertext, 0, input, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, input, ciphertext.Length, TagSize);

            var output = new byte[cipher.GetOutputSize(input.Length)];
            var len = cipher.ProcessBytes(input, 0, input.Length, output, 0);
            len += cipher.DoFinal(output, len);

            var plaintext = new byte[len];
            Buffer.BlockCopy(output, 0, plaintext, 0, len);
            return plaintext;
#endif
        }
    }
}