using System.Security.Principal;

namespace Takt.Sistem;

/// <summary>Whether the program may read the sensors that need the driver. Shown in the header, never guessed.</summary>
public static class Admin
{
    public static bool JeAdmin { get; } = Odredi();

    public static string Stanje => JeAdmin ? Tekst.T("administrator") : Tekst.T("običan korisnik");

    private static bool Odredi()
    {
        try
        {
            using var identitet = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identitet).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Provera administratorskih prava", ex);
            return false;
        }
    }
}
