using System;
using System.Text;
using System.Threading;
using Renci.SshNet;
using Renci.SshNet.Common;

public class SshTerminal
{
    // --- Configuration ---
    private const string Host = "10.126.90.34";
    private const string Username = "user";
    private const string Password = "Racal96$"; // Or use a private key file

    public static void Main(string[] args)
    {
        Console.WriteLine($"Attempting to connect to {Host} as {Username}...");

        try
        {
            // 1. Establish Connection
            using (var client = new SshClient(Host, Username, Password))
            {
                client.Connect();
                Console.WriteLine("Connection successful. Opening shell...");
                Console.WriteLine("-----------------------------------------------------");
                string output = "";
                // 2. Open an Interactive Shell (PTY)
                // Note: The 'client' object is needed for the connection check, so we pass it implicitly
                using (var shell = client.CreateShellStream("xterm", 80, 24, 800, 600, 1024))
                {
                    shell.WriteLine($"ssh -o StrictHostKeyChecking=no ats@192.168.1.13{Environment.NewLine}");
                    while (!shell.DataAvailable) ;
                    Thread.Sleep(500);
                    shell.WriteLine($"7038@");

                    // 3. Start a separate thread to stream the output
                    // PASS THE CLIENT OBJECT HERE
                    var outputThread = new Thread(() => StreamOutput(client, shell));
                    outputThread.Start();

                    // 4. Main thread handles input
                    // PASS THE CLIENT OBJECT HERE
                    StreamInput(client, shell);

                    outputThread.Join();
                }

                client.Disconnect();
                Console.WriteLine("\nDisconnected from the server.");
            }
        }
        catch (SshAuthenticationException authEx)
        {
            Console.WriteLine($"Authentication Error: {authEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }

    // New signature includes SshClient client
    private static void StreamOutput(SshClient client, ShellStream shell)
    {
        var buffer = new byte[1024];
        var encoder = Encoding.UTF8;
        int bytesRead;

        try
        {
            // CHECK THE CLIENT'S CONNECTION STATUS
            while (client.IsConnected && shell.CanRead)
            {
                bytesRead = shell.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    var output = encoder.GetString(buffer, 0, bytesRead);
                    Console.Write(output);
                }

                Thread.Sleep(50);
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions during read operation
            if (client.IsConnected)
            {
                Console.WriteLine($"\n[Output Stream Error: {ex.Message}]");
            }
        }
    }

    // New signature includes SshClient client
    private static void StreamInput(SshClient client, ShellStream shell)
    {
        // CHECK THE CLIENT'S CONNECTION STATUS
        while (client.IsConnected)
        {
            var input = Console.ReadLine();

            if (input != null)
            {
                // Write the line to the shell stream
                shell.WriteLine(input);

                // Check for the 'exit' command to gracefully stop the session
                if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
        }
    }
}