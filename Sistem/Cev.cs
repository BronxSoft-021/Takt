using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Takt.Sistem;

/// <summary>
/// The channel between whoever reads the hardware (the service, or the temporary helper) and the window.
/// The rights are written out instead of left to Windows' defaults: an ordinary user may read it (the window
/// must not need administrator), administrators and the system may do anything, nobody else gets in.
/// </summary>
public static class Cev
{
    /// <summary>
    /// "BronxSoftTakt-nadzor". TAKT_CEV changes it for one process only: the tests use their own name, or the
    /// installed service (which holds the real pipe) would answer them, or refuse them.
    /// </summary>
    public static string Ime { get; } =
        Environment.GetEnvironmentVariable("TAKT_CEV") is { Length: > 0 } zadato ? zadato : "BronxSoftTakt-nadzor";

    public static NamedPipeServerStream Napravi()
    {
        var prava = new PipeSecurity();

        using (var ja = WindowsIdentity.GetCurrent())
        {
            if (ja.User is { } korisnik)
                prava.AddAccessRule(new PipeAccessRule(korisnik, PipeAccessRights.ReadWrite, AccessControlType.Allow));
        }

        prava.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));
        prava.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));

        // The service runs as LocalSystem and the window as the logged-in user: everyone signed in may read,
        // or the window of another account would be left without sensors. Only reading: the pipe is one-way.
        prava.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.Read, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(Ime, PipeDirection.Out, 1, PipeTransmissionMode.Byte,
            PipeOptions.None, 0, 0, prava)!;
    }
}
