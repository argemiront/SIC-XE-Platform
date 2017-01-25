using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;

namespace Simulador
{
    class AADF : SetInstrucoes
    {
        public AADF()
        {
            base.tamanho = 3;
            base.codigo = 0x58;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.F] += ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x18;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "AADF";
        }
    }
}
