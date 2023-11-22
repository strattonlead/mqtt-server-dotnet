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
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"No file at path: {path}. Please provide a certificate.");
            }
            return new X509Certificate2(File.ReadAllBytes(path));
        }
    }
}
