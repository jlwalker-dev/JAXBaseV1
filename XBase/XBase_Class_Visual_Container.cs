using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    internal class XBase_Class_Visual_Container : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Container";
        public new string MyDefaultName { get; } = "container";


        public CustomBorder container => (CustomBorder)me.avaloniaObject!;
        public Avalonia.Controls.Canvas InnerCanvas;

        public XBase_Class_Visual_Container(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            InnerCanvas = new Avalonia.Controls.Canvas { Name = "innercanvas" };

            var bordered = new CustomBorder
            {
                BorderThickness = new Avalonia.Thickness(2),
                BorderBrush = Avalonia.Media.Brushes.Black,
                Width = 200,
                Height = 200,
                Child = InnerCanvas
            };

            SetVisualObject(bordered, "Container", "container", true, UserObject.URW);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * Add an object to the end of the objects array
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;
            string msg = value.Class;

            if (CanUseObjects && CanWriteObjects)
            {
                try
                {
                    // Add valid controls to the canvas
                    if (value.avaloniaObject is not null)
                        InnerCanvas.Children.Add(value.avaloniaObject!);
                    else if (value.nvObject is Avalonia.Controls.Shapes.Path)
                        InnerCanvas.Children.Add((Avalonia.Controls.Shapes.Path)value.nvObject!);
                    else if (value.nvObject is not null)
                    {
                        // It's something else then add it to the form's objects collection
                        UserProperties["objects"].Add(value);
                        UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                        value.SetParent(me);
                    }
                }
                catch (Exception ex)
                {
                    err = 9999;
                    msg = ex.Message;
                }
            }
            else
                err = 3019;

            if (err > 0)
            {
                _AddError(err, 0, msg, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|{msg}|{value.JOWName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
        }





        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *     >0   - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;

            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    switch (propertyName)
                    {
                        case "backcolor":
                            InnerCanvas.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tk.AsString())));
                            objValue = JAXUtilities.ReturnColorInt(tk.AsString());
                            break;

                        case "forecolor":

                            break;

                        case "picture":
                            break;

                        case "tabstop":
                            if (tk.Element.Type.Equals("L"))
                                InnerCanvas.IsTabStop = tk.AsBool();
                            else
                                result = 11;
                            break;

                        case "tabindex":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                    InnerCanvas.TabIndex = tk.AsInt();
                            }
                            else
                                result = 11;
                            break;

                        case "tooltiptext":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (string.IsNullOrWhiteSpace(tk.AsString()))
                                    Avalonia.Controls.ToolTip.SetTip(InnerCanvas, null);
                                else
                                    Avalonia.Controls.ToolTip.SetTip(InnerCanvas, tk.AsString());
                            }
                            else
                                result = 11;

                            break;

                        case "borderstyle":
                            if (tk.Element.Type.Equals("N"))
                            {
                                // Make sure it's a number between 0 and 4
                                if (JAXLib.Between(tk.AsInt(), 0, 6))
                                {
                                    objValue = tk.AsInt();
                                    container.SetBorderStyle(tk.AsInt());
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "dock":
                            if (tk.Element.Type.Equals("N"))
                            {
                                // Make sure it's an integer
                                objValue = tk.AsInt();

                                //switch (tk.AsInt())
                                //{
                                //    case 0: container.Dock = DockStyle.None; break;
                                //    case 1: container.Dock = DockStyle.Left; break;
                                //    case 2: container.Dock = DockStyle.Right; break;
                                //    case 3: container.Dock = DockStyle.Top; break;
                                //    case 4: container.Dock = DockStyle.Bottom; break;
                                //    case 5: container.Dock = DockStyle.Fill; break;
                                //    default:
                                //        objValue = UserProperties["dock"];
                                //        break;
                                //}

                                //container.Invalidate(); // in case there's a grid
                                //container.Refresh();
                            }
                            else
                                result = 11;
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                        {
                            // We processed it or just need to save the property
                            // Ignore the CA1854 as it won't put the value into the property
                            UserProperties[propertyName].Element.Value = objValue;
                        }
                        result = 0;
                    }
                }
                else
                    result = 1559;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }
            else
                result = 0;

            return result;
        }


        /*------------------------------------------------------------------------------------------*
             * GetProperty method returns 
             *      0 = Successfully returning value
             *      1 = Not processed, returning .F.
             *      
             *    >10 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                // Get the property and fill in the value
                returnToken.CopyFrom(UserProperties[propertyName]);

                switch (propertyName)
                {
                    case "borderstyle":
                        //returnToken.Element.Value = container.BorderStyle switch
                        //{
                        //    BorderStyle.FixedSingle => 1,
                        //    BorderStyle.Fixed3D => 2,
                        //    _ => 0
                        //};
                        break;

                    case "height":
                        returnToken.Element.Value = container.Height;
                        break;

                    case "width":
                        returnToken.Element.Value = container.Width;
                        break;


                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 0, 10))
                {
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;

                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty", "addobject", "move", "readexpression", "readmethod", "refresh", "resettodefault",
                "saveasclass", "settooriginalvalue", "setfocus", "writeexpression", "writemethod", "zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","dragdrop","dragover","error","gotfocus",
                "init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","visiblechanged","when"
                ];
        }

        /*------------------------------------------------------------------------------------------*
             * property data types
             *      C = Character
             *      N = Numeric         I=Integer       R=Color
             *      D = Date
             *      T = DateTime
             *      L = Logical         LY = Yes/No logical
             *      
             *      Attributes
             *          ! Protected - can't change after initialization
             *          $ Special Handling - do not auto process
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return [
                "activecontrol,N!,0","anchor,n,0",
                "backcolor,R,240|240|240","backstyle,n,1","bordercolor,R,100|100|100","borderstyle,N,0","borderwidth,N,1",
                "baseclass,C!,container",
                "class,C!,container","classlibrary,C!,","comment,C,","controlcount,N!,0",
                "dock,n,0",
                "Enabled,L,true",
                "forecolor,R,0",
                "Height,N,200",
                "keypreview,L,false",
                "left,N,0",
                "name,C,container",
                "objects,*,",
                "parent,o!,","parentclass,C!,","picture,C,",
                "tag,C,","tabindex,N,1","tabstop,L,true","top,N,0","tooltiptext,c,",
                "visible,L,true",
                "width,N,200"
                ];
        }
    }
}