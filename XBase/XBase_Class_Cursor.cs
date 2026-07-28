/*
 * 2026.06.09 - JLW
 *      Used to provide information on the header of the current cursor, table, or (in the future) a database.
 *      
 */
using JAXBase.Core;
using JAXBase.Data;

namespace JAXBase.XBase
{
    public class XBase_Class_Cursor : XBase_Avalonia
    {
        public new string MyBaseClass = "Header";
        public new string MyDefaultName = "header";

        public XBase_Class_Cursor(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, "empty", string.Empty, false, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            if (me.App.CurrentDS.CurrentWA is not null && me.App.CurrentDS.CurrentWA.DbfInfo.DBFStream is not null)
            {
                // Fill the header
                JAXDirectDBF.DBFInfo DbfInfo = me.App.CurrentDS.CurrentWA.DbfInfo;

                await SetProperty("alias", DbfInfo.Alias, 0);
                await SetProperty("fqfn", DbfInfo.FQFN, 0);
                await SetProperty("tablename", DbfInfo.TableName, 0);
                await SetProperty("tabletype", DbfInfo.TableType, 0);
                await SetProperty("connection", DbfInfo.Connection, 0);
                await SetProperty("tableref", DbfInfo.TableRef, 0);
                await SetProperty("Buffered", DbfInfo.Buffered, 0);
                await SetProperty("CodePage", DbfInfo.CodePage, 0);
                await SetProperty("DBCLink", DbfInfo.DBCLink, 0);
                await SetProperty("Exclusive", DbfInfo.Exclusive, 0);
                await SetProperty("HasMemo", DbfInfo.HasMemo, 0);
                await SetProperty("HeaderByte", DbfInfo.HeaderByte, 0);
                await SetProperty("headerlen", DbfInfo.HeaderLen, 0);
                await SetProperty("IsDBC", DbfInfo.IsDBC, 0);
                await SetProperty("LastUpdate", DbfInfo.LastUpdate, 0);
                await SetProperty("NoUpdate", DbfInfo.NoUpdate, 0);
                await SetProperty("RecordLen", DbfInfo.RecordLen, 0);

                JAXObjects.Token fields = new();
                for (int i = 1; i < DbfInfo.VisibleFields * 18; i++)
                    fields._avalue.Add(new());

                fields.Col = 18;
                fields.Row = DbfInfo.Fields.Count;
                for (int i = 0; i < DbfInfo.VisibleFields; i++)
                {
                    fields._avalue[i * 18 + 0].Value = DbfInfo.Fields[i].FieldName;
                    fields._avalue[i * 18 + 1].Value = DbfInfo.Fields[i].FieldType;
                    fields._avalue[i * 18 + 2].Value = DbfInfo.Fields[i].FieldLen;
                    fields._avalue[i * 18 + 3].Value = DbfInfo.Fields[i].FieldDec;
                    fields._avalue[i * 18 + 4].Value = DbfInfo.Fields[i].NullOK;
                    fields._avalue[i * 18 + 5].Value = DbfInfo.Fields[i].NoCPTrans;
                    fields._avalue[i * 18 + 6].Value = string.Empty;
                    fields._avalue[i * 18 + 7].Value = string.Empty;
                    fields._avalue[i * 18 + 8].Value = string.Empty;
                    fields._avalue[i * 18 + 9].Value = string.Empty;
                    fields._avalue[i * 18 + 11].Value = string.Empty;
                    fields._avalue[i * 18 + 12].Value = string.Empty;
                    fields._avalue[i * 18 + 13].Value = string.Empty;
                    fields._avalue[i * 18 + 14].Value = string.Empty;
                    fields._avalue[i * 18 + 15].Value = DbfInfo.Fields[i].Comment;
                    fields._avalue[i * 18 + 16].Value = DbfInfo.Fields[i].AutoIncNext;
                    fields._avalue[i * 18 + 17].Value = DbfInfo.Fields[i].AutoIncrement;
                }

                // Need to directly replace the token
                UserProperties["fields"] = fields;

                JAXObjects.Token idx = new();
                for (int i = 1; i < DbfInfo.IDX.Count * 6; i++)
                    idx._avalue.Add(new());

                for (int i = 0; i < DbfInfo.IDX.Count; i++)
                {
                    idx._avalue[i * 6 + 0].Value = DbfInfo.IDX[i].Name;
                    idx._avalue[i * 6 + 1].Value = DbfInfo.IDX[i].IsRegistered;
                    idx._avalue[i * 6 + 2].Value = DbfInfo.IDX[i].KeyClause;
                    idx._avalue[i * 6 + 3].Value = DbfInfo.IDX[i].ForClause;
                    idx._avalue[i * 6 + 4].Value = DbfInfo.IDX[i].Descending ? "DESCENDING" : "ASCENDING";
                    idx._avalue[i * 6 + 5].Value = DbfInfo.IDX[i].IsCandidate ? "C" : DbfInfo.IDX[i].IsUnique ? "U" : "R";
                }

                UserProperties["idx"] = idx;
            }
            else
            {
                // It's an empty header
                JAXObjects.Token emptyArray = new();
                emptyArray.TType = "A";
                UserProperties["fields"] = emptyArray;

                emptyArray = new();
                emptyArray.TType = "A";
                UserProperties["idx"] = emptyArray;
            }

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public override string[] JAXProperties()
        {
            return [
                "alias,C,",
                "FQFN,C,",
                "tablename,c,",
                "tabletype,c,T",
                "connection,c,",
                "tableref,c,",
                "Buffered,l,false",
                "CodePage,n,3",
                "DBCLink,c,",
                "Exclusive,l,false",
                "HasMemo,l,false",
                "HeaderByte,n,0",
                "HeaderLen,n,0",
                "IsDBC,l,false",
                "LastUpdate,c,",
                "NoUpdate,l,false",
                "RecordLen,n,0",
                "Fields,,",
                "IDX,,"
                ];
        }
    }
}
