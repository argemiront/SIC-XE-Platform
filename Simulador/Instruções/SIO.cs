using System;
using System.Text.RegularExpressions;

namespace Simulador
{
    class SIO : SetInstrucoes
    {
        public SIO()
        {
            base.tamanho = 1;
            base.codigo = 0xFFF;
        }

        override public void Execute(ULA ula)
        {
        }

        //override public string OPCode(SIC sic)
        //{
        //    return "0";
        //}

        public override string ToString()
        {
            return "SIO";
        }
    }
}