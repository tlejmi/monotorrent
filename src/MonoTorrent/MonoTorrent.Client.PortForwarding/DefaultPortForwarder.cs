//
// DefaultPortForwarder.cs
//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2020 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;

namespace MonoTorrent.Client.PortForwarding
{
    public class DefaultPortForwarder : IPortForwarder
    {
        public bool Active => NatUtility.IsSearching;

        HashSet<INatDevice> Devices { get; }

        List<(ushort internalPort, ushort externalPort)> Requests;

        public DefaultPortForwarder ()
        {
            Devices = new HashSet<INatDevice> ();
            Requests = new List<(ushort internalPort, ushort externalPort)> ();
            NatUtility.DeviceFound += async (o, e) => {
                await ClientEngine.MainLoop;
                Devices.Add (e.Device);
            };

            NatUtility.DeviceLost += async (o, e) => {
                await ClientEngine.MainLoop;
                Devices.Remove (e.Device);
            };
        }

        public async Task ForwardPortAsync (ushort port, CancellationToken token)
        {
        }

        public async Task ForwardPortAsync (ushort externalPort, ushort internalPort, CancellationToken token)
        {
            throw new NotImplementedException ();
        }

        public async Task StartAsync ()
        {
            if (!Active) {
                await MainLoop.SwitchToThreadpool ();
                NatUtility.StartDiscovery (NatProtocol.Pmp, NatProtocol.Upnp);
            }
        }

        public Task StopAsync ()
        {
            NatUtility.StopDiscovery ();
            return Task.CompletedTask;
        }
    }
}
