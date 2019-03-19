
using System;

namespace PlataformaServerCommandEvent
{
    class Program
    {
        static void Main(string[] args)
        {
            Receiver receiver = new Receiver();

            Console.ReadLine();

            receiver.Finish();
        }
    }
}
