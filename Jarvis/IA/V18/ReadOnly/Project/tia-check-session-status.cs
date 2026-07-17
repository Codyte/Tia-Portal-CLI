using System;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Multiuser;

internal static class Program
{
    private static int Main()
    {
        TiaPortal tia = null;

        try
        {
            var process = TiaPortal.GetProcesses().FirstOrDefault();
            if (process == null)
            {
                Console.Error.WriteLine("Nenhuma instancia do TIA Portal foi encontrada.");
                return 1;
            }

            tia = process.Attach();

            if (tia.LocalSessions.Count == 0)
            {
                Console.Error.WriteLine("Nenhuma sessao local Multiuser foi encontrada nesta instancia.");
                return 1;
            }

            var session = tia.LocalSessions[0];
            var project = session.Project;
            if (project == null)
            {
                Console.Error.WriteLine("A sessao local nao contem um projeto valido.");
                return 1;
            }

            Console.WriteLine("PROJECT=" + project.Name);
            Console.WriteLine("SESSION_INDEX=0");
            Console.WriteLine("SESSION_TYPE=" + typeof(LocalSession).FullName);
            Console.WriteLine("IS_UP_TO_DATE=" + session.IsUptoDate());
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.GetBaseException().Message);
            return 1;
        }
        finally
        {
            if (tia != null)
            {
                tia.Dispose();
            }
        }
    }
}
