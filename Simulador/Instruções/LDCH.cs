using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LDCH : SetInstrucoes
    {
        public LDCH()
        {
            base.tamanho = 3;
            base.codigo = 0x50;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.A] = ula.parametroCarregado & 0xFF;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x50;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LDCH";
        }
    }
}