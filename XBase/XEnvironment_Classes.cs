using JAXBase.Core;
using System.Management;
using static JAXBase.XBase.XClass_AuxCode;

namespace JAXBase.XBase
{
    public static class XEnvironment_Classes
    {
        public static JAXObjectWrapper _JAX(AppClass app)
        {
            List<ParameterClass> xParameters = [];
            ParameterClass p = new() { PName = "path" };
            xParameters.Add(p);

            p = new() { PName = "version" };
            xParameters.Add(p);

            p = new() { PName = "x64" };
            xParameters.Add(p);

            p = new() { PName = "classeditor" };
            xParameters.Add(p);

            p = new() { PName = "config" };
            xParameters.Add(p);

            p = new() { PName = "fileeditor" };
            xParameters.Add(p);

            p = new() { PName = "formeditor" };
            xParameters.Add(p);

            p = new() { PName = "imageeditor" };
            xParameters.Add(p);

            p = new() { PName = "labeleditor" };
            xParameters.Add(p);

            p = new() { PName = "menueditor" };
            xParameters.Add(p);

            p = new() { PName = "projecteditor" };
            xParameters.Add(p);

            p = new() { PName = "reporteditor" };
            xParameters.Add(p);

            p = new() { PName = "tableeditor" };
            xParameters.Add(p);

            JAXObjectWrapper custom = new(app, "empty", "_JAX", xParameters);
            return custom;
        }

        public static async Task<List<JAXObjectWrapper>> _CPU(AppClass app)
        {
            List<JAXObjectWrapper> custom = [];
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            int i = 0;

            List<ParameterClass> xParameters = [];
            ParameterClass p = new() { PName = "cpuName" };
            p.token.Element.Value = "Unknown";
            xParameters.Add(p);

            p = new() { PName = "X64" };
            p.token.Element.Value = false;
            xParameters.Add(p);

            p = new() { PName = "corecount" };
            p.token.Element.Value = 0;
            xParameters.Add(p);

            p = new() { PName = "logicalcores" };
            p.token.Element.Value = 0;
            xParameters.Add(p);

            p = new() { PName = "clockspeed" };
            p.token.Element.Value = "0 MHz";
            xParameters.Add(p);

            JAXObjectWrapper blankEntry = new(app, "empty", "_CPU", xParameters);


            foreach (ManagementObject processor in searcher.Get())
            {
                try
                {
                    xParameters = [];
                    p = new() { PName = "cpuName" };
                    p.token.Element.Value = processor["Name"] ?? "Unknown";
                    xParameters.Add(p);

                    p = new() { PName = "X64" };
                    p.token.Element.Value = (int)processor["AddressWidth"] == 64;
                    xParameters.Add(p);

                    p = new() { PName = "corecount" };
                    p.token.Element.Value = (int)processor["NumberOfCores"];
                    xParameters.Add(p);

                    p = new() { PName = "logicalcores" };
                    p.token.Element.Value = (int)processor["NumberOfLogicalProcessors"];
                    xParameters.Add(p);

                    p = new() { PName = "clockspeed" };
                    p.token.Element.Value = processor["MaxClockSpeed"].ToString() + " MHz";
                    xParameters.Add(p);

                    custom.Add(new(app, "custom", $"_cpu{i++}", xParameters));
                }
                catch (Exception ex)
                {
                    await blankEntry.AddError(9999, 0, "|" + ex.Message, string.Empty);
                    custom.Add(blankEntry);
                }
            }

            return custom;
        }

        public static async Task<List<JAXObjectWrapper>> _Drives(AppClass app)
        {

            List<JAXObjectWrapper> custom = [];

            List<ParameterClass> xParameters = [];
            ParameterClass p = new() { PName = "dvsname" };
            xParameters.Add(p);

            p = new() { PName="format"};
            xParameters.Add(p);
            
            p = new() { PName ="removable"};
            p.token.Element.Value = false;
            xParameters.Add(p);

            p = new() { PName ="size"};
            p.token.Element.Value = 0;
            xParameters.Add(p);

            p = new() { PName ="type"};
            p.token.Element.Value = "Unknown";
            xParameters.Add(p);

            p = new() { PName ="totalfree"};
            p.token.Element.Value = 0;
            xParameters.Add(p);

            p = new() { PName ="userfree"};
            p.token.Element.Value = 0;
            xParameters.Add(p);

            p = new() { PName ="volume"}; 
            xParameters.Add(p);

            JAXObjectWrapper blankEntry = new(app, "empty", "_drive", xParameters);
            int i = 0;

            // Loop through each drive / volume / share found
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    double freeSpace = drive.TotalFreeSpace;
                    double totalSpace = drive.TotalSize;

                    xParameters = [];
                    p = new() { PName = "dvsname" };
                    p.token.Element.Value = drive.Name;
                    xParameters.Add(p);

                    p = new() { PName = "format" };
                    p.token.Element.Value = drive.DriveFormat;
                    xParameters.Add(p);

                    p = new() { PName = "removable" };
                    p.token.Element.Value = drive.DriveType.ToString().Equals("removable", StringComparison.OrdinalIgnoreCase);
                    xParameters.Add(p);

                    p = new() { PName = "size" };
                    p.token.Element.Value = drive.TotalSize;
                    xParameters.Add(p);

                    p = new() { PName = "type" };
                    p.token.Element.Value = drive.DriveType.ToString();
                    xParameters.Add(p);

                    p = new() { PName = "totalfree" };
                    p.token.Element.Value = drive.TotalFreeSpace;
                    xParameters.Add(p);

                    p = new() { PName = "userfree" };
                    p.token.Element.Value = drive.AvailableFreeSpace;
                    xParameters.Add(p);

                    p = new() { PName = "volume" };
                    p.token.Element.Value = drive.VolumeLabel;
                    xParameters.Add(p);

                    custom.Add(new(app, "empty", $"_drive{i++}", xParameters));
                }
                catch (UnauthorizedAccessException ex)
                {
                    string driveName = "???";
                    try { driveName = drive.Name; } catch { }

                    await blankEntry.AddError(2222, 0, driveName + "|" + ex.Message, string.Empty);
                    custom.Add(blankEntry);
                }
                catch (DriveNotFoundException ex)
                {
                    string driveName = "???";
                    try { driveName = drive.Name; } catch { }

                    await blankEntry.AddError(9103, 0, "Drive " + driveName + " was not found." + "|" + ex.Message, string.Empty);
                    custom.Add(blankEntry);
                }
                catch (IOException ex)
                {
                    await blankEntry.AddError(334, 0, "|" + ex.Message, string.Empty);
                    custom.Add(blankEntry);
                }
                catch (Exception ex)
                {
                    await blankEntry.AddError(9999, 0, "|" + ex.Message, string.Empty);
                    custom.Add(blankEntry);
                }
            }


            return custom;
        }
    }
}
