using Avalonia.Controls;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Monitor : XBase_Class_Avalonia
    {
        public new string MyBaseClass = "Monitor";
        public new string MyDefaultName = "monitor";
        public new bool Register = false;

        private int _monitor = 0;

        // One time set
        public int Monitor
        {
            get { return _monitor; }
            set
            {
                if (_monitor == 0)
                    _monitor = value;
            }
        }

        // This list holds the row source array followed by important related values
        public ObservableSortedDictionary<int, JAXObjects.Token> Screens = [];

        public XBase_Class_Monitor(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, MyBaseClass, string.IsNullOrWhiteSpace(name) ? MyDefaultName : name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        /* ------------------------------------------------------------------------------------------*
         * GetProperty
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            Avalonia.Platform.Screen[] screens = MonitorLib.GetAllAvailableScreens(JAXApp.MainWindowInstance);

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "height":
                        if (JAXLib.Between(Monitor, 1, screens.Length) == false)
                            returnToken.Element.Value = screens[Monitor - 1].WorkingArea.Height;
                        else
                            result = 9780;
                        break;

                    case "left":
                        if (JAXLib.Between(Monitor, 1, screens.Length) == false)
                            returnToken.Element.Value = screens[Monitor - 1].WorkingArea.TopLeft.X;
                        else
                            result = 9780;
                        break;

                    case "monitor":
                        if (JAXLib.Between(Monitor, 1, screens.Length) == false)
                            returnToken.Element.Value = Monitor;
                        else
                            returnToken.Element.Value = 0;
                        break;

                    case "primary":
                        if (JAXLib.Between(Monitor, 1, screens.Length) == false)
                            returnToken.Element.Value = screens[Monitor - 1].IsPrimary;
                        else
                            returnToken.Element.Value = false;
                        break;

                    case "scaling":
                        if (JAXLib.Between(Monitor, 1, screens.Length) == false)
                            returnToken.Element.Value = screens[Monitor - 1].Scaling * 100D;
                        else
                            result = 9780;
                        break;

                    case "top":
                        if (JAXLib.Between(Monitor, 1, screens.Length) == false)
                            returnToken.Element.Value = screens[Monitor - 1].WorkingArea.TopLeft.Y;
                        else
                            result = 9780;
                        break;

                    case "width":
                        if (JAXLib.Between(Monitor, 1, screens.Length) == false)
                            returnToken.Element.Value = screens[Monitor - 1].WorkingArea.Width;
                        else
                            result = 9780;
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /* ------------------------------------------------------------------------------------------*
         * SetProperty
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      9   - Success, do no further processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *      >0  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "monitor":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 1, MonitorLib.GetAvailableMonitorCount()))
                                    Monitor = tk.AsInt();
                                else
                                    result = 11;
                            }
                            else
                                result = 41;
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
                            break;
                    }

                    // Was the property retrieved?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                result = -1;
            }

            return result;
        }


        public override string[] JAXMethods()
        {
            return [];
        }

        public override string[] JAXEvents()
        {
            return [];
        }

        public override string[] JAXProperties()
        {
            return
            [
                "class,c!,monitor","classlibrary,c!,","comment,c,","parent,o,","parentclass,c!,","name,c,","tag,c,",
                "height,N!,0","left,N!,0","monitor,N!,0","primary,L,.F.","scaling,N!,100","top,N!,0","width,N!,0"
            ];
        }
    }
}

