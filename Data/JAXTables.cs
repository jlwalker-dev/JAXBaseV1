/*
 * Classes and helper routines for the JAX Tables
 */
using JAXBase.Core;
using JAXBase.Utilities;
using System.Data;
using System.Reflection;

namespace JAXBase.Data
{
    public class JAXTables()
    {
        /*---------------------------------------------------------------------------------------------*
         * Field attributes are held in this class
         *      Unless otherewise stated, fields are stored as-is in UTF8 format
         *      
         * Field Types
         *      C = Character                   Text right filled with spaces
         *      Y = Currency                    4 bytes
         *      D = Date                        Text in YYYYMMDD format or spaces
         *      T = DateTime                    Two integers (date & ms from midnight)
         *      B = Double                      8 bytes
         *      F = Float                       Text in <FieldLenx>.<FieldDec> format
         *      G = General                     Bytes
         *      H = High Precision Double       emulates COBOL high precision values
         *      I = Integer                     4 bytes
         *      J = JSON (future)               4 bytes - number of blocks * block size
         *      L = Logical                     "T"/"F" or space as false
         *      M = Memo                        Text
         *      N = Numeric                     Text in <FieldLenx>.<FieldDec> format
         *      O = Long                        8 byte integer (High Precision compatible)
         *      Q = Varbinary                   Text
         *      S = TimeStamp (future)          8 bytes
         *      T = DateTime                    8 bytes
         *      V = Varchar (Text/Binary)       Bytes, 0 filled right, last byte is length
         *      W = Blob                        Bytes
         *      0 = NullField (system)          Bytes
         *      1 = $Changes (system)           String "0" filled length of number of visible fields
         *                                      Exists for tables, views, and cursors
         *---------------------------------------------------------------------------------------------*/
        public class FieldInfo
        {
            public string FieldName = string.Empty;     // Actual or DBC field name
            public string TableName = string.Empty;     // Actual name in table header
            public string FieldType = string.Empty;     // xBase field type (listed above)
            public int FieldLen = 0;                    // Length of field
            public int FieldDec = 0;                    // Number of digits right of decimal
            public JAXObjects.Token EmptyValue = new(); // Actual value of empty field
            public string DefaultValue = string.Empty;  // User assigned default value
            public string Valid = string.Empty;         // User assigned validation clause
            public string ValidMessage = string.Empty;  // User assigned validation error message
            public string Comment = string.Empty;       // Comment
            public string Caption = string.Empty;       // Caption to use in Edit/Browse/Append
            public string InputMask = string.Empty;     // Input mask to use in Edit/Browse/Append
            public string Format = string.Empty;        // Format masK
            public int Displacement = 0;                // Starting location in record stream
            public int AutoIncNext = 0;                 // Next value to use for auto increment field
            public int AutoIncStep = 0;                 // Auto increment step value
            public int NullFieldCount = 0;              // Position in null field tracker
            public bool SystemColumn = false;           // Is this a system column?
            public bool NoCPTrans = false;
            public bool NullOK = false;                 // Is it ok to have a null value?
            public bool BinaryData = false;             // Is this binary data?
            public bool AutoIncrement = false;          // Is this an auto increment field
        }


        /*
         * Class to handle memo fields.
         */
        [Serializable]
        public class JAXMemo
        {
            public int Changed = 0;     // 0=not loaded, 1=loaded, 2=changed
            public int Pointer = 0;
            public string Value = string.Empty;
        }
    }



    /*
     * This is a class I'm using to tinker around with for SQL support
     */
    public class ColumnInfo
    {
        public string SqlType { get; set; } = "";
        public string DotNetType { get; set; } = "";
        public int MaxLength { get; set; } = -1;
        public bool AllowDBNull { get; set; } = true;
        public object SampleValue { get; set; } = DBNull.Value;

        public override string ToString() =>
            $"{SqlType} ({DotNetType}) {(AllowDBNull ? "NULL" : "NOT NULL")}";
    }


    public class TableHelper()
    {

