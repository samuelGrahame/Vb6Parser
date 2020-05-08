using System;

namespace Vb6Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            var frmParser = new FrmParser("DIR", "FORM_NAME");
            frmParser.Parse();
        }
    }
}
