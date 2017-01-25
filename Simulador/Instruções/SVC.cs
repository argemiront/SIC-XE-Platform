using System;
using System.Text.RegularExpressions;

namespace Simulador
{
    class SVC : SetInstrucoes
    {
        public SVC()
        {
            base.tamanho = 2;
            base.codigo = 0xB0;
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
            return "SVC";
        }
    }
}