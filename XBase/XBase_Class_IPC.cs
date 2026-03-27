/*
 * This may be all wrong because I'm not sure I was getting what
 * I want across to Grok.  If so, my bad.
 *
 * Starting point for code interfacing.

 *=====================================================================
 * MAIN.PRG - Modern VFP IPC in 2025 - NO BRIDGE, NO SERVICE
 * Just like SYS(2335,2,"CMD") but actually works!
 *=====================================================================

 CLEAR
 SET PROCEDURE TO VFP_IPC_Handler ADDITIVE

 *-- Your app name (MUST be same in ALL apps that talk)
 lcAppName = "FoxInventory2025"

 *-- Create the magic object
 loIPC = CREATEOBJECT("IPC", lcAppName)

 ? "VFP IPC Started"
 ? "AppName        :", loIPC.AppName
 ? "PipeName       :", loIPC.PipeName
 ? "Is Server      :", loIPC.IsServer
 ? "Is First       :", loIPC.IsFirstInstance
 ? "Connected      :", loIPC.IsConnected
 ?

 IF loIPC.IsFirstInstance
    ? "You are the MASTER instance - others will connect to you"
 ELSE
    ? "Connected to master instance - sending test..."
    loIPC.Send("PING|Hello from " + SYS(0))
 ENDIF

 *-- Keep alive and listen
 DO EVENTS
 READ EVENTS



 *=====================================================================
 * VFP_IPC_Handler.prg - Receive commands from other VFP apps
 *=====================================================================

 *-- This is called from C# via COM events
 PROCEDURE OnCommandReceived(toIPC, tcCommand)
    ? ""
    ? TIME() + " RECEIVED FROM OTHER APP: " + tcCommand
    ?

    DO CASE
       CASE "PING|" $ tcCommand
          ? "   → Pong back!"
          toIPC.Send("PONG|" + PROGR())

       CASE "SHOW" $ UPPER(tcCommand)
          THISFORM.Show()
          THISFORM.WindowState = 0  && Normal

       CASE "RESTOCK|" $ tcCommand
          lcItem = SUBSTR(tcCommand, AT("|", tcCommand)+1)
          RestockItem(lcItem)

       CASE "QUIT" $ UPPER(tcCommand)
          CLEAR EVENTS

       OTHERWISE
          ? "   → Unknown command, ignoring"
    ENDCASE
 ENDPROC

 PROCEDURE OnError(toIPC, tcError)
    ? "IPC ERROR: " + tcError
 ENDPROC



 *-- Send command and quit (like old SYS(2335,2))
 lo = CREATEOBJECT("VFP_IPC.VFP_IPC", "FoxInventory2025", .T.)
 IF lo.IsConnected
    lo.Send("RESTOCK|XYZ999|100")
    ? "Command sent!"
 ELSE
    ? "No running instance found"
 ENDIF
 lo.Release()

 *
 *
 */
using System.IO.Pipes;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_IPC : XBase_Avalonia, IDisposable
    {
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        // PUBLIC VFP-STYLE PROPERTIES
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        public string AppName { get; set; } = "";
        public string PipeName => pipeName;                   // Read-only, like HIDDEN in VFP
        public bool IsServer { get; private set; } = false;
        public bool IsConnected { get; private set; } = false;
        public bool IsFirstInstance { get; private set; } = true;
        public string LastError { get; private set; } = "";
        public string LastMessage { get; private set; } = "";

        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        // EVENTS - Exactly like VFP
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        public event EventHandler<string>? OnCommandReceived;   // Like SYS(2335,3)
        public event EventHandler<string>? OnError;
        public event EventHandler? OnBecameServer;
        public event EventHandler? OnConnectedToServer;

        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        // PRIVATE FIELDS
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        private string pipeName = "";
        private Mutex? appMutex;
        private PipeStream? pipe;
        private Task? listener;
        private bool disposed = false;

        public XBase_Class_IPC(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "ipc" : name;
            SetVisualObject(null, "IPC", name, false, UserObject.urw);
        }

        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        // SERVER: First instance becomes server
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        private void StartServer()
        {
            pipeName = $"Global\\VFP_IPC_{UserProperties["pipename"].AsString()}";

            // Try to become the one and only server
            try
            {
                appMutex = new Mutex(true, pipeName + "_MUTEX", out bool createdNew);
                IsFirstInstance = createdNew;

                if (createdNew)
                {
                    // WE ARE THE SERVER
                    IsServer = true;
                    StartServer();
                    OnBecameServer?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // We are a CLIENT — connect to the real server
                    appMutex.Dispose();
                    appMutex = null;

                    var server = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.InOut,
                        10,  // Up to 10 clients (like 10 VFP users)
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    pipe = server;

                    listener = Task.Run(async () =>
                    {
                        while (!disposed)
                        {
                            try
                            {
                                await server.WaitForConnectionAsync();
                                IsConnected = true;
                                _ = Task.Run(() => HandleClient(server)); // Fire and forget per client
                                server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                                pipe = server;
                            }
                            catch { break; }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SetError("Mutex/Pipe init failed: " + ex.Message);
            }

        }

        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        // CLIENT: Connect to first instance
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        private void ConnectToServer()
        {
            Task.Run(async () =>
            {
                while (!disposed)
                {
                    try
                    {
                        var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                        await client.ConnectAsync(1000);
                        pipe = client;
                        IsConnected = true;
                        OnConnectedToServer?.Invoke(this, EventArgs.Empty);
                        _ = HandleClient(client);
                        break;
                    }
                    catch
                    {
                        await Task.Delay(1000); // Retry every second
                    }
                }
            });
        }

        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        // Send command — works from ANY instance
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        public bool Send(string cCommand)
        {
            if (!IsConnected || pipe == null || !pipe.IsConnected)
            {
                SetError("Not connected");
                return false;
            }

            try
            {
                var data = Encoding.UTF8.GetBytes(cCommand);
                pipe.Write(data, 0, data.Length);
                pipe.Flush();
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                IsConnected = false;
                return false;
            }
        }

        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        // Handle incoming messages
        // ≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡≡
        private async Task HandleClient(PipeStream clientPipe)
        {
            var buffer = new byte[65536];
            try
            {
                while (IsConnected && clientPipe.IsConnected)
                {
                    int read = await clientPipe.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, read);
                    LastMessage = msg;
                    OnCommandReceived?.Invoke(this, msg);
                }
            }
            catch { }
            finally
            {
                IsConnected = false;
                try { clientPipe.Dispose(); } catch { }
            }
        }

        private void SetError(string msg)
        {
            LastError = msg;
            OnError?.Invoke(this, msg);
        }

        public new void Dispose()
        {
            if (disposed) return;
            disposed = true;

            try { pipe?.Dispose(); } catch { }
            try { appMutex?.Dispose(); } catch { }
        }
    }
}

