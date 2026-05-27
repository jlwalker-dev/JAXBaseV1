/*
 * I'm trying to keep as many of the self-contained solutions I find on-line in one place with references
 * for each so it will be easier to track down any need for updates.
 */
using JAXBase.Core;
using JAXBase.XBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Text;

namespace JAXBase.Utilities
{
    public static class JAXUtilities
    {
        /*
         * stackoverflow.com/questions/78536/deep-cloning-objects/78568#78568
         */
        /// <summary>
        /// Perform a deep Copy of the object, using Json as a serialization method. NOTE: Private members are not cloned using this method.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T? CloneJson<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (source is null) return default;

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        }

        /// <summary>
        /// Creates a JSON string containing all readable properties of the JAXObjectWrapper.
        /// Assumes every property is TType="S" and Element.Type != "O".
        /// </summary>
        public static async Task<string> SimpleJOWToJSON(JAXObjectWrapper wrapper)
        {
            var properties = wrapper.GetPropertyList() ?? new List<string>();
            var dict = new Dictionary<string, object?>();

            foreach (var Prop in properties)
            {
                string prop = Prop.ToLower();

                JAXObjects.Token token = await wrapper.GetProperty(prop, 0);
                object? value;

                if (token.TType.Equals("A"))
                {
                    // It's an array, don't include it
                    value = null;
                }
                else
                {
                    value = token.Element.Type switch
                    {
                        "C" => token.AsString(),
                        "D" => token.AsDate(),
                        "L" => token.AsBool(),
                        "N" => token.AsDouble(),
                        "O" => null,
                        "T" => token.AsDateTime(),
                        "X" => null,
                        _ => token.AsString()
                    };
                }

                if (value is not null)
                    dict[prop] = value;
                else
                    dict.Remove(prop);


            }

            return JsonConvert.SerializeObject(dict, Formatting.None);
        }


        /* -------------------------------------------------------------------------------------------------*
         * Convert Token to JSON string and back
         * 
         * Supplied by Grok - 2006-01-31 with touch ups and minor fixes by JLW
         * 
         * TODO - Will not create an exact duplicate of the JAXObjectWrapper class:
         *      - New ClassID
         *      - AError reset to 1 empty row
         *      - Arrays need work
         * 
         * Usage:
         *      string json = JAXObjectWrapperJsonSerializer.ToJson(form, Formatting.Indented);
         *      JAXObjects.Token rootToken = JAXObjectWrapperJsonSerializer.FromJson(json, App);
         *      
         * -------------------------------------------------------------------------------------------------*/
        public static class JAXObjectWrapperJsonSerializer
        {
            /// <summary>
            /// Serializes a JAXObjectWrapper and all its properties to JSON.
            /// Supports:
            /// - Single values (TType="S")
            /// - 1D arrays (TType="A", Row=0, Col>0) → JSON array [ ... ]
            /// - 2D arrays (TType="A", Row>0, Col>0) → array of arrays [ [..], [..], ... ]
            /// - Nested objects (TType="S", Type="O" → Value is JAXObjectWrapper) → nested object { ... }
            /// </summary>
            public static async Task<string> ToJson(JAXObjectWrapper wrapper, Formatting formatting = Formatting.Indented)
            {
                if (wrapper == null) return "{}";

                List<string> props = wrapper.GetPropertyList() ?? [];
                if (props.Count == 0) return "{}";

                JObject root = [];

                foreach (var name in props)
                {
                    var token = await wrapper.GetProperty(name, 0);
                    if (token.Element.IsNull())
                    {
                        root[name] = null;
                        continue;
                    }

                    root[name] = await TokenToJToken(token);
                }

                return root.ToString(formatting);
            }

