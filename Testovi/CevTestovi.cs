using System.IO.Pipes;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>The pipe between the reader and the window: it must be made, and a client must get in.</summary>
public static class CevTestovi
{
    public static Task PraviIPusta()
    {
        using var cev = Cev.Napravi();
        Proveri.Da(cev is not null, "cev je napravljena");

        using var klijent = new NamedPipeClientStream(".", Cev.Ime, PipeDirection.In);
        klijent.Connect(2000);
        Proveri.Da(klijent.IsConnected, "klijent se povezao");

        // The server has to see the client too, or nobody could ever send anything.
        var cekanje = cev.WaitForConnectionAsync();
        Proveri.Da(cekanje is not null, "server čeka klijenta");
        Proveri.Da(cekanje.Wait(TimeSpan.FromSeconds(2)), "server vidi klijenta");
        return Task.CompletedTask;
    }
}