        /* -----------------------------------------------------------------------------------------
         * Create a cursor from a JAXBase table description string such as:
         * "Name C(30), Address M, City C(30), State C(2), Zip C(5), Balance N(10,2)"
         * ----------------------------------------------------------------------------------------- */
        public static async Task<int> CreateCursor(string lcSQL, string cursorName)
        {
            JAXDirectDBF thisDBF = Program.CurrentApp.CurrentDS.GetWorkAreaObject(Program.CurrentApp.CurrentDS.CurrentWorkArea());
            JAXDirectDBF.DBFInfo dbfInfo = new()
            {
                FQFN = Program.CurrentApp.AppWorkFolder + Program.CurrentApp.SystemCounter()+".dbf",    // All cursors go into the work folder
                Fields = MakeFieldsFromString(lcSQL),   // Get the fields
                TableType = "C",                        // Mark it as a cursor
                Alias = cursorName                      // And finally the alias name
            };

            int result = 0;

            // Create the cursor
            if (await thisDBF.DBFCreateDBF(dbfInfo, true) == false)
                result = 1554;

            return result;
        }


        /* -----------------------------------------------------------------------------------------
         * Take a data table and create a JAXBase compatible cursor using information already in 
         * the data table.
         * 
         * WARNING #1: This is not intended to be a end-all be-all solution as JAXBase data types 
         * do not map one-to-one nor do they have the max size and attributes of SQL Server, MySQL, 
         * or PostgreSQL. You are expected to work with tables that are compatible with JAXBase
         * data types and limits.
         * 
         * WARNING #2: This is a basic implementation and does not handle the full range of data 
         * types and attributes that may be present, take into account the actual maximum range 
         * and lengths of the SQL fields, nor does it handle error conditions.
         * 
         * One glaring example is the different INT types which can be less than 4 bytes in 
         * length, but here we assume all INT types are 4 bytes, except for the 8 byte type
         * which are pushed over as double, which is a large floating point data type.
         * 
         * Another is that text types may be larger than the capacity of a memo field, but here 
         * we assume anything over 254 characters is a memo field and anything that's too large 
         * will be truncated.
         * 
         * Note: I would like to see Version 2 attempt to address most of these issues by 
         * including several new data types to match more closely with teh rrange of datatypes 
         * found in SQL Server, MySQL, PostgreSQL, Oracle, and MongoDB.
         * 
         * ----------------------------------------------------------------------------------------- */
        public static async Task MakeCursorForDataTable(DataTable dt, string alias)
        {
            int i = 0;
            AppClass app = Program.CurrentApp;

            // Ensure we have a work area
            if (app.CurrentDS.CurrentWA is null || app.CurrentDS.CurrentWA.DbfInfo.DBFStream is not null)
                app.CurrentDS.SelectWorkArea(0);

            DataRow row = dt.Rows.Count > 0 ? dt.Rows[0] : dt.NewRow();

            JAXDirectDBF.DBFInfo dbInfo = new();
            dbInfo.Alias = alias;
            dbInfo.TableName = app.SystemCounter();
            dbInfo.FQFN = JAXLib.Addbs(app.AppWorkFolder) + dbInfo.TableName + ".dbf";
            dbInfo.TableType = "C";

            List<JAXTables.FieldInfo> fieldInfo = [];

            // Interrogate the columns
            foreach (DataColumn col in dt.Columns)
            {
                string fieldName = col.ColumnName;
                string fieldType = col.DataType.Name;
                string caption = col.Caption;
                int maxLength = col.MaxLength;
                string dbType = string.Empty;
                string dnType = string.Empty;

                // I'm doing something wrong because this is the
                // only way I can get the extended properties
                if (col.ExtendedProperties["Info"] is ColumnInfo info)
                {
                    dbType = info.SqlType.ToUpper();
                    dnType = info.DotNetType;
                    maxLength = info.MaxLength;
                }

                // DBType has the info in format similar to CHAR(10)
                int[] dbparts = { 0, 0 };
                string[] dbbreak = dbType.Split('(');
                int len = 0;
                int dec = 0;

                // If there is ([,]) then get length and optional decimal parts
                if (dbbreak.Length > 1)
                {
                    dbbreak[1] = dbbreak[1].Trim(')');
                    string[] iparts = dbbreak[1].Split(',');
                    if (iparts[0].Equals("max", StringComparison.OrdinalIgnoreCase))
                        len = 9999; // Arbitrary large size for to trigger Memo type
                    else
                    {
                        if (int.TryParse(iparts[0], out len) == false) len = 0;
                        if (iparts.Length > 1 && int.TryParse(iparts[1], out dec) == false) dec = 0;
                    }
                }

                // Fix max length for UUID type
                if (dbbreak[0].Equals("UUID")) len = 36;

                // List of all supported types between MSSQL, MySQL, and PostgreSQL
                string fType = dbbreak[0] switch
                {
                    "CHAR" or "NCHAR" => "C",
                    "VARCHAR" or "NVARCHAR" => "V",
                    "TEXT" or "NTEXT" or "XML" or "JSON" or "ENUM" or "SET" or "XML" or "CITEXT" or "JSONB" => "M",
                    "DOUBLE" or "FLOAT8" or "NUMBER" or "BINARY_DOUBLE" or "DOUBLE PRECISION" or "REAL" => "B",
                    "SMALLSERIAL" or "SERIAL" or "SERIAL4" or "INTEGER" or "INT" or "MEDIUMINT" or "SMALLINT" or "TINYINT" => "I",
                    "INT8" or "BIGINT" or "BIGSERIAL" => "B",
                    "DECIMAL" or "NUMERIC" or "MONEY" or "DEC" or "FIXED" or "SMALLMONEY" or "FLOAT" or "SINGLE" => "N",
                    "BIT" or "BOOL" or "BOOLEAN" or "BIT VARYING" or "BYTEA" => "L",
                    "DATE" => "D",
                    "TIME WITHOUT TIME ZONE" or "DATETIME" or "SMALLDATETIME" or "DATETIME2" or "TIMESTAMP" => "T",
                    "BINARY" or "VARBINARY" => "Q",
                    "IMAGE" => "G",
                    "BLOB" or "CLOB" or "NCLOB" => "W",
                    "UUID" => "C",
                    _ => "C"
                };


                // Determine if binary data
                bool binaryData = dbbreak[0] switch
                {
                    "NCHAR" or "NVARCHAR" or "NTEXT" or "BINARY" or "VARBINARY" or "IMAGE" or "JSONB" => true,
                    _ => false
                };

                if ("CV".Contains(fType) && maxLength > 254)
                    fType = "M";

                len = fType switch
                {
                    "C" => len > 0 ? len : (maxLength > 0 ? maxLength : 254),
                    "V" => len > 0 ? len : (maxLength > 0 ? maxLength : 254),
                    "N" => len > 0 ? len : 18,
                    "B" => 8,
                    "L" => 1,
                    "D" => 8,
                    "T" => 8,
                    "M" or "G" or "W" => 4,
                    _ => 50
                };

                JAXTables.FieldInfo fi = new()
                {
                    FieldName = fieldName,
                    Caption = caption,
                    FieldType = fType,
                    FieldLen = len > 0 ? len : 0,
                    FieldDec = dec,
                    BinaryData = binaryData,
                    NullOK = false, // col.AllowDBNull,
                    NoCPTrans = true
                };

                dbInfo.Fields.Add(fi);
                i++;
            }

            // At this point we have the field info and are ready to create the cursor
            await app.CurrentDS.CurrentWA!.DBFCreateDBF(dbInfo, false);
            await app.CurrentDS.CurrentWA!.DBFAppendForeignRecord(dt);
            await app.CurrentDS.CurrentWA!.DBFGotoRecord("top");
        }




