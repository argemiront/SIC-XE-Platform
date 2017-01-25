using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class FIX : SetInstrucoes
    {
        public FIX()
        {
            base.tamanho = 2;
            base.codigo = 0xC4;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.A] = (int)ula.registradores[ula.F];
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "9C";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "FIX";
        }
    }
}