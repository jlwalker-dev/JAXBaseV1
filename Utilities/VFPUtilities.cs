using DynamicData;
using JAXBase.Data;
using JAXBase.XBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using ZXing;

namespace JAXBase.Utilities
{
    public class VFPUtilities
    {
        /* ========================================================================================= *  
         *  XML Section
         * ========================================================================================= */

        /* ========================================================================================= *
         * Write a datatable out to a VFP compatible XML file.  
         * xml:space="preserve" is added to keep leading spaces in the data.
         * 
         * 
         * From the VFP help file (for development purposes)
         * nOuputFormat     Description  
         * 1 - ELEMENTS     (Default) Element-centric XML 
         * 2 – ATTRIBUTES   Attribute-centric XML
         * 3 – RAW          Generic, attribute-centric XML
         * 
         * nFlags 
         * Specifies the formatting of the XML that is produced and its destination. The following table lists the values for nFlags.
         * 
         * nFlags   Bit             Output description  
         * 0        0000            (Default) Produce XML in UTF-8 format. 
         *                          This setting creates a memory variable if one does not exist when specified by cOutput and returns XML to the memory variable.
         *                          The XML declaration does not contain an Encoding= attribute; that is, no encoding attribute is set to UTF-8.
         *                  
         * 1        0001            Produce unformatted XML as a continuous string.
         * 
         * 2        0010            Enclose empty elements with open and closing elements, for example, <cc04><cc04/>.
         * 
         * 4        0100            Preserve white space in fields.
         * 
         * 8        1000            Wrap Memo fields in CDATA sections.
         * 
         * 16       10000           Output encoding. Output is set to the cursor code page. 
         *                          To ensure accurate character translation, the Visual FoxPro default code page must match the code page of the cursor. You can 
         *                          accomplish this by setting character and memo fields in the cursor to NOCPTRAN (character binary/memo binary).
         * 
         *                          When setting this value with tables using any of the code pages, the encoding attribute in the XML is set to an empty string (""). 
         *                          To change to the correct encoding attribute, use the STRTRAN( ) function. 
         * 
         *                          For example, for code page 936, provide the following to the resulting XML string:
         *                              strxml=STRTRAN(strxml, 'encoding=""', 'encoding="gb2312"'
         *                        
         * 32       100000          Output encoding.
         * 512      1000000000      Output to the file specified by cOutput.
         *                          If a file does not exist, it is created. If the file already exists, it is overwritten. The setting for SET SAFETY is observed.
         *                      
         * 4096     1000000000000   Disables base64 encoding. 
         *                          CURSORTOXML( ) exports Memo (Binary) fields as xsd:base64binary unless you use nFlags set to 4096. In Visual FoxPro, base64 
         *                          encoding is meant for encoding only binary data.
         *                          
         * 32768    none            Indicates that a code page should be used.
         * 
         * The following table describes how the encoding attribute is written when output encoding defaults to the cursor or table code page. 
         * 
         * Note:  Encoding flags are set by combining bits 4 and 5 (0010000).
         * 
         * Encoding flag    Bits 4 and 5    Description  
         * +0               00              (Default) Windows-1252. 
         * +16              01              Set output encoding attribute to the cursor code page.
         * +32              10              Set output encoding attribute to UTF-8 with no character translation.
         * +48              11              Set output encoding attribute to UTF-8 and translate character data to UTF-8.
         * 
         * The following table lists common Windows-compatible code pages. 
         * 
         * Code page    Platform                Encoding attribute in XML Declaration       Comments  
         * 437          MS-DOS, US              ibm437                                      
         * 850          MS-DOS, International   ibm850                              
         * 865          MS-DOS, Nordic          Empty string ("")
         * 866          MS-DOS, Russian         cp866
         * 932          Windows, Japanese       shift-jis
         * 936          Simplified Chinese      gb2312
         * 949          Windows, Korean         iso-2022-kr                                 or: ks_c_5601-1987
         * 950          Windows, Chinese/Taiwan big5
         * 1250         Windows, East European  Windows-1250                                Note case.
         * 1251         Windows, Russian        Windows-1251        
         * 1252         Windows, US, W Euro     Windows-1252                        
         * 1253         Windows, Greek          Windows-1253        
         * 1254         Windows, Turkish        Windows-1254
         * 1255         Windows, Hebrew         Windows-1255
         * 1256         Windows, Arabic         Windows-1256
         * 
         * 
         * cSchemaName  Description  
         * cSchemaName  Specifies the name and path of the external file for the schema (scoped to the root element of the XML). 
         *              Note  
         *              If cSchemaName contains a file name and cSchemaLocation is not provided or is blank, the contents of cSchemaName 
         *              are written to the xsi:schemaLocation or xsi:noNamespaceSchemaLocation attribute in the XML.
         *              
         *              In the following example, Visual FoxPro generates a generic XML file named MyXMLFile.xml from the Labels.dbf file 
         *              in the "Labels" alias and the schema file named MySchema in the same folder. 
         *                      CURSORTOXML("LABELS", "myXMLFile.xml", 1, 512, 0, "mySchema.xsd")
         *              
         *              If cSchemaName includes a URI, the schema is written to the current directory and must be uploaded to the server to 
         *              be accessed by the browser or parser. External schemas always are written to the same location as the XML file.
         * 
         * "1"          Specifies an inline schema is produced. For example, the following code produces an inline schema: 
         *                      CURSORTOXML("LABELS", "myXMLFile.xml", 1, 512, 0, "1")
         *                      
         * ""           Specifies that no schema is produced. 
         * 
         * ========================================================================================= */
        public static string MakeXMLFromCursor(DataTable dt, string cursorname, int vfpFlags)
        {
            string slXML = "";

            try
            {
                // Set the cursor name
                dt.TableName = cursorname;

                //Create the XML string from the data table with schema
                StringWriter SWX = new();
                dt.WriteXml(SWX, XmlWriteMode.WriteSchema, false);
                slXML = SWX.ToString();
                SWX.Close();

                // Update the string and write the file
                slXML = slXML.Replace("<NewDataSet>", "<NewDataSet xml:space=\"preserve\">");
            }
            catch (NotSupportedException ex) { throw new Exception($"333||{ex.Message}"); }
            catch (IOException ex) { throw new Exception($"401||{ex.Message}"); }
            catch (ArgumentNullException ex) { throw new Exception($"1220||{ex.Message}"); }
            catch (Exception ex) { throw new Exception($"9999||{ex.Message}"); }

            return slXML;
        }