        /* -----------------------------------------------------------------------------------------
         * Create a List<JAXTables.FieldInfo> based on a table description string such as:
         * "Name C(30), Address M, City C(30), State C(2), Zip C(5), Balance N(10,2)"
         * ----------------------------------------------------------------------------------------- */
        public static List<JAXTables.FieldInfo> MakeFieldsFromString(string fieldString)
        {
            List<JAXTables.FieldInfo> fields = [];

            while (fieldString.Length > 0)
            {
                // get the next field definition
                fieldString = Program.CurrentApp.JaxCompiler.GetNextToken(fieldString, ",", out string fld);

                if (fld.Length > 0)
                {
                    // break up the field definition
                    fld = Program.CurrentApp.JaxCompiler.GetNextToken(fld, " ", out string fname);
                    string type = "";

                    if (fld.Length > 0)
                    {
                        type = fld[..1];

                        if (fld.Length > 1)
                            fld = fld[1..];
                        else
                            fld = "";
                    }

                    int w = 0;
                    int d = 0;

                    if (fld.Length > 0)
                    {
                        string[] c = fld.Split(',');
                        int.TryParse(c[0], out w);

                        if (c.Length > 1)
                            int.TryParse(c[1], out d);
                    }

                    JAXTables.FieldInfo f = new()
                    {
                        FieldName = fld,
                        FieldType = type,
                        FieldLen = w,
                        FieldDec = d
                    };

                    fields.Add(f);
                }
            }

            return fields;
        }
    }
}
