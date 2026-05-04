using JAXBase.XBase;

namespace JAXBase.Core
{
    public static class AppLoop
    {
        // Return the current flow control object
        public static string GetLoopStack()
        {
            return Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Count > 0 ? Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack[^1] : string.Empty;
        }

        // Add a new flow control object to the stack
        public static string AddLoop(string ltype)
        {
            if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Count > 999) throw new Exception("Control object stack overflow");
            Program.CurrentApp.utl.Conv64(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopCounter++, 2, out string lvl);
            string lp = string.Format("{0}{1}", ltype, lvl);
            Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Add(lp);
            return lp;
        }

        // Add an existing flow control object to the stack
        public static void PushLoop(string lcontrol)
        {
            if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Count > 999) throw new Exception("Control object stack overflow");
            Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Add(lcontrol);

            if (lcontrol[0] == 'F')
            {
                // Add the for loop
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ForLoops.Add(lcontrol, new());
            }
            else if (lcontrol[0] == 'T')
            {
                // Add the for loop
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack.Add(new());
            }

        }


        // Remove the most recent loop stack object
        public static string PopLoopStack()
        {

            if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Count == 0) throw new Exception("Control object stack underflow");
            string pop = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack[^1];
            Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.RemoveAt(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Count - 1);

            // Toss the dictionary entry for the FOR loop
            if (pop[0] == 'F')
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ForLoops.Remove(pop);

            return pop;
        }


        public static void PushAppLevel(string prgName, JAXObjectWrapper? wrapper, string wrapperMethod)
        {
            AppLevel appLevel = new()

            {
                PrgName = prgName,
                ThisObject = wrapper,
                ThisObjectMethod = wrapperMethod
            };

            Program.CurrentApp.AppLevels.Add(appLevel);
            Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count();
        }


        public static void Cancel()
        {
            // Cancel running application
            if (Program.CurrentApp.RuntimeFlag)
            {
                // Cancel everything
            }

            Program.CurrentApp.CancelFlag = true;
            Program.CurrentApp.RuntimeFlag = false;

            // IDE cancel will release all app levels greater than 0 and
            // reset the vars to blank
        }

        public static void PopAppLevel()
        {
            Program.CurrentApp.AppLevels.RemoveAt(Program.CurrentApp.AppLevels.Count - 1);
            Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count - 1;
        }

    }
}
