/* =============================================================== *
 * JAXBase Media Registry
 * 
 * ===============================================================
 * 
 * Hisory
 * -------------
 *  2025-12-11 - JLW
 *      Start of definition.
 *      
 *      The image class and images in general have not yet
 *      defined, so I'm switching all image refernces to
 *      SixLabors imaging.
 *      
 *  2026-02-11 - JLW
 *      Converted to Avalonia imaging which has enough
 *      power to do what is needed.  SixLabors is VERY
 *      powerful, but for this purpose, that power is
 *      definitely not needed.
 *      
 *      It also greatly simplified image handling as
 *      SixLabors is not directly compatible with
 *      WinForm controls.
 *      
 * 2025-03-22 - JLW
 *      Converted code to also register Sound and 
 *      Video files in order to make it a media
 *      registry for the system.
 *      
 * ===============================================================*/

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Utilities
{
    public class JAXMediaLibrary
    {
        Dictionary<string, MediaEntry> MediaLibrary = [];
        AppClass App;

        public JAXMediaLibrary(AppClass app)
        {
            App = app;

            // Register the generic x'd out box
            Avalonia.Media.Imaging.Bitmap shape = CreateBitmapFromPath("M0 0 H100 V100 H0 Z M0 0 L100 100 M100 0 L0 100");
            MediaEntry m = new MediaEntry() { Media = shape };
            MediaLibrary.Add("*nog*", m);
            
            shape = CreateBitmapFromPath("M 0,0 L 300,0 L 300,300 L 0,300 Z M 150,150 m -100,0 a 100,100 0 1,0 200,0 a 100,100 0 1,0 -200,0");
            m = new MediaEntry() { Media = shape };
            MediaLibrary.Add("*win*", m);

            shape = CreateBitmapFromPath("M 0,0 L 300,0 L 300,300 L 0,300 Z " +     // Outer box
                    "M 150,80 " +                                                   // Skull (no lower jaw)
                    "C 80,80 50,120 60,170 " +
                    "C 60,200 80,220 100,225 " +
                    "C 110,240 130,245 150,230 " +
                    "C 170,245 190,240 200,225 " +
                    "C 220,200 240,170 240,120 " +
                    "C 250,100 220,70 150,60 Z " +
                    "M 115,140 C 115,155 125,165 140,165 C 155,165 165,155 165,140 C 165,125 155,115 140,115 C 125,115 115,125 115,140 " +  // Eye sockets
                    "M 185,140 C 185,155 195,165 210,165 C 225,165 235,155 235,140 C 235,125 225,115 210,115 C 195,115 185,125 185,140 " +
                    "M 150,170 L 140,195 L 160,195 Z " +                            // Nasal cavity
                    "M 87.5,235 L 212.5,285 M 87.5,285 L 212.5,235");               // Crossbones (X)
            m = new MediaEntry() { Media = shape };
            MediaLibrary.Add("*rgr*", m);

            // TODO - make this an asset
            RegisterImage(@"C:\ProgramData\JAXBase\JAX.ico", "*jax*", out _);
        }

        /*
         * Returns 0 for Image loaded in library
         *      1 - File not found
         *      x - Not authorized
         *    519 - Invalid image name
         *    599 - Internal error
         */
        /// <summary>
        /// Load an image from a file into the registry
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="imageName"></param>
        /// <returns>int</returns>
        public int RegisterImage(string fileName, string? imageName, out string imgName)
        {
            return RegisterMedia(fileName, "I", imageName, out imgName);
        }

        /// <summary>
        /// Load/Update a media object file into the registry
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="imageName"></param>
        /// <returns>int</returns>
        public int RegisterMedia(string fileName, string type, string? imageName, out string imgName)
        {
            int result = 0;
            string msg = "";

            imgName = JAXLib.JustStem(fileName).ToLower();
            imageName = (string.IsNullOrEmpty(imageName) ? imgName : imageName).ToLower();

            // Remove all valid characters and see if anything is left
            string testName = JAXLib.ChrTran(imgName, "abcdefghijklmnopqrstuvwxyz0123456790_", "");

            if (testName.Length > 0 || string.IsNullOrWhiteSpace(imgName) || JAXLib.Between(imgName[0], 'a', 'z') == false)
                result = 519;   // Invalid image name
            else
            {
                // Need to load image in library
                if (File.Exists(fileName) == false)
                {
                    // Make sure is not a URL or URI
                    if (fileName.Contains(':') == false)
                    {
                        // Look for the image in search path using naming conventions
                        string filePath = AppHelper.FindPathForFile(fileName);
                        fileName = filePath + AppHelper.FixFileCase(string.Empty, JAXLib.JustFName(fileName), App.CurrentDS.JaxSettings.Naming, App.CurrentDS.JaxSettings.NamingAll);
                    }
                }

                type = type.Trim().ToUpper();
                type = type[..1];
                MediaEntry? m = null;

                // Is it an Image, sound, or video file?
                if ("ISV".Contains(type))
                {
                    if (File.Exists(fileName))
                    {
                        // Found the file
                        string fileExt = JAXLib.JustExt(fileName);

                        if (string.IsNullOrWhiteSpace(type))
                        {
                            // What media type
                            if (JAXLib.InListC(fileExt, "MP3", "AAC", "WAV"))
                                type = "S";
                            else if (JAXLib.InListC(fileExt, "MP4", "MKV", "MOV", "AVI", "WMV"))
                                type = "V";
                            else
                                type = "I";
                        }


                        // Load the file into the registry
                        try
                        {
                            m = new() { FileName = fileName, Type = type[0] };

                            long fileLength = new FileInfo(fileName).Length;

                            if (fileLength <= App.CurrentDS.JaxSettings.MaxMediaSize)
                            {
                                m.SubType = "F";    // Loaded file into registry bitmap or memory stream

                                if (type == "I")
                                {
                                    // Load the image file
                                    m.Media = new Avalonia.Media.Imaging.Bitmap(fileName);
                                }
                                else
                                {
                                    // Load the sound & vido file as a memory stream
                                    m.Media = JAXUtilities.LoadFileToMemoryStream(fileName);
                                }
                            }
                            else
                            {
                                string device = "";
                                int f = fileName.IndexOf(":");
                                if (f > 0)
                                    device = fileName[..f].Trim();

                                if (string.IsNullOrWhiteSpace(device) || JAXLib.InListC(device, "http", "https") == false)
                                    m.Media = "N";  // Refencing URI (file name)


                            }

                            //if (MediaLibrary.ContainsKey(imgName))
                            //    MediaLibrary[imgName] = m;    // Replace media
                            //else
                            //    MediaLibrary.Add(imgName, m); // Add media

                            // Add or replace media
                            if (MediaLibrary.TryAdd(imageName, m) == false)
                                MediaLibrary[imageName] = m;    // Replace media
                        }
                        catch (ArgumentException ex) { result = 11; msg = ex.Message; }
                        catch (FileNotFoundException ex) { result = 1; msg = ex.Message; }
                        catch (OutOfMemoryException ex) { result = 499; msg = ex.Message; }
                        catch (Exception ex) { result = 599; msg = ex.Message; }
                    }
                    else
                    {
                        // Is it a URI?
                        string device = "";
                        int f = fileName.IndexOf(":");
                        if (f > 0)
                            device = fileName[..f].Trim();

                        m = new() { FileName = fileName, Type = type[0] };

                        if (string.IsNullOrWhiteSpace(device) || JAXLib.InListC(device, "http", "https") == false)
                            m.SubType = "L";    // Its a URL
                        else
                            m.SubType = "I";    // It's a URI

                        m.Media = fileName;
                    }

                    if (m is null)
                    {
                        result = 501;
                        msg = "Invalid media type";
                    }
                }
                else
                {
                    result = 504;
                    msg = "Unknown Media Type " + type;
                }
            }

            if (string.IsNullOrWhiteSpace(msg) == false)
            {
                AppIO.DebugLog($"RegisterImage tossed an error {result} with exception: {msg}");
            }

            return result;
        }


        /// <summary>
        /// Load/Update a media object file into the registry
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="imageName"></param>
        /// <returns>int</returns>
        public int RegisterAVBytes(byte[] byteArray, string type, string imageName)
        {
            int result = 0;
            string msg = "";

            imageName = imageName.ToLower();

            // Remove all valid characters and see if anything is left
            string testName = JAXLib.ChrTran(imageName, "abcdefghijklmnopqrstuvwxyz0123456790._-", "");

            if (testName.Length > 0 || string.IsNullOrWhiteSpace(imageName) || JAXLib.Between(imageName[0], 'a', 'z') == false)
            {
                result = 519;   // Invalid image name
                msg = $"Invalid media name {imageName}";
            }
            else
            {
                if ("SV".Contains(type))
                {

                    // Load the file into the registry
                    try
                    {
                        MediaEntry m = new() { Type = type[0], SubType = "F", Media = new MemoryStream(byteArray, writable: false) { Position = 0 } };

                        if (!MediaLibrary.TryAdd(imageName, m))
                            MediaLibrary[imageName] = m;    // Replace media
                    }
                    catch (ArgumentException ex) { result = 11; msg = ex.Message; }
                    catch (FileNotFoundException ex) { result = 1; msg = ex.Message; }
                    catch (OutOfMemoryException ex) { result = 499; msg = ex.Message; }
                    catch (Exception ex) { result = 599; msg = ex.Message; }
                }
                else
                {
                    result = 504;
                    msg = "Unsupported Media Type " + type;
                }

                if (string.IsNullOrWhiteSpace(msg) == false)
                {
                    AppIO.DebugLog($"RegisterImage raised an error {result} with message: {msg}");
                }
            }

            return result;
        }





        /*
         * Returns 0 for image no longer in library
         *    599 - Internal error
         */
        /// <summary>
        /// Remove an image from the registry
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns>int</returns>
        public int UnRegisterMedia(string imageName)
        {
            int result = 0;

            try
            {
                if (MediaLibrary.ContainsKey(imageName))
                    MediaLibrary.Remove(imageName);
            }
            catch (Exception ex)
            {
                result = 599;
                AppErrorHandling.SetError(599, $"599|{ex.Message}", "UnRegisterImage");
            }
            return result;
        }


        /// <summary>
        /// Returns true if the image exists in the library
        /// </summary>
        /// <param name="imagename"></param>
        /// <returns></returns>
        public bool HasImage(string imagename) { return HasMedia(imagename, 'I'); }

        /// <summary>
        /// Returns true if the sound media exists in the library
        /// </summary>
        /// <param name="imagename"></param>
        /// <returns></returns>
        public bool HasSound(string imagename) { return HasMedia(imagename, 'S'); }

        /// <summary>
        /// Returns true if the video media exists in the library
        /// </summary>
        /// <param name="imagename"></param>
        /// <returns></returns>
        public bool HasVideo(string imagename) { return HasMedia(imagename, 'V'); }

        /// <summary>
        /// Returns true if the specified media of type exists in the library
        /// </summary>
        /// <param name="imagename"></param>
        /// <returns></returns>
        public bool HasMedia(string imagename, char type)
        {
            imagename = imagename.ToLower();
            return MediaLibrary.ContainsKey(imagename) && MediaLibrary[imagename].Type == type && MediaLibrary[imagename].Media is not null;
        }

        /// <summary>
        /// Returns an image of specified size
        /// </summary>
        /// <param name="imagename"></param>
        /// <returns></returns>
        //public Avalonia.Media.Imaging.Bitmap Resize(IImage? source, int maxWidth, int maxHeight)
        //{
        //    if (source == null || maxWidth <= 0 || maxHeight <= 0)
        //        return null!;

        //    var original = source.Size;
        //    if (original.Width <= 0 || original.Height <= 0)
        //        return null!;

        //    double ratioX = (double)maxWidth / original.Width;
        //    double ratioY = (double)maxHeight / original.Height;
        //    double ratio = System.Math.Min(ratioX, ratioY);

        //    double newWidth = (int)(original.Width * ratio);
        //    double newHeight = (int)(original.Height * ratio);

        //    var resizedBitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)newWidth, (int)newHeight));

        //    using (var context = resizedBitmap.CreateDrawingContext())
        //    {
        //        // Clear background if needed (optional, for transparency)
        //        context.FillRectangle(Avalonia.Media.Brushes.Transparent, new Rect(0, 0, newWidth, newHeight));

        //        // Draw scaled
        //        context.DrawImage(source, new Rect(0, 0, original.Width, original.Height), new Rect(0, 0, newWidth, newHeight));
        //    }

        //    return resizedBitmap;
        //}

        public Avalonia.Media.Imaging.Bitmap Resize(IImage? source, int maxWidth, int maxHeight)
        {
            if (source == null || maxWidth <= 0 || maxHeight <= 0)
                return null!;

            // If source is already a Bitmap, use the built-in high-quality scaler
            if (source is Avalonia.Media.Imaging.Bitmap bmp)
            {
                var ratioX = (double)maxWidth / bmp.Size.Width;
                var ratioY = (double)maxHeight / bmp.Size.Height;
                var ratio = System.Math.Min(ratioX, ratioY);

                int newWidth = (int)(bmp.Size.Width * ratio);
                int newHeight = (int)(bmp.Size.Height * ratio);

                return bmp.CreateScaledBitmap(
                    new PixelSize(newWidth, newHeight),
                    BitmapInterpolationMode.HighQuality);
            }

            // Fallback for other IImage types (RenderTargetBitmap approach)
            var original = source.Size;
            double ratioX2 = (double)maxWidth / original.Width;
            double ratioY2 = (double)maxHeight / original.Height;
            double ratio2 = System.Math.Min(ratioX2, ratioY2);

            int newW = (int)(original.Width * ratio2);
            int newH = (int)(original.Height * ratio2);

            var resized = new RenderTargetBitmap(new PixelSize(newW, newH));

            using (var ctx = resized.CreateDrawingContext())
            {
                ctx.DrawImage(source,
                    new Rect(0, 0, original.Width, original.Height),   // source rect
                    new Rect(0, 0, newW, newH));                       // destination rect
            }

            return resized;
        }



        /// <summary>
        /// Returns a Avalonia IImage, if it exists, otherwise null if not found 
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        public Avalonia.Media.IImage? GetImage(string imageName, out string imgName)
        {
            imgName = JAXLib.JustStem(imageName);
            MediaEntry? m;

            if (MediaLibrary.ContainsKey(imageName.ToLower()))
                m = MediaLibrary[imageName.ToLower()];
            else
            {
                if (RegisterImage(imageName, imageName, out imgName) == 0)
                    m = MediaLibrary[imageName.ToLower()];
                else
                    m = null;
            }

            // Is there an entry and is it an image?
            if (m is null || m.Media is null || m.Type != 'I')
                m = null;

            return m is null ? null : (Avalonia.Media.IImage)m.Media!;
        }


        /// <summary>
        /// Returns a Avalonia IImage from the registry, if it exists, otherwise null if not found - if nwidth=0, no resizing. if nheight=0, resizes to nwidth x nwidth.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="nSize"></param>
        /// <returns>SixLabors.ImageSharp.Image?</returns>
        public Avalonia.Media.IImage? GetImage(string imageName, int nwidth, int nheight = 0)
        {
            Avalonia.Media.IImage? temp = null;
            nwidth = nwidth < 1 ? 0 : nwidth;
            nheight = nheight < 1 ? nwidth : nheight;
            if (MediaLibrary.ContainsKey(imageName.ToLower()))
            {
                try
                {
                    MediaEntry? m;

                    if (MediaLibrary.ContainsKey(imageName.ToLower()))
                        m = MediaLibrary[imageName.ToLower()];
                    else
                    {
                        if (RegisterImage(imageName, imageName, out string imgName) == 0)
                            m = MediaLibrary[imageName.ToLower()];
                        else
                            m = null;
                    }

                    // Is there an entry and is it an image?
                    if (m is null || m.Media is null || m.Type != 'I')
                        m = null;

                    if (m is not null)
                    {
                        temp = (Avalonia.Media.IImage)m.Media!;

                        if (nwidth > 0)
                        {
                            if (nheight == 0)
                                temp = Resize(temp, nwidth, nwidth);
                            else
                                temp = Resize(temp, nwidth, nheight);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppIO.DebugLog($"GetImage tossed an exception: {ex.Message}");
                    temp = null;
                }
            }

            return temp;
        }

        /*
         * Returns 0 for success
         *      x - File already exists
         *    502 - Image does not exist
         *      x - No access or not authorized
         *    520 - Media error
         *    599 - Internal error
         */
        public int SaveMedia(string imageName, string fileName, bool overwrite)
        {
            int result = 0;
            string msg = string.Empty;

            imageName = imageName.ToLower().Trim();
            if (MediaLibrary.ContainsKey(imageName))
            {
                try
                {
                    MediaEntry? m = MediaLibrary[imageName];
                    if (m is not null && m.Media is not null)
                    {
                        if (m.Type == 'I')
                        {
                            Avalonia.Media.IImage img = (Avalonia.Media.IImage)m.Media!;
                            var pixelSize = new PixelSize((int)System.Math.Ceiling(img.Size.Width), (int)System.Math.Ceiling(img.Size.Height));
                            Avalonia.Media.Imaging.Bitmap bm = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize, new Vector(96, 96));
                            bm.Save(fileName);
                        }
                        else if (m.Type == 'S')
                        {
                        }
                        else if (m.Type == 'V')
                        {

                        }
                        else
                        {
                            result = 520;
                        }
                    }
                    else
                        result = 520;
                }
                catch (AccessViolationException ex) { result = 2226; msg = ex.Message; }
                catch (Exception ex) { result = 599; msg = ex.Message; }

                if (string.IsNullOrWhiteSpace(msg) == false)
                    AppIO.DebugLog($"Failed to save image with error {result}: {msg}");
            }
            else
                result = 502;

            return result;
        }

        public Avalonia.Media.Imaging.Bitmap CreateBitmapFromPath(string pathData)
        {
            // Parse the SVG-like path string into a Geometry
            var geometry = Geometry.Parse(pathData);

            // Define the size of the bitmap (e.g., 100x100 based on the path coordinates)
            var size = new Avalonia.Size(100, 100);
            var pixelSize = new PixelSize((int)size.Width, (int)size.Height);
            var dpi = new Vector(96, 96);  // Standard DPI

            // Create a RenderTargetBitmap
            var bitmap = new RenderTargetBitmap(pixelSize, dpi);

            // Get the drawing context
            using var context = bitmap.CreateDrawingContext();

            // Set up a pen for drawing the outline (black stroke, thickness 1)
            var pen = new Avalonia.Media.Pen(Avalonia.Media.Brushes.Black, 1);

            // Fill the shape if desired (e.g., transparent or white background)
            context.FillRectangle(Avalonia.Media.Brushes.White, new Rect(size));  // Optional: White background

            // Draw the geometry
            context.DrawGeometry(Avalonia.Media.Brushes.Transparent, pen, geometry);  // Transparent fill, black stroke

            // Return the bitmap instead of saving
            return bitmap;
        }
    }
}
