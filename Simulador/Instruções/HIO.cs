using System;
using System.Text.RegularExpressions;

namespace Simulador
{
    class HIO : SetInstrucoes
    {
        public HIO()
        {
            base.tamanho = 1;
            base.codigo = 0xFFFF;
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
            return "HIO";
        }
    }
}