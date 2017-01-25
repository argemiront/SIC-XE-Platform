using System;
using System.Windows;
using System.Text.RegularExpressions;

namespace Simulador
{
    class RSUB : SetInstrucoes
    {
        public RSUB()
        {
            base.tamanho = 3;
            base.codigo = 0x4C;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.PC] = ula.registradores[ula.L];
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x4C;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "RSUB";
        }
    }
}