using System.IO;
using System.IO.Pipes;
using System.Text;
using GameGuard.Core;

namespace GameGuard.Agent;

public class PipeClient
{
    private readonly string _pipeName;
    public PipeClient(string pipeName) => _pipeName = pipeName;

    public PipeResponse Send(PipeRequest request)
    {
        using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
        client.Connect(3000);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };
        using var reader = new StreamReader(client, Encoding.UTF8);
        writer.WriteLine(PipeProtocol.SerializeRequest(request));
        var line = reader.ReadLine() ?? throw new IOException("no response");
        return PipeProtocol.DeserializeResponse(line);
    }
}
