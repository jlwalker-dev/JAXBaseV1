/*
 * DEBUGGER/STEPPER
 */
namespace JAXBase.Executor
{
    public class JAXBase_Debugger
    {
        public static void Stepper()
        {
            //    // If debugger screen is not active, start it up
            //    if (App.JaxDebugger is null)
            //    {
            //        App.JaxDebugger = new(App);
            //        // At the very start of debugging (once)
            //        App.JaxDebugger.BeginDebugging();
            //    }

            //    bool debugging = true;

            //    while (debugging && App.JaxDebugger is not null)
            //    {
            //        DebugAction action = App.JaxDebugger.GetResponse();  // This now WORKS and is responsive

            //        switch (action)
            //        {
            //            case JAXDebuggerForm.DebugAction.Step:
            //                debugging = false;
            //                break;

            //            case JAXDebuggerForm.DebugAction.StepInto:
            //                debugging = false;
            //                break;

            //            case JAXDebuggerForm.DebugAction.Cancel:
            //                debugging = false;
            //                App.CurrentDS.JaxSettings.Step = false;
            //                App.JaxDebugger?.EndDebugging();
            //                App.JaxDebugger = null;
            //                JAXBase_Executor_C.Cancel(this, null);
            //                return "Z";

            //            case JAXDebuggerForm.DebugAction.Resume:
            //                debugging = false;
            //                App.CurrentDS.JaxSettings.Step = false;
            //                App.JaxDebugger.EndDebugging();
            //                App.JaxDebugger = null;
            //                break;
            //        }
            //    }
        }
    }
}
