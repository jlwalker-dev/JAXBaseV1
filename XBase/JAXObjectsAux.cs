/******************************************************************************************************************************************
 * This code is used to support the JAXObjects class by removing single use and
 * specialty routines that take up a lot of room.
 *
 * In order to really emulate JAX, I fear we'd need a 16 core/32 thread processor just to keep it from
 * being useless because of performance issues.  C++ would have been a better choice, but I have no clue
 * on that language.  A JavaScript version could do a lot better, but I think there would still be
 * lots of limitations and shoutcuts.
 *
 *
 * 2024-07-11 - Jon Walker
 *      Added full support for "empty", "custom", "collection", "container", and "form"
 *      Will need new routines to support user created classes (if I go there) and
 *      a lot of work if I try to emulate most objects (unlikely at this time).
 *      I just wanted to set up the basics here in case I get bored and want to 
 *      try expanding on some of the base classes.
 *
 * 2024-07-19 - JLW
 *      Discovered I needed to implement an Interface so that I could reference the Get/Set/Add/Remove
 *      property methods in order to use the classes correctly.  Completely gutted the original object
 *      handler and the speed/memory differences should be significant, though at the cost of a lot
 *      more source code.
 *
 *
 * 2024-07-21 - JLW
 *
 *      Refactored the interface and classes to be passing back JAXObjects.Token objects instead
 *      of object.  That way it's more in line with how the variable class works.
 *
 *      Updated the timer to interface with APP timer methods.  Still not sure it will work.
 *      And even if it does, I then will need to go back and make everything thread safe. Ugh!
 *      I may need some realy good C# help on this.
 *
 *      Also realized that I may need to go back and refactor everything again, because the properties
 *      may actually need to be made into JAXObjects.Token instead of their base class like int, string,
 *      bool, etc.  Time will tell, but I think it will be cleaner if I don't have to do that.
 *      
 * 2024-08-09 - JLW
 *      Base properties that are of a data type will remain as they are.  Some properties, like Value,
 *      can be any type and therefore must be Tokens.  Easiest way to handle those is to make them
 *      part of the dictionary and not worry (right now) if anytone tries to remove them.
 *      
 *      Added JAXEditBox & JAXCommandGroup
 *      
 *      
 * 2025-01-19 - JLW
 *      Formalized DOS properties for controls.  Since the DOS version does not actually treat controls
 *      as actual objects that you can access, I've added properties that begin with "DOS" and are
 *      only created and used for the DOS version of these controls.
 *      
 *      By treating them as objects, it unifies the system between DOS and modern applications and 
 *      requires fewer code changes between the legacy and modern environments.
 * 
 * 2025-05-04 - JLW
 *      Came up with a new object:  BrowseWindow
 *      A BrowseWindow will look and act like an XBase browse window but will be addressable and
 *      changeable via code just like a JAX programmer would expect.
 *      
 * 2025-05-05 - JLW
 *      Splitting the various classes into separate xClass_<object>.cs files because I'm pretty sure
 *      things are going to start getting more detailed and I'll want to not have so much code in
 *      any one file.
 *      
 *      Also decided it's better if the .Net classes that match the xBase classes are part of the
 *      class and hook things directly into that.  May also need to rethink my method handling
 *      code, but will figure tht out as I go along.
 *      
 * 2025-05-12
 *      I've started a major overhaul of the xClass classes.  There are no more long lists of
 *      property variables.  EVERYTHING except the actual parent reference will either go into
 *      a UserProperty[] entry or into the MyObj object (an acutal .Net instance of the class).
 *      
 *      I've created an XClass_AuxCode.cs helper utility to convert color values, anchoring, etc.
 *      
 *      I also updated the JAXObjects to allow that a token be limited to one specific datatype
 *      at startup.  That way the UserProperties can hold the properties of the class and
 *      not have to worry about the wront data type from being used.
 *      
 *      Once I have the code back up to working order, I'm going to tackle the PageFrame, Page,
 *      image, and hyperlink classes.  That will leave Line, shape, and separator if I'm not
 *      mistaken.  I'm sure I'm going to add Line and Shape, but they may get folded into
 *      the xDrawing class I'm contemplating.
 *      
 *      Separator is a space between radio/command buttons and I'm just not sure what I'll
 *      do about it yet.  Need to do more reading.
 *      
 * 2025-11-10 - JLW
 *      I've got the basics for web classes but need to wire Grok's code into
 *      the system.  I don't know how well Grok's code will work, but the big
 *      thing is that I have an idea of the methods, events, and properties 
 *      that I'll need, at a minimum.  I may have to look over the docs for
 *      for WestWind and VFPX for ideas on what I need to expand.
 *      
 *      I'm also trying to set it up so that the TCP and UDP are the base
 *      classes and everything else inherits one of those classes.
 *      
 *      My plan is to have a basic web tool kit, TCP, UDP, HTTP(S), FTP
 *      (including secure connections), IRC, POP3, SMTP, and eventually
 *      toss in SMS for the cherry on top.
 *      
 *      I'm also considering something like NOSTR for peer-to-peer.  But
 *      if I did that, I'd also need to include public/private keys to 
 *      prevent hackers from man-in-the-middle attacks and other such
 *      crap.  I've got a ways to go before I decide that's a good idea.
 *      
 *      
 * 2025-11-11 - JLW - Thank you Veterans!
 *      I was going to join the Air Force right after I graduated high
 *      school in '79, but President Carter took away the college tuition 
 *      benefit, which is really the only reason I planned to join.
 *      
 *      During my first semester at UW Stevens Point, I discoverd that
 *      I was pretty good at programming the Burroughs 6900 using cards 
 *      and Dec-Writers, creating Fortran, Cobol, and BASIC programs.
 *      By the end of my second semester, I was showing upper-class 
 *      programmers how to fix or improve their code.
 *      
 *      The next year, UWSP created a micro-computer lab full of 
 *      Apple ][+ computers with two floppy drives.  They also hired
 *      me on as a lab assistant, helping people with issues and
 *      operating the main-frame (which had 128k RAM and this thing
 *      called a hard drive the size of large steamer trunk).
 *      
 *      The next year I went to UW Eau Claire which was a big mistake,
 *      because they were all about making everyone into good little
 *      corporate workers.  During this time, I started selling my 
 *      programming skills to local companies.  Since my grades were 
 *      not great and I really didn't like the culture there, I left
 *      the UWEC and struck out on my own.
 *      
 *      These events set me down the path of lone-wolf developer 
 *      instead of the corporate worker-bees that many of my 
 *      contemporaries became.  Shortly after leaving, I discovered
 *      DBase II and in '86, FoxBase+ helped change my earning 
 *      potential dramatically to the upside.  From there on it
 *      was near-constant employment and one hell of a fun ride.
 *      
 *      OK, I'm wool-gathering as my dad would say.  Back to business.
 *      
 *      ----
 *      
 *      Grok just taught me about a factory method that will cause a class
 *      initialization to return a null if something goes wrong during 
 *      instantiation.  As I said, I'm not a C# developer.  I'm tempted to
 *      drop everything and redo my classes (AGAIN!), but Grok said that
 *      just tossing and catching an exception is a common practice, even
 *      though the Create() method strikes me as being way more elegant.
 *      
 *      I guess this will just be a lesson to remember for for Version 2.
 *      
 *      According to Grok and other sources, my idea of  using Libre Office
 *      for report work is definitely possible in a cross platform way. So
 *      that means the Report class is probably going to work out like I
 *      had hoped.
 *      
 *      My plan for the Printer class is going to be pretty much limited to:
 *          1. Showing what printers are available
 *          2. Allowing user to select the default printer
 *          3. Sending strings out to the printer for debug and the like
 *          4. Basic control (paper size, detect if on-line, paper-out, etc)
 *          
 *      However, the SQL class is next.  First stop is basic connection,
 *      data retrieval, and the start of multi-engine support.  I've got
 *      a sweet VFP class I developed some time ago that works well with
 *      both MySQL and SQL Server.  I'm going to use that model to
 *      support both in JAXBase and make it pretty straight forward to 
 *      add more.  You can read more of my mad scientist thinking in the
 *      SQL class code.
 *      
 * 2026-02-15 - JLW
 *      I've begun work on moving all visual classes over to Avalonia
 *      so that I can have chance compiling the project on a Linux box.
 *      
 *      
 ******************************************************************************************************************************************/
