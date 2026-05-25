using JAXBase.Core;
using JAXBase.Utilities;
using System.Runtime.InteropServices;
using ZXing;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_BarCode : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "BarCode";
        public new string MyDefaultName { get; } = "barcode";

        public Avalonia.Controls.Image img => (Avalonia.Controls.Image)me.avaloniaObject!;

        public XBase_Class_Visual_BarCode(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Image(), "Barcode", "barcode", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            img.Stretch = Avalonia.Media.Stretch.Uniform;
            img.IsTabStop = false;
            img.TabIndex = 0;

            if (Program.CurrentApp.JaxImages is not null)
                img.Source = Program.CurrentApp.JaxImages.GetImage("*nog*", out _);

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Was not found - not yet processed
         *      2   - Requires special handling, did not process
         *      3   - Not a class property
         *      9   - Processed and saved, do not do anything else
         *      10  - Processed and saved
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

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    JAXObjects.Token tk = new();
                    tk.Element.Value = objValue;

                    switch (propertyName)
                    {
                        case "forecolor":
                        case "backcolor":
                            if (tk.Element.Type.Equals("N"))
                            {
                                UserProperties[propertyName].Element.Value = tk.AsInt();
                                RenderBarCode();
                                result = 9;
                            }
                            else
                                result = 11;
                            break;

                        case "picture":
                            if (tk.Element.Type.Equals("C"))
                            {
                                string fName = tk.AsString();
                                if (string.IsNullOrWhiteSpace(fName) == false)
                                {
                                    if (File.Exists(fName))
                                    {
                                        img.Source = new Avalonia.Media.Imaging.Bitmap(fName);
                                        DecodeBarCode();
                                    }
                                    else
                                        result = 1;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "symbology":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 0, 16))
                                {
                                    UserProperties[propertyName].Element.Value = tk.AsInt();
                                    RenderBarCode();
                                    result = 9;
                                }
                                else
                                    result = 41;
                            }
                            break;

                        case "value":
                            UserProperties[propertyName].Element.Value = tk.AsString();
                            RenderBarCode();
                            result = 9;
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;  // 0 -> 9 = successfully processed.  Don't do anything else!
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
                else
                    result = 1559;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * GetProperty method returns 
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    default:
                        // Process standard properties
                        returnToken =await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }

            return returnToken;
        }



       
        private bool RenderBarCode()
        {
            bool result = true;
            string text = UserProperties["value"].AsString();
            if (string.IsNullOrEmpty(text))
                return false;

            int sym = UserProperties["symbology"].AsInt();
            bool OneD = sym < 10;
            int imgW = UserProperties["imagewidth"].AsInt() < 150 ? 300 : UserProperties["imagewidth"].AsInt();

            ZXing.BarcodeFormat format = sym switch
            {
                1 => ZXing.BarcodeFormat.CODE_93,
                2 => ZXing.BarcodeFormat.CODE_128,
                3 => ZXing.BarcodeFormat.ITF,
                4 => ZXing.BarcodeFormat.EAN_8,
                5 => ZXing.BarcodeFormat.EAN_13,
                6 => ZXing.BarcodeFormat.MSI,
                7 => ZXing.BarcodeFormat.UPC_A,
                8 => ZXing.BarcodeFormat.UPC_E,
                9 => ZXing.BarcodeFormat.CODABAR,
                10 => ZXing.BarcodeFormat.AZTEC,
                11 => ZXing.BarcodeFormat.DATA_MATRIX,
                12 => ZXing.BarcodeFormat.PDF_417,
                13 => ZXing.BarcodeFormat.QR_CODE,
                14 => ZXing.BarcodeFormat.MAXICODE,
                15 => ZXing.BarcodeFormat.PHARMA_CODE,
                16 => ZXing.BarcodeFormat.RSS_14,
                _ => ZXing.BarcodeFormat.CODE_39
            };


            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = format,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = imgW,
                    Height = OneD ? imgW/4 : imgW
                }
            };

            try
            {
                var pixelData = writer.Write(text);

                // Get custom colors (ARGB format)
                uint foreArgb = ConvertToARGB(UserProperties["forecolor"].AsInt()); // Default black
                uint backArgb = ConvertToARGB(UserProperties["backcolor"].AsInt()); // Default white

                // Post-process pixels to replace default colors (ARGB order)
                // Post-process pixels to replace default colors (ARGB order)
                byte[] src = pixelData.Pixels;
                for (int i = 0; i < src.Length; i += 4)
                {
                    // Extract current ARGB as uint
                    uint currentArgb = ((uint)src[i] << 24) | ((uint)src[i + 1] << 16) | ((uint)src[i + 2] << 8) | (uint)src[i + 3];

                    uint newArgb;
                    if (currentArgb == 0xFFFFFFFFU)
                    {
                        // New background color
                        newArgb = backArgb;
                    }
                    else
                    {
                        // New foreground color
                        newArgb = foreArgb;
                    }

                    // Set new ARGB bytes
                    src[i] = (byte)((newArgb >> 24) & 0xFF);     // A
                    src[i + 1] = (byte)((newArgb >> 16) & 0xFF); // R
                    src[i + 2] = (byte)((newArgb >> 8) & 0xFF);  // G
                    src[i + 3] = (byte)(newArgb & 0xFF);         // B
                }

                // Create Avalonia WriteableBitmap
                var bitmap = new Avalonia.Media.Imaging.WriteableBitmap(
                    new Avalonia.PixelSize(pixelData.Width, pixelData.Height),
                    new Avalonia.Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Opaque);

                // Convert ARGB to BGRA (B, G, R, A)
                using (var framebuffer = bitmap.Lock())
                {
                    byte[] dst = new byte[src.Length];
                    for (int i = 0; i < src.Length; i += 4)
                    {
                        dst[i] = src[i + 3];     // B
                        dst[i + 1] = src[i + 2]; // G
                        dst[i + 2] = src[i + 1]; // R
                        dst[i + 3] = src[i];     // A
                    }
                    System.Runtime.InteropServices.Marshal.Copy(dst, 0, framebuffer.Address, dst.Length);
                }

                // Save to memory stream and load as immutable Bitmap
                using (var ms = new System.IO.MemoryStream())
                {
                    bitmap.Save(ms); // Defaults to PNG for streams
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    img.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                }
                bitmap.Save(@"C:\jax\test");
                img.InvalidateVisual(); // Force redraw
            }
            catch (Exception ex)
            {
                result = false;
                AppErrorHandling.SetError(1525, $"{1525}|{ex.Message}|{text}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            return result;
        }

        private bool DecodeBarCode()
        {
            bool result = true;

            var bitmap = img.Source as Avalonia.Media.Imaging.WriteableBitmap;

            if (bitmap == null)
            {
                UserProperties["value"].Element.Value = "";
                return false;
            }

            using (var fb = bitmap.Lock())
            {
                int width = fb.Size.Width;
                int height = fb.Size.Height;
                byte[] pixels = new byte[width * height * 4];
                byte[] rowBuffer = new byte[width * 4];

                for (int y = 0; y < height; y++)
                {
                    IntPtr srcRow = fb.Address + y * fb.RowBytes;
                    Marshal.Copy(srcRow, rowBuffer, 0, rowBuffer.Length);
                    Buffer.BlockCopy(rowBuffer, 0, pixels, y * rowBuffer.Length, rowBuffer.Length);
                }

                var luminance = new ZXing.RGBLuminanceSource(pixels, width, height, ZXing.RGBLuminanceSource.BitmapFormat.BGRA32);
                var reader = new BarcodeReaderGeneric();
                var bctext = reader.Decode(luminance);

                if (bctext is null)
                    UserProperties["value"].Element.Value = "";
                else
                    UserProperties["value"].Element.Value = bctext;
            }


            return result;
        }


        private uint ConvertToARGB(int rgb)
        {
            // Assume rgb is 0xRRGGBB, add opaque alpha (0xFF)
            return unchecked(0xFF000000 | (uint)rgb);
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty","move","readexpression","readmethod","refresh","resettodefault",
                "saveasclass","setfocus","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","doubleclick","destroy","error","gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","valid","visiblechanged","when"
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
         *          ! Protected - read only unless you go direct 
         *              (eg UserProperties[propertyName].Element.Value = objValue;)
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return
                [
                "anchor,n,0",
                "backcolor,R,255|255|255","BaseClass,C!,Barcode","bordercolor,R,0","borderwidth,N,0",
                "Class,C!,Image","ClassLibrary,C!,","Comment,C,",
                "forecolor,R,0",
                "height,N,300",
                "imagewidth,n,600",
                "left,N,0",
                "name,c,barcode",
                "parent,o!,","parentclass,C!,","picture,c,",
                "symbology,n,0",
                "tag,C,","top,N,0","tooltiptext,c,",
                "value,c,","visible,l,true",
                "width,N,300"
                ];
        }
    }
}

