using System;
using System.IO;
using System.Net;
using System.Text;
using LumiSoft.Net;
using LumiSoft.Net.POP3.Server;

namespace CrawlDown
{
    /// <summary>
    /// Imported from <see href="https://gist.github.com/alexfalkowski/5442421" />.
    /// </summary>
    public class Pop3Server : IDisposable
    {
        private readonly POP3_Server _server = new POP3_Server();
        public Uri Uri
        {
            get;
        }

        // TODO: this might be better accepting one or more FileInfo instances
        public Pop3Server(string message)
        {
            var localBind = new IPBindInfo("localhost", BindInfoProtocol.TCP, IPAddress.Loopback, 110);
            var scheme = localBind.SslMode == SslMode.None ? "pop" : "pops";
            Uri = new UriBuilder(scheme, localBind.HostName, localBind.Port).Uri;
            _server.Bindings = new[]
            {
                localBind,
            };

            _server.SessionCreated += (sender, args) =>
            {
                var session = args.Session;
                session.Authenticate += (o, authenticate) =>
                    authenticate.IsAuthenticated = true;

                // TODO: loop through the FileInfo instances provided and use FileInfo.Length
                session.GetMessagesInfo += (o, info) =>
                    info.Messages.Add(new POP3_ServerMessage(Guid.NewGuid().ToString(), message.Length));

                // TODO: create stream to the resource files, remembering them
                session.GetMessageStream += (o, stream) =>
                    stream.MessageStream = new MemoryStream(Encoding.UTF8.GetBytes(message));
            };
        }

        public static Pop3Server Start(string pathToEmailFile)
        {
            var emailFileInfo = new FileInfo(pathToEmailFile);
            return Start(emailFileInfo);
        }

        public static Pop3Server Start(FileInfo emailFileInfo)
        {
            using var reader = emailFileInfo.OpenText();
            var message = reader.ReadToEnd();
            var result = new Pop3Server(message);
            result.Start();
            return result;
        }

        public void Start()
        {
            _server.Start();
        }

        public void Dispose()
        {
            _server.Stop();
        }
    }
}
