// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace fCraft
{
    /// <summary> Static class for assisting with making web requests. </summary>
    public static class HttpUtil
    {
        // Dns lookup, to make sure that IPv4 is preferred for connections
        static readonly Dictionary<string, IPAddress> TargetAddresses = new Dictionary<string, IPAddress>();
        static DateTime nextDnsLookup = DateTime.MinValue;
        static readonly TimeSpan DnsRefreshInterval = TimeSpan.FromMinutes(30);

        static IPAddress RefreshTargetAddress([NotNull] Uri requestUri)
        {
            if (requestUri == null) throw new ArgumentNullException("requestUri");

            string hostName = requestUri.Host.ToLowerInvariant();
            IPAddress targetAddress;
            if (!TargetAddresses.TryGetValue(hostName, out targetAddress) || DateTime.UtcNow >= nextDnsLookup) {
                IPAddress[] allAddresses = null;
                try {
                    // Perform a DNS lookup on given host. Throws SocketException if no host found.
                    try {
                        allAddresses = Dns.GetHostAddresses(requestUri.Host);
                    } catch {
                        return null;
                    }
                    // Find a suitable IPv4 address. Throws InvalidOperationException if none found.
                    targetAddress = allAddresses.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                } catch (SocketException ex) {
                    Logger.Log(LogType.Error,
                        "HttpUtil.RefreshTargetAddress: Error looking up server URL: {0}", ex);
                } catch (InvalidOperationException) {
                    Logger.Log(LogType.Warning, "HttpUtil.RefreshTargetAddress: {0} does not have an IPv4 address!",
                        requestUri.Host);
                } catch (UriFormatException) {
                    Logger.Log(LogType.Warning, "Invalid URI: The hostname could not be parsed.");
                    
                }
                TargetAddresses[hostName] = targetAddress;
                nextDnsLookup = DateTime.UtcNow + DnsRefreshInterval;
            }
            return targetAddress;
        }
        

        // Creates an HTTP request object to the given URL
        [NotNull]
        public static HttpWebRequest CreateRequest([NotNull] Uri uri, TimeSpan timeout) {
            if (uri == null) throw new ArgumentNullException("uri");
            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(uri);
            
            req.ReadWriteTimeout = (int)timeout.TotalMilliseconds;
            req.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            req.Timeout = (int)timeout.TotalMilliseconds;
            req.UserAgent = Updater.UserAgent;
            
            if (uri.Scheme == "http") {
                req.Proxy = new WebProxy("http://" + RefreshTargetAddress(uri) + ":" + uri.Port);
            }
            return req;
        }
    }
}