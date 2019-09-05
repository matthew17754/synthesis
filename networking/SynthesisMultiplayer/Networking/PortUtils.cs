using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Multiplayer.Networking
{
    public static class PortUtils
    {
        public static int GetAvailablePort(int start, params int[] exclusionList) =>
            Enumerable.Range(start, 65536-start).Except(
                IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
                .Where(e => e.Port > start)
                .Select(e => e.Port))
            .Except(exclusionList).First();

        public static List<int> GetAvailablePorts(int start, params int[] exclusionList) =>
            Enumerable.Range(start, 65536-start).Except(
                IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
                .Where(e => e.Port > start)
                .Select(e => e.Port))
            .Except(exclusionList)
            .ToList();
    }
}
