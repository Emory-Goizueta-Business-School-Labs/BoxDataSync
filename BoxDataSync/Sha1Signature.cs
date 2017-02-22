/* 
 * Author: Jamie Anne Harrell (jamie.harrell@emory.edu)
 * Date: 2/17/2017
 * Credits: http://stackoverflow.com/questions/1993903/how-do-i-do-a-sha1-file-checksum-in-c
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
using System.IO;

namespace BoxDataSync
{

    /// <summary>
    /// Simple static method access to get an SHA1 file signature. Borrowed from mgbowen http://stackoverflow.com/questions/1993903/how-do-i-do-a-sha1-file-checksum-in-c
    /// </summary>
    public static class Sha1Signature
    {

        /// <summary>
        /// Returns the human readable SHA1 hash / checksum of the file. 
        /// </summary>
        /// <param name="path">Path to the file to checksum. eg, c:\path\to\a\file.txt</param>
        /// <returns>The SHA1 signature / checksum of the file.</returns>
        public static String getFileSignature(String path)
        {
            if (!File.Exists(path)) { return ""; }
            using (FileStream fs = new FileStream(path, FileMode.Open))
                using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }
                    return formatted.ToString().ToLower();
                }
            }
        }
    }
}