            private static async Task<JToken> TokenToJToken(JAXObjects.Token token)
            {
                if (token == null) return JValue.CreateNull();

                // Handle null value
                if (token.TType == "S" && token.Element.Type == "X")
                {
                    return JValue.CreateNull();
                }

                // Single value
                if (token.TType == "S")
                {
                    if (token.Element.Type == "O")
                    {
                        if (token.Element.Value is JAXObjectWrapper nestedWrapper)
                        {
                            return await WrapperToJObject(nestedWrapper);
                        }
                        // Fallback if marked as object but not a wrapper
                        return new JValue(token.AsString() ?? "null");
                    }

                    // Primitive single values
                    return token.Element.Type switch
                    {
                        "C" => new JValue(token.AsString()),
                        "N" => new JValue(token.AsDouble()),
                        "L" => new JValue(token.AsBool()),
                        "D" => new JValue(token.AsDate()),
                        "T" => new JValue(token.AsDateTime()),
                        _ => new JValue(token.AsString() ?? "")
                    };
                }

                // Not an array → fallback
                if (token.TType != "A")
                    return new JValue(token.AsString() ?? "");

                // ── Array handling ─────────────────────────────────────────────
                int rows = token.Row;
                int cols = token.Col;

                if (cols <= 0)
                {
                    throw new Exception("235|");
                }

                var jArray = new JArray();

                if (rows <= 0)
                {
                    // 1D array: Row = 0, Col > 0
                    // Elements are accessed as (1,1) to (1,cols)
                    for (int c = 1; c <= cols; c++)
                    {
                        var elementToken = new JAXObjects.Token();
                        elementToken.CopyFrom(token);          // copy full state
                        elementToken.SetElement(1, c);         // position to this cell
                        jArray.Add(TokenToJToken(elementToken));
                    }
                }
                else
                {
                    // 2D array: Row > 0, Col > 0
                    // Result: array of rows [[...], [...], ...]
                    for (int r = 1; r <= rows; r++)
                    {
                        var rowArray = new JArray();

                        for (int c = 1; c <= cols; c++)
                        {
                            var elementToken = new JAXObjects.Token();
                            elementToken.CopyFrom(token);      // copy full state
                            elementToken.SetElement(r, c);     // position to this cell
                            rowArray.Add(TokenToJToken(elementToken));
                        }

                        jArray.Add(rowArray);
                    }
                }

                return jArray;
            }

            private static async Task<JObject> WrapperToJObject(JAXObjectWrapper wrapper)
            {
                if (wrapper == null) return new();

                List<string> props = wrapper.GetPropertyList() ?? [];
                JObject obj = new();

                foreach (var name in props)
                {
                    JAXObjects.Token token = await wrapper.GetProperty(name, 0);
                    if (token.Element.IsNull())
                    {
                        obj[name] = null;
                        continue;
                    }

                    obj[name] = await TokenToJToken(token);
                }

                return obj;
            }

            // -------------------------------------------------------------------------------------------------------------------
            // <summary>
            /// Creates a JAXObjects.Token from a JSON string that was previously serialized
            /// using ToJson(...) or has compatible structure.
            /// </summary>
            /// <param name="json">The JSON string to deserialize</param>
            /// <param name="app">The App instance needed to create JAXObjectWrapper instances for nested objects</param>
            /// <returns>A Token representing the JSON value (scalar, 1D array, 2D array, or object)</returns>
            // -------------------------------------------------------------------------------------------------------------------
            public static async Task<JAXObjects.Token> FromJson(string json, AppClass app)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return new JAXObjects.Token(); // default = single bool false

                JToken jToken;
                try
                {
                    jToken = JToken.Parse(json);
                }
                catch (JsonReaderException ex)
                {
                    throw new Exception($"2500|{nameof(json)}|{ex.Message}");
                }

                JAXObjects.Token token = new();
                await PopulateTokenFromJToken(app, token, jToken);
                return token;
            }

