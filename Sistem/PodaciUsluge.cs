using System.Security.AccessControl;
using System.Security.Principal;

namespace Takt.Sistem;

/// <summary>
/// Where the service keeps its history and its log: <c>C:\ProgramData\BronxSoft Takt</c>, locked so that only the
/// system and administrators write there and everyone else reads.
/// The service runs as LocalSystem. Writing into the user's own %APPDATA% would let anything running as that user
/// swap the folder for a junction and have the service create or append files wherever it points - a way to
/// write as SYSTEM. A folder the user cannot change closes that.
/// </summary>
public static class PodaciUsluge
{
    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BronxSoft Takt");

    public static string Baza => Path.Combine(Folder, "takt.db");

    /// <summary>
    /// Makes the folder safe before the service writes a byte: a link or a folder somebody else made first is
    /// removed (with whatever it holds - a hard link planted inside would otherwise survive the new rights), then
    /// the folder is made again with rights that are not inherited from ProgramData (which lets users add files).
    /// Called by the installer (administrator) and by the service on every start (LocalSystem).
    /// </summary>
    public static void Pripremi() => Pripremi(Folder);

    public static void Pripremi(string folder)
    {
        bool napravljenIznova = Ocisti(folder);
        new DirectoryInfo(folder).SetAccessControl(Prava());

        if (napravljenIznova)
            Dnevnik.Upisi($"Usluga: folder podataka {folder} nije bio siguran (veza ili tuđi vlasnik); napravljen je iznova.");
    }

    /// <summary>
    /// The first half of <see cref="Pripremi(string)"/>, without the rights (setting the owner needs an
    /// administrator, so a test runs only this): a link is removed as a link, a folder made by somebody else is
    /// removed with its content, and the folder exists afterwards. Returns true when something was removed.
    /// </summary>
    public static bool Ocisti(string folder)
    {
        var info = new DirectoryInfo(folder);
        bool napravljenIznova = false;

        // Nothing may be logged before the folder is safe: the log itself lives in this folder, and writing it
        // through a planted link is exactly what this guards against.
        if (info.Exists && (info.Attributes.HasFlag(FileAttributes.ReparsePoint) || !NasVlasnik(info)))
        {
            // A junction or symbolic link is removed as a link; the target is not touched.
            if (info.Attributes.HasFlag(FileAttributes.ReparsePoint)) info.Delete();
            else info.Delete(recursive: true);
            napravljenIznova = true;
        }

        Directory.CreateDirectory(folder);
        return napravljenIznova;
    }

    /// <summary>System and administrators: everything. Users: read. Nothing inherited from above.</summary>
    public static DirectorySecurity Prava()
    {
        var prava = new DirectorySecurity();
        prava.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        prava.SetOwner(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));

        const InheritanceFlags nasledjuje = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        prava.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl, nasledjuje, PropagationFlags.None, AccessControlType.Allow));
        prava.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            FileSystemRights.FullControl, nasledjuje, PropagationFlags.None, AccessControlType.Allow));
        prava.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
            FileSystemRights.ReadAndExecute, nasledjuje, PropagationFlags.None, AccessControlType.Allow));
        return prava;
    }

    /// <summary>True when the folder belongs to the system or to the administrators, i.e. we (or an admin) made it.</summary>
    private static bool NasVlasnik(DirectoryInfo info)
    {
        try
        {
            var vlasnik = info.GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
            return vlasnik is not null &&
                   (vlasnik.IsWellKnown(WellKnownSidType.LocalSystemSid) ||
                    vlasnik.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid));
        }
        catch (Exception ex)
        {
            // Not knowing is no reason to throw the history away: the rights set right after take ownership anyway.
            Dnevnik.Greska("Usluga: vlasnik foldera podataka", ex);
            return true;
        }
    }
}
