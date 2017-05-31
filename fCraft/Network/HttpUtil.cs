// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Static class for assisting with making web requests. </summary>
    public static class HttpUtil {
        
        // Dns lookup, to make sure that IPv4 is preferred for connections
        static readonly Dictionary<string, IPAddress> cache = new Dictionary<string, IPAddress>();
        static DateTime nextDnsLookup = DateTime.MinValue;
        static readonly TimeSpan DnsRefreshInterval = TimeSpan.FromMinutes(30);
        static readonly object dnsLock = new object();

        static IPAddress LookupIPv4([NotNull] Uri uri) {
            if (uri == null) throw new ArgumentNullException("requestUri");
            string hostName = uri.Host.ToLowerInvariant();
            IPAddress ip;
            
            lock (dnsLock) {
                if (cache.TryGetValue(hostName, out ip) && DateTime.UtcNow < nextDnsLookup) return ip;
            }
            
            try {
                // Perform a DNS lookup on given host. Throws SocketException if no host found.
                IPAddress[] allIPs = null;
                try {
                    allIPs = Dns.GetHostAddresses(uri.Host);
                } catch {
                    return null;
                }
                
                // Find a suitable IPv4 address. Throws InvalidOperationException if none found.
                ip = allIPs.First(address => address.AddressFamily == AddressFamily.InterNetwork);
            } catch (SocketException ex) {
                Logger.Log(LogType.Error, "HttpUtil.LookupIPv4: Error looking up server URL: {0}", ex);
            } catch (InvalidOperationException) {
                Logger.Log(LogType.Warning, "HttpUtil.LookupIPv4: {0} does not have an IPv4 address!", uri.Host);
            } catch (UriFormatException) {
                Logger.Log(LogType.Warning, "Invalid URI: The hostname could not be parsed.");
                
            }
            
            lock (dnsLock) { cache[hostName] = ip; }
            nextDnsLookup = DateTime.UtcNow + DnsRefreshInterval;
            return ip;
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
                IPAddress ipv4 = LookupIPv4(uri);
                if (ipv4 != null) {
                    req.Proxy = new WebProxy("http://" + LookupIPv4(uri) + ":" + uri.Port);
                }
            }
            return req;
        }
    }
}