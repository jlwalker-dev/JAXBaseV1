/************************************************************************************
 * Basic file handling for JAX
 * 
 * Written by Jon Walker
 * 
 * Routines
 * ----------------------------------------
 * CheckPath(string path)
 * CopyFile(string source, string destination)
 * DecryptFile(string source)
 * DeleteFile(string source, string destination)
 * EncryptFile(string source)
 * GetDirectory(string targetDirectory, out string[] fileEntries)
 * GetFileInfo(string FileName)
 * GetFiles(string targetDirectory,out string[] fileEntries)
 * GetFiles(string targetDirectory,string slSearch, out string[] fileEntries)
 * MakeDir(string dirName)
 * MakeJSONHumanReadable(string json, out string jsonout)
 * MoveFile(string source, string destination)
 * ProcessDirectory(string targetDirectory, out string[] fileEntries)
 * RemoveDir(string dirName)
 * WriteJAXXML(string fullfilename, DataTable dt)
 * 
 * Error Codes
 *   202 Invalid Path
 *   333 Not Supported
 *   400 Cryptography Exception
 *   401 IO Exception
 *   999 Unspecified exception
 *  1220 Invalid argument
 *  1705 Unauthorized access
 *  1963 Directory or file not found
 *  
 ************************************************************************************/
using System.Data;
using System.Security.Cryptography;
using System.Text;

//#pragma warning disable IDE0063 // Simplification warning
namespace JAXBase.Utilities
{
    public class FilerLib
    {
        private static int ErrNo = 0;
        private static string ErrMessage = string.Empty;
        private static string ErrProcedure = string.Empty;

        private static void SetError(int ErrNo, string ErrMsg) { throw new Exception(string.Format("{0}||{1}", ErrNo, ErrMsg)); }

        /*
         * TODO - Make directory
         */
        public static int MakeDir(string dirName)
        {
            return 0;
        }

