using MQTTnet.Certificates;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace MQTTServer.Services
{
    public class CertificateProvider : ICertificateProvider
    {
        public X509Certificate2 GetCertificate()
        {
            var path = Environment.GetEnvironmentVariable("X509_CERTIFICATE_PATH");
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"No file at path: {path}. Please provide a certificate.");
                }
                return new X509Certificate2(File.ReadAllBytes(path));
            }

            var certPemPath = Environment.GetEnvironmentVariable("CERT_PEM_PATH");
            if (string.IsNullOrWhiteSpace(certPemPath))
            {
                throw new FileNotFoundException($"No file at path: {certPemPath}. Please provide a cert.pem.");
            }

            var keyPemPath = Environment.GetEnvironmentVariable("KEY_PEM_PATH");
            if (string.IsNullOrWhiteSpace(keyPemPath))
            {
                throw new FileNotFoundException($"No file at path: {keyPemPath}. Please provide a key.pem.");
            }

            var certPem = File.ReadAllText(certPemPath);
            var keyPem = File.ReadAllText(keyPemPath);

            return X509Certificate2.CreateFromPem(
                certPem,
                keyPem
            );
        }
    }
}