using JAXBase.Core;

namespace JAXBase.XBase
{
    public class JAXObjectsAux
    {
        /*-------------------------------------------------------------------------------------------------*
         * Here is where you define what class you want to use
         *-------------------------------------------------------------------------------------------------*/
        public static IJAXAvaClass? GetClass(JAXObjectWrapper jow, string className, string name)
        {
            IJAXAvaClass? stoken = null;

            try
            {
                // TODO - How do we deal with a class that fails to initialize properly
                stoken = className.ToLower() switch
                {
                    "barcode" => new XBase_Class_Visual_BarCode(jow, name),
                    //"browser" => new XBase_Class_Visual_FormBrowser(jow, name),
                    "checkbox" => new XBase_Class_Visual_CheckBox(jow, name),
                    "codebox" => new XBase_Class_Visual_CodeBox(jow, name),
                    "collection" => new XBase_Class_Collection(jow, name),
                    "column" => new XBase_Class_Visual_Column(jow, name),
                    "combobox" => new XBase_Class_Visual_ComboBox(jow, name),
                    "commandbutton" => new XBase_Class_Visual_CommandButton(jow, name),
                    "commandgroup" => new XBase_Class_Visual_CommandGroup(jow, name),
                    "container" => new XBase_Class_Visual_Container(jow, name),
                    //"cursor" => new XClass_Cursor(jow, name),
                    "custom" => new XBase_Class_Custom(jow, name),
                    "editbox" => new XBase_Class_Visual_EditBox(jow, name),
                    "empty" => new XBase_Class_Empty(jow, name),
                    "file" => new XBase_Class_File(jow, name),
                    "form" => new XBase_Class_Visual_Form(jow, name),
                    "formset" => new XBase_Class_Formset(jow, name),
                    "ftp" => new XBase_Class_FTPClient(jow, name),
                    "grid" => new XBase_Class_Visual_Grid(jow, name),
                    "httpclient" => new XBase_Class_HTTPClient(jow, name),
                    "image" => new XBase_Class_Visual_Image(jow, name),
                    "jax" => new XBase_Class_JAX(jow, name),
                    "jaxedit" => new XBase_Class_Visual_JAXEdit(jow, name),
                    "ircclient" => new XBase_Class_IRCClient(jow, name),
                    "label" => new XBase_Class_Visual_Label(jow, name),
                    "line" => new XBase_Class_Visual_Line(jow, name),
                    "listbox" => new XBase_Class_Visual_ListBox(jow, name),
                    "menu" => new XBase_Class_Visual_Menu(jow, name),
                    "menuitem" => new XBase_Class_Visual_MenuItem(jow, name),
                    "mqttclient" => new XBase_Class_MQTTClient(jow, name),
                    "nostrclient" => new XBase_Class_NOSTRClient(jow, name),
                    "optionbutton" => new XBase_Class_Visual_OptionButton(jow, name),
                    "optiongroup" => new XBase_Class_Visual_OptionGroup(jow, name),
                    "page" => new XBase_Class_Visual_Page(jow, name),
                    "pageframe" => new XBase_Class_Visual_PageFrame(jow, name),
                    //"pgp" => new XBase_Class_PGP(jow, name),
                    //"pipe" => new XClass_Pipe(jow, name),
                    "pop3" => new XBase_Class_POP3(jow, name),
                    //"printer" => new XClass_Printer(jow, name),
                    "screen" => new XBase_Class_Screen(jow, name),
                    "separator" => new XBase_Class_Visual_Separator(jow, name),
                    "shape" => new XBase_Class_Visual_Shape(jow, name),
                    //"sms" => new XClass_SMS(jow, name),
                    "smtp" => new XBase_Class_SMTP(jow, name),
                    "sound" => new XBase_Class_Visual_Sound(jow, name),
                    "spinner" => new XBase_Class_Visual_Spinner(jow, name),
                    "sql" => new XBase_Class_SQL(jow, name),
                    "tcpclient" => new XBase_Class_TCPClient(jow, name),
                    "tcpserver" => new XBase_Class_TCPServer(jow, name),
                    "textbox" => new XBase_Class_Visual_TextBox(jow, name),
                    //"timer" => new XClass_Timer(jow, name),
                    "toolbar" => new XBase_Class_Visual_ToolBar(jow, name),
                    "toolbutton" => new XBase_Class_Visual_ToolButton(jow, name),
                    "udp" => new XBase_Class_UDPClient(jow, name),
                    "video" => new XBase_Class_Visual_Video(jow, name),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                // Something went wrong during initialization.  If it's not a JAX exception, make
                // it into a 1902 and include the original exception information for the log file.
                string msg = ex.Message.Contains('|') ? ex.Message : $"1902|{className}|{ex.Message}";
                AppErrorHandling.SetError(1901, msg, $"JAXObjectsAux.GetClass() for {className} instantiation");
            }

            return stoken;
        }

        /*-------------------------------------------------------------------------------------------------*
         * Tie in method support
         * Define the method class, and then define parameters that are sent to the
         * method when it's called.  If a user adds parameters, then that's supported through
         * the standard process, however, these are required parameters.
         *-------------------------------------------------------------------------------------------------*/
        public class MethodClass
        {
            public string PrgCall = string.Empty;
            public string CompiledCode = string.Empty;
            public List<string> ParameterList = [];
            public string Comment = string.Empty;
            public string Type = string.Empty;  // [M]ethod or [E]vent
            public string Tag = "U";            // Used to mark as [U]ser (default), [N]ative, or [I]nherited Native, In[H]erited User
            public bool Protected = false;
            public bool Hidden = false;
            public bool Inherited = false;
            public bool Changed = false;
        }

        public class MemberList
        {
            public string Name = string.Empty;
            public string Type = string.Empty;
        }


        public static MethodClass GetMethod(string method)
        {
            MethodClass mc = new();

            switch (method.ToLower())
            {
                case "additem": mc = new MethodClass { ParameterList = ["cItem", "nIndex", "nColumn"] }; break;
                case "addilisttem": mc = new MethodClass { ParameterList = ["cItem", "nItemID", "nColumn"] }; break;
                case "addproperty": mc = new MethodClass { ParameterList = ["cPropertyName", "eNewValue", "nVisibility", "cDescription"] }; break;
                case "addobject": mc = new MethodClass { ParameterList = [] }; break;
                case "error": mc = new MethodClass { ParameterList = ["nError", "cMethod", "nLine"] }; break;
                case "mouseenter": mc = new MethodClass { ParameterList = ["nButton", "nShift", "nXCoord", "nYCoord"] }; break;
                case "mouseleave": mc = new MethodClass { ParameterList = ["nButton", "nShift", "nXCoord", "nYCoord"] }; break;
                case "move": mc = new MethodClass { ParameterList = ["nLeft", "nTop", "nWidth", "nHeight"] }; break;
                case "removeobject": mc = new MethodClass { ParameterList = ["cObjectName"] }; break;
                case "saveasclass": mc = new MethodClass { ParameterList = ["ClassLibName", "ClassName", "Description"] }; break;
                case "writeexpression": mc = new MethodClass { ParameterList = ["cPropertyName", "cExpression"] }; break;
                case "writemethod": mc = new MethodClass { ParameterList = ["cMethodName", "cMethodText", "lCreateMethod", "nVisibility", "cDescription"] }; break;
                case "zorder": mc = new MethodClass { ParameterList = ["nZorder"] }; break;
            }

            return mc;
        }

    }
}