            private static async Task PopulateTokenFromJToken(AppClass app, JAXObjects.Token targetToken, JToken source, string? propertyNameForWrapper = null)
            {
                if (source == null || source.Type == JTokenType.Null)
                {
                    targetToken.Element.MakeNull(); // sets Type = "X"
                    return;
                }

                switch (source.Type)
                {
                    case JTokenType.String:
                        if (source.Value<string>() is null)
                            targetToken.Element.MakeNull();
                        else
                            targetToken.Element.Value = source.Value<string>()!;
                        break;

                    case JTokenType.Integer:
                    case JTokenType.Float:
                        targetToken.Element.Value = source.Value<double>(); // VFP-like numeric
                        break;

                    case JTokenType.Boolean:
                        targetToken.Element.Value = source.Value<bool>();
                        break;

                    case JTokenType.Date:
                        DateTime dateValue = source.Value<DateTime>();
                        // Decide D vs T — simple heuristic (midnight = DateOnly)
                        if (dateValue.TimeOfDay == TimeSpan.Zero)
                            targetToken.Element.Value = DateOnly.FromDateTime(dateValue);
                        else
                            targetToken.Element.Value = dateValue;
                        break;

                    case JTokenType.Object:
                        // Nested object → becomes TType="S", Type="O", Value = JAXObjectWrapper
                        var jObj = (JObject)source;

                        JAXObjectWrapper wrapper = new(app, "object", propertyNameForWrapper ?? "jsonObject", []);

                        foreach (var prop in jObj.Properties())
                        {
                            var propToken = new JAXObjects.Token();
                            await PopulateTokenFromJToken(app, propToken, prop.Value, prop.Name);

                            // We have some clean up chores --------------------------------------------------
                            // If it doesn't exist, assume it's a user property and needs to be added
                            if (wrapper.thisObject!.UserProperties.ContainsKey(prop.Name) == false)
                                await wrapper.AddProperty(prop.Name, new(), 0, string.Empty);

                            // We can't copy class ids - they have to be unique
                            if (prop.Name.Equals("classid", StringComparison.OrdinalIgnoreCase))
                                propToken.Element.Value = app.SystemCounter();

                            // We don't copy the aerror array - set as blank
                            if (prop.Name.Equals("aerror", StringComparison.OrdinalIgnoreCase))
                            {
                                propToken.SetDimension(1, 4, true);
                                propToken._avalue[0].Value = false;
                                propToken._avalue[1].Value = false;
                                propToken._avalue[2].Value = false;
                                propToken._avalue[3].Value = false;
                            }
                            // -------------------------------------------------------------------------------

                            // Update the property
                            await wrapper.SetProperty(prop.Name, propToken);
                        }

                        targetToken.Element.Value = wrapper;
                        break;

                    case JTokenType.Array:
                        var jArray = (JArray)source;

                        if (jArray.Count == 0)
                            throw new Exception("235|");    // Invalid array

                        // Detect 1D vs 2D
                        bool is2D = jArray.Count > 0 && jArray[0].Type == JTokenType.Array;

                        if (!is2D)
                        {
                            // 1D array → Row=0, Col=count
                            int cols = jArray.Count;
                            targetToken.SetDimension(0, cols, true);

                            for (int c = 1; c <= cols; c++)
                            {
                                var elemToken = new JAXObjects.Token();
                                await PopulateTokenFromJToken(app, elemToken, jArray[c - 1]);

                                targetToken.SetElement(1, c);
                                targetToken.CopyFrom(elemToken); // or use your copy mechanism
                            }
                        }
                        else
                        {
                            // 2D array → Row=outer count, Col=inner count (assume uniform)
                            int rows = jArray.Count;
                            int cols = ((JArray)jArray[0]).Count; // assume all rows same length

                            targetToken.SetDimension(rows, cols, true);

                            for (int r = 1; r <= rows; r++)
                            {
                                var row = (JArray)jArray[r - 1];
                                for (int c = 1; c <= cols; c++)
                                {
                                    var elemToken = new JAXObjects.Token();
                                    await PopulateTokenFromJToken(app, elemToken, row[c - 1]);
                                    targetToken.SetElement(r, c);
                                    targetToken.CopyFrom(elemToken);
                                }
                            }
                        }
                        break;

                    default:
                        // Fallback - treat as string
                        targetToken.Element.Value = source.ToString();
                        break;
                }
            }
        }





        /* -------------------------------------------------------------------------------------------------*
         * Compress/Decompress string routines
         * stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp
         * -------------------------------------------------------------------------------------------------*/
        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        public static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.ReadExactly(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }


        /*
         * stackoverflow.com/questions/4638993/difference-in-months-between-two-dates
         */
        public struct DateTimeSpan
        {
            public int Years { get; } = 0;
            public int Months { get; } = 0;
            public int Days { get; } = 0;
            public int Hours { get; } = 0;
            public int Minutes { get; } = 0;
            public int Seconds { get; } = 0;
            public int Milliseconds { get; } = 0;

            public DateTimeSpan(int years, int months, int days, int hours, int minutes, int seconds, int milliseconds)
            {
                Years = years;
                Months = months;
                Days = days;
                Hours = hours;
                Minutes = minutes;
                Seconds = seconds;
                Milliseconds = milliseconds;
            }

            enum Phase { Years, Months, Days, Done }

            public DateTimeSpan CompareDates(DateTime date1, DateTime date2)
            {
                if (date2 < date1)
                {
                    var sub = date1;
                    date1 = date2;
                    date2 = sub;
                }

                DateTime current = date1;
                int years = 0;
                int months = 0;
                int days = 0;

                Phase phase = Phase.Years;
                DateTimeSpan span = new DateTimeSpan();
                int officialDay = current.Day;

                while (phase != Phase.Done)
                {
                    switch (phase)
                    {
                        case Phase.Years:
                            if (current.AddYears(years + 1) > date2)
                            {
                                phase = Phase.Months;
                                current = current.AddYears(years);
                            }
                            else
                            {
                                years++;
                            }
                            break;
                        case Phase.Months:
                            if (current.AddMonths(months + 1) > date2)
                            {
                                phase = Phase.Days;
                                current = current.AddMonths(months);
                                if (current.Day < officialDay && officialDay <= DateTime.DaysInMonth(current.Year, current.Month))
                                    current = current.AddDays(officialDay - current.Day);
                            }
                            else
                            {
                                months++;
                            }
                            break;
                        case Phase.Days:
                            if (current.AddDays(days + 1) > date2)
                            {
                                current = current.AddDays(days);
                                var timespan = date2 - current;
                                span = new DateTimeSpan(years, months, days, timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);
                                phase = Phase.Done;
                            }
                            else
                            {
                                days++;
                            }
                            break;
                    }
                }

                return span;
            }
        }


        /// <summary>
        /// Find the next command/variable starting at the beginning of the string and ending with a 
        /// space, comma, or other punctuation.  Include quoted material.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="tokens"></param>
        /// <param name="cmdToken"></param>
        /// <returns>OUT: string of command found.  RETURNS: remainder of command string.</returns>
        public static string GetNextToken(string command, string tokens, out string cmdToken)
        {
            cmdToken = string.Empty;
            string cmdLine = command.Trim();    // clean up leading and trailing spaces
            string cmd;
            char inQuote = '\0';
            int quoteCount = 0;
            int i = 0;
            tokens = tokens.Length == 0 ? " " : tokens;

            try
            {
                while (true)
                {
                    if (tokens.Contains(cmdLine[i]))
                    {
                        if (inQuote == '\0')
                        {
                            cmdToken = cmdLine[..i].Trim();
                            cmd = cmdLine.Substring(i).Trim();
                            break;
                        }
                    }

                    if ("(['\"".Contains(cmdLine[i]))
                    {
                        inQuote = cmdLine[i].Equals('(') ? ')' : cmdLine[i].Equals('[') ? ']' : cmdLine[i];
                        quoteCount++;
                    }

                    if (cmdLine[i] == inQuote)
                    {
                        inQuote = '\0';
                        quoteCount--;
                    }

                    i++;
                    if (i >= cmdLine.Length)
                    {
                        cmdToken = cmdLine;
                        cmd = string.Empty;
                        break;
                    }
                }
            }
            catch
            {
                cmd = string.Empty;
                cmdToken = string.Empty;
            }

            return cmd;
        }

        // Source - https://stackoverflow.com/questions/11121936/dotnet-soundex-function
        // Posted by Damian Vogel, modified by community. See post 'Timeline' for change history
        // Retrieved 2025-12-08, License - CC BY-SA 4.0

        public static string Soundex(string data)
        {
            StringBuilder result = new StringBuilder();

            if (data != null && data.Length > 0)
            {
                string previousCode = "", currentCode = "", currentLetter = "";
                result.Append(data[0]); // keep initial char

                for (int i = 0; i < data.Length; i++) //start at 0 in order to correctly encode "Pf..."
                {
                    currentLetter = data[i].ToString().ToLower();
                    currentCode = "";

                    if ("bfpv".Contains(currentLetter))
                        currentCode = "1";
                    else if ("cgjkqsxz".Contains(currentLetter))
                        currentCode = "2";
                    else if ("dt".Contains(currentLetter))
                        currentCode = "3";
                    else if (currentLetter == "l")
                        currentCode = "4";
                    else if ("mn".Contains(currentLetter))
                        currentCode = "5";
                    else if (currentLetter == "r")
                        currentCode = "6";

                    if (currentCode != previousCode && i > 0) // do not add first code to result string
                        result.Append(currentCode);

                    if (result.Length == 4) break;

                    previousCode = currentCode; // always retain previous code, even empty
                }
            }
            if (result.Length < 4)
                result.Append(new String('0', 4 - result.Length));

            return result.ToString().ToUpper();
        }


        /*
         * String format parser
         * 
         * Use the .NET string.format codes, but we don't care if there
         * aren't enough format codes for the strings sent.
         * 
         * First element of jaxVals is the string format template
         * followed by values to place in the template.
         */
        public static string StrFormat(AppClass app, string[] jaxVals)
        {
            if (jaxVals.Length < 2) throw new Exception("14|");
            JAXObjects.Token answer = new();

            object? val = AppHelper.Convert2STValue(jaxVals[0]);
            if (val is null)
                answer.Element.Value = ".NULL.";
            else
                answer.Element.Value = val;

            if (answer.Element.Type.Equals("C") == false)
                throw new Exception("11|");

            string result = answer.AsString();
            string[] setVals = new string[jaxVals.Length - 1];

            int j = 0;

            for (int i = 1; i < jaxVals.Length; i++)
            {
                answer = new();
                val = AppHelper.Convert2STValue(jaxVals[i]);
                if (val is null)
                    answer.Element.Value = ".NULL.";
                else
                {
                    answer.Element.Value = val;

                    if (answer.Element.Type.Equals("N"))
                    {
                        // Fix the value
                        double dVal = answer.AsDouble();

                        if (dVal == Convert.ToInt64(dVal))
                            answer.Element.Value = dVal.ToString($"F0");    // All whole numbers are shown as integers
                        else
                            answer.Element.Value = dVal.ToString($"F{app.CurrentDS.JaxSettings.Decimals}");
                    }
                }
                setVals[j++] = answer.AsString();
            }

            return SafeFormat(result, setVals);
        }


        public static string SafeFormat(this string format, params object?[] args)
        {
            if (string.IsNullOrEmpty(format)) return format;

            // Very naive & fast version
            var sb = new StringBuilder(format.Length + args.Length * 8);
            int i = 0;

            while (i < format.Length)
            {
                if (format[i] == '{' && i + 1 < format.Length && char.IsDigit(format[i + 1]))
                {
                    int start = i;
                    i++; // skip {

                    // read number
                    int num = 0;
                    while (i < format.Length && char.IsDigit(format[i]))
                    {
                        num = num * 10 + (format[i] - '0');
                        i++;
                    }

                    if (i < format.Length && format[i] == '}')
                    {
                        i++; // skip }
                        if (num < args.Length)
                            sb.Append(args[num]?.ToString() ?? "");
                    }
                    else
                    {
                        // malformed → copy as is
                        sb.Append(format.AsSpan(start, i - start + 1));
                    }
                }
                else
                {
                    sb.Append(format[i++]);
                }
            }

            return sb.ToString();
        }


        /*
         * Return the color integer value.
         * If number is passed, then return as integer
         * If string is passed, parse based on r|g|b format
         * Otherwise ERROR!
         */
        public static int ReturnColorInt(object? thisColor)
        {
            int result = 0;

            if (thisColor is string)
            {
                // Users put colors in as r,g,b but we store it
                // internally as r|g|b so let's make it work
                string[] rparts = ((string)thisColor).Replace("|", ",").Split(',');

                // Only bother if it's got 3 parts
                if (rparts.Length == 3)
                {
                    if (int.TryParse(rparts[0], out int rp0) == false) rp0 = 0;
                    if (int.TryParse(rparts[1], out int rp1) == false) rp1 = 0;
                    if (int.TryParse(rparts[2], out int rp2) == false) rp2 = 0;

                    result = rp0 + rp1 * 256 + rp2 * 65536;
                }
                else
                {
                    // Try converting to a integer
                    int test = -1;
                    if (rparts.Length == 1)
                    {
                        // Is it a number?
                        string junk = JAXLib.ChrTran(rparts[0], "0123456789", "");
                        if (junk.Length == 0 || junk.Equals("."))
                        {
                            // Can we parse the number and is it in range?
                            if (int.TryParse(rparts[0], out test) == false) test = -1;
                            if (JAXLib.Between(test, 0, 16777215) == false) test = -1;
                        }
                    }

                    if (test < 0)
                        throw new Exception($"67||{(string)thisColor}");
                    else
                        result = test;
                }
            }
            else
            {
                try
                {
                    result = Convert.ToInt32(thisColor);    // convert the (expressed) numeric value to int32

                    // Make sure it's a valid color value
                    if (JAXLib.Between(result, 0, 16843008) == false)
                        throw new Exception("11|");
                }
                catch (Exception ex)
                {
                    throw new Exception($"3400|{thisColor}|Number conversion error- {ex.Message}");
                }
            }
            return result;
        }


        public static bool IsValidName(string name)
        {
            bool result = true;
            name = name.ToLower().Trim();

            // Does it only have legal values?
            if (string.IsNullOrWhiteSpace(name) == false && JAXLib.ChrTran(name, "abcdefghijklmnopqrstuvwxyz0123456789_", "").Length == 0)
                result = "abcdefghijklmnopqrstuvwxyz_".Contains(name[0]);

            return result;
        }



        /// <summary>
        /// Loads an audio or video file into a MemoryStream using protected (non-paged) memory when possible.
        /// Works on Windows and Linux (and macOS).
        /// </summary>
        /// <param name="filePath">Full path to the media file</param>
        /// <returns>MemoryStream positioned at 0, ready to be used</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static MemoryStream LoadFileToMemoryStream(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new Exception("402|");
            }

            string normalizedPath = Path.GetFullPath(filePath);

            if (!File.Exists(normalizedPath))
            {
                throw new Exception($"1|{normalizedPath}");
            }

            byte[] buffer;

            try
            {
                // Most common & performant way on both Windows and Linux
                buffer = File.ReadAllBytes(normalizedPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"401|{normalizedPath}|{ex.Message}");
            }

            var ms = new MemoryStream(buffer, writable: false) { Position = 0 };

            return ms;
        }
    }
}
