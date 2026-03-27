/* ----------------------------------------------------------------------------------------------------------*
 * WELCOME TO JAXBase (Just Another XBase) - Proof of Concept Version
 * When FoxPro was purchased by Microsoft, everyone said that was the end.  While I agreed
 * with that consensous, I disagreed with the timeline.  Most thought that Microsoft
 * would just toss it and be done.  However, I figured MS would milk it for everything
 * that it was worth and then drop it.  While they cut it off a bit earlier than I would
 * have expected, I'm sure it made perfect business sense for them.
 * However, over 15 years after passed since they announced the end of VFP and it's still
 * in use (wide use in some countries).  I myself thought VFP was an amazing tool and
 * still work with it and make a very good living producing VFP code for people.  However, my
 * hope that someone else would come along and create a widely used VFP replacement is now 
 * officially dashed and I'm forced to try the feat myself.  I don't expect to succeed 
 * completely, but I do hope to start something that gets others interested in creating 
 * something even more powerful.
 * Since I haven't worked with C in over 25 years and have never gotten into C++, I'm 
 * forced to use C# to lessen the learning curve.  I would be crushed if I spent the 
 * next 3 years relearning C to the level that would allow me to match my very modest 
 * C# skill level and then discovered some flaw in my design and realized I could not 
 * continue. Thus, I turn to C#.  If I realize that C# can't do something and C can, 
 * then that will be OK, but I don't want to compound the learning curve by relearning 
 * C and working on this at the same time.
 * So far, my experience has been positive and the speed is not bad.  However, the only 
 * way I'll consider C++ is if someone takes intererest and hires me to lead a team.  
 * C++ is at least one level of difficulty more than I want to deal with at this
 * stage of my life.  At this writing, I've got 6 years left before retirement, and 
 * after that, I want to go fishing.  So I really don't have the time to learn the
 * syntax and ins and outs of C++ just to get this going.
 * JAXBase will be designed to be an independantly developed XBase IDE and compiler
 * with an eventual true compiler. 
 * Ideas for Language Expansion in the C version
 *      - Better graphics commands
 *      - Better graphic file support
 *      - I'd like to include direct PDF manipulation
 *      - Built in encryption
 * History
 * ----------------------------------------------------------------------------------------------------------*
 *  2024-01-05 - JLW
 *      I have an initial design for a VFP to C# conversion program.  I began writing
 *      some code off the cuff and realize that I can create a way to emulate the
 *      XBase variable and VFP object handling.
 *  2024-05-01 - JLW
 *      I developed a math parser back in the 90's based on an article that I 
 *      read in one of the popular computer magazines.  The only thing I really got
 *      out of it was the concept of converting a human readable math statement into
 *      a RPN List<string> so that you have something that a computer can use.  I'm
 *      expanding it to support common xBase language features (I only had about 30 
 *      of the more common functions coded).
 *  2024-07-01 - JLW
 *      I have the beginnings of a working system.  No table support yet, but I can
 *      definitely see a way to make this all work.  I don't understand why this
 *      hasn't been done already.
 *  2024-10-01 - JLW
 *      I have the basics of data table support used from QB code that I wrote back
 *      in the last century.  My indexing code was poorly designed and I need to
 *      work on that.
 *  2025-01-04 - JLW
 *      I have the basics of indexing started and I feel good about the design.  However, I
 *      now have to take a break and deal with a legal matter between me and my employer.
 *  2025-04-01 - JLW
 *      The legal matter is resolved.  My employer will receive monthly copies of this and
 *      related projects and it is still my property.  They can do what they want with 
 *      it, as can I.  
 *      I now believe I can successfully write a XBase language.  
 *      The first time ended in failure because I could not crack the variable issue, and 
 *      it was slow because it was directly interpreting the source code.  I tried to
 *      create a p-code compiler, but that project ended in failure. Now, 25 years later, I've 
 *      learned a lot more and can clearly see a path to make it work.
 *      I really wish I had started this 10 years ago when I first thought about trying it again.
 *  2025-04-07 - JLW
 *      Added the last big thing for number compatibility: decimal places being
 *      set like many xBase flavors.  Added a Dec field to the simple token class, so 
 *      the math and related routines will need to update everything appropriately as 
 *      numbers are manipulated in memory.
 *  2025-05-12 - JLW
 *      I've decided that when I get this project to a place where I can run separate compiled
 *      files (rather than a compiled APP), which will include tables, a form with a grid, textboxes,
 *      labels, checkboxes, an editbox, and buttons that I will likely call a halt and begin demonstrating
 *      it to people as a Proof of Concept.  I can't afford to take the time to learn C++, so C# will be the 
 *      target language of Version 1.0
 *      At the pace I'm going, I'm thinking by end of summer I will have a pretty polished POC with language
 *      documentation.  Then things will get interesting.  If I completely misread the market, then this will 
 *      be a labor of love and a way to keep my higher-level cognative abilities for as long as possible.
 *  2025-06-01 - JLW
 *      I've got the compiler and executor components working and have the ability to run a few very simple
 *      programs that don't do much.  The code base is huge for the compiler, but the executer looks to
 *      be reasonable as it's just handling tockenized strings.
 *  2025-07-15 - JLW
 *      I'm an idiot.  I just read a short overview on LEX and YACC.  I've wasted weeks on a brute force
 *      compiler that only handles about 25 commands.  Time to rethink things.
 *  2025-08-20 - JLW
 *      The compiler has been rewritten and I've tossed so much code!  The new tokenized strings are
 *      way simpler and easier to follow.  The executer takes the tokenized string and puts everything
 *      into a class that holds all possible code components and each execution block just takes
 *      what it needs.  It's faster to compile and the excution speed seems like it will have to be
 *      better.  Regardless, it's definitely easier to follow the flow and debug problems, which is
 *      my gold standard on what's better.
 *  2025-10-18 - JLW
 *      The compiler is working well, but there are a few possible snags yet to be discoverd that will
 *      require kludges because I'm too new at this to do it right the first time.  However, I've got
 *      Version 0.4 almost ready to release as a POC.
 *      I've begun the process of setting up JAXBase.exe to execute as a runtime engine.  Putting /rt into
 *      the command line will cause it to run in runtime mode rather than IDE mode.  Another way is to
 *      rename JAXBase.exe to jaxrt.exe and it will automatically run in runtime mode.
 *  2025-11-22 - JLW
 *      I've been using Grok for some of the features that I had no clue on how to handle.  Two that come
 *      to mind are implementing READ EVENTS and ON KEY LABEL logic.  Grok has eventually given me code 
 *      that I could use, though I've learned that is best to copy, paste, and test the code immediately.  
 *      Perhaps my prompt writig ability is lacking as I have gone through a bunch of revisions ("Grok,
 *      I'm getting errror C0130 at..." or "Grok, it's not..."), before getting it right.  Sometimes Grok 
 *      completely tosses out the code and goes a different route.  However, I have to admit that it's a 
 *      lot faster using Grok then pounding through web sites that give half a solution because they expect 
 *      you to already know certain things.
 *      I absolutely do not ever want to use code from an AIs without understanding what's actually 
 *      happening.  However, I've learned some pretty cool tricks working with Grok, which is turning out
 *      to be a pretty decent reference tool that has saved me a lot time and frustration.
 *      So that's my official shout out to Grok.
 *  2025-01-17 - JLW
 *      I've made a lot of progress!  I'm working on the bootstrap form editor and decided I really
 *      needed grid support.  That's actually turning out to be much less intensive than I feared.
 * ----------------------------------------------------------------------------------------------------------*/

