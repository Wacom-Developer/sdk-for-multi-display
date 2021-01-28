using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Wacom.Kiosk.IntegratorUI
{
    public static class HashingUtility
    {
        public static string GetHash(byte[] data)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(data);

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string GetHash(string data)
        {
            return GetHash(Encoding.UTF8.GetBytes(data));
        }
    }
}
