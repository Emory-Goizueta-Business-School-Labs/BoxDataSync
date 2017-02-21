/* 
 * Author: Jamie Anne Harrell (jamie.harrell@emory.edu)
 * Date: 2/17/2017
 * Mostly FROM MSDN https://msdn.microsoft.com/en-us/library/2fh8203k(v=vs.110).aspx
 * 
 * License: MIT / Open Source
 * 
 * Copyright (c) 2017 Emory Goizueta Business School
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. 
 * 
 */
using System;
using System.Text;
using System.Security.Cryptography;

// 

namespace BoxDataSync
{
    class Encryption
    {

        static byte[] s_aditionalEntropy = { 2, 8, 4, 7, 2 };


        public static String Protect64(String input, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            try
            {
                byte[] indata = Encoding.UTF8.GetBytes(input.ToCharArray());
                byte[] outdata = Protect(indata, scope);
                return System.Convert.ToBase64String(outdata);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public static String Unprotect64(String input, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            try
            {
                byte[] indata = System.Convert.FromBase64String(input);
                byte[] outdata = Unprotect(indata, scope);
                return Encoding.UTF8.GetString(outdata);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public static byte[] Protect(byte[] data, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            try
            {
                return ProtectedData.Protect(data, s_aditionalEntropy, scope);
            }
            catch (CryptographicException e)
            {
                //Console.WriteLine("Data was not encrypted. An error occurred.");
                //Console.WriteLine(e.ToString());
                return null;
            }
        }

        public static byte[] Unprotect(byte[] data, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser.
                return ProtectedData.Unprotect(data, s_aditionalEntropy, scope);
            }
            catch (CryptographicException e)
            {
                //Console.WriteLine("Data was not decrypted. An error occurred.");
                //Console.WriteLine(e.ToString());
                return null;
            }
        }
    }
}
