// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="United States Government">
//   © 2024 United States Government, as represented by the Secretary of the Army.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Text;

internal class Program
{
    private const uint SCARD_LEAVE_CARD = 0x0000;
    private const uint SCARD_PROTOCOL_RAW = 0x0004;
    private const uint SCARD_PROTOCOL_T0 = 0x0001;
    private const uint SCARD_PROTOCOL_T1 = 0x0002;

    // Constants
    private const uint SCARD_SCOPE_SYSTEM = 2;
    private const uint SCARD_SHARE_SHARED = 2;

    private static void Main(string[] args)
    {
        IntPtr hContext = IntPtr.Zero;
        IntPtr hCard = IntPtr.Zero;
        string readerName = string.Empty;

        try
        {
            // Establish context for smart card operations
            int result = SCardEstablishContext(SCARD_SCOPE_SYSTEM, IntPtr.Zero, IntPtr.Zero, out hContext);
            if (result != 0)
            {
                Console.WriteLine("Failed to establish context: " + result);
                return;
            }

            // List available smart card readers
            uint pcchReaders = 0;
            result = SCardListReaders(hContext, null, null, ref pcchReaders);
            if (result != 0)
            {
                Console.WriteLine("Failed to list readers: " + result);
                return;
            }

            byte[] mszReaders = new byte[pcchReaders * 2];
            result = SCardListReaders(hContext, null, mszReaders, ref pcchReaders);
            if (result != 0)
            {
                Console.WriteLine("Failed to list readers: " + result);
                return;
            }

            // Get the first reader name
            readerName = Encoding.ASCII.GetString(mszReaders).Split('\0')[0];

            // Connect to the smart card reader
            uint dwActiveProtocol;
            result = SCardConnect(hContext, readerName, SCARD_SHARE_SHARED, SCARD_PROTOCOL_T0 | SCARD_PROTOCOL_T1, out hCard, out dwActiveProtocol);
            if (result != 0)
            {
                Console.WriteLine("Failed to connect to reader: " + result);
                return;
            }

            // Example command to read from the smart card
            byte[] command = { 0x00, 0xB0, 0x00, 0x00, 0x10 }; // Example command to read 16 bytes from the card
            byte[] response = new byte[256];
            uint responseLength = (uint)response.Length;
            SCARD_IO_REQUEST pioSendPci = new SCARD_IO_REQUEST { dwProtocol = (int)dwActiveProtocol, cbPciLength = Marshal.SizeOf(typeof(SCARD_IO_REQUEST)) };

            result = SCardTransmit(hCard, ref pioSendPci, command, (uint)command.Length, IntPtr.Zero, response, ref responseLength);
            if (result != 0)
            {
                Console.WriteLine("Failed to transmit command: " + result);
                return;
            }

            // Display the response data
            string responseData = BitConverter.ToString(response, 0, (int)responseLength).Replace("-", " ");
            Console.WriteLine("Response Data: " + responseData);
        }
        finally
        {
            // Disconnect from the smart card reader
            if (hCard != IntPtr.Zero)
            {
                SCardDisconnect(hCard, SCARD_LEAVE_CARD);
            }

            // Release context handle
            if (hContext != IntPtr.Zero)
            {
                SCardReleaseContext(hContext);
            }
        }
    }

    [DllImport("winscard.dll")]
    private static extern int SCardConnect(IntPtr hContext, string szReader, uint dwShareMode, uint dwPreferredProtocols, out IntPtr phCard, out uint pdwActiveProtocol);

    [DllImport("winscard.dll")]
    private static extern int SCardDisconnect(IntPtr hCard, uint dwDisposition);

    // Import required Windows API functions
    [DllImport("winscard.dll")]
    private static extern int SCardEstablishContext(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

    [DllImport("winscard.dll")]
    private static extern int SCardListReaders(IntPtr hContext, byte[] mszGroups, byte[] mszReaders, ref uint pcchReaders);

    [DllImport("winscard.dll")]
    private static extern int SCardReleaseContext(IntPtr hContext);

    [DllImport("winscard.dll")]
    private static extern int SCardTransmit(IntPtr hCard, ref SCARD_IO_REQUEST pioSendPci, byte[] pbSendBuffer, uint cbSendLength, IntPtr pioRecvPci, byte[] pbRecvBuffer, ref uint pcbRecvLength);

    // Structure for sending and receiving commands
    private struct SCARD_IO_REQUEST
    {
        public int cbPciLength;
        public int dwProtocol;
    }
}