        /*
         * Remove a directory
         */
        public static int RemoveDir(string dirName)
        {

            try { Directory.Delete(dirName); }
            catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (PathTooLongException ex) { SetError(202, ex.Message); }
            catch (IOException ex) { SetError(334, ex.Message); }
            catch (ArgumentException ex) { SetError(1220, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return 0;
        }

        /*
         * Get list of files from location
         */
        public static int GetFiles(string targetDirectory, out string[] fileEntries)
        {
            return GetFiles(targetDirectory, @"*.*", out fileEntries);
        }
        public static int GetFiles(string targetDirectory, string slSearch, out string[] fileEntries)
        {
            fileEntries = [];

            // Process the list of files found in the directory
            try { fileEntries = Directory.GetFiles(targetDirectory, slSearch); }
            catch (DirectoryNotFoundException ex) { fileEntries = []; SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { fileEntries = []; SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { fileEntries = []; SetError(333, ex.Message); }
            catch (ArgumentException ex) { fileEntries = []; SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { fileEntries = []; SetError(202, ex.Message); }
            catch (Exception ex) { fileEntries = []; SetError(9999, ex.Message); }

            if (fileEntries.Length == 0)
                fileEntries = [];

            return ErrNo;
        }

        /*
         * Return file list of directory, no walking
         */
        public static int GetDirectory(string targetDirectory, out string[] fileEntries)
        {
            fileEntries = Array.Empty<string>();

            try
            {
                string targetDir = JAXLib.JustPath(targetDirectory);
                string targetFile = JAXLib.JustFName(targetDirectory);
                targetFile = string.IsNullOrEmpty(targetFile) ? "*.*" : targetFile;

                // Process the list of files found in the directory.
                fileEntries = Directory.GetFiles(targetDir, targetFile, System.IO.SearchOption.TopDirectoryOnly);
            }
            catch (DirectoryNotFoundException ex) { fileEntries = []; SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { fileEntries = []; SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { fileEntries = []; SetError(333, ex.Message); }
            catch (ArgumentException ex) { fileEntries = []; SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { fileEntries = []; SetError(202, ex.Message); }
            catch (Exception ex) { fileEntries = []; SetError(9999, ex.Message); }

            return ErrNo;
        }


        /*
         * Tree walk location, return file list
         */
        public static int ProcessDirectory(string targetDirectory, out string[] fileEntries)
        {
            fileEntries = Array.Empty<string>();

            try
            {
                // Process the list of files found in the directory.
                fileEntries = Directory.GetFiles(targetDirectory);

                // Recurse into subdirectories of this directory.
                string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
                foreach (string subdirectory in subdirectoryEntries)
                {
                    if (ProcessDirectory(subdirectory, out string[] subEntries) > 0)
                        fileEntries = fileEntries.Concat(subEntries).ToArray();

                    // An error stops everything
                    if (ErrNo != 0) break;
                }
            }
            catch (DirectoryNotFoundException ex) { fileEntries = []; SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { fileEntries = []; SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { fileEntries = []; SetError(333, ex.Message); }
            catch (ArgumentException ex) { fileEntries = []; SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { fileEntries = []; SetError(202, ex.Message); }
            catch (Exception ex) { fileEntries = []; SetError(9999, ex.Message); }

            return ErrNo;
        }


        /*
         * Get File Information in array Filename, size, date, attributes
         */
        public static int GetFileInfo(string FileName, out string[] fileEntries)
        {
            fileEntries = new string[4];
            bool err = false;

            try
            {
                fileEntries[0] = JAXLib.JustFName(FileName);
                long length = new FileInfo(FileName).Length;

                fileEntries[1] = length.ToString();
                fileEntries[2] = TimeLib.LocaltoUCT(File.GetLastWriteTime(FileName), TimeZoneInfo.Local).ToString("yyyy-MM-ddTHH:mm:ss");

                // WAY MORE info than standard xBase
                FileAttributes att = File.GetAttributes(FileName);
                fileEntries[3] = (att & FileAttributes.Archive) == FileAttributes.Archive ? "A" : "";
                fileEntries[3] += (att & FileAttributes.Compressed) == FileAttributes.Compressed ? "C" : "";
                fileEntries[3] += (att & FileAttributes.Directory) == FileAttributes.Directory ? "D" : "";
                fileEntries[3] += (att & FileAttributes.Encrypted) == FileAttributes.Encrypted ? "E" : "";
                fileEntries[3] += (att & FileAttributes.Hidden) == FileAttributes.Hidden ? "H" : "";
                fileEntries[3] += (att & FileAttributes.Offline) == FileAttributes.Offline ? "O" : "";
                fileEntries[3] += (att & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ? "R" : "";
                fileEntries[3] += (att & FileAttributes.System) == FileAttributes.System ? "S" : "";
            }
            catch (UnauthorizedAccessException ex) { err = true; SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { err = true; SetError(333, ex.Message); }
            catch (ArgumentException ex) { err = true; SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { err = true; SetError(202, ex.Message); }
            catch (Exception ex) { err = true; SetError(9999, ex.Message); }

            // An error clears everything
            if (err)
            {
                fileEntries[0] = string.Empty;
                fileEntries[1] = string.Empty;
                fileEntries[2] = string.Empty;
                fileEntries[3] = string.Empty;
            }

            return ErrNo;
        }


        /*
         * Copy all matching files to location
         */
        public static int CopyFile(string source, string destination)
        {
            try
            {
                string sourceFolder = JAXLib.JustPath(source);
                string filematch = JAXLib.JustFName(source);
                string destinationFolder = JAXLib.JustPath(destination);
                var items = Directory.GetFiles(sourceFolder, filematch, System.IO.SearchOption.TopDirectoryOnly);
                foreach (String filePath in items)
                {
                    var newFile = Path.Combine(destinationFolder, Path.GetFileName(filePath));
                    File.Copy(filePath, newFile);
                }
            }
            catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (ArgumentException ex) { SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { SetError(202, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }


        /*
         * Move all matching files to location
         */
        public static int MoveFile(string source, string destination)
        {
            try
            {
                string sourceFolder = JAXLib.JustPath(source);
                string filematch = JAXLib.JustFName(source);
                string destinationFolder = JAXLib.JustPath(destination);

                var items = Directory.GetFiles(sourceFolder, filematch, System.IO.SearchOption.TopDirectoryOnly);
                foreach (String filePath in items)
                {
                    var newFile = Path.Combine(destinationFolder, Path.GetFileName(filePath));
                    File.Move(filePath, newFile);
                }
            }
            catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (ArgumentException ex) { SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { SetError(202, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }


        /*
         * Delete all matching files in location
         */
        public static int DeleteFile(string source)
        {
            string filematch = string.Empty;

            try
            {
                string sourceFolder = JAXLib.JustPath(source);
                filematch = JAXLib.JustFName(source);

                if (ErrNo == 0)
                {
                    var items = Directory.GetFiles(sourceFolder, filematch, System.IO.SearchOption.TopDirectoryOnly);
                    foreach (String filePath in items) { File.Delete(filePath); }
                }
            }
            catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (ArgumentException ex) { SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { SetError(202, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }


        /*
         * Encrypt a path\filename to a new path\filename
         * Parameters
         *   sourceFilename - unencrypted source file with full path
         *   destinationFilename - target file with full path for encrypted version
         *   password - should be 24 chars for 192 bits or 32 chars for 256 bits
         *   salt/iv - should be 16 chars
         *
         * Took code from multiple web sites to come up with a working version
         * that didn't use depricated encryption routines
         */
        public static int EncryptFile(string sourceFilename, string destinationFilename, string password, string salt)
        {

            try
            {
                byte[] key = Encoding.ASCII.GetBytes(password);
                byte[] iv = Encoding.ASCII.GetBytes((salt + "0123456789ABCDEF")[..16]);  // must be 16 characters

                using (var sourceStream = File.OpenRead(sourceFilename))
                using (var destinationStream = File.Create(destinationFilename))
                using (var provider = Aes.Create())
                using (var cryptoTransform = provider.CreateEncryptor(key, iv))
                using (var cryptoStream = new CryptoStream(destinationStream, cryptoTransform, CryptoStreamMode.Write))
                {
                    //destinationStream.Write(provider.IV, 0, provider.IV.Length);
                    sourceStream.CopyTo(cryptoStream);
                }
            }
            catch (CryptographicException e) { ErrNo = 400; ErrMessage = e.Message; }
            catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (ArgumentException ex) { SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { SetError(202, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }

        /****************************************************************************
         * Decrypt a path\filename to a new path\filename
         * Parameters
         *   sourceFilename - decrypted source file with full path
         *   destinationFilename - target file with full path for decrypted version
         *   password - should be 24 chars for 192 bits or 32 chars for 256 bits
         *   salt/iv - should be 16 chars
         *
         * Took code from multiple web sites to come up with a working version
         * that didn't use depricated encryption routines
         ****************************************************************************/
        public static int DecryptFile(string sourceFilename, string destinationFilename, string password, string salt)
        {

            try
            {
                byte[] key = Encoding.ASCII.GetBytes(password);
                byte[] iv = Encoding.ASCII.GetBytes((salt + "0123456789ABCDEF")[..16]);

                // Decrypt the source file and write it to the destination file.
                using (var sourceStream = File.OpenRead(sourceFilename))
                using (var destinationStream = File.Create(destinationFilename))
                using (var provider = Aes.Create())
                {
                    //var iv = new byte[provider.iv.Length];
                    //sourceStream.Read(iv, 0, iv.Length);
                    using (var cryptoTransform = provider.CreateDecryptor(key, iv))
                    using (var cryptoStream = new CryptoStream(sourceStream, cryptoTransform, CryptoStreamMode.Read))
                    {
                        cryptoStream.CopyTo(destinationStream);
                    }
                }
            }
            catch (CryptographicException e) { ErrNo = 400; ErrMessage = e.Message; }
            catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (ArgumentException ex) { SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { SetError(202, ex.Message); }
            catch (IOException e) { SetError(401, e.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }

        /*
         * The JSON serializer puts everything into one long string with no
         * line breaks to make is easier to look at in a text editor.  Since
         * this may be a real need during debugging, this routine will make
         * it easier to look at the json string by inserting a line break
         * after each JSON {} row.
         */
        public static int MakeJSONHumanReadable(string json, out string jsonout)
        {
            jsonout = json.Trim();

            try
            {
                if (json[0] == '[' && json.IndexOf("]") > 0)
                {
                    jsonout = jsonout.Replace("[{", "[\r\n{");
                    jsonout = jsonout.Replace("},", "},\r\n");
                    jsonout = jsonout.Replace("}]", "}\r\n]");
                }
                else SetError(1435, "JSON parse error");
            }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }

        /******************************************************************************************
         * Write a datatable out to a compatible XML file to some xBase systems.
         * xml:space="preserve" is added to keep leading spaces in the data.
         ******************************************************************************************/
        public static int WriteJAXXML(string filename, DataTable dt, string? cursorname)
        {

            try
            {
                // Set the cursor name
                string cn = string.IsNullOrEmpty(cursorname) ? "SQLResults" : cursorname.Trim();
                dt.TableName = cn;

                //Create the XML string from the data table with schema
                StringWriter SWX = new();
                dt.WriteXml(SWX, XmlWriteMode.WriteSchema, false);
                string slXML = SWX.ToString();
                SWX.Close();

                // Prep the file write
                TextWriter TWX = File.CreateText(filename);

                // Update the string and write the file
                TWX.Write(slXML.Replace("<NewDataSet>", "<NewDataSet xml:space=\"preserve\">"));
                TWX.Close();
            }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (IOException ex) { SetError(401, ex.Message); }
            catch (ArgumentNullException ex) { SetError(1220, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }

        /*
         * Create a folder if it doesn't already exist and return
         * error code if an exception occurs
         */
        public static int CheckPath(string path)
        {

            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (IOException ex) { SetError(401, ex.Message); }
            catch (ArgumentNullException ex) { SetError(1220, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return ErrNo;
        }


        /* 
         * 2025.11.08 - JLW
         * I modified the routine that GROK suggested by removing some 
         * parameters that JAXBase just didn't need.
         * 
         * Write all bytes to a filestream with error checking
         */
        public static int TryWriteAllBytes(
            FileStream fs,
            string stringBuffer,
            long position = -1)
        {
            int result = 0;
            byte[] buffer = new byte[stringBuffer.Length];
            buffer = Encoding.UTF8.GetBytes(stringBuffer);

            if (fs == null || buffer == null || !fs.CanWrite)
                result = 2084;
            else
            {
                result = buffer.Length;

                try
                {
                    if (position >= 0)
                    {
                        // Are we appending some bytes to the file first?
                        if (position > fs.Length)
                            fs.SetLength(position + result);

                        // Set the position
                        fs.Seek(position, SeekOrigin.Begin);
                    }

                    // Write the bytes and get the length
                    fs.Write(buffer, 0, result);
                }
                catch
                {
                    // Failed to write all bytes
                    result = 2084;
                }
            }

            return result;
        }

        /*
         * Another GROK solution to quickly count how many lines
         * are in a streamreader.  Since the streamreader, 
         * streamwriter, and filestream all share the same
         * record pointer, it works out pretty well.
         */
        public static long CountLines(StreamReader sr)
        {
            sr.DiscardBufferedData(); // Important if stream was used before
            sr.BaseStream.Seek(0, SeekOrigin.Begin);

            long count = 0;
            var buffer = new char[8192];
            int read;

            // Grab the file 8k at a time and count the
            // number of newline characters.  This will
            // work for both Windows and Linux as C#
            // handles the conversion internally.
            while ((read = sr.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                    if (buffer[i] == '\n') count++;
            }

            // Handle file ending without final \n
            if (sr.BaseStream.Position > 0 &&
                sr.BaseStream.Position == sr.BaseStream.Length &&
                buffer[read - 1] != '\n')
                count++;

            return count;
        }
    }
}