        /* ========================================================================================= *
         *     nFlag    Description
         *      1       Preserves white space in the data
         *      2       Appends data to cCursor
         *      4 	    Map character fields to Varchar if set, otherwise character fields
         *      8 	    Ignore text data over 254 characters
         *      16 	    Ignore binary fields over 254 characters
         * 
         * ========================================================================================= */
        public static bool MakeCursorFromXML(string XMLschema, string cursorname, int vfpFlags)
        {
            bool result = false;

            return result;
        }



        /* ========================================================================================= *  
         *  JSON Section
         * ========================================================================================= */

        /* ========================================================================================= *  
         *  Convert a Cursor/Table to JSON
         * ========================================================================================= */
        /// <summary>
        /// Converts a DataTable (cursor) to a JSON string containing structure and/or data using Newtonsoft.Json.
        /// </summary>
        /// <param name="cursor">The DataTable representing the cursor.</param>
        /// <param name="includeData">If true, includes row data. If false, returns structure only.</param>
        /// <param name="formatting">Optional formatting (Indented or None).</param>
        /// <returns>JSON string representing the cursor.</returns>
        public static int CursorToJSON(string fileName, JAXDirectDBF dbf, bool includeData)
        {
            int records = 0;
            Formatting formatting = Formatting.Indented;

            if (dbf.DbfInfo.DBFStream is null)
                throw new Exception($"52||CursorToJSON");

            string strJSON;

            try
            {
                var result = new CursorJsonExport
                {
                    TableName = dbf.DbfInfo.TableName,
                    Columns = []
                };

                List<string> Fields = [];

                // Build DBF structure
                for (int i = 0; i < dbf.DbfInfo.Fields.Count; i++)
                {
                    if (dbf.DbfInfo.Fields[i].SystemColumn == false)
                    {
                        Fields.Add(dbf.DbfInfo.Fields[i].FieldName);

                        result.Columns.Add(new ColumnDefinition
                        {
                            Name = dbf.DbfInfo.Fields[i].FieldName,
                            FieldType = dbf.DbfInfo.Fields[i].FieldType,
                            FieldWidth = dbf.DbfInfo.Fields[i].FieldLen,
                            FieldPrecision = dbf.DbfInfo.Fields[i].FieldDec
                        });
                    }
                }

                // Add data if requested
                if (includeData)
                {
                    result.Rows = [];

                    for (int i = 1; i <= dbf.DbfInfo.RecCount; i++)
                    {
                        List<RowFieldValue> rowDict = [];
                        dbf.DBFGotoRecord(i).Wait();

                        if (dbf.DbfInfo.currentRowIsDeleted == false)
                        {
                            for (int j = 0; j < dbf.DbfInfo.VisibleFields; j++)
                            {
                                object? value = dbf.DbfInfo.CurrentRow.Rows[0][j + 1];
                                RowFieldValue rfv = new()
                                {
                                    Name = Fields[j],
                                    Value = value
                                };

                                rowDict.Add(rfv);
                            }

                            result.Rows.Add(rowDict);
                            records++;
                        }
                    }
                }

                // Set up the JSON Serializer
                var settings = new JsonSerializerSettings
                {
                    Formatting = formatting,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                strJSON = JsonConvert.SerializeObject(result, settings);

                JAXLib.StrToFile(strJSON, fileName, 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"9999|{ex.Message}|CursorToJSON");
            }

            return records;
        }


        /* ========================================================================================= *  
         *  Get a value from JSON with possible null return
         * ========================================================================================= */
        /// <summary>
        /// Extracts a single value from a JSON string by property name.
        /// </summary>
        /// <typeparam name="T">The type to return the value as.</typeparam>
        /// <param name="jsonString">The JSON string.</param>
        /// <param name="propertyName">The name of the property to extract.</param>
        /// <param name="defaultValue">Default value if property is not found or null.</param>
        /// <returns>The extracted value converted to type T.</returns>
        public static T? GetJsonValue<T>(string jsonString, string propertyName, T? defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return defaultValue;

            try
            {
                JObject? jobject = JObject.Parse(jsonString);

                if (jobject == null)
                    return defaultValue;

                JToken? token = jobject.SelectToken(propertyName, errorWhenNoMatch: false);

                if (token == null || token.Type == JTokenType.Null)
                    return defaultValue;

                return token.ToObject<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Extracts a single value as string (most common use case).
        /// </summary>
        /* ========================================================================================= *  
         *  Minimal get a string from JSON 
         * ========================================================================================= */
        /// <param name="jsonString">The JSON string.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value as string, or empty string if not found.</returns>
        public static string GetJsonStringValue(string jsonString, string propertyName)
        {
            return GetJsonValue<string>(jsonString, propertyName, string.Empty) ?? string.Empty;
        }
    }
}