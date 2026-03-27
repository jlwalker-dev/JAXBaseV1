using JAXBase.Core;
using JAXBase.UI.Dialogs;
using JAXBase.Utilities.Utilities;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace JAXBase.Utilities
{
    public class UtilitiesLib
    {
        const string base64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz+=";

        private AppClass App;

        public string SpInstance { get; private set; } = string.Empty;
        public string SpRegCode { get; private set; } = string.Empty;

        private readonly JAXUtilities.DateTimeSpan oDateTimeSpan = new();

        // Variables for contructing a Primary Key
        private readonly DateTime epoch = new DateTime(1970, 1, 1).ToUniversalTime();
        private string spDateCode = string.Empty; // YYMD -> years, months, days
        private long npInc93 = 0;
        private readonly int npIncPow = 2;

#pragma warning disable IDE0044 // Add readonly modifier
        private long npMaxInc93 = 0;
#pragma warning restore IDE0044 // Add readonly modifier


        /* -------------------------------------------------------------------------------------------------*
         * Password Hashing support
         * -------------------------------------------------------------------------------------------------*/
        const int nHashKeySize = 128;
        const int nHashIteragtions = 953000;
        private readonly HashAlgorithmName oHashAlgorithm = HashAlgorithmName.SHA512;

        public UtilitiesLib(AppClass app)
        {
            Console.WriteLine("Utilities Startup");

            // Fix up any variables right away
            App = app;
            npMaxInc93 = (long)System.Math.Pow(36, npIncPow);
        }



        /* -------------------------------------------------------------------------------------------------*
         * Recieve a clear text password and send out a string in the format
         * of PasswordHash64|Salt64
         *
         * Returns 0 if success
         *
         * -------------------------------------------------------------------------------------------------*/
        public int HashPassword(string stPassword, out string stHashedPassword)
        {
            try
            {
                byte[] salt = RandomNumberGenerator.GetBytes(nHashKeySize);

                App.ClearErrors();
                stHashedPassword = string.Empty;

                var vlHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(stPassword),
                    salt,
                    nHashIteragtions,
                    oHashAlgorithm,
                    nHashKeySize);

                stHashedPassword += Convert.ToBase64String(vlHash) + "|" + Convert.ToBase64String(salt);
            }
            catch (Exception ex) { App.SetError(9999, ex.Message, "HashPassword"); stHashedPassword = string.Empty; }

            return App.LastErrorNo();
        }


        /* -------------------------------------------------------------------------------------------------*
         * Take a clear text password and a hashed pw|salt string and 
         * compare the results.  If PW matches the hashed PW then
         * return 0.  If it does not match, return 1, and if an
         * exception then return 9999.
         *
         * ErrMessage will hold the text of the error message
         * -------------------------------------------------------------------------------------------------*/
        public int HashVerify(string stPassword, string stHashedPassword)
        {
            try
            {
                string[] stHashedPW = stHashedPassword.Split("|");
                byte[] stHPW = Convert.FromBase64String(stHashedPW[0]);
                byte[] salt = Convert.FromBase64String(stHashedPW[1]);
                App.ClearErrors();

                var vlHashToCompare = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(stPassword),
                    salt,
                    nHashIteragtions,
                    oHashAlgorithm,
                    nHashKeySize);

                bool test = CryptographicOperations.FixedTimeEquals(vlHashToCompare, stHPW);

                if (!test)
                {
                    App.SetError(9001, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }
            catch (Exception ex) { App.SetError(9999, ex.Message, "HashVerify"); }

            return App.LastErrorNo();
        }



        /* -------------------------------------------------------------------------------------------------*
         * ****************************************************************************
         * ****************************************************************************
         * Get the next primary key
         *
         * Primary key is returned in Base36 and designed as follows
         *  Length   Description
         *  ------  ---------------------------------------------------
         *  1       Microservice Instance Code
         *  4       Registration code
         *  9       Milliseconds since epoch
         *  2       Increment value
         * ============================================================================
         *-------------------------------------------------------------------------------------------------*/
        public int GetNextKey(out string sResult)
        {
            sResult = string.Empty;
            App.ClearErrors();

            try
            {
                if (SpRegCode == null || SpRegCode.Length != 4)
                {
                    // Need a valid registration code
                    App.SetError(9002, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
                else
                {

                    if (npInc93 > npMaxInc93 - 5) // I loath running up to the end
                        npInc93 = 0;

                    if (npInc93 == 0)
                        MilCode(out spDateCode);

                    if (App.LastErrorNo() == 0 && Conv36(npInc93, npIncPow, out string inc3) == 0)
                        sResult = SpInstance + SpRegCode + spDateCode + inc3;
                    else
                        sResult = string.Empty;

                    npInc93++;
                }
            }
            catch (Exception ex)
            {
                App.SetError(9999, ex.Message, "GetNextKey");
            }

            return App.LastErrorNo();
        }



        /*------------------------------------------------------------------------------------------ 
         * The registration code just gives an extra layer of uniqueness making it possible to move
         * records between databases.
         *
         * Set the registration code for the application. An example of a good registration code would be
         * a 4 character code representing each customer. Having 4 charactes allows over 1.6 million
         * different registration codes.
         *
         * The registration code is used in conjucntion with the instance code and date code to create
         * a unique key that cannot repeat as long as you stick with the rules.
         *
         * Rules:
         *      1) Each running microservice instance using a database has a unique instance code
         *      2) Registration code could be based on customer, user, workstation, or ip.
         *
         *------------------------------------------------------------------------------------------*/
        public int SetRegistration(string stRegCode)
        {
            App.ClearErrors();

            if (stRegCode.Length == 4)
            {
                // Update the date code
                MilCode(out spDateCode);

                for (int i = 0; i < stRegCode.Length; i++)
                {
                    int j = base64.IndexOf(stRegCode[i].ToString());
                    if (j < 0 || j > 35)
                    {
                        stRegCode = string.Empty;
                        App.SetError(9999, string.Format("Invalid charter '{0} in registration code", stRegCode[i]), "SetRegistration");
                        break;
                    }
                }

                SpRegCode = stRegCode.Length > 0 ? stRegCode : string.Empty;
            }
            else
            {
                App.SetError(9999, "Registration code must be four characters in length", "SetRegistration");
            }

            return App.LastErrorNo();
        }


        /****************************************************************************
         * Basic base conversions for indexing
         * Returns set length with overflow detection
         * Supports base2 to base64 with same characterset and build logic
         ****************************************************************************/

        /*------------------------------------------------------------------------------------------ 
         * Base36 (0-Z)
         * While base 36 needs more characters to do the same work as Base64, it
         * has the advantage of not confusing the SQL engine.  Many SQL engines
         * are case insensitive for plain text indexes.  If you use a binary
         * character or text field, then you can usually use a higher base value.
         *------------------------------------------------------------------------------------------*/
        public int Conv36(long nValue, int setlength, out string sResult)
        {
            return ConvBase(nValue, setlength, 36, out sResult);
        }

        /*------------------------------------------------------------------------------------------ 
         * Base52 (0-z)
         *------------------------------------------------------------------------------------------*/
        public int Conv52(long nValue, int setlength, out string sResult)
        {
            return ConvBase(nValue, setlength, 52, out sResult);
        }


        public int Conv64ToInt(string b64)
        {
            long ln = Conv64ToLong(b64);

            if (ln > 2_147_483_647)
                throw new Exception("Interger overflow in Conv64ToInt");

            return (int)ln;
        }

        public long Conv64ToLong(string b64)
        {
            int l = 0;
            try
            {
                for (int i = 0; i < b64.Length; i++)
                {
                    int j = base64.IndexOf(b64[i]);
                    if (j < 0)
                        throw new Exception("Invalid Base 64 string");
                    else
                        l = l * 64 + j;
                }
            }
            catch (Exception e)
            {
                l = 0;
                App.SetError(9999, e.ToString(), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            return l;
        }

        public long Conv52ToLong(string b52)
        {
            string bb = base64[..52];
            int l = 0;
            try
            {
                for (int i = b52.Length; i > 0; i++)
                {
                    int j = bb.IndexOf(b52[i - 1]);
                    if (j < 0)
                        throw new Exception("Invalid Base 52 string");
                    else
                        l = l * 52 + j;
                }
            }
            catch (Exception e)
            {
                l = 0;
                App.SetError(9999, e.ToString(), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            return l;
        }

        /*------------------------------------------------------------------------------------------ 
         * Uses 0-9,A-Z,a-z, and +=
         * This is NOT compatible with the base64 conversion function and is
         * expected to be use to create a string, based off a number, that
         * will primarilly be used for index keys or license plates
         *------------------------------------------------------------------------------------------*/
        public int Conv64(long ntValue, int ntSetlength, out string stResult)
        {
            return ConvBase(ntValue, ntSetlength, 64, out stResult);
        }

        /*------------------------------------------------------------------------------------------ 
         * Convert a long to a base 2 through 64 license indexable value
         *------------------------------------------------------------------------------------------*/
        public int ConvBase(long ntValue, int ntSetLength, int ntBaseValue, out string stResult)
        {
            StringBuilder sb = new();
            stResult = string.Empty;

            try
            {
                if (ntSetLength < 0 || ntSetLength > 64)
                {
                    App.SetError(9999, string.Format("Property nBase has invalid value '{0}' (2-64 allowed)", ntBaseValue), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
                else
                {
                    if (ntValue < 0)
                    {
                        App.SetError(9999, "Cannot create index code using a negative value", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    else
                    {
                        // create the base x string
                        while (ntValue > 0)
                        {
                            long j = ntValue % ntBaseValue;
                            ntValue -= j;
                            ntValue /= ntBaseValue;
                            sb.Append(base64[(int)j]);      // Most significant digit last
                        }

                        // Fix the length?
                        if (sb.Length < ntSetLength) sb.Append(new string('0', ntSetLength - sb.Length));

                        if (ntSetLength > 0 && sb.Length > ntSetLength)
                        {
                            stResult = string.Empty;
                            App.SetError(39, "Numeric overflow. Data was lost", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }

                        // Make most significant digit is first
                        char[] sbc = sb.ToString().ToCharArray();
                        Array.Reverse(sbc);
                        stResult = new string(sbc);
                    }
                }
            }
            catch (Exception ex)
            {
                App.SetError(9999, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                stResult = string.Empty;
            }

            return App.LastErrorNo();
        }



        /*------------------------------------------------------------------------------------------ 
         * Returns a Base36 code based on the current time span since the
         * computer epoch (max 77,712,312,459) in 7 bytes
         *------------------------------------------------------------------------------------------*/
#pragma warning disable IDE0051 // Remove unused private members
        private int DateTimeCode(out string stResult)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var dlDateSpan = oDateTimeSpan.CompareDates(epoch, DateTime.UtcNow);
            long llSpanSinceEpoch = dlDateSpan.Years * 100000000L
                + dlDateSpan.Months * 1000000L
                + dlDateSpan.Days * 10000L
                + dlDateSpan.Hours * 100L
                + dlDateSpan.Minutes;  // 1671231 max 1 day shy of or 168 years

            llSpanSinceEpoch %= 77712312459;
            if (Conv36(llSpanSinceEpoch, 7, out stResult) > 0) stResult = string.Empty;

            return App.LastErrorNo();
        }


        /*------------------------------------------------------------------------------------------ 
         * Return years, months, days since epoch in a 4 character Code36 format
         *------------------------------------------------------------------------------------------*/
        public int DateCode(out string stResult)
        {
            var dlDateSpan = oDateTimeSpan.CompareDates(epoch, DateTime.UtcNow);
            long llSpanSinceEpoch = dlDateSpan.Years * 10000L + dlDateSpan.Months * 100L + dlDateSpan.Days;
            llSpanSinceEpoch %= 1671232L;  // 1671231 max 1 day shy of or 168 years
            if (Conv36(llSpanSinceEpoch, 4, out stResult) > 0) stResult = string.Empty;

            return App.LastErrorNo();
        }


        /*------------------------------------------------------------------------------------------ 
         *------------------------------------------------------------------------------------------*/
        public int MilCode(out string stResult)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();

            // reset the incrementing value
            npInc93 = 0;

            if (Conv36(unixTimeMilliseconds, 9, out stResult) > 0)
                stResult = string.Empty;

            return App.LastErrorNo();
        }


        /*------------------------------------------------------------------------------------------ 
         * Used to extract a column as a string from a row and if
         * empty or null, create the correct exception to catch
         *------------------------------------------------------------------------------------------*/
        public string CheckForNullOrEmpty(DataRow dr, string fld)
        {
            string str = (dr[fld].ToString() ?? string.Empty) ??
                throw new ArgumentNullException(string.Format("Null value exception in field '{0}'", fld));

            if (string.IsNullOrEmpty(str))
                throw new ArgumentException(string.Format("Empty value exception in field '{0}'", fld));

            return str.Trim();
        }

        /*==========================================================================================
         * Some numeric/byte functions that will be used in the system
         * CV* and MK* act similar to QB CVI/MKI functions
         *==========================================================================================*/

        /*------------------------------------------------------------------------------------------ 
         * Turn 8 bytes into a ushort
         *------------------------------------------------------------------------------------------*/
        public ushort CVU(string bytes)
        {
            ushort result = 0;
            try
            {
                byte[] b = Convert.FromBase64String(bytes);
                result = BitConverter.ToUInt16(b);
            }
            catch
            {
                result = 0;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn 4 bytes into an integer
         *------------------------------------------------------------------------------------------*/
        public int CVI(string bytes)
        {
            int result = 0;
            try
            {
                byte[] b = Convert.FromBase64String(bytes);
                result = BitConverter.ToInt32(b);
            }
            catch
            {
                result = 0;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn 8 bytes into a long
         *------------------------------------------------------------------------------------------*/
        public long CVL(string bytes)
        {
            long result = 0;
            try
            {
                byte[] b = Convert.FromBase64String(bytes);
                result = BitConverter.ToInt64(b);
            }
            catch
            {
                result = 0;
            }

            return result;
        }


        /*------------------------------------------------------------------------------------------ 
         * Turn 8 bytes into a double
         *------------------------------------------------------------------------------------------*/
        public double CVD(string bytes)
        {
            double result = 0;
            try
            {
                byte[] b = Convert.FromBase64String(bytes);
                result = BitConverter.ToDouble(b);
            }
            catch
            {
                result = 0;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn 4 bytes into a float
         *------------------------------------------------------------------------------------------*/
        public float CVF4(string bytes)
        {
            float result = 0;
            try
            {
                byte[] b = Convert.FromBase64String(bytes);
                result = BitConverter.ToSingle(b);
            }
            catch
            {
                result = 0;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn 8 bytes into a float
         *------------------------------------------------------------------------------------------*/
        public float CVF(string bytes)
        {
            float result = 0;
            try
            {
                byte[] b = Convert.FromBase64String(bytes);
                result = (float)BitConverter.ToDouble(b);
            }
            catch
            {
                result = 0;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn an unsigned integer to 2 bytes
         *------------------------------------------------------------------------------------------*/
        public string MKU(ushort iVal)
        {
            string result = string.Empty;
            try
            {
                byte[] bb = BitConverter.GetBytes(iVal);
                result = Convert.ToBase64String(bb);
            }
            catch
            {
                result = "\0\0";
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn an integer to 4 bytes
         *------------------------------------------------------------------------------------------*/
        public string MKI(int iVal)
        {
            string result = string.Empty;
            try
            {
                byte[] bb = BitConverter.GetBytes(iVal);
                result = Convert.ToBase64String(bb);
            }
            catch
            {
                result = "\0\0\0\0";
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn an float to 4 bytes
         *------------------------------------------------------------------------------------------*/
        public string MKS(float fVal)
        {
            string result = string.Empty;
            try
            {
                byte[] bb = BitConverter.GetBytes(fVal);
                result = Convert.ToBase64String(bb);
            }
            catch
            {
                result = "\0\0\0\0";
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn a long to 8 bytes
         *------------------------------------------------------------------------------------------*/
        public string MKL(long iVal)
        {
            string result = string.Empty;
            try
            {
                byte[] bb = BitConverter.GetBytes(iVal);
                result = Convert.ToBase64String(bb);
            }
            catch
            {
                result = "\0\0\0\0\0\0\0\0";
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn a double to 8 bytes
         *------------------------------------------------------------------------------------------*/
        public string MKD(double dVal)
        {
            string result = string.Empty;
            try
            {
                byte[] bb = BitConverter.GetBytes(dVal);
                result = Convert.ToBase64String(bb);
            }
            catch
            {
                result = "\0\0\0\0\0\0\0\0";
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn a float to 8 bytes
         *------------------------------------------------------------------------------------------*/
        public string MKF(float dVal)
        {
            string result = string.Empty;
            try
            {
                byte[] bb = BitConverter.GetBytes(dVal);
                result = Convert.ToBase64String(bb);
            }
            catch
            {
                result = "\0\0\0\0\0\0\0\0";
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn 8 bytes into a long - least significatn byte first
         *------------------------------------------------------------------------------------------*/
        // Convert a byte array into a long value
        public long Bin2Long(byte[] binBytes)
        {
            long result = 0;
            for (int i = 0; i < binBytes.Length; i++)
                result = result + (long)System.Math.Pow(256L, i) * binBytes[i];

            return result;
        }


        /*------------------------------------------------------------------------------------------ 
         * Convert long to bytes - least significant byte first
         *------------------------------------------------------------------------------------------*/
        public byte[] Long2Bin(long val)
        {
            string num = val.ToString().Trim();
            byte[] bytes = { 0, 0, 0, 0, 0, 0, 0, 0 };

            if (val < 0)
                bytes = [255, 255, 255, 255, 255, 255, 255, 255];
            else
            {
                int i = 0;

                while (val > 0)
                {
                    bytes[i++] = (byte)(val % 256L);
                    val = val / 256;
                }
            }

            return bytes;
        }


        /*------------------------------------------------------------------------------------------ 
         * Turn array of bytes into a long - most significant byte first
         *------------------------------------------------------------------------------------------*/
        public long RevBin2Long(byte[] binBytes)
        {
            Array.Reverse(binBytes);
            return Bin2Long(binBytes);
        }

        public int RevBin2Int(byte[] binBytes)
        {
            Array.Reverse(binBytes);
            return (int)Bin2Long(binBytes);
        }


        /*------------------------------------------------------------------------------------------ 
         * Convert a long to array of bytes - most signifcant byte first
         *------------------------------------------------------------------------------------------*/
        public byte[] RevLong2Bin(long val)
        {
            string num = val.ToString().Trim();
            byte[] bytes = Long2Bin(val);
            Array.Reverse(bytes);
            return bytes;
        }

        public byte[] RevInt2Bin(long val)
        {
            string num = val.ToString().Trim();
            byte[] bytes = Long2Bin(val);
            byte[] iBytes = new byte[4];
            Array.Copy(bytes, 0, iBytes, 0, 4);
            Array.Reverse(iBytes);
            return iBytes;
        }

        /*------------------------------------------------------------------------------------------ 
         * Turn a DateTime into a 8 byte array with Date as an integer in the first
         * four bytes, and the milliseconds from midnight as the second four bytes
         *------------------------------------------------------------------------------------------*/
        public byte[] DT28Bytes(DateTime dtime)
        {
            // create the byte array as a blank result
            byte[] Data = { 0, 0, 0, 0, 0, 0, 0, 0 };

            if (dtime > DateTime.MinValue)
            {
                // Get the date part and make it a Julian int
                string date = dtime.ToString("yyyy-MM-dd").Replace("-", "");
                if (int.TryParse(date, out int idate) == false) idate = 0;
                int dd = (int)(dtime.ToOADate() + 2415019);

                // Get the time part and make it into an integer
                string time = dtime.ToString("HH:mm:ss").Replace(":", "");
                if (int.TryParse(time, out int itime) == false) itime = 0;
                int k = itime / 10000;
                itime = itime - (k * 10000);
                int ms = k * 3600000;
                k = itime / 100;
                itime = itime - (k * 100);
                ms = ms + k * 60000;
                ms = ms + itime;

                // Encode the two integers
                string Field = MKI(dd);
                byte[] rc = Convert.FromBase64String(Field);
                Array.Copy(rc, 0, Data, 0, 4);

                Field = MKI(ms);
                rc = Convert.FromBase64String(Field);
                Array.Copy(rc, 0, Data, 4, 4);
            }

            return Data;
        }


        /*------------------------------------------------------------------------------------------ 
         * DateTime is stored as 2 integers - first 4 bytes is days since 1899-12-31
         * and the second 4 bytes is number of milliseconds since midnight but
         * you need to add 1 to get the correct value (Weird).
         *------------------------------------------------------------------------------------------*/
        public DateTime Bytes2DT(byte[] buffer)
        {
            const uint ZERO_DATE = 1721426; // 1899-12-31
            DateTime result = DateTime.MinValue;

            byte[] buffer1 = new byte[4];   // Days since
            byte[] buffer2 = new byte[4];   // ms since midnight - 1
            Array.Copy(buffer, 0, buffer1, 0, 4);
            Array.Copy(buffer, 4, buffer2, 0, 4);

            uint d = BitConverter.ToUInt32(buffer1, 0);
            uint daysFromZeroDate = d - ZERO_DATE;
            uint miliseconds = BitConverter.ToUInt32(buffer2, 0);

            try
            {
                result = new DateTime(1, 1, 1).AddDays(daysFromZeroDate).AddMilliseconds(miliseconds + 1);
            }
            catch
            {
                // TODO - just go on
                result = DateTime.MinValue;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Receive a string and return the next expression, as defined by end character(s).
         * 
         * Quoted material is anything between [], (), single or double quotes.
         * 
         * Example:
         *      string tx = GetNextExpr("space(3) as field1, space(7) as field2",",", out string ex)
         *      
         *      The second parameer (",") is the only character to use for end of experession
         *
         *      When the end of the string from the first parameter is reached, that's also 
         *      and end of expression trigger.
         *      
         *      Upon return:
         *          tx will contain the remaining expression from parameter 1: "space(7) as field2"
         *          ex will contain the first expression found in parameter 1: "space(3) as field1"
         *------------------------------------------------------------------------------------------*/
        public string GetNextExpr(string Text, string EndChars, out string Expr)
        {
            StringBuilder expr = new();
            char startQuote = '\0';
            char inQuote = '\0';
            int quoteCount = 0;
            int j = 0;

            while (j < Text.Length)
            {
                char c = Text[j++];

                if ("[('\"".Contains(c) && inQuote == '\0')
                {
                    startQuote = c;

                    // We're not in quotes and found the beginning of a quote
                    if (c == '(')
                        inQuote = ')';
                    else if (c == '[')
                        inQuote = ']';
                    else
                        inQuote = c;

                    quoteCount++;
                    expr.Append(c.ToString());
                }
                else if (inQuote != '\0')
                {
                    // In quote, so just add it to the outgoing expression
                    expr.Append(c.ToString());

                    if (c == startQuote)
                        quoteCount++;

                    if (c == inQuote)
                    {
                        quoteCount--;
                        if (quoteCount < 1)
                        {
                            inQuote = '\0';
                            startQuote = '\0';
                        }
                    }
                }
                else
                {
                    // Not in quotes, so look for end characters
                    // and if found, break out, otherwise just
                    // keep adding to the outgoing expression
                    if (EndChars.Contains(c))
                        break;
                    else
                        expr.Append(c.ToString());
                }

            }

            Expr = expr.ToString();
            return j >= Text.Length ? string.Empty : Text[j..].Trim();
        }

        public string GetFileCheckSum_MD5(string text)
        {
            string result = string.Empty;

            using (var md5 = MD5.Create())
            {
                byte[] s = Encoding.UTF8.GetBytes(text);
                byte[] r = md5.ComputeHash(s);
                result = Convert.ToBase64String(r);
            }

            return result;
        }

        public Bitmap StringToBMP(string inputString64)
        {
            //byte[] imageBytes = Encoding.Unicode.GetBytes(inputString64);
            byte[] imageBytes = Convert.FromBase64String(inputString64);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                return new Bitmap(ms);
            }
        }

        public Icon StringToIcon(string inputString64)
        {
            //byte[] imageBytes = Encoding.Unicode.GetBytes(inputString64);
            byte[] imageBytes = Convert.FromBase64String(inputString64);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                return new Icon(ms);
            }
        }

        // Trying to find non-printing characters in a string can be nearly 
        // impossible as C# treats them like control characters.  The away
        // around that is to convert the string to a byte array and
        // search for the byte pattern.
        //
        // We use iso-8859-1 because the standard UTF8 will expand certain
        // control characters because they are expected to represent 
        // specific multi-byte codes.
        //
        // Because of how C# works, a FOR loop is about as fast of a search
        // as you can get.  You could use Span, which a bit faster, but it
        // doesn't let you look for a second match in the source.
        public int FindByteSequence(string sourceStr, string patternStr, int nStart)
        {
            int result = -1;

            byte[] source = Encoding.GetEncoding("iso-8859-1").GetBytes(sourceStr);
            byte[] pattern = Encoding.GetEncoding("iso-8859-1").GetBytes(patternStr);

            if (pattern.Length > 0)
            {
                for (int i = nStart; i <= source.Length - pattern.Length; i++)
                {
                    // Found the first byte, try to find the rest
                    bool match = true;
                    for (int j = 0; j < pattern.Length; j++)
                    {
                        if (source[i + j] != pattern[j])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        // Got a match?  Remember where and get out!
                        result = i;
                        break;
                    }
                }
            }

            return result;
        }

        public async Task ShowOpenFileDialog(string fileType, string fileExt)
        {
            DialogHelper dialogHelper= new DialogHelper();
            await dialogHelper.ShowFilePicker(App);
        }
    }
}
