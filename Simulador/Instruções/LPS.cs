using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LPS : SetInstrucoes
    {
        public LPS()
        {
            base.tamanho = 3;
            base.codigo = 0xD0;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0xD0;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LPS";
        }
    }
}