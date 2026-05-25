/*
 * Create a sound player object
 * Uses LibVLCSharp for cross-platform compatibility.
 * 
 */
using JAXBase.Core;
using JAXBase.Utilities;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using ZXing;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Sound : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Sound";
        public new string MyDefaultName { get; } = "sound";


        public VideoView videoViewer => (VideoView)me.nvObject!;  // Place holder
        public readonly LibVLC _libVLC = new LibVLC();
        public readonly LibVLCSharp.Shared.MediaPlayer mediaPlayer;

        public XBase_Class_Visual_Sound(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            SetVisualObject(null, "sound", "sound", false, UserObject.urw);
            me.nvObject = mediaPlayer;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            ClearEvents(false);

            mediaPlayer.Stopped += MediaPlayer_Stopped;
            mediaPlayer.Paused += MediaPlayer_Paused;
            mediaPlayer.Playing += MediaPlayer_Playing;

            bool result = await base.PostInit(callBack, parameterList);
            return result;
        }

        private async void MediaPlayer_Paused(object? sender, EventArgs e)
        {
            UserProperties["status"].Element.Value = 2;
            AppIO.DebugLog("Sound player has paused");
            await me.MethodCall("paused");
        }

        private async void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            UserProperties["status"].Element.Value = 1;
            AppIO.DebugLog("Sound player is playing");
            await me.MethodCall("playing");
        }

        private async void MediaPlayer_Stopped(object? sender, EventArgs e)
        {
            AppIO.DebugLog("Sound player has stopped");
            UserProperties["status"].Element.Value = 0;
            await me.MethodCall("stopped");
        }

        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
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
         *     -1  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public new virtual async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            string msg = "";

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
                        case "status":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() >= 0)
                                {
                                    switch (tk.AsInt())
                                    {
                                        case 0: // Stop
                                            mediaPlayer.Stop();
                                            break;

                                        case 1: // Play
                                            mediaPlayer.Play();
                                            break;

                                        case 2: // Pause
                                            mediaPlayer.Pause();
                                            break;

                                        case 7: // Skip back 5 seconds
                                            if (mediaPlayer.Time > 5000)
                                                mediaPlayer.Time -= 5000;
                                            else
                                                mediaPlayer.Time = 0;

                                            if (mediaPlayer.IsPlaying == false)
                                                mediaPlayer.Play();

                                            result = 9;
                                            break;

                                        case 8: // Skip forward 15 seconds
                                            if (mediaPlayer.Length - mediaPlayer.Time > 15000)
                                                mediaPlayer.Time += 15000;
                                            else
                                                mediaPlayer.Time = mediaPlayer.Length - 2000;

                                            if (mediaPlayer.IsPlaying == false)
                                                mediaPlayer.Play();

                                            result = 9;
                                            break;

                                        case 9: // Rewind to beginning and start playing
                                            mediaPlayer.Stop();
                                            mediaPlayer.Time = 0;
                                            mediaPlayer.Play();
                                            result = 9;
                                            break;

                                        default:
                                            result = 9852;
                                            msg = $"{tk.AsInt()}";
                                            mediaPlayer.Stop();
                                            objValue = 0;
                                            break;
                                    }
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;

                            break;


                        case "time":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (mediaPlayer.Length < 0)
                                    result = 9851;
                                else
                                {
                                    if (tk.AsInt() < 0)
                                        result = 41;
                                    else
                                        mediaPlayer.Time = (tk.AsInt() < mediaPlayer.Length) ? tk.AsInt() : mediaPlayer.Length;
                                }
                            }
                            else
                                result = 11;

                            break;

                        case "sound":
                            if (tk.Element.Type.Equals("C"))
                            {
                                // Grab the file or Uri location
                                var media = tk.AsString().Contains("http", StringComparison.OrdinalIgnoreCase) ? new Media(_libVLC, tk.AsString()) : new Media(_libVLC, new Uri(tk.AsString()));

                                if (media is not null)
                                {
                                    mediaPlayer.Media = media;
                                    await media.Parse();

                                    UserProperties["bitrate"].Element.Value = -1;

                                    foreach (var track in media.Tracks)
                                    {
                                        if (track.TrackType == TrackType.Audio)
                                        {
                                            if (track.Bitrate > 0)
                                            {
                                                UserProperties["bitrate"].Element.Value = track.Bitrate;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Invalid media location
                                    result = 8950;
                                    msg = tk.AsString();
                                }
                            }
                            else
                                result = 11;

                            break;


                        // Intercept special handling of properties
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
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{msg}|{propertyName}={tk.AsString()}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

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
         *    >10 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "duration":
                        returnToken.Element.Value = mediaPlayer.Length < 0 ? 0 : System.Math.Round(Convert.ToDouble(mediaPlayer.Length) / 1000.00, 1);
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
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty","readexpression","readmethod","resettodefault",
                "saveasclass","saveaudio","writeexpression","writemethod"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "destroy","error","init","load","statuschanged"
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
            return
                [
                "BaseClass,C!,image","bitrate,n,0",
                "Class,C!,Grid","ClassLibrary,C!,","Comment,C,",
                "duration,n,0",
                "location,n,0","loop,L,.f.",
                "name,c,command",
                "parent,o!,","parentclass,C!,",
                "sound,c,","soundlevel,n,50","status,n,0",
                "tag,C,"
                ];
        }

        public override async void CleanUp(bool disposing)
        {
            await me.MethodCall("destroy");
            mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            AppIO.DebugLog("Disposed video components");

            base.CleanUp(disposing);
        }
    }
}
