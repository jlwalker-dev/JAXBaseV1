/*
 * Create a video player object
 * Uses LibVLCSharp for cross-platform compatibility.
 * 
 */
using JAXBase.Core;
using JAXBase.Utilities.Utilities;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Video : XBase_Class_Avalonia
    {
        public VideoView videoViewer => (VideoView)me.avaloniaObject!;  // Place holder
        public readonly LibVLC _libVLC = new LibVLC();
        public readonly LibVLCSharp.Shared.MediaPlayer mediaPlayer;

        public XBase_Class_Visual_Video(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            SetVisualObject(new VideoView(), "Video", "video", true, UserObject.urw);
            videoViewer.MediaPlayer = mediaPlayer;
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
            App.DebugLog("Video paused");
            await me.MethodCall("paused");
        }

        private async void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            UserProperties["status"].Element.Value = 1;
            App.DebugLog("Video playing");
            await me.MethodCall("playing");
        }

        private async void MediaPlayer_Stopped(object? sender, EventArgs e)
        {
            App.DebugLog("Video stopped");
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
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
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
                        case "playrate":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), -4, 4))
                                    mediaPlayer.SetRate(tk.AsInt());
                                else
                                    result = 41;
                            }
                            else
                                result = 11;

                            break;

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

                                        case 5: // Reverse (progressive)
                                            int rate = UserProperties["rate"].AsInt();
                                            if (rate < 0)
                                            {
                                                if (rate > -4)
                                                    rate -= 1;
                                                else
                                                    rate = -1;
                                            }
                                            else
                                                rate = -1;

                                            UserProperties["rate"].Element.Value = rate;
                                            result = 9;
                                            break;

                                        case 6: // Fast Forward (progressive)
                                            rate = UserProperties["rate"].AsInt();
                                            if (rate > 0)
                                            {
                                                if (rate < 4)
                                                    rate += 1;
                                                else
                                                    rate = 1;
                                            }
                                            else
                                                rate = 2;

                                            UserProperties["rate"].Element.Value = rate;
                                            result = 9;
                                            break;

                                        case 7: // Skip back 10 seconds
                                            if (mediaPlayer.Time > 10000)
                                                mediaPlayer.Time -= 10000;
                                            else
                                                mediaPlayer.Time = 0;

                                            if (mediaPlayer.IsPlaying == false)
                                                mediaPlayer.Play();

                                            result = 9;
                                            break;

                                        case 8: // Skip forward 30 seconds
                                            if (mediaPlayer.Length - mediaPlayer.Time > 30000)
                                                mediaPlayer.Time += 30000;
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

                        case "video":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (string.IsNullOrWhiteSpace(tk.AsString()))
                                {

                                }
                                else
                                {
                                    // Grab the file or Uri location
                                    var media = tk.AsString().Contains("http", StringComparison.OrdinalIgnoreCase) ? new Media(_libVLC, new Uri(tk.AsString())) : new Media(_libVLC, tk.AsString());

                                    if (media is not null)
                                    {
                                        mediaPlayer.Media = media;
                                        await media.Parse();

                                        UserProperties["framerate"].Element.Value = -1;
                                        UserProperties["bitrate"].Element.Value = -1;

                                        foreach (var track in media.Tracks)
                                        {
                                            if (track.TrackType == TrackType.Video)
                                            {
                                                uint num = track.Data.Video.FrameRateNum;
                                                uint den = track.Data.Video.FrameRateDen;
                                                if (den != 0)
                                                {
                                                    UserProperties["framerate"].Element.Value = num / den;
                                                    break;
                                                }
                                            }
                                        }

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
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{msg}|{propertyName}={tk.AsString()}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

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
            string msg = "";

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

                        if (returnToken.Element.IsNull()==false)
                            returnToken.Element.Value = UserProperties[propertyName].Element.Value; 
                        break;
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{msg}|{propertyName}={returnToken.AsString()}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

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
                "addproperty","move","readexpression","readmethod","refresh","resettodefault",
                "saveasclass","settooriginalvalue","setfocus","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","destroy","error","gotfocus","init","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "paused","rightclick","playing","saveaudio","savevideo","stopped","visiblechanged","when"
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
                "anchor,n,0","autosize,l,false",
                "BaseClass,C!,image","bitrate,n,0","bordercolor,R,0","borderwidth,N,0",
                "Class,C!,Grid","ClassLibrary,C!,","Comment,C,",
                "framerate,n,0",
                "Height,N,0",
                "left,N,0",
                "name,c,command",
                "parent,o!,","parentclass,C!,","picture,c,","playrate,n,1",
                "status,n,0",
                "tabstop,L!,false","tag,C,","top,N,0","tooltiptext,c,",
                "video,c,","visible,l,true",
                "width,N,10"
                ];
        }

        public override async void CleanUp(bool disposing)
        {
            await me.MethodCall("destroy");
            mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            App?.DebugLog("Disposed video components");

            base.CleanUp(disposing);
        }
    }
}
