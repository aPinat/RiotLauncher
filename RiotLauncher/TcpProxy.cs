using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace RiotLauncher;

public class TcpProxy
{
    private readonly string _hostname;
    protected readonly TcpListener _listener;
    protected readonly int _port;

    public TcpProxy(string hostname, int port)
    {
        _hostname = hostname;
        _port = port;
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        Task.Run(AcceptClientLoopAsync);
    }

    private protected virtual async Task AcceptClientLoopAsync()
    {
        while (true)
        {
            var incoming = await _listener.AcceptTcpClientAsync();
            var ipEndPoint = incoming.Client.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"Incoming connection from {ipEndPoint}");

            var outgoing = new TcpClient(_hostname, _port);
            var outgoingSslStream = new SslStream(outgoing.GetStream());
            await outgoingSslStream.AuthenticateAsClientAsync(_hostname);
            new TcpProxyThread(incoming, outgoing, outgoingSslStream).StartThreads();
            Console.WriteLine($"Finished setting up proxy for {ipEndPoint} to {_hostname}:{_port}");
        }
        // ReSharper disable once FunctionNeverReturns
    }

    protected class TcpProxyThread : IDisposable
    {
        protected readonly TcpClient _incoming;
        private readonly TcpClient _outgoing;
        protected readonly SslStream _outgoingSslStream;
        protected bool _connected;

        public TcpProxyThread(TcpClient incoming, TcpClient outgoing, SslStream outgoingSslStream)
        {
            _incoming = incoming;
            _outgoing = outgoing;
            _outgoingSslStream = outgoingSslStream;
            _connected = true;
        }

        public void Dispose()
        {
            _connected = false;
            _incoming.Dispose();
            _outgoing.Dispose();
            _outgoingSslStream.Dispose();
        }

        public void StartThreads()
        {
            Task.Run(IncomingAsync);
            Task.Run(OutgoingAsync);
        }

        protected virtual async Task IncomingAsync()
        {
            try
            {
                int byteCount;
                var bytes = new byte[8192];

                do
                {
                    byteCount = await _incoming.GetStream().ReadAsync(bytes);
                    await _outgoingSslStream.WriteAsync(bytes.AsMemory(0, byteCount));
                } while (byteCount != 0 && _connected);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Trace.WriteLine("Incoming errored.");
            }
            finally
            {
                Trace.WriteLine("Incoming closed.");
                if (_connected)
                    Dispose();
            }
        }

        protected virtual async Task OutgoingAsync()
        {
            try
            {
                int byteCount;
                var bytes = new byte[8192];

                do
                {
                    byteCount = await _outgoingSslStream.ReadAsync(bytes);
                    await _incoming.GetStream().WriteAsync(bytes.AsMemory(0, byteCount));
                } while (byteCount != 0 && _connected);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Trace.WriteLine("Outgoing errored.");
            }
            finally
            {
                Trace.WriteLine("Outgoing closed.");
                if (_connected)
                    Dispose();
            }
        }
    }
}
