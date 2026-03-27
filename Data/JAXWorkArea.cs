// TODO - Make sure I wasn't drunk when I wrote this code
//
using JAXBase.Core;

namespace JAXBase.Data
{
    public class JAXWorkArea : IDisposable
    {
        AppClass App;

        int OrigDS = 0;
        int OrigWA = 0;
        int OrigRec = 0;
        int OrigIDX = 0;

        JAXDirectDBF.DBFInfo? OrigDBF = null;
        JAXDirectDBF.DBFInfo? CurrDBF = null;

        bool Back2DS = true;
        bool Back2WA = true;
        bool Back2Rec = true;
        bool Back2IDX = true;

        /*
         * session
         *      -1 = stay in current
         *       0 = go to lowest available
         *       1+= go to this datasession
         *       
         * wa
         *      string = go to work area with this alias
         *      -1     = stay in current
         *       0     = go to lowest available
         *       1+    = go to this work area
         */
        public JAXWorkArea(AppClass app, int session, object wa, bool back2DS, bool back2WA, bool back2Rec, bool back2IDX)
        {
            App = app;

            // Get the current datasession and workarea
            OrigDS = App.CurrentDataSession;
            OrigWA = App.CurrentDS.CurrentWorkArea();

            // If a table is open, get it's current recno and controlling index
            if (App.CurrentDS.CurrentWA is not null)
            {
                OrigDBF = App.CurrentDS.CurrentWA.DbfInfo;

                if (OrigDBF.DBFStream is not null)
                {
                    OrigRec = OrigDBF.RecNo;
                    OrigIDX = OrigDBF.ControllingIDX;
                }
            }

            Back2DS = back2DS;
            Back2WA = back2WA;
            Back2Rec = back2Rec;
            Back2IDX = back2IDX;

            if (session == 0)
            {
                // Create a new datasession
                App.CreateNewDataSession(string.Empty);
            }
            else if (session > 0)
            {
                // Go to this datasession
                if (App.jaxDataSession.ContainsKey(session))
                    App.SetDataSession(session);
                else
                    throw new Exception("");
            }

            if (wa.GetType() == typeof(string))
            {
                string a = (string)wa;

                // Go to the work area with this allias
                App.CurrentDS.SelectWorkArea(a);
            }
            else if (wa.GetType() == typeof(int))
            {
                int w = (int)wa;

                if (w > 0)
                {
                    // Go to this work area
                    if (App.CurrentDS.WorkAreas.ContainsKey(w) == false)
                        App.CurrentDS.WorkAreas.Add(w, new(app));
                }
                else if (w == 0)
                {
                    // go to lowest open workarea
                    int i = 1;
                    while (App.CurrentDS.WorkAreas.ContainsKey(i) && App.CurrentDS.WorkAreas[i].DbfInfo.DBFStream is not null) i++;

                    if (App.CurrentDS.WorkAreas.ContainsKey(i) == false)
                        App.CurrentDS.WorkAreas.Add(i, new(app));

                    App.CurrentDS.SelectWorkArea(i);
                }

                if (App.CurrentDS.CurrentWA is not null)
                    CurrDBF = App.CurrentDS.CurrentWA.DbfInfo;
            }
            else
                throw new Exception("");
        }

        // Fix things up when this class is destroyed
        public void Dispose()
        {
            int curds = App.CurrentDataSession;
            int curdb = App.CurrentDS.CurrentWorkArea();

            App.SetDataSession(OrigDS);
            App.CurrentDS.SelectWorkArea(OrigWA);

            if (Back2DS)
                App.SetDataSession(OrigDS);
            else
                App.SetDataSession(curds);

            App.CurrentDS.SelectWorkArea(curdb);
        }
    }
}
