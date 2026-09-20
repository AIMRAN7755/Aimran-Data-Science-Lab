using System;
using MudBlazor;

public class Test
{
    public static void Main()
    {
        foreach (var name in Enum.GetNames(typeof(MudBlazor.Typo)))
        {
            Console.WriteLine("Typo: " + name);
        }
        foreach (var m in typeof(IDialogService).GetMethods())
        {
            if (m.Name.Contains("MessageBox"))
                Console.WriteLine("IDialogService method: " + m.Name);
        }
    }
}