using Avalonia;
using System;

namespace JAXBase.Core
{
    internal static class Program
    {
        // Static property to access the AppClass instance from other classes (e.g., JAXApp, MainWindow)
        public static AppClass CurrentApp { get; private set; } = null!;  // Initialized in Main

        // The main entry point for the application.
        [STAThread]
        static int Main(string[] args)
        {
            // Create the App class and parse any command line parameters
            AppClass App = new AppClass();
            CurrentApp = App;  // Set the static reference
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "IDE";
            string parm1 = args.Length > 0 ? args[0] : string.Empty;

            // Determine mode
            bool parm1IsRT = parm1.Equals("/rt", StringComparison.OrdinalIgnoreCase) ||
                             parm1.Equals("-rt", StringComparison.OrdinalIgnoreCase);

            bool isRuntimeMode = appName.Contains("jaxrt.exe", StringComparison.OrdinalIgnoreCase) || parm1IsRT;

            if (isRuntimeMode)
            {
                App.DebugLog("Starting JAXBase in Runtime Mode");
                App.RuntimeFlag = true;

                int startIndex = parm1IsRT ? 1 : 0;
                int paramCounter = 0;

                for (int i = startIndex; i < args.Length; i++)
                {
                    if (i == startIndex)
                    {
                        // First non-flag arg is the file to run
                        App.DebugLog($"File to run: {args[i]}");
                        App.RTFileName = args[i];
                    }
                    else
                    {
                        // Additional params passed to the app
                        App.DebugLog($"Runtime Parameter {++paramCounter}: {args[i]}");
                        var tk = new JAXBase.XBase.ParameterClass();
                        tk.token.Element.Value = args[i];
                        App.ParameterClassList.Add(tk);
                    }
                }

                // NOTE: Runtime execution logic isn't in the current repo code (it just sets flags and exits).
                // We'll add handling for this in a later step if needed (e.g., execute without window).
            }
            else
            {
                App.DebugLog("Starting JAXBase in IDE Mode");
            }

            // Build and start the Avalonia app (replaces WinForms Application.Run)
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            App.DebugLog("Exiting JAXBase");
            return App.ReturnValue.Element.Type.Equals("N") ? App.ReturnValue.AsInt() : 0;
        }

        // Avalonia configuration (minimal; also used by designer if you add one later)
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<JAXApp>()
                .UsePlatformDetect()
                .WithInterFont()  // Basic font support (requires Avalonia.Fonts.Inter package)
                .LogToTrace();    // Debug logging for Avalonia issues
    }
}