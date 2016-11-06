﻿using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Netling.Core.Extensions;

namespace Netling.Core.Performance
{
    internal static class HttpHelper
    {
        public static ResponseType GetResponseType(byte[] buffer, int start, int end)
        {
            int index;
            int length;

            if (SeekHeader(buffer, HttpHeaders.ContentLength, start, end, out index, out length))
                return ResponseType.ContentLength;

            if (SeekHeader(buffer, HttpHeaders.TransferEncoding, start, end, out index, out length))
                return ResponseType.Chunked;

            return ResponseType.Unknown;
        }

        public static int GetStatusCode(byte[] buffer, int start, int end)
        {
            return buffer.ConvertToInt(start + 9, 3, end);
        }

        // HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!
        public static bool SeekHeader(byte[] buffer, byte[] header, int start, int end, out int index, out int length)
        {
            if (end < start + header.Length)
            {
                index = -1;
                length = 0;
                return false;
            }

            for (var i = 0; i < header.Length; i++)
            {
                if (start + i < end)
                {
                    if ((header[i] | 0x20) == (buffer[start + i] | 0x20))
                        continue;

                    var next = SeekReturn(buffer, start + i, end);

                    if (next != -1)
                        return SeekHeader(buffer, header, next, end, out index, out length);
                }

                index = -1;
                length = 0;
                return false;
            }

            index = start + header.Length;
            var r = SeekReturn(buffer, index, end);
            length = r - index;
            return true;
        }

        private static int SeekReturn(byte[] buffer, int start, int end)
        {
            while (start + 1 < end)
            {
                if (buffer[start] == 13)
                    return start;

                start++;
            }

            return -1;
        }
        
        public static int SeekHeaderEnd(byte[] buffer, int start, int end)
        {
            while (start + 3 < end)
            {
                if (buffer[start] == 13 &&
                    buffer[start + 1] == 10 &&
                    buffer[start + 2] == 13 &&
                    buffer[start + 3] == 10)
                    return start;

                start++;
            }

            return -1;
        }

        public static Stream GetStream(TcpClient client, Uri uri)
        {
            if (uri.Scheme == Uri.UriSchemeHttp)
                return client.GetStream();
            
            var stream = new SslStream(client.GetStream());
            var xc = new X509Certificate2Collection();
            stream.AuthenticateAsClient(uri.Host, xc, SslProtocols.Tls, false);
            return stream;
        }
    }
}